import { apiFetch, apiProblem, apiRequest } from './client'
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
  materials: WorkPlanMaterial[]
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

export type AssistantMessageRole = 'User' | 'Assistant'
export type AssistantActionKind =
  'CreateTask' | 'UpdateTask' | 'CreateProject' | 'AddTaskComment'
export type AssistantActionStatus = 'Pending' | 'Confirmed' | 'Rejected'

export interface AssistantMessage {
  id: string
  role: AssistantMessageRole
  content: string
  createdAt: string
}

export interface AssistantActionPayload {
  title?: string
  name?: string
  description?: string
  taskId?: string
  projectId?: string
  parentTaskId?: string
  status?: string
  priority?: Priority
  assignedMemberId?: string
  dueDate?: string
  body?: string
  mentionedMemberIds?: string[]
  materials?: WorkPlanMaterial[]
}

export interface AssistantAction {
  id: string
  kind: AssistantActionKind
  status: AssistantActionStatus
  payload: AssistantActionPayload
  createdAt: string
  completedAt?: string
  resultEntityId?: string
  concurrencyToken: string
}

export interface AssistantConversation {
  id?: string
  tone: AssistantTone
  messages: AssistantMessage[]
  actions: AssistantAction[]
}

export interface ConfirmedAssistantAction {
  actionId: string
  kind: AssistantActionKind
  resultEntityId: string
  alreadyConfirmed: boolean
}

export type AssistantStreamEvent =
  | {
      type: 'message_ack'
      conversationId: string
      message: AssistantMessage
    }
  | { type: 'delta'; delta: string }
  | { type: 'action'; action: AssistantAction }
  | { type: 'done'; message: AssistantMessage }
  | { type: 'error'; message: string }

const assistantBase = (organizationId: string) =>
  `/api/organizations/${organizationId}/assistant`

export function getAiAssistantAvailability(organizationId: string) {
  return apiRequest<AiAssistantAvailability>(
    `${assistantBase(organizationId)}/availability`,
  )
}

export function getAssistantChat(organizationId: string) {
  return apiRequest<AssistantConversation>(
    `${assistantBase(organizationId)}/chat`,
  )
}

export async function streamAssistantMessage(
  organizationId: string,
  message: string,
  tone: AssistantTone,
  onEvent: (event: AssistantStreamEvent) => void,
  signal?: AbortSignal,
) {
  const response = await apiFetch(
    `${assistantBase(organizationId)}/chat/messages`,
    {
      method: 'POST',
      body: JSON.stringify({ message, tone }),
      signal,
    },
  )
  if (!response.ok) {
    throw await apiProblem(response)
  }

  if (!response.body) {
    throw new Error('Der Browser kann die Chat-Antwort nicht streamen.')
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  while (true) {
    const { value, done } = await reader.read()
    buffer += decoder.decode(value, { stream: !done })
    const lines = buffer.split('\n')
    buffer = lines.pop() ?? ''
    for (const line of lines) {
      if (line.trim()) {
        onEvent(JSON.parse(line) as AssistantStreamEvent)
      }
    }

    if (done) {
      if (buffer.trim()) {
        onEvent(JSON.parse(buffer) as AssistantStreamEvent)
      }
      break
    }
  }
}

export function confirmAssistantAction(
  organizationId: string,
  actionId: string,
  concurrencyToken: string,
) {
  return apiRequest<ConfirmedAssistantAction>(
    `${assistantBase(organizationId)}/actions/${actionId}/confirm`,
    {
      method: 'POST',
      body: JSON.stringify({ concurrencyToken }),
    },
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
