import { apiRequest } from './client'

export type MysteryDifficulty = 'Easy' | 'Medium' | 'Hard'
export type MysteryGameStatus = 'Active' | 'ReadyForFinale' | 'Completed'
export type MysterySceneKind =
  | 'Story'
  | 'Dialogue'
  | 'Evidence'
  | 'Puzzle'
  | 'Decision'
  | 'RealTask'
  | 'LocationChange'

export interface MysteryLocationInput {
  id: string
  description: string
  availableFromProgress: number
  preferredUse: string
}

export interface CreateMysterySessionInput {
  players: string[]
  durationMinutes: number
  difficulty: MysteryDifficulty
  genre: string
  atmosphere: string
  locations: MysteryLocationInput[]
  availableItems: string[]
}

export interface MysterySession {
  id: string
  joinCode: string
  title: string
  status: MysteryGameStatus
  gameMaster: string
  notice?: string
  version: string
  chapter: number
  phase: string
  players: string[]
  difficulty: MysteryDifficulty
  durationMinutes: number
  genre: string
  atmosphere: string
  currentScene?: MysteryScene
  evidence: MysteryEvidence[]
  puzzles: MysteryPuzzleArchive[]
  characters: MysteryCharacter[]
  decisions: MysteryDecision[]
  solvedPuzzleCount: number
  usedHintCount: number
  visitedLocations: string[]
  notes: string[]
  questions: MysteryQuestion[]
  finale?: MysteryFinale
}

export interface MysteryScene {
  id: string
  chapter: number
  isOpening: boolean
  kind: MysterySceneKind
  title: string
  narrative: string
  prompt?: string
  puzzle?: MysteryPuzzle
  choices: MysteryChoice[]
  locationId?: string
  canAdvance: boolean
}

export interface MysteryPuzzle {
  id: string
  prompt: string
  inputType: 'text' | 'code'
  isSolved: boolean
}

export interface MysteryPuzzleArchive {
  id: string
  sceneTitle: string
  chapter: number
  prompt: string
  isSolved: boolean
}

export interface MysteryChoice {
  id: string
  label: string
}

export interface MysteryEvidence {
  id: string
  title: string
  description: string
}

export interface MysteryCharacter {
  id: string
  name: string
  role: string
  description: string
}

export interface MysteryDecision {
  sceneId: string
  choiceId: string
  choiceLabel: string
}

export interface MysteryQuestion {
  question: string
  answer: string
  askedAt: string
}

export interface MysteryFinale {
  correctCulprit: boolean
  culpritId: string
  culpritName: string
  motive: string
  timeline: string
  resolution: string
  redHerrings: string[]
  usedHints: number
  score: number
}

export interface MysteryHintResult {
  level: number
  hint: string
  session: MysterySession
}

export interface MysteryPuzzleResult {
  correct: boolean
  message: string
  session: MysterySession
}

export interface MysteryQuestionResult {
  answer: string
  session: MysterySession
}

export function createMysterySession(input: CreateMysterySessionInput) {
  return apiRequest<MysterySession>('/api/mistery/sessions', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function getMysterySession(sessionId: string) {
  return apiRequest<MysterySession>(`/api/mistery/sessions/${sessionId}`)
}

export function getMysterySessionByCode(joinCode: string) {
  return apiRequest<MysterySession>(
    `/api/mistery/sessions/code/${encodeURIComponent(joinCode)}`,
  )
}

export function advanceMysterySession(sessionId: string, version: string) {
  return sessionRequest<MysterySession>(sessionId, '/advance', { version })
}

export function submitMysteryPuzzle(
  sessionId: string,
  answer: string,
  version: string,
) {
  return sessionRequest<MysteryPuzzleResult>(sessionId, '/puzzle', {
    answer,
    version,
  })
}

export function submitMysteryDecision(
  sessionId: string,
  choiceId: string,
  version: string,
) {
  return sessionRequest<MysterySession>(sessionId, '/decision', {
    choiceId,
    version,
  })
}

export function requestMysteryHint(
  sessionId: string,
  level: number,
  version: string,
) {
  return sessionRequest<MysteryHintResult>(sessionId, '/hints', {
    level,
    version,
  })
}

export function askMysteryQuestion(
  sessionId: string,
  question: string,
  version: string,
) {
  return sessionRequest<MysteryQuestionResult>(sessionId, '/questions', {
    question,
    version,
  })
}

export function updateMysteryNotes(
  sessionId: string,
  notes: string[],
  version: string,
) {
  return apiRequest<MysterySession>(
    `/api/mistery/sessions/${sessionId}/notes`,
    {
      method: 'PUT',
      body: JSON.stringify({ notes, version }),
    },
  )
}

export function submitMysteryFinale(
  sessionId: string,
  input: {
    culpritId: string
    motive: string
    sequence?: string
    version: string
  },
) {
  return sessionRequest<MysterySession>(sessionId, '/finale', input)
}

function sessionRequest<T>(sessionId: string, path: string, body: unknown) {
  return apiRequest<T>(`/api/mistery/sessions/${sessionId}${path}`, {
    method: 'POST',
    body: JSON.stringify(body),
  })
}
