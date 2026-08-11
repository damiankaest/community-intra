import { apiRequest } from './client'

export type FootballTeamRole = 'Player' | 'Coach' | 'Staff'
export type FootballPosition = 'Goalkeeper' | 'Defender' | 'Midfielder' | 'Forward'
export type FootballExerciseCategory =
  | 'Stability'
  | 'Strength'
  | 'Mobility'
  | 'Endurance'
  | 'Speed'
  | 'Technique'
  | 'Tactics'
export type FootballExerciseLocation = 'Pitch' | 'Home' | 'Gym' | 'Anywhere'
export type FootballIntensity = 'Low' | 'Medium' | 'High'
export type FootballAttendanceStatus = 'Pending' | 'Accepted' | 'Declined' | 'Maybe'
export type FootballSessionKind = 'Training' | 'Match' | 'Individual' | 'PerformanceTest'
export type FootballAvailabilityStatus = 'Fit' | 'Limited' | 'ReturnToPlay' | 'Injured'

export interface FootballMemberProfile {
  id: string
  organizationId: string
  memberId: string
  teamRole: FootballTeamRole
  position?: FootballPosition
  shirtNumber?: number
  description?: string
  strengths: string[]
  developmentAreas: string[]
  secondaryPositions: string[]
  updatedAt: string
}

export interface UpsertFootballProfileInput {
  teamRole: FootballTeamRole
  position?: FootballPosition
  shirtNumber?: number
  description?: string
  strengths: string[]
  developmentAreas: string[]
  secondaryPositions: string[]
}

export interface FootballPlayerAvailability {
  id: string
  organizationId: string
  memberId: string
  status: FootballAvailabilityStatus
  maxLoadPercent: number
  note?: string
  updatedAt: string
  updatedByMemberId: string
}

export interface FootballExercise {
  id: string
  organizationId: string
  title: string
  description: string
  category: FootballExerciseCategory
  location: FootballExerciseLocation
  intensity: FootballIntensity
  minPlayers: number
  maxPlayers?: number
  defaultDurationMinutes: number
  focus: string
  equipment: string[]
  tags: string[]
  createdByMemberId: string
  createdAt: string
  updatedAt: string
  isArchived: boolean
}

export interface CreateFootballExerciseInput {
  title: string
  description?: string
  category: FootballExerciseCategory
  location: FootballExerciseLocation
  intensity: FootballIntensity
  minPlayers: number
  maxPlayers?: number
  defaultDurationMinutes: number
  focus?: string
  equipment: string[]
  tags: string[]
}

export interface FootballSession {
  id: string
  organizationId: string
  kind: FootballSessionKind
  title: string
  focus?: string
  location?: string
  opponent?: string
  startsAt: string
  durationMinutes: number
  createdByMemberId: string
  createdAt: string
  updatedAt: string
  isCancelled: boolean
}

export interface CreateFootballSessionInput {
  kind: FootballSessionKind
  title: string
  focus?: string
  location?: string
  opponent?: string
  startsAt: string
  durationMinutes: number
}

export interface FootballAttendance {
  id: string
  organizationId: string
  sessionId: string
  memberId: string
  status: FootballAttendanceStatus
  note?: string
  updatedAt: string
}

export interface FootballSessionLoad {
  id: string
  organizationId: string
  sessionId: string
  memberId: string
  rpe: number
  minutesCompleted?: number
  note?: string
  updatedAt: string
}

export interface FootballTrainingBlock {
  id: string
  organizationId: string
  sessionId: string
  exerciseId?: string
  title: string
  description?: string
  coachingPoints?: string
  sortOrder: number
  durationMinutes: number
  responsibleMemberId?: string
  aiReason?: string
}

export interface FootballSessionDetail {
  session: FootballSession
  attendance: FootballAttendance[]
  blocks: FootballTrainingBlock[]
  load: FootballSessionLoad[]
  availability: FootballPlayerAvailability[]
}

export interface FootballTrainingBlockInput {
  exerciseId?: string
  title: string
  description?: string
  coachingPoints?: string
  durationMinutes: number
  responsibleMemberId?: string
  aiReason?: string
}

export interface FootballTrainingHistoryEntry {
  session: FootballSession
  load?: FootballSessionLoad
  plannedMinutes: number
  trainingLoad?: number
}

export interface FootballExerciseFeedback {
  id: string
  organizationId: string
  sessionId: string
  trainingBlockId: string
  exerciseId?: string
  memberId: string
  fun: number
  difficulty: number
  benefit: number
  comment?: string
  createdAt: string
  updatedAt: string
}

export interface FootballExerciseFeedbackSummary {
  trainingBlockId: string
  exerciseId?: string
  count: number
  fun: number
  difficulty: number
  benefit: number
}

export interface FootballSessionFeedback {
  feedback: FootballExerciseFeedback[]
  summary: FootballExerciseFeedbackSummary[]
}

export interface FootballTrainingPlanPlayerContext {
  memberId: string
  position?: FootballPosition
  availability: FootballAvailabilityStatus
  maxLoadPercent: number
  recentLoad: number
  developmentAreas: string[]
}

export interface FootballTrainingPlanBlockSuggestion {
  exerciseId?: string
  title: string
  description?: string
  coachingPoints?: string
  durationMinutes: number
  responsibleMemberId?: string
  reason: string
  intensity: FootballIntensity
}

export interface FootballTrainingPlanSuggestion {
  sessionId: string
  focus: string
  playerCount: number
  players: FootballTrainingPlanPlayerContext[]
  blocks: FootballTrainingPlanBlockSuggestion[]
  warnings: string[]
}

const base = (organizationId: string) =>
  `/api/organizations/${organizationId}/football`

export const listFootballProfiles = (organizationId: string) =>
  apiRequest<FootballMemberProfile[]>(`${base(organizationId)}/profiles`)

export const upsertFootballProfile = (
  organizationId: string,
  memberId: string,
  input: UpsertFootballProfileInput,
) =>
  apiRequest<FootballMemberProfile>(`${base(organizationId)}/profiles/${memberId}`, {
    method: 'PUT',
    body: JSON.stringify(input),
  })

export const listFootballAvailability = (organizationId: string) =>
  apiRequest<FootballPlayerAvailability[]>(`${base(organizationId)}/availability`)

export const updateFootballAvailability = (
  organizationId: string,
  memberId: string,
  status: FootballAvailabilityStatus,
  maxLoadPercent: number,
  note?: string,
) =>
  apiRequest<FootballPlayerAvailability>(
    `${base(organizationId)}/availability/${memberId}`,
    {
      method: 'PUT',
      body: JSON.stringify({ status, maxLoadPercent, note }),
    },
  )

export const listFootballExercises = (
  organizationId: string,
  category?: FootballExerciseCategory,
  players?: number,
) => {
  const params = new URLSearchParams()
  if (category) params.set('category', category)
  if (players) params.set('players', String(players))
  const query = params.size ? `?${params}` : ''
  return apiRequest<FootballExercise[]>(`${base(organizationId)}/exercises${query}`)
}

export const createFootballExercise = (
  organizationId: string,
  input: CreateFootballExerciseInput,
) =>
  apiRequest<FootballExercise>(`${base(organizationId)}/exercises`, {
    method: 'POST',
    body: JSON.stringify(input),
  })

export const listFootballSessions = (
  organizationId: string,
  from?: string,
  to?: string,
) => {
  const params = new URLSearchParams()
  if (from) params.set('from', from)
  if (to) params.set('to', to)
  const query = params.size ? `?${params}` : ''
  return apiRequest<FootballSession[]>(`${base(organizationId)}/sessions${query}`)
}

export const createFootballSession = (
  organizationId: string,
  input: CreateFootballSessionInput,
) =>
  apiRequest<FootballSession>(`${base(organizationId)}/sessions`, {
    method: 'POST',
    body: JSON.stringify(input),
  })

export const getFootballSession = (organizationId: string, sessionId: string) =>
  apiRequest<FootballSessionDetail>(`${base(organizationId)}/sessions/${sessionId}`)

export const suggestFootballTrainingPlan = (
  organizationId: string,
  sessionId: string,
) =>
  apiRequest<FootballTrainingPlanSuggestion>(
    `${base(organizationId)}/sessions/${sessionId}/plan/suggest`,
    { method: 'POST' },
  )

export const updateFootballAttendance = (
  organizationId: string,
  sessionId: string,
  memberId: string,
  status: FootballAttendanceStatus,
  note?: string,
) =>
  apiRequest<FootballAttendance>(
    `${base(organizationId)}/sessions/${sessionId}/attendance/${memberId}`,
    {
      method: 'PUT',
      body: JSON.stringify({ status, note }),
    },
  )

export const updateFootballSessionLoad = (
  organizationId: string,
  sessionId: string,
  memberId: string,
  rpe: number,
  minutesCompleted?: number,
  note?: string,
) =>
  apiRequest<FootballSessionLoad>(
    `${base(organizationId)}/sessions/${sessionId}/load/${memberId}`,
    {
      method: 'PUT',
      body: JSON.stringify({ rpe, minutesCompleted, note }),
    },
  )

export const getFootballTrainingHistory = (
  organizationId: string,
  memberId: string,
  take = 20,
) =>
  apiRequest<FootballTrainingHistoryEntry[]>(
    `${base(organizationId)}/members/${memberId}/history?take=${take}`,
  )

export const getFootballSessionFeedback = (
  organizationId: string,
  sessionId: string,
) =>
  apiRequest<FootballSessionFeedback>(
    `${base(organizationId)}/sessions/${sessionId}/feedback`,
  )

export const updateFootballExerciseFeedback = (
  organizationId: string,
  sessionId: string,
  trainingBlockId: string,
  memberId: string,
  input: { fun: number; difficulty: number; benefit: number; comment?: string },
) =>
  apiRequest<FootballExerciseFeedback>(
    `${base(organizationId)}/sessions/${sessionId}/blocks/${trainingBlockId}/feedback/${memberId}`,
    {
      method: 'PUT',
      body: JSON.stringify(input),
    },
  )

export const replaceFootballTrainingBlocks = (
  organizationId: string,
  sessionId: string,
  blocks: FootballTrainingBlockInput[],
) =>
  apiRequest<FootballTrainingBlock[]>(
    `${base(organizationId)}/sessions/${sessionId}/blocks`,
    {
      method: 'PUT',
      body: JSON.stringify({ blocks }),
    },
  )
