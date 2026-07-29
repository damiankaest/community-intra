import { apiFetch, apiProblem, apiRequest } from './client'

export type Priority = 'Low' | 'Normal' | 'High' | 'Critical'
export type ProjectStatus =
  'Idea' | 'Planned' | 'InProgress' | 'Blocked' | 'Completed' | 'Cancelled'
export type TaskStatus =
  'Open' | 'InProgress' | 'Blocked' | 'Done' | 'Cancelled'
export type IncidentSeverity =
  'Informational' | 'Low' | 'Medium' | 'High' | 'Catastrophic'
export type IncidentStatus =
  'Reported' | 'UnderInvestigation' | 'Resolved' | 'Rejected'

export interface Project {
  id: string
  name: string
  description?: string
  status: ProjectStatus
  priority: Priority
  ownerMemberId?: string
  startDate?: string
  dueDate?: string
  createdAt: string
  updatedAt: string
  completedAt?: string
  concurrencyToken: string
}

export interface SaveProjectInput {
  name: string
  description?: string
  status: ProjectStatus
  priority: Priority
  ownerMemberId?: string
  startDate?: string
  dueDate?: string
  concurrencyToken?: string
}

export interface WorkTask {
  id: string
  projectId?: string
  parentTaskId?: string
  title: string
  description?: string
  status: TaskStatus
  priority: Priority
  assignedMemberId?: string
  createdByMemberId: string
  dueDate?: string
  createdAt: string
  updatedAt: string
  completedAt?: string
  concurrencyToken: string
}

export interface SaveTaskInput {
  title: string
  description?: string
  status: TaskStatus
  priority: Priority
  projectId?: string
  parentTaskId?: string
  assignedMemberId?: string
  dueDate?: string
  concurrencyToken?: string
}

export interface TaskComment {
  id: string
  taskId: string
  authorMemberId: string
  authorDisplayName?: string
  body: string
  createdAt: string
}

export interface TaskAttachment {
  id: string
  taskId: string
  uploadedByMemberId: string
  uploadedByDisplayName?: string
  fileName: string
  mediaType: string
  size: number
  createdAt: string
  contentUrl: string
  thumbnailUrl?: string
}

export interface TaskDetails {
  task: WorkTask
  subtasks: WorkTask[]
  comments: TaskComment[]
  attachments: TaskAttachment[]
}

export interface Incident {
  id: string
  title: string
  description: string
  category: string
  severity: IncidentSeverity
  status: IncidentStatus
  reportedByMemberId: string
  responsibleMemberId?: string
  resolution?: string
  lessonsLearned?: string
  occurredAt: string
  createdAt: string
  updatedAt: string
  resolvedAt?: string
  concurrencyToken: string
}

export interface SaveIncidentInput {
  title: string
  description: string
  category: string
  severity: IncidentSeverity
  status: IncidentStatus
  responsibleMemberId?: string
  resolution?: string
  lessonsLearned?: string
  occurredAt: string
  concurrencyToken?: string
}

export interface Award {
  id: string
  name: string
  description: string
  awardedToMemberId: string
  awardedByMemberId: string
  awardedAt: string
  icon: string
  category: string
  isPublic: boolean
}

export interface GrantAwardInput {
  name: string
  description: string
  awardedToMemberId: string
  icon: string
  category: string
  isPublic: boolean
}

export interface AwardTemplate {
  name: string
  descriptionTemplate: string
}

export interface Activity {
  id: string
  activityType: string
  actorMemberId: string
  actorDisplayName?: string
  entityType: string
  entityId: string
  data: Record<string, string | undefined>
  eventVersion: number
  createdAt: string
}

export interface CurrentAward {
  id: string
  name: string
  description: string
  awardedToDisplayName?: string
  awardedAt: string
  icon: string
  category: string
}

export interface Dashboard {
  memberCount: number
  openTaskCount: number
  activeProjectCount: number
  openIncidentCount: number
  currentAward?: CurrentAward
  recentActivities: Activity[]
  systemMessage: string
}

const base = (organizationId: string, resource: string) =>
  `/api/organizations/${organizationId}/${resource}`

export function listProjects(organizationId: string) {
  return apiRequest<Project[]>(base(organizationId, 'projects'))
}

export function createProject(organizationId: string, input: SaveProjectInput) {
  return apiRequest<Project>(base(organizationId, 'projects'), {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function updateProject(
  organizationId: string,
  projectId: string,
  input: SaveProjectInput,
) {
  return apiRequest<Project>(
    `${base(organizationId, 'projects')}/${projectId}`,
    { method: 'PUT', body: JSON.stringify(input) },
  )
}

export function listTasks(organizationId: string) {
  return apiRequest<WorkTask[]>(base(organizationId, 'tasks'))
}

export function createTask(organizationId: string, input: SaveTaskInput) {
  return apiRequest<WorkTask>(base(organizationId, 'tasks'), {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function updateTask(
  organizationId: string,
  taskId: string,
  input: SaveTaskInput,
) {
  return apiRequest<WorkTask>(`${base(organizationId, 'tasks')}/${taskId}`, {
    method: 'PUT',
    body: JSON.stringify(input),
  })
}

export function getTaskDetails(organizationId: string, taskId: string) {
  return apiRequest<TaskDetails>(
    `${base(organizationId, 'tasks')}/${taskId}/details`,
  )
}

export function addTaskComment(
  organizationId: string,
  taskId: string,
  body: string,
  mentionedMemberIds: string[] = [],
) {
  return apiRequest<TaskComment>(
    `${base(organizationId, 'tasks')}/${taskId}/comments`,
    {
      method: 'POST',
      body: JSON.stringify({ body, mentionedMemberIds }),
    },
  )
}

export function uploadTaskScreenshot(
  organizationId: string,
  taskId: string,
  file: File,
  thumbnail?: File,
) {
  const body = new FormData()
  body.set('file', file)
  if (thumbnail) body.set('thumbnail', thumbnail)
  return apiRequest<TaskAttachment>(
    `${base(organizationId, 'tasks')}/${taskId}/attachments`,
    { method: 'POST', body },
  )
}

export async function downloadTaskAttachment(contentUrl: string) {
  const response = await apiFetch(contentUrl)
  if (!response.ok) {
    throw await apiProblem(response)
  }

  return response.blob()
}

export function changeTaskStatus(
  organizationId: string,
  taskId: string,
  status: TaskStatus,
  concurrencyToken: string,
) {
  return apiRequest<WorkTask>(
    `${base(organizationId, 'tasks')}/${taskId}/status`,
    {
      method: 'PATCH',
      body: JSON.stringify({ status, concurrencyToken }),
    },
  )
}

export function deleteTask(organizationId: string, taskId: string) {
  return apiRequest<void>(`${base(organizationId, 'tasks')}/${taskId}`, {
    method: 'DELETE',
  })
}

export function listIncidents(organizationId: string) {
  return apiRequest<Incident[]>(base(organizationId, 'incidents'))
}

export function createIncident(
  organizationId: string,
  input: SaveIncidentInput,
) {
  return apiRequest<Incident>(base(organizationId, 'incidents'), {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function resolveIncident(
  organizationId: string,
  incidentId: string,
  resolution: string,
  concurrencyToken: string,
) {
  return apiRequest<Incident>(
    `${base(organizationId, 'incidents')}/${incidentId}/resolve`,
    {
      method: 'POST',
      body: JSON.stringify({ resolution, concurrencyToken }),
    },
  )
}

export function listAwards(organizationId: string) {
  return apiRequest<Award[]>(base(organizationId, 'awards'))
}

export function listAwardTemplates(
  organizationId: string,
  themePackKey: string,
) {
  return apiRequest<AwardTemplate[]>(
    `${base(organizationId, 'awards')}/templates?themePackKey=${encodeURIComponent(themePackKey)}`,
  )
}

export function grantAward(organizationId: string, input: GrantAwardInput) {
  return apiRequest<Award>(base(organizationId, 'awards'), {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function listActivities(organizationId: string, limit = 50) {
  return apiRequest<Activity[]>(
    `${base(organizationId, 'activities')}?limit=${limit}`,
  )
}

export function getDashboard(organizationId: string) {
  return apiRequest<Dashboard>(base(organizationId, 'dashboard'))
}
