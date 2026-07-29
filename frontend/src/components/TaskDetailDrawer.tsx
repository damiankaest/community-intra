import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AtSign,
  CalendarDays,
  Check,
  ChevronRight,
  ImagePlus,
  MessageSquare,
  PackageCheck,
  Paperclip,
  Plus,
  Trash2,
  X,
} from 'lucide-react'
import {
  addTaskComment,
  addTaskMaterial,
  changeTaskStatus,
  changeTaskMaterialState,
  createTask,
  deleteTask,
  deleteTaskMaterial,
  downloadTaskAttachment,
  getTaskDetails,
  uploadTaskScreenshot,
  type Project,
  type TaskAttachment,
  type TaskStatus,
} from '../api/features'
import type { Member } from '../api/members'
import { prepareScreenshot } from '../imageProcessing'

interface TaskDetailDrawerProps {
  organizationId: string
  taskId: string
  projects: Project[]
  members: Member[]
  onSelectTask: (taskId: string) => void
  onClose: () => void
}

const statuses: TaskStatus[] = [
  'Open',
  'InProgress',
  'Blocked',
  'Done',
  'Cancelled',
]

const statusLabels: Record<TaskStatus, string> = {
  Open: 'Offen',
  InProgress: 'In Arbeit',
  Blocked: 'Blockiert',
  Done: 'Erledigt',
  Cancelled: 'Abgebrochen',
}

export function TaskDetailDrawer({
  organizationId,
  taskId,
  projects,
  members,
  onSelectTask,
  onClose,
}: TaskDetailDrawerProps) {
  const queryClient = useQueryClient()
  const [comment, setComment] = useState('')
  const [mentionedMemberIds, setMentionedMemberIds] = useState<string[]>([])
  const [subtaskTitle, setSubtaskTitle] = useState('')
  const [materialName, setMaterialName] = useState('')
  const [materialQuantity, setMaterialQuantity] = useState('1')
  const [celebration, setCelebration] = useState<string>()
  const details = useQuery({
    queryKey: ['task-details', organizationId, taskId],
    queryFn: () => getTaskDetails(organizationId, taskId),
  })

  useEffect(() => {
    const close = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', close)
    return () => window.removeEventListener('keydown', close)
  }, [onClose])

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: ['task-details', organizationId, taskId],
      }),
      queryClient.invalidateQueries({ queryKey: ['tasks', organizationId] }),
      queryClient.invalidateQueries({
        queryKey: ['dashboard', organizationId],
      }),
    ])
  }
  const statusMutation = useMutation({
    mutationFn: ({
      id,
      status,
      token,
    }: {
      id: string
      status: TaskStatus
      token: string
    }) => changeTaskStatus(organizationId, id, status, token),
    onSuccess: async (_, variables) => {
      if (variables.status === 'Done') celebrate('Aufgabe abgeschlossen!')
      await invalidate()
    },
  })
  const commentMutation = useMutation({
    mutationFn: () =>
      addTaskComment(organizationId, taskId, comment, mentionedMemberIds),
    onSuccess: async () => {
      setComment('')
      setMentionedMemberIds([])
      await invalidate()
    },
  })
  const subtaskMutation = useMutation({
    mutationFn: () =>
      createTask(organizationId, {
        title: subtaskTitle,
        description: `Ziel: ${subtaskTitle}\n\nFertig, wenn dieser konkrete Schritt abgeschlossen und geprüft ist.`,
        status: 'Open',
        priority: details.data?.task.priority ?? 'Normal',
        projectId: details.data?.task.projectId,
        parentTaskId: taskId,
      }),
    onSuccess: async () => {
      setSubtaskTitle('')
      await invalidate()
    },
  })
  const uploadMutation = useMutation({
    mutationFn: async (file: File) => {
      const prepared = await prepareScreenshot(file)
      return uploadTaskScreenshot(
        organizationId,
        taskId,
        prepared.file,
        prepared.thumbnail,
      )
    },
    onSuccess: invalidate,
  })
  const addMaterialMutation = useMutation({
    mutationFn: () =>
      addTaskMaterial(organizationId, taskId, {
        name: materialName,
        quantity: materialQuantity,
      }),
    onSuccess: async () => {
      setMaterialName('')
      setMaterialQuantity('1')
      await invalidate()
    },
  })
  const materialStateMutation = useMutation({
    mutationFn: ({
      id,
      isPrepared,
      token,
    }: {
      id: string
      isPrepared: boolean
      token: string
    }) =>
      changeTaskMaterialState(organizationId, taskId, id, isPrepared, token),
    onSuccess: async (_, variables) => {
      const materials = details.data?.materials ?? []
      const remaining = materials.filter(
        (item) => !item.isPrepared && item.id !== variables.id,
      ).length
      if (variables.isPrepared && remaining === 0) {
        celebrate('Alles liegt bereit – los geht’s!')
      }
      await invalidate()
    },
  })
  const deleteMaterialMutation = useMutation({
    mutationFn: (materialItemId: string) =>
      deleteTaskMaterial(organizationId, taskId, materialItemId),
    onSuccess: invalidate,
  })
  const deleteMutation = useMutation({
    mutationFn: () => deleteTask(organizationId, taskId),
    onSuccess: async () => {
      await invalidate()
      onClose()
    },
  })

  const task = details.data?.task
  const project = projects.find((item) => item.id === task?.projectId)
  const assignee = members.find((item) => item.id === task?.assignedMemberId)
  const error =
    details.error ??
    statusMutation.error ??
    commentMutation.error ??
    subtaskMutation.error ??
    uploadMutation.error ??
    addMaterialMutation.error ??
    materialStateMutation.error ??
    deleteMaterialMutation.error ??
    deleteMutation.error

  const materials = details.data?.materials ?? []
  const preparedMaterialCount = materials.filter(
    (item) => item.isPrepared,
  ).length

  function celebrate(message: string) {
    setCelebration(message)
    window.setTimeout(() => setCelebration(undefined), 2_800)
  }

  return (
    <div
      className="fixed inset-0 z-40 flex justify-end bg-black/65 backdrop-blur-sm"
      onMouseDown={(event) => {
        if (event.currentTarget === event.target) onClose()
      }}
    >
      <aside
        aria-label="Aufgabendetails"
        className="h-full w-full max-w-2xl overflow-y-auto border-l border-white/10 bg-[var(--theme-background)] shadow-2xl"
      >
        <header className="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-white/10 bg-[var(--theme-background)]/90 px-5 py-5 backdrop-blur-xl sm:px-8">
          <div>
            <p className="text-xs font-bold tracking-wider text-[var(--theme-primary)] uppercase">
              {project ? project.name : 'Einzelaufgabe'}
            </p>
            <h2 className="mt-2 text-2xl font-black text-white">
              {task?.title ?? 'Aufgabe wird geladen …'}
            </h2>
          </div>
          <div className="flex items-center gap-2">
            {task && (
              <button
                type="button"
                aria-label="Aufgabe löschen"
                className="task-delete-button"
                disabled={deleteMutation.isPending}
                onClick={() => {
                  const subject = details.data?.subtasks.length
                    ? `Aufgabe „${task.title}“ und ihre ${details.data.subtasks.length} Subtask(s)`
                    : `Aufgabe „${task.title}“`
                  if (
                    window.confirm(
                      `${subject} wirklich dauerhaft löschen? Kommentare und Screenshots werden ebenfalls gelöscht.`,
                    )
                  ) {
                    deleteMutation.mutate()
                  }
                }}
              >
                <Trash2 size={17} />
                <span>Löschen</span>
              </button>
            )}
            <button
              type="button"
              aria-label="Details schließen"
              className="rounded-xl border border-white/10 p-2 text-[var(--theme-muted)] hover:text-white"
              onClick={onClose}
            >
              <X size={20} />
            </button>
          </div>
        </header>

        <div className="grid gap-6 p-5 sm:p-8">
          {celebration && (
            <div className="task-celebration" role="status">
              <Check size={18} />
              {celebration}
            </div>
          )}
          {error && <ErrorNotice message={error.message} />}
          {task && (
            <>
              <section className="task-detail-section">
                <div className="flex flex-wrap items-center gap-2">
                  <select
                    aria-label="Aufgabenstatus"
                    className="task-inline-select"
                    value={task.status}
                    onChange={(event) =>
                      statusMutation.mutate({
                        id: task.id,
                        status: event.target.value as TaskStatus,
                        token: task.concurrencyToken,
                      })
                    }
                  >
                    {statuses.map((status) => (
                      <option key={status} value={status}>
                        {statusLabels[status]}
                      </option>
                    ))}
                  </select>
                  <span className="task-meta-pill">{task.priority}</span>
                  {task.dueDate && (
                    <span className="task-meta-pill">
                      <CalendarDays size={13} />
                      {new Date(`${task.dueDate}T00:00:00`).toLocaleDateString(
                        'de-DE',
                      )}
                    </span>
                  )}
                  {assignee && (
                    <span className="task-meta-pill">
                      {assignee.displayName}
                    </span>
                  )}
                </div>
                <h3>Was ist zu tun?</h3>
                <p className="whitespace-pre-wrap">
                  {task.description ??
                    'Noch keine Beschreibung. Ergänzt am besten Ziel, konkrete Schritte und wann die Aufgabe als fertig gilt.'}
                </p>
              </section>

              <section className="task-detail-section task-material-section">
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <h3 className="mt-0 flex items-center gap-2">
                      <PackageCheck size={18} />
                      Vorbereiten
                    </h3>
                    <p>Material und Werkzeuge abhaken, bevor ihr loslegt.</p>
                  </div>
                  <span className="task-count">
                    {preparedMaterialCount}/{materials.length}
                  </span>
                </div>
                {materials.length > 0 && (
                  <div className="material-progress" aria-hidden="true">
                    <span
                      style={{
                        width: `${Math.round(
                          (preparedMaterialCount / materials.length) * 100,
                        )}%`,
                      }}
                    />
                  </div>
                )}
                <div className="mt-4 grid gap-2">
                  {materials.map((material) => {
                    const preparedBy = members.find(
                      (member) => member.id === material.preparedByMemberId,
                    )
                    return (
                      <div
                        key={material.id}
                        className={`material-row ${
                          material.isPrepared ? 'is-prepared' : ''
                        }`}
                      >
                        <button
                          type="button"
                          className={`subtask-check ${
                            material.isPrepared ? 'is-done' : ''
                          }`}
                          aria-label={`${material.name} ${
                            material.isPrepared
                              ? 'wieder als fehlend markieren'
                              : 'als vorbereitet markieren'
                          }`}
                          onClick={() =>
                            materialStateMutation.mutate({
                              id: material.id,
                              isPrepared: !material.isPrepared,
                              token: material.concurrencyToken,
                            })
                          }
                        >
                          {material.isPrepared && <Check size={14} />}
                        </button>
                        <span className="material-quantity">
                          {material.quantity}
                        </span>
                        <div className="min-w-0 flex-1">
                          <strong>{material.name}</strong>
                          {(material.notes || preparedBy) && (
                            <small>
                              {material.notes}
                              {material.notes && preparedBy ? ' · ' : ''}
                              {preparedBy
                                ? `bereitgelegt von ${preparedBy.displayName}`
                                : ''}
                            </small>
                          )}
                        </div>
                        <button
                          type="button"
                          className="material-delete"
                          aria-label={`${material.name} entfernen`}
                          onClick={() =>
                            deleteMaterialMutation.mutate(material.id)
                          }
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    )
                  })}
                  {materials.length === 0 && (
                    <p>
                      Für diese Aufgabe ist noch nichts vorzubereiten. Der Chat
                      ergänzt bei neuen Aufgaben passende Listen automatisch.
                    </p>
                  )}
                  <div className="material-add-row">
                    <input
                      value={materialQuantity}
                      onChange={(event) =>
                        setMaterialQuantity(event.target.value)
                      }
                      maxLength={80}
                      aria-label="Materialmenge"
                      placeholder="Menge"
                      className="task-text-input material-quantity-input"
                    />
                    <input
                      value={materialName}
                      onChange={(event) => setMaterialName(event.target.value)}
                      maxLength={160}
                      aria-label="Materialname"
                      placeholder="Material oder Werkzeug …"
                      className="task-text-input"
                      onKeyDown={(event) => {
                        if (
                          event.key === 'Enter' &&
                          materialName.trim() &&
                          materialQuantity.trim()
                        ) {
                          addMaterialMutation.mutate()
                        }
                      }}
                    />
                    <button
                      type="button"
                      className="task-icon-button"
                      aria-label="Material hinzufügen"
                      disabled={
                        !materialName.trim() ||
                        !materialQuantity.trim() ||
                        addMaterialMutation.isPending
                      }
                      onClick={() => addMaterialMutation.mutate()}
                    >
                      <Plus size={18} />
                    </button>
                  </div>
                </div>
              </section>

              <section className="task-detail-section">
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <h3 className="mt-0">Subtasks</h3>
                    <p>Kleine Schritte, die einzeln abgehakt werden können.</p>
                  </div>
                  <span className="task-count">
                    {details.data?.subtasks.length ?? 0}
                  </span>
                </div>
                <div className="mt-4 grid gap-2">
                  {details.data?.subtasks.map((subtask) => (
                    <div key={subtask.id} className="subtask-row">
                      <button
                        type="button"
                        aria-label={`${subtask.title} erledigen`}
                        className={`subtask-check ${
                          subtask.status === 'Done' ? 'is-done' : ''
                        }`}
                        onClick={() =>
                          statusMutation.mutate({
                            id: subtask.id,
                            status: subtask.status === 'Done' ? 'Open' : 'Done',
                            token: subtask.concurrencyToken,
                          })
                        }
                      >
                        {subtask.status === 'Done' && <Check size={14} />}
                      </button>
                      <button
                        type="button"
                        className={`min-w-0 flex-1 truncate text-left ${
                          subtask.status === 'Done'
                            ? 'text-[var(--theme-muted)] line-through'
                            : 'text-white'
                        }`}
                        onClick={() => onSelectTask(subtask.id)}
                      >
                        {subtask.title}
                      </button>
                      <button
                        type="button"
                        aria-label={`${subtask.title} öffnen`}
                        className="text-[var(--theme-muted)] hover:text-white"
                        onClick={() => onSelectTask(subtask.id)}
                      >
                        <ChevronRight size={16} />
                      </button>
                    </div>
                  ))}
                  <div className="mt-2 flex gap-2">
                    <input
                      value={subtaskTitle}
                      onChange={(event) => setSubtaskTitle(event.target.value)}
                      placeholder="Kleinen Schritt hinzufügen …"
                      className="task-text-input"
                      onKeyDown={(event) => {
                        if (event.key === 'Enter' && subtaskTitle.trim()) {
                          subtaskMutation.mutate()
                        }
                      }}
                    />
                    <button
                      type="button"
                      aria-label="Subtask hinzufügen"
                      className="task-icon-button"
                      disabled={
                        !subtaskTitle.trim() || subtaskMutation.isPending
                      }
                      onClick={() => subtaskMutation.mutate()}
                    >
                      <Plus size={18} />
                    </button>
                  </div>
                </div>
              </section>

              <section className="task-detail-section">
                <h3 className="mt-0 flex items-center gap-2">
                  <Paperclip size={17} />
                  Screenshots
                </h3>
                <p>
                  PNG, JPEG, WebP oder GIF bis 5 MB. Die Bilder bleiben in eurer
                  eigenen Datenbank.
                </p>
                <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
                  {details.data?.attachments.map((attachment) => (
                    <AttachmentImage
                      key={attachment.id}
                      attachment={attachment}
                    />
                  ))}
                  <label className="grid min-h-28 cursor-pointer place-items-center rounded-2xl border border-dashed border-white/20 bg-white/[0.03] text-center text-xs text-[var(--theme-muted)] hover:border-[var(--theme-primary)]/60 hover:text-white">
                    <span className="grid place-items-center gap-2">
                      <ImagePlus size={22} />
                      Screenshot hinzufügen
                    </span>
                    <input
                      type="file"
                      accept="image/png,image/jpeg,image/webp,image/gif"
                      className="sr-only"
                      onChange={(event) => {
                        const file = event.target.files?.[0]
                        if (file) uploadMutation.mutate(file)
                        event.currentTarget.value = ''
                      }}
                    />
                  </label>
                </div>
              </section>

              <section className="task-detail-section">
                <h3 className="mt-0 flex items-center gap-2">
                  <MessageSquare size={17} />
                  Kommentare
                </h3>
                <div className="mt-4 grid gap-3">
                  {details.data?.comments.map((item) => (
                    <article
                      key={item.id}
                      className="rounded-2xl bg-black/15 px-4 py-3"
                    >
                      <p className="text-sm whitespace-pre-wrap text-white">
                        {item.body}
                      </p>
                      <p className="mt-2 text-[11px] text-[var(--theme-muted)]">
                        {members.find(
                          (member) => member.id === item.authorMemberId,
                        )?.displayName ?? 'Mitglied'}{' '}
                        · {new Date(item.createdAt).toLocaleString('de-DE')}
                      </p>
                    </article>
                  ))}
                  {details.data?.comments.length === 0 && (
                    <p>Noch keine Kommentare – alles verdächtig eindeutig.</p>
                  )}
                  <div className="mention-picker">
                    <span>
                      <AtSign size={14} />
                      Person erwähnen
                    </span>
                    <div>
                      {members.map((member) => {
                        const selected = mentionedMemberIds.includes(member.id)
                        return (
                          <button
                            key={member.id}
                            type="button"
                            className={selected ? 'is-selected' : ''}
                            onClick={() =>
                              setMentionedMemberIds((current) =>
                                selected
                                  ? current.filter((id) => id !== member.id)
                                  : [...current, member.id],
                              )
                            }
                          >
                            @{member.displayName}
                          </button>
                        )
                      })}
                    </div>
                  </div>
                  <div className="flex items-end gap-2">
                    <textarea
                      value={comment}
                      onChange={(event) => setComment(event.target.value)}
                      rows={2}
                      maxLength={2000}
                      placeholder="Frage, Hinweis oder Fortschritt …"
                      className="task-text-input min-h-20 resize-y"
                    />
                    <button
                      type="button"
                      aria-label="Kommentar speichern"
                      className="task-icon-button"
                      disabled={!comment.trim() || commentMutation.isPending}
                      onClick={() => commentMutation.mutate()}
                    >
                      <MessageSquare size={18} />
                    </button>
                  </div>
                </div>
              </section>
            </>
          )}
        </div>
      </aside>
    </div>
  )
}

function AttachmentImage({ attachment }: { attachment: TaskAttachment }) {
  const previewUrl = attachment.thumbnailUrl ?? attachment.contentUrl
  const image = useQuery({
    queryKey: ['task-attachment-preview', previewUrl],
    queryFn: async () =>
      URL.createObjectURL(await downloadTaskAttachment(previewUrl)),
    staleTime: Number.POSITIVE_INFINITY,
  })

  useEffect(
    () => () => {
      if (image.data) URL.revokeObjectURL(image.data)
    },
    [image.data],
  )

  return (
    <button
      type="button"
      className="group overflow-hidden rounded-2xl border border-white/10 bg-black/20"
      title={attachment.fileName}
      onClick={async () => {
        const popup = window.open('', '_blank')
        try {
          const fullImageUrl = URL.createObjectURL(
            await downloadTaskAttachment(attachment.contentUrl),
          )
          if (popup) popup.location.href = fullImageUrl
          window.setTimeout(() => URL.revokeObjectURL(fullImageUrl), 60_000)
        } catch {
          popup?.close()
        }
      }}
    >
      {image.data ? (
        <img
          src={image.data}
          alt={attachment.fileName}
          className="h-28 w-full object-cover transition group-hover:scale-105"
        />
      ) : (
        <div className="grid h-28 place-items-center text-[var(--theme-muted)]">
          <ImagePlus size={20} />
        </div>
      )}
      <p className="truncate px-3 py-2 text-[11px] text-[var(--theme-muted)]">
        {attachment.fileName}
      </p>
    </button>
  )
}

function ErrorNotice({ message }: { message: string }) {
  return (
    <div className="rounded-2xl border border-[var(--theme-danger)]/30 bg-[var(--theme-danger)]/10 p-4 text-sm text-[var(--theme-danger)]">
      {message}
    </div>
  )
}
