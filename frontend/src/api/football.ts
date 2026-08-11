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
