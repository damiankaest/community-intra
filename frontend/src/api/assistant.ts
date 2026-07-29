import { apiRequest } from './client'
import type { Priority } from './features'

export type AssistantTone = 'Theme' | 'Neutral'

export interface WorkPlanMaterial {
  name: string
  quantity: string
  notes?: string
}

export interface WorkPlanTask {
  title: string
  description: string
  priority: Priority
  acceptanceCriteria: string[]
}

export interface WorkPlanProposal {
  title: string
  executiveSummary: string
  managementMessage: string
  materials: WorkPlanMaterial[]
  tasks: WorkPlanTask[]
}

export interface WorkPlanDraft {
  id: string
  tone: AssistantTone
  prompt: string
  proposal: WorkPlanProposal
  model: string
  createdAt: string
  expiresAt: string
  confirmedAt?: string
  projectId?: string
  concurrencyToken: string
}

export interface ConfirmedWorkPlan {
  draftId: string
  projectId: string
  taskIds: string[]
  alreadyConfirmed: boolean
}

export interface AiAssistantAvailability {
  isConfigured: boolean
  model: string
}

const assistantBase = (organizationId: string) =>
  `/api/organizations/${organizationId}/assistant`

export function getAiAssistantAvailability(organizationId: string) {
  return apiRequest<AiAssistantAvailability>(
    `${assistantBase(organizationId)}/availability`,
  )
}

export function prepareWorkPlan(
  organizationId: string,
  prompt: string,
  tone: AssistantTone,
) {
  return apiRequest<WorkPlanDraft>(
    `${assistantBase(organizationId)}/work-plan-drafts`,
    {
      method: 'POST',
      body: JSON.stringify({ prompt, tone }),
    },
  )
}

export function confirmWorkPlan(
  organizationId: string,
  draftId: string,
  concurrencyToken: string,
) {
  return apiRequest<ConfirmedWorkPlan>(
    `${assistantBase(organizationId)}/work-plan-drafts/${draftId}/confirm`,
    {
      method: 'POST',
      body: JSON.stringify({ concurrencyToken }),
    },
  )
}
