import { apiFetch, apiRequest } from './client'

export type Cs2DemoStatus = 'Uploaded' | 'Processing' | 'Completed' | 'Failed'
export type Cs2Availability = 'Yes' | 'Maybe' | 'No'
export type Cs2TrainingKind =
  | 'Flick'
  | 'Reaction'
  | 'TargetSwitching'
  | 'Tracking'
  | 'Utility'
  | 'Teamplay'

export interface Cs2MatchSummary {
  id: string
  seasonId: string
  status: Cs2DemoStatus
  mapName?: string
  playedAt?: string
  teamAName?: string
  teamBName?: string
  teamAScore: number
  teamBScore: number
  communityTeam?: 'A' | 'B'
  win?: boolean
  originalFileName: string
  uploadedAt: string
  completedAt?: string
  failureCode?: string
  failureMessage?: string
  attemptCount: number
}

export interface Cs2PlayState {
  sessionId?: string
  plannedStart?: string
  yes: number
  maybe: number
  missing: number
  substitutes: number
  fullStack: boolean
  mine?: Cs2Availability
  participants: Array<{
    userId: string
    displayName: string
    avatarUrl?: string
    availability: Cs2Availability
    availableFrom?: string
  }>
}

export interface Cs2Dashboard {
  season: { id: string; name: string; startsAt: string; endsAt?: string }
  summary: {
    matches: number
    wins: number
    losses: number
    winRate: number
    streak: number
    streakType: 'W' | 'L'
  }
  lastMatch?: Cs2MatchSummary
  play: Cs2PlayState
  leaders: Cs2Leaderboards
  awards: Cs2Award[]
  highlights: Cs2Highlight[]
  recommendation: Cs2Recommendation
}

export interface Cs2PlayerStats {
  userId: string
  displayName: string
  avatarUrl?: string
  matches: number
  kd: number
  adr: number
  kast: number
  headshotPercent: number
  hltvRating: number
  firstKills: number
  entryDifference: number
  tradeKills: number
  utilityDamage: number
  clutchesWon: number
  threeKills: number
  fourKills: number
  aces: number
}

export interface Cs2Record {
  matches: number
  wins: number
  losses: number
  winRate: number
}

export interface Cs2Squad {
  settings: { squadName?: string; squadTag?: string }
  readiness: {
    totalMembers: number; activePlayers: number; substitutes: number; inactivePlayers: number
    steamConnected: number; rolesAssigned: number; completedDemos: number
    completedSteps: number; totalSteps: number
  }
  players: Array<{
    id: string
    displayName: string
    avatarUrl?: string
    steamId64?: string
    steamName?: string
    steamAvatarUrl?: string
    role: string
    rosterStatus: 'Active' | 'Substitute' | 'Inactive'
    stats?: {
      matches: number
      wins: number
      losses: number
      kd: number
      adr: number
      kast: number
      headshotPercent: number
      hltvRating: number
      aces: number
      clutchesWon: number
    }
  }>
  summary: {
    playerRecord: Cs2Record
    fullSquadRecord: Cs2Record
  }
}

export interface Cs2Clip {
  id: string
  title: string
  description?: string
  originalFileName: string
  mimeType: string
  sizeBytes: number
  createdAt: string
  uploader: string
  avatarUrl?: string
  contentUrl: string
  canDelete: boolean
}

export interface Cs2Leaderboards {
  performance: Cs2PlayerStats[]
  impact: Cs2PlayerStats[]
  clutch: Cs2PlayerStats[]
  multiKills: Cs2PlayerStats[]
}

export interface Cs2Award {
  id: string
  name: string
  description: string
  icon: string
  displayName: string
  value: number
}

export interface Cs2Highlight {
  id: string
  matchId: string
  playerName: string
  roundNumber: number
  type: string
  title: string
  score: number
  startTick?: number
  endTick?: number
  videoStoragePath?: string
  createdAt?: string
  reactions?: Array<{ reaction: string; count: number }>
}

export interface Cs2Recommendation {
  key: string
  title: string
  reason: string
  kind: Cs2TrainingKind
  priority: number
  route: string
}

export interface Cs2Training {
  recommendations: Cs2Recommendation[]
  plan: {
    id: string
    planDate: string
    plannedMinutes: number
    recommendationReason?: string
    completedAt?: string
  }
  exercises: Array<{
    id: string
    kind: Cs2TrainingKind
    name: string
    description: string
    durationMinutes: number
  }>
  history: Cs2TrainingResult[]
}

export interface Cs2TrainingResult {
  id: string
  kind: Cs2TrainingKind
  hits: number
  misses: number
  accuracy: number
  reactionTimeMs: number
  flickTimeMs: number
  trackingPercent: number
  repetitions: number
  completedAt: string
}

export interface Cs2Trend {
  matches: number
  kd: number
  adr: number
  kast: number
  rating: number
}

export interface Cs2PlayerProfile {
  player: { id: string; displayName: string; avatarUrl?: string }
  steam?: {
    steamId64: string
    displayName: string
    avatarUrl?: string
    linkedAt: string
  }
  role: string
  favoriteMap?: string
  stats?: {
    matches: number
    wins: number
    kills: number
    deaths: number
    assists: number
    adr: number
    kast: number
    headshotPercent: number
    hltvRating: number
    utilityDamage: number
    clutchesWon: number
    aces: number
  }
  trends: { last5: Cs2Trend; last20: Cs2Trend }
  awards: Cs2Award[]
  highlights: Cs2Highlight[]
  training: {
    sessions: number
    averageAccuracy: number
    recent: Cs2TrainingResult[]
  }
}

export interface Cs2Recap {
  season: { id: string; name: string; startsAt: string; endsAt?: string }
  summary: { matches: number; wins: number; losses: number; winRate: number }
  bestMap?: { map: string; matches: number; wins: number; winRate: number }
  worstMap?: { map: string; matches: number; wins: number; winRate: number }
  maps: Array<{ map: string; matches: number; wins: number; winRate: number }>
  highlights: Cs2Highlight[]
  awards: Cs2Award[]
  winStreak: number
  aces: number
  clutches: number
}

const base = (organizationId: string) =>
  `/api/organizations/${organizationId}/counter-strike`

export const getCs2Dashboard = (organizationId: string) =>
  apiRequest<Cs2Dashboard>(`${base(organizationId)}/dashboard`)

export const getCs2Play = (organizationId: string) =>
  apiRequest<Cs2PlayState>(`${base(organizationId)}/play`)

export const updateCs2Play = (
  organizationId: string,
  input: {
    availability: Cs2Availability
    availableFrom?: string
    plannedStart?: string
  },
) =>
  apiRequest<Cs2PlayState>(`${base(organizationId)}/play`, {
    method: 'PUT',
    body: JSON.stringify(input),
  })

export const listCs2Matches = (organizationId: string) =>
  apiRequest<Cs2MatchSummary[]>(`${base(organizationId)}/matches`)

export async function uploadCs2Match(organizationId: string, demo: File) {
  const form = new FormData()
  form.append('demo', demo)
  return apiRequest<Cs2MatchSummary>(`${base(organizationId)}/matches`, {
    method: 'POST',
    body: form,
  })
}

export const retryCs2Match = (organizationId: string, matchId: string) =>
  apiRequest<Cs2MatchSummary>(
    `${base(organizationId)}/matches/${matchId}/retry`,
    { method: 'POST' },
  )

export const getCs2Match = (organizationId: string, matchId: string) =>
  apiRequest<{
    match: Cs2MatchSummary
    players: Array<{
      id: string
      displayName: string
      teamName: string
      kills: number
      deaths: number
      assists: number
      adr: number
      kast: number
      headshotPercent: number
      utilityDamage: number
      firstKills: number
      firstDeaths: number
      tradeKills: number
      bombPlants: number
      bombDefuses: number
      hltvRating: number
    }>
    highlights: Cs2Highlight[]
    story: string[]
  }>(`${base(organizationId)}/matches/${matchId}`)

export const getCs2Season = (organizationId: string) =>
  apiRequest<{
    id: string
    name: string
    startsAt: string
    endsAt?: string
    isActive: boolean
    matches: number
    wins: number
    losses: number
    winRate: number
  }>(`${base(organizationId)}/seasons/current`)

export const createCs2Season = (organizationId: string, name: string) =>
  apiRequest<{ id: string; name: string }>(`${base(organizationId)}/seasons`, {
    method: 'POST',
    body: JSON.stringify({ name }),
  })

export const closeCs2Season = (organizationId: string, seasonId: string) =>
  apiRequest<void>(`${base(organizationId)}/seasons/${seasonId}/close`, {
    method: 'POST',
  })

export const getCs2Leaderboards = (organizationId: string) =>
  apiRequest<Cs2Leaderboards>(`${base(organizationId)}/leaderboards`)

export const getCs2Recap = (organizationId: string) =>
  apiRequest<Cs2Recap>(`${base(organizationId)}/recap`)

export const listCs2Highlights = (organizationId: string) =>
  apiRequest<Cs2Highlight[]>(`${base(organizationId)}/highlights`)

export const toggleCs2Reaction = (
  organizationId: string,
  highlightId: string,
  reaction: string,
) =>
  apiRequest<void>(
    `${base(organizationId)}/highlights/${highlightId}/reactions`,
    { method: 'POST', body: JSON.stringify({ reaction }) },
  )

export const getCs2Squad = (organizationId: string) =>
  apiRequest<Cs2Squad>(`${base(organizationId)}/squad/overview`)

export const updateCs2Role = (organizationId: string, role: string) =>
  apiRequest<{ role: string }>(`${base(organizationId)}/squad/me/role`, {
    method: 'PUT',
    body: JSON.stringify({ role }),
  })

export const updateCs2SquadSettings = (organizationId: string, squadName: string, squadTag: string) =>
  apiRequest(`${base(organizationId)}/squad/settings`, {
    method: 'PUT', body: JSON.stringify({ squadName, squadTag }),
  })

export const updateCs2RosterStatus = (organizationId: string, userId: string, status: string) =>
  apiRequest(`${base(organizationId)}/squad/${userId}/status`, {
    method: 'PUT', body: JSON.stringify({ status }),
  })

export const listCs2Clips = (organizationId: string) =>
  apiRequest<Cs2Clip[]>(`${base(organizationId)}/clips`)

export async function uploadCs2Clip(organizationId: string, title: string, description: string, file: File) {
  const form = new FormData()
  form.append('title', title)
  form.append('description', description)
  form.append('file', file)
  return apiRequest<{ id: string }>(`${base(organizationId)}/clips`, { method: 'POST', body: form })
}

export const deleteCs2Clip = (organizationId: string, clipId: string) =>
  apiRequest<void>(`${base(organizationId)}/clips/${clipId}`, { method: 'DELETE' })

export async function fetchCs2ClipContent(path: string) {
  const response = await apiFetch(path)
  if (!response.ok) throw new Error('Clip konnte nicht geladen werden.')
  return response.blob()
}

export const getCs2PlayerProfile = (
  organizationId: string,
  userId: string,
) => apiRequest<Cs2PlayerProfile>(`${base(organizationId)}/squad/${userId}`)

export const getCs2Training = (organizationId: string) =>
  apiRequest<Cs2Training>(`${base(organizationId)}/training`)

export const getCs2Utility = (organizationId: string) =>
  apiRequest<
    Array<{
      id: string
      mapName: string
      name: string
      description: string
      position?: string
      target?: string
      mediaUrl?: string
    }>
  >(`${base(organizationId)}/training/utility`)

export const saveCs2TrainingResult = (
  organizationId: string,
  input: {
    kind: Cs2TrainingKind
    durationSeconds: number
    hits: number
    misses: number
    reactionTimeMs: number
    flickTimeMs: number
    trackingPercent: number
    repetitions: number
    trainingPlanId?: string
    trainingExerciseId?: string
  },
) =>
  apiRequest<Cs2TrainingResult>(
    `${base(organizationId)}/training/results`,
    { method: 'POST', body: JSON.stringify(input) },
  )

export const getCs2Challenges = (organizationId: string) =>
  apiRequest<
    Array<{
      id: string
      name: string
      description: string
      targetValue: number
      endsAt: string
      mine?: { value: number; completedAt?: string }
      squad: Array<{ userId: string; value: number; completedAt?: string }>
    }>
  >(`${base(organizationId)}/challenges`)
