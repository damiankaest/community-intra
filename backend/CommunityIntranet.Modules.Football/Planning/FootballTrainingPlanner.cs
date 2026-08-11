using CommunityIntranet.Modules.Football.Domain;
using CommunityIntranet.Modules.Football.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Football.Planning;

public interface IFootballTrainingPlanner
{
    Task<FootballTrainingPlanSuggestion?> SuggestAsync(Guid organizationId, Guid sessionId, CancellationToken ct);
}

public sealed record FootballTrainingPlanSuggestion(
    Guid SessionId,
    string Focus,
    int PlayerCount,
    IReadOnlyList<FootballTrainingPlanPlayerContext> Players,
    IReadOnlyList<FootballTrainingPlanBlockSuggestion> Blocks,
    IReadOnlyList<string> Warnings);

public sealed record FootballTrainingPlanPlayerContext(
    Guid MemberId,
    FootballPosition? Position,
    FootballAvailabilityStatus Availability,
    int MaxLoadPercent,
    int RecentLoad,
    string[] DevelopmentAreas);

public sealed record FootballTrainingPlanBlockSuggestion(
    Guid? ExerciseId,
    string Title,
    string? Description,
    string? CoachingPoints,
    int DurationMinutes,
    Guid? ResponsibleMemberId,
    string Reason,
    FootballIntensity Intensity);

internal sealed record FootballExerciseFeedbackAggregate(
    Guid ExerciseId,
    int Count,
    double Benefit,
    double Difficulty,
    double Fun);

public sealed class FootballTrainingPlanner(IFootballDbContext db, TimeProvider clock) : IFootballTrainingPlanner
{
    public async Task<FootballTrainingPlanSuggestion?> SuggestAsync(Guid organizationId, Guid sessionId, CancellationToken ct)
    {
        var session = await db.FootballSessions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == sessionId && !x.IsCancelled, ct);
        if (session is null) return null;

        var acceptedIds = await db.FootballAttendances.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.SessionId == sessionId && x.Status == FootballAttendanceStatus.Accepted)
            .Select(x => x.MemberId)
            .ToArrayAsync(ct);

        var profiles = await db.FootballMemberProfiles.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && acceptedIds.Contains(x.MemberId))
            .ToArrayAsync(ct);
        var availability = await db.FootballPlayerAvailability.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && acceptedIds.Contains(x.MemberId))
            .ToArrayAsync(ct);

        var recentSessionIds = await db.FootballSessions.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.StartsAt < session.StartsAt && x.StartsAt >= session.StartsAt.AddDays(-14) && !x.IsCancelled)
            .Select(x => x.Id)
            .ToArrayAsync(ct);
        var recentLoads = await db.FootballSessionLoads.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && acceptedIds.Contains(x.MemberId) && recentSessionIds.Contains(x.SessionId))
            .GroupBy(x => x.MemberId)
            .Select(group => new
            {
                MemberId = group.Key,
                Load = group.Sum(x => x.Rpe * (x.MinutesCompleted ?? 0))
            })
            .ToDictionaryAsync(x => x.MemberId, x => x.Load, ct);

        var playerContexts = acceptedIds.Select(memberId =>
        {
            var profile = profiles.FirstOrDefault(x => x.MemberId == memberId);
            var readiness = availability.FirstOrDefault(x => x.MemberId == memberId);
            return new FootballTrainingPlanPlayerContext(
                memberId,
                profile?.Position,
                readiness?.Status ?? FootballAvailabilityStatus.Fit,
                readiness?.MaxLoadPercent ?? 100,
                recentLoads.GetValueOrDefault(memberId),
                profile?.DevelopmentAreas ?? []);
        }).ToArray();

        var availablePlayers = playerContexts
            .Where(x => x.Availability != FootballAvailabilityStatus.Injured && x.MaxLoadPercent > 0)
            .ToArray();
        var restrictedPlayers = availablePlayers
            .Where(x => x.Availability is FootballAvailabilityStatus.Limited or FootballAvailabilityStatus.ReturnToPlay || x.MaxLoadPercent < 100)
            .ToArray();

        var feedbackCutoff = clock.GetUtcNow().AddDays(-120);
        var feedbackRows = await db.FootballExerciseFeedback.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.ExerciseId != null && x.UpdatedAt >= feedbackCutoff)
            .GroupBy(x => x.ExerciseId!.Value)
            .Select(group => new FootballExerciseFeedbackAggregate(
                group.Key,
                group.Count(),
                group.Average(x => x.Benefit),
                group.Average(x => x.Difficulty),
                group.Average(x => x.Fun)))
            .ToArrayAsync(ct);
        var exerciseFeedback = feedbackRows.ToDictionary(x => x.ExerciseId);

        var exercises = await db.FootballExercises.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && !x.IsArchived && x.MinPlayers <= availablePlayers.Length && (x.MaxPlayers == null || x.MaxPlayers >= availablePlayers.Length))
            .ToArrayAsync(ct);

        var coachId = await db.FootballMemberProfiles.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.TeamRole == FootballTeamRole.Coach)
            .Select(x => (Guid?)x.MemberId)
            .FirstOrDefaultAsync(ct);

        var targetDuration = Math.Clamp(session.DurationMinutes, 30, 150);
        var focus = string.IsNullOrWhiteSpace(session.Focus) ? "Spielfähigkeit" : session.Focus.Trim();
        var blocks = BuildBlocks(targetDuration, focus, availablePlayers, restrictedPlayers, exercises, exerciseFeedback, coachId);
        var warnings = BuildWarnings(playerContexts, availablePlayers, restrictedPlayers);

        return new FootballTrainingPlanSuggestion(session.Id, focus, availablePlayers.Length, playerContexts, blocks, warnings);
    }

    private static List<FootballTrainingPlanBlockSuggestion> BuildBlocks(
        int targetDuration,
        string focus,
        IReadOnlyList<FootballTrainingPlanPlayerContext> players,
        IReadOnlyList<FootballTrainingPlanPlayerContext> restricted,
        IReadOnlyList<FootballExercise> exercises,
        IReadOnlyDictionary<Guid, FootballExerciseFeedbackAggregate> feedback,
        Guid? coachId)
    {
        var remaining = targetDuration;
        var result = new List<FootballTrainingPlanBlockSuggestion>();

        var activationMinutes = Math.Min(15, Math.Max(10, targetDuration / 8));
        result.Add(new(null, "Aktivierung & Mobilität", "Dynamische Aktivierung mit Ball und kontrollierte Bewegungsqualität.", "Bewegungsradius sauber aufbauen; eingeschränkte Spieler individuell steuern.", activationMinutes, coachId, restricted.Count > 0 ? $"{restricted.Count} Spieler mit Belastungseinschränkung berücksichtigt." : "Gemeinsame körperliche Vorbereitung.", FootballIntensity.Low));
        remaining -= activationMinutes;

        var desiredCategory = InferCategory(focus);
        var candidates = exercises
            .Where(x => x.Category == desiredCategory || desiredCategory is null)
            .OrderByDescending(x => FeedbackScore(x.Id, feedback))
            .ThenBy(x => x.Intensity)
            .ToArray();

        var preferredIntensity = restricted.Count > 0 ? FootballIntensity.Medium : FootballIntensity.High;
        var mainExercise = candidates.FirstOrDefault(x => x.Intensity <= preferredIntensity) ?? candidates.FirstOrDefault();
        var mainMinutes = Math.Min(30, Math.Max(18, remaining / 2));
        if (mainExercise is not null)
        {
            result.Add(new(mainExercise.Id, mainExercise.Title, mainExercise.Description, mainExercise.Focus, mainMinutes, coachId, BuildExerciseReason(mainExercise, feedback, restricted.Count), mainExercise.Intensity));
        }
        else
        {
            result.Add(new(null, focus, "Hauptteil passend zum Schwerpunkt und zur verfügbaren Kadergröße.", "Tempo und Raumgröße an die aktuelle Belastbarkeit anpassen.", mainMinutes, coachId, "Keine passende Playbook-Übung für Teilnehmerzahl und Schwerpunkt gefunden.", preferredIntensity));
        }
        remaining -= mainMinutes;

        var forwards = players.Count(x => x.Position == FootballPosition.Forward);
        var defenders = players.Count(x => x.Position == FootballPosition.Defender);
        var transferTitle = forwards > defenders ? "Abschluss & Gegenpressing" : defenders > forwards ? "Spielaufbau & Restverteidigung" : "Spielform mit Schwerpunkt";
        var transferMinutes = Math.Max(12, remaining - 8);
        result.Add(new(null, transferTitle, "Spielform mit Gegnerdruck und klaren Coaching-Triggern.", "Belastete Spieler über Jokerrolle, kleinere Laufwege oder Pausen steuern.", transferMinutes, coachId, $"Positionsverteilung berücksichtigt: {forwards} Offensive / {defenders} Defensive.", preferredIntensity));
        remaining -= transferMinutes;

        if (remaining > 0)
        {
            result.Add(new(null, "Cooldown & Kurzfeedback", "Runterfahren, Beweglichkeit und kurze Rückmeldung zur Einheit.", "RPE und Blockfeedback direkt nach der Einheit erfassen.", remaining, coachId, "Schließt die Belastungsschleife für die nächste Planung.", FootballIntensity.Low));
        }

        return result;
    }

    private static FootballExerciseCategory? InferCategory(string focus)
    {
        var normalized = focus.ToLowerInvariant();
        if (normalized.Contains("stabi") || normalized.Contains("core")) return FootballExerciseCategory.Stability;
        if (normalized.Contains("kraft")) return FootballExerciseCategory.Strength;
        if (normalized.Contains("mobil")) return FootballExerciseCategory.Mobility;
        if (normalized.Contains("ausdauer") || normalized.Contains("kondition")) return FootballExerciseCategory.Endurance;
        if (normalized.Contains("sprint") || normalized.Contains("schnell")) return FootballExerciseCategory.Speed;
        if (normalized.Contains("technik") || normalized.Contains("pass") || normalized.Contains("abschluss")) return FootballExerciseCategory.Technique;
        if (normalized.Contains("takt") || normalized.Contains("press") || normalized.Contains("spielaufbau")) return FootballExerciseCategory.Tactics;
        return null;
    }

    private static double FeedbackScore(Guid exerciseId, IReadOnlyDictionary<Guid, FootballExerciseFeedbackAggregate> feedback)
    {
        if (!feedback.TryGetValue(exerciseId, out var item) || item.Count < 2) return 3.0;
        return item.Benefit * 0.55 + item.Fun * 0.25 + (6.0 - item.Difficulty) * 0.20;
    }

    private static string BuildExerciseReason(FootballExercise exercise, IReadOnlyDictionary<Guid, FootballExerciseFeedbackAggregate> feedback, int restrictedCount)
    {
        var parts = new List<string> { $"Passt zur Kategorie {exercise.Category} und zur aktuellen Teilnehmerzahl." };
        if (feedback.TryGetValue(exercise.Id, out var item) && item.Count >= 2)
            parts.Add($"Bisheriges Feedback: Nutzen {item.Benefit:0.0}/5 bei {item.Count} Bewertungen.");
        if (restrictedCount > 0 && exercise.Intensity < FootballIntensity.High)
            parts.Add("Intensität ist mit den aktuellen Einschränkungen vereinbar.");
        return string.Join(" ", parts);
    }

    private static List<string> BuildWarnings(
        IReadOnlyList<FootballTrainingPlanPlayerContext> all,
        IReadOnlyList<FootballTrainingPlanPlayerContext> available,
        IReadOnlyList<FootballTrainingPlanPlayerContext> restricted)
    {
        var warnings = new List<string>();
        var injured = all.Count(x => x.Availability == FootballAvailabilityStatus.Injured || x.MaxLoadPercent == 0);
        if (injured > 0) warnings.Add($"{injured} zugesagte Spieler werden wegen Verletzung/0 % Belastbarkeit nicht für aktive Blöcke eingeplant.");
        if (restricted.Count > 0) warnings.Add($"{restricted.Count} Spieler benötigen reduzierte Belastung oder Return-to-Play-Steuerung.");
        if (available.Count < 6) warnings.Add("Kleine Trainingsgruppe: Spielformen und Feldgrößen entsprechend reduzieren.");
        var highRecentLoad = available.Count(x => x.RecentLoad >= 1200);
        if (highRecentLoad > 0) warnings.Add($"{highRecentLoad} Spieler haben in den letzten 14 Tagen eine hohe dokumentierte RPE-Last.");
        return warnings;
    }
}
