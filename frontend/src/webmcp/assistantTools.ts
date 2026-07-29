import {
  addTaskComment,
  changeTaskStatus,
  createTask,
  getTaskDetails,
  listProjects,
  listTasks,
  updateTask,
  type Priority,
  type TaskStatus,
} from '../api/features'
import { listMembers } from '../api/members'
import { getLiveServerStatus } from '../api/liveOperations'

interface RegisterAssistantToolsOptions {
  organizationId: string
  onChanged: () => void
  onOpenChat: () => void
}

export function registerAssistantTools({
  organizationId,
  onChanged,
  onOpenChat,
}: RegisterAssistantToolsOptions) {
  if (!document.modelContext) {
    return { isSupported: false, unregister: () => undefined }
  }

  const controller = new AbortController()
  const register = (tool: WebMcpTool) => {
    try {
      void Promise.resolve(
        document.modelContext?.registerTool(tool, {
          signal: controller.signal,
        }),
      ).catch(() => undefined)
    } catch {
      // The built-in chat remains available when an experimental browser
      // implementation rejects a tool definition.
    }
  }

  register({
    name: 'community_list_projects',
    title: 'Projekte laden',
    description:
      'Lädt die vorhandenen Projekte der aktuell geöffneten Community. Nur lesend.',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
      properties: {},
    },
    annotations: {
      readOnlyHint: true,
      untrustedContentHint: true,
    },
    execute: async () => {
      onOpenChat()
      const projects = await listProjects(organizationId)
      return projects.map(({ id, name, description, status, priority }) => ({
        id,
        name,
        description,
        status,
        priority,
      }))
    },
  })

  register({
    name: 'community_list_tasks',
    title: 'Aufgaben laden',
    description:
      'Lädt die vorhandenen Aufgaben und Subtasks der aktuell geöffneten Community. Nur lesend.',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
      properties: {},
    },
    annotations: {
      readOnlyHint: true,
      untrustedContentHint: true,
    },
    execute: async () => {
      onOpenChat()
      const tasks = await listTasks(organizationId)
      return tasks.map(
        ({
          id,
          projectId,
          parentTaskId,
          title,
          description,
          status,
          priority,
          dueDate,
        }) => ({
          id,
          projectId,
          parentTaskId,
          title,
          description,
          status,
          priority,
          dueDate,
        }),
      )
    },
  })

  register({
    name: 'community_list_members',
    title: 'Mitglieder laden',
    description:
      'Lädt aktive Mitglieder mit ihren IDs. Vor Zuweisungen oder Erwähnungen verwenden.',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
      properties: {},
    },
    annotations: {
      readOnlyHint: true,
      untrustedContentHint: true,
    },
    execute: async () => {
      onOpenChat()
      return (await listMembers(organizationId))
        .filter((member) => member.isActive)
        .map(({ id, displayName, visibleTitle }) => ({
          id,
          displayName,
          visibleTitle,
        }))
    },
  })

  register({
    name: 'community_get_task',
    title: 'Aufgabendetails laden',
    description:
      'Lädt eine konkrete Aufgabe inklusive Subtasks, Kommentaren und Screenshot-Metadaten.',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
      properties: {
        taskId: {
          type: 'string',
          description: 'ID der Aufgabe.',
        },
      },
      required: ['taskId'],
    },
    annotations: {
      readOnlyHint: true,
      untrustedContentHint: true,
    },
    execute: async (input) => {
      onOpenChat()
      return getTaskDetails(organizationId, String(input.taskId ?? ''))
    },
  })

  register({
    name: 'community_get_live_server_status',
    title: 'Gameserver-Status laden',
    description:
      'Lädt den aktuellen read-only Status des verbundenen Satisfactory-Servers.',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
      properties: {},
    },
    annotations: {
      readOnlyHint: true,
      untrustedContentHint: true,
    },
    execute: async () => {
      onOpenChat()
      return getLiveServerStatus(organizationId)
    },
  })

  register({
    name: 'community_create_task',
    title: 'Aufgabe erstellen',
    description:
      'Erstellt nach sichtbarer Bestätigung genau eine verständliche Aufgabe oder einen Subtask.',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
      properties: {
        title: { type: 'string', description: 'Kurzer Aufgabentitel.' },
        description: {
          type: 'string',
          description: 'Konkrete Beschreibung mit Ziel und Fertig-Kriterium.',
        },
        priority: {
          type: 'string',
          enum: ['Low', 'Normal', 'High', 'Critical'],
        },
        projectId: { type: ['string', 'null'] },
        parentTaskId: { type: ['string', 'null'] },
      },
      required: [
        'title',
        'description',
        'priority',
        'projectId',
        'parentTaskId',
      ],
    },
    annotations: {
      readOnlyHint: false,
      untrustedContentHint: true,
    },
    execute: async (input) => {
      onOpenChat()
      const title = String(input.title ?? '').trim()
      const confirmed = window.confirm(`Aufgabe „${title}“ wirklich anlegen?`)
      if (!confirmed) {
        return { confirmed: false, reason: 'User cancelled' }
      }

      const task = await createTask(organizationId, {
        title,
        description: String(input.description ?? '').trim() || undefined,
        status: 'Open',
        priority: asPriority(input.priority),
        projectId: nullableString(input.projectId),
        parentTaskId: nullableString(input.parentTaskId),
      })
      onChanged()
      return { confirmed: true, task }
    },
  })

  register({
    name: 'community_change_task_status',
    title: 'Aufgabenstatus ändern',
    description:
      'Ändert nach sichtbarer Bestätigung den Status genau einer vorhandenen Aufgabe.',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
      properties: {
        taskId: { type: 'string' },
        status: {
          type: 'string',
          enum: ['Open', 'InProgress', 'Blocked', 'Done', 'Cancelled'],
        },
      },
      required: ['taskId', 'status'],
    },
    annotations: {
      readOnlyHint: false,
      untrustedContentHint: false,
    },
    execute: async (input) => {
      onOpenChat()
      const taskId = String(input.taskId ?? '')
      const status = asTaskStatus(input.status)
      const task = (await listTasks(organizationId)).find(
        (item) => item.id === taskId,
      )
      if (!task) {
        return { confirmed: false, reason: 'Task not found' }
      }

      const confirmed = window.confirm(
        `Status von „${task.title}“ wirklich auf ${status} setzen?`,
      )
      if (!confirmed) {
        return { confirmed: false, reason: 'User cancelled' }
      }

      const updated = await changeTaskStatus(
        organizationId,
        task.id,
        status,
        task.concurrencyToken,
      )
      onChanged()
      return { confirmed: true, task: updated }
    },
  })

  register({
    name: 'community_assign_task',
    title: 'Aufgabe zuweisen',
    description:
      'Weist nach sichtbarer Bestätigung genau eine Aufgabe einem aktiven Mitglied zu.',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
      properties: {
        taskId: { type: 'string' },
        memberId: { type: 'string' },
      },
      required: ['taskId', 'memberId'],
    },
    annotations: {
      readOnlyHint: false,
      untrustedContentHint: false,
    },
    execute: async (input) => {
      onOpenChat()
      const taskId = String(input.taskId ?? '')
      const memberId = String(input.memberId ?? '')
      const [task, member] = await Promise.all([
        getTaskDetails(organizationId, taskId).then((details) => details.task),
        listMembers(organizationId).then((members) =>
          members.find((item) => item.id === memberId && item.isActive),
        ),
      ])
      if (!member) return { confirmed: false, reason: 'Member not found' }
      if (!window.confirm(`„${task.title}“ an ${member.displayName} geben?`)) {
        return { confirmed: false, reason: 'User cancelled' }
      }

      const updated = await updateTask(organizationId, task.id, {
        ...task,
        assignedMemberId: member.id,
      })
      onChanged()
      return { confirmed: true, task: updated }
    },
  })

  register({
    name: 'community_add_task_comment',
    title: 'Kommentar schreiben',
    description:
      'Schreibt nach sichtbarer Bestätigung einen Kommentar und kann Mitglieder erwähnen.',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
      properties: {
        taskId: { type: 'string' },
        body: { type: 'string' },
        mentionedMemberIds: {
          type: 'array',
          items: { type: 'string' },
          maxItems: 10,
        },
      },
      required: ['taskId', 'body', 'mentionedMemberIds'],
    },
    annotations: {
      readOnlyHint: false,
      untrustedContentHint: true,
    },
    execute: async (input) => {
      onOpenChat()
      const taskId = String(input.taskId ?? '')
      const body = String(input.body ?? '').trim()
      if (!window.confirm(`Kommentar wirklich schreiben?\n\n${body}`)) {
        return { confirmed: false, reason: 'User cancelled' }
      }

      const mentionedMemberIds = Array.isArray(input.mentionedMemberIds)
        ? input.mentionedMemberIds.map(String).slice(0, 10)
        : []
      const comment = await addTaskComment(
        organizationId,
        taskId,
        body,
        mentionedMemberIds,
      )
      onChanged()
      return { confirmed: true, comment }
    },
  })

  return {
    isSupported: true,
    unregister: () => controller.abort(),
  }
}

function nullableString(value: unknown) {
  return typeof value === 'string' && value.trim() ? value.trim() : undefined
}

function asPriority(value: unknown): Priority {
  return ['Low', 'Normal', 'High', 'Critical'].includes(String(value))
    ? (value as Priority)
    : 'Normal'
}

function asTaskStatus(value: unknown): TaskStatus {
  return ['Open', 'InProgress', 'Blocked', 'Done', 'Cancelled'].includes(
    String(value),
  )
    ? (value as TaskStatus)
    : 'Open'
}
