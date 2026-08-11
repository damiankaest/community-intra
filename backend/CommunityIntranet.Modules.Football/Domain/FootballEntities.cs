using CommunityIntranet.BuildingBlocks.Tenancy;

namespace CommunityIntranet.Modules.Football.Domain;

public enum FootballTeamRole { Player = 0, Coach = 10, Staff = 20 }
public enum FootballPosition { Goalkeeper = 0, Defender = 10, Midfielder = 20, Forward = 30 }
public enum FootballExerciseCategory { Stability = 0, Strength = 10, Mobility = 20, Endurance = 30, Speed = 40, Technique = 50, Tactics = 60 }
public enum FootballExerciseLocation { Pitch = 0, Home = 10, Gym = 20, Anywhere = 30 }
public enum FootballIntensity { Low = 0, Medium = 10, High = 20 }
public enum FootballAttendanceStatus { Pending = 0, Accepted = 10, Declined = 20, Maybe = 30 }
public enum FootballSessionKind { Training = 0, Match = 10, Individual = 20, PerformanceTest = 30 }
public enum FootballAvailabilityStatus { Fit = 0, Limited = 10, ReturnToPlay = 20, Injured = 30 }

public sealed class FootballMemberProfile : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MemberId { get; set; }
    public FootballTeamRole TeamRole { get; set; } = FootballTeamRole.Player;
    public FootballPosition? Position { get; set; }
    public int? ShirtNumber { get; set; }
    public string? Description { get; set; }
    public string[] Strengths { get; set; } = [];
    public string[] DevelopmentAreas { get; set; } = [];
    public string[] SecondaryPositions { get; set; } = [];
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class FootballPlayerAvailability : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MemberId { get; set; }
    public FootballAvailabilityStatus Status { get; set; } = FootballAvailabilityStatus.Fit;
    public int MaxLoadPercent { get; set; } = 100;
    public string? Note { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedByMemberId { get; set; }
}

public sealed class FootballExercise : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FootballExerciseCategory Category { get; set; }
    public FootballExerciseLocation Location { get; set; }
    public FootballIntensity Intensity { get; set; }
    public int MinPlayers { get; set; } = 1;
    public int? MaxPlayers { get; set; }
    public int DefaultDurationMinutes { get; set; } = 10;
    public string Focus { get; set; } = string.Empty;
    public string[] Equipment { get; set; } = [];
    public string[] Tags { get; set; } = [];
    public Guid CreatedByMemberId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsArchived { get; set; }
}

public sealed class FootballSession : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public FootballSessionKind Kind { get; set; } = FootballSessionKind.Training;
    public string Title { get; set; } = string.Empty;
    public string? Focus { get; set; }
    public string? Location { get; set; }
    public string? Opponent { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 90;
    public Guid CreatedByMemberId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsCancelled { get; set; }
}

public sealed class FootballAttendance : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SessionId { get; set; }
    public Guid MemberId { get; set; }
    public FootballAttendanceStatus Status { get; set; } = FootballAttendanceStatus.Pending;
    public string? Note { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class FootballSessionLoad : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SessionId { get; set; }
    public Guid MemberId { get; set; }
    public int Rpe { get; set; }
    public int? MinutesCompleted { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class FootballTrainingBlock : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SessionId { get; set; }
    public Guid? ExerciseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoachingPoints { get; set; }
    public int SortOrder { get; set; }
    public int DurationMinutes { get; set; }
    public Guid? ResponsibleMemberId { get; set; }
    public string? AiReason { get; set; }
}
