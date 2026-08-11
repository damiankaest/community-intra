import { apiRequest } from './client'
import type { FootballTrainingBlock } from './football'

export type FootballLiveTrainingStatus = 'NotStarted' | 'Running' | 'Paused' | 'Completed'

export interface FootballLiveTrainingRun {
  id: string
  organizationId: string
  sessionId: string
  status: FootballLiveTrainingStatus
  activeTrainingBlockId?: string
  startedAt?: string
  pausedAt?: string
  completedAt?: string
  accumulatedPausedSeconds: number
  updatedByMemberId: string
  updatedAt: string
}

export interface FootballLiveTrainingBlockRun {
  id: string
  organizationId: string
  sessionId: string
  trainingBlockId: string
  startedAt?: string
  pausedAt?: string
  completedAt?: string
  accumulatedSeconds: number
  isCompleted: boolean
  updatedAt: string
}

export interface FootballTrainingCoachTask {
  id: string
  organizationId: string
  sessionId: string
  trainingBlockId: string
  memberId: string
  role: string
  task: string
  sortOrder: number
  updatedAt: string
  updatedByMemberId: string
}

export interface FootballTrainingCoachTaskInput {
  memberId: string
  role: string
  task: string
}

export interface FootballLiveTrainingState {
  serverNow: string
  run?: FootballLiveTrainingRun
  blocks: FootballTrainingBlock[]
  blockRuns: FootballLiveTrainingBlockRun[]
}

const base = (organizationId: string, sessionId: string) =>
  `/api/organizations/${organizationId}/football/sessions/${sessionId}/live`

const blockBase = (organizationId: string, sessionId: string) =>
  `/api/organizations/${organizationId}/football/sessions/${sessionId}/blocks`

export const getFootballLiveTraining = (organizationId: string, sessionId: string) =>
  apiRequest<FootballLiveTrainingState>(`${base(organizationId, sessionId)}/`)

export const listFootballTrainingCoachTasks = (organizationId: string, sessionId: string) =>
  apiRequest<FootballTrainingCoachTask[]>(`${blockBase(organizationId, sessionId)}/coach-tasks`)

export const replaceFootballTrainingCoachTasks = (
  organizationId: string,
  sessionId: string,
  blockId: string,
  tasks: FootballTrainingCoachTaskInput[],
) => apiRequest<FootballTrainingCoachTask[]>(`${blockBase(organizationId, sessionId)}/${blockId}/coach-tasks`, {
  method: 'PUT',
  body: JSON.stringify({ tasks }),
})

export const updateFootballTrainingBriefing = (
  organizationId: string,
  sessionId: string,
  blockId: string,
  setupAndFlow?: string,
  coachingPoints?: string,
) => apiRequest<FootballTrainingBlock>(`${blockBase(organizationId, sessionId)}/${blockId}/briefing`, {
  method: 'PUT',
  body: JSON.stringify({ setupAndFlow, coachingPoints }),
})

export const startFootballLiveTraining = (organizationId: string, sessionId: string) =>
  apiRequest<FootballLiveTrainingState>(`${base(organizationId, sessionId)}/start`, { method: 'POST' })

export const pauseFootballLiveTraining = (organizationId: string, sessionId: string) =>
  apiRequest<FootballLiveTrainingState>(`${base(organizationId, sessionId)}/pause`, { method: 'POST' })

export const resumeFootballLiveTraining = (organizationId: string, sessionId: string) =>
  apiRequest<FootballLiveTrainingState>(`${base(organizationId, sessionId)}/resume`, { method: 'POST' })

export const completeFootballLiveTraining = (organizationId: string, sessionId: string) =>
  apiRequest<FootballLiveTrainingState>(`${base(organizationId, sessionId)}/complete`, { method: 'POST' })

export const activateFootballLiveTrainingBlock = (organizationId: string, sessionId: string, blockId: string) =>
  apiRequest<FootballLiveTrainingState>(`${base(organizationId, sessionId)}/blocks/${blockId}/activate`, { method: 'POST' })

export const pauseFootballLiveTrainingBlock = (organizationId: string, sessionId: string, blockId: string) =>
  apiRequest<FootballLiveTrainingState>(`${base(organizationId, sessionId)}/blocks/${blockId}/pause`, { method: 'POST' })

export const resumeFootballLiveTrainingBlock = (organizationId: string, sessionId: string, blockId: string) =>
  apiRequest<FootballLiveTrainingState>(`${base(organizationId, sessionId)}/blocks/${blockId}/resume`, { method: 'POST' })

export const resetFootballLiveTrainingBlock = (organizationId: string, sessionId: string, blockId: string) =>
  apiRequest<FootballLiveTrainingState>(`${base(organizationId, sessionId)}/blocks/${blockId}/reset`, { method: 'POST' })

export const completeFootballLiveTrainingBlock = (organizationId: string, sessionId: string, blockId: string) =>
  apiRequest<FootballLiveTrainingState>(`${base(organizationId, sessionId)}/blocks/${blockId}/complete`, { method: 'POST' })
