import { useEffect, useState, type ReactNode } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertTriangle,
  Award as AwardIcon,
  CheckCircle2,
  ChevronRight,
  ClipboardCheck,
  FolderKanban,
  Gauge,
  Plus,
  ScrollText,
  Server,
  Users,
  X,
} from 'lucide-react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import type { CurrentUser } from '../api/auth'
import {
  changeTaskStatus,
  createIncident,
  createProject,
  createTask,
  getDashboard,
  grantAward,
  listActivities,
  listAwards,
  listAwardTemplates,
  listIncidents,
  listProjects,
  listTasks,
  resolveIncident,
  updateProject,
  type Activity,
  type GrantAwardInput,
  type IncidentSeverity,
  type Priority,
  type Project,
  type ProjectStatus,
  type SaveIncidentInput,
  type SaveProjectInput,
  type SaveTaskInput,
  type TaskStatus,
  type WorkTask,
} from '../api/features'
import { listMembers } from '../api/members'
import { getOrganization } from '../api/organizations'
import { getThemePack } from '../api/themePacks'
import { applyTheme, resetTheme } from '../theme'
import { AiAssistantPanel } from './AiAssistantPanel'
import { NotificationBell } from './NotificationBell'
import { TaskDetailDrawer } from './TaskDetailDrawer'

interface PhaseSixPageProps {
  user: CurrentUser
}

const projectStatuses: ProjectStatus[] = [
  'Idea',
  'Planned',
  'InProgress',
  'Blocked',
  'Completed',
  'Cancelled',
]
const projectStatusLabels: Record<ProjectStatus, string> = {
  Idea: 'Idee',
  Planned: 'Geplant',
  InProgress: 'In Arbeit',
  Blocked: 'Blockiert',
  Completed: 'Erledigt',
  Cancelled: 'Abgebrochen',
}
const taskStatusLabels: Record<TaskStatus, string> = {
  Open: 'Offen',
  InProgress: 'In Arbeit',
  Blocked: 'Blockiert',
  Done: 'Erledigt',
  Cancelled: 'Abgebrochen',
}
const priorityLabels: Record<Priority, string> = {
  Low: 'Niedrig',
  Normal: 'Normal',
  High: 'Hoch',
  Critical: 'Kritisch',
}
const incidentSeverities: IncidentSeverity[] = [
  'Informational',
  'Low',
  'Medium',
  'High',
  'Catastrophic',
]

function usePhaseSixContext() {
  const { organizationId = '' } = useParams()
  const organization = useQuery({
    queryKey: ['organization', organizationId],
    queryFn: () => getOrganization(organizationId),
    enabled: Boolean(organizationId),
  })
  const themePack = useQuery({
    queryKey: ['theme-pack', organization.data?.themePackKey],
    queryFn: () => getThemePack(organization.data!.themePackKey),
    enabled: Boolean(organization.data?.themePackKey),
  })

  useEffect(() => {
    applyTheme(themePack.data)
    return resetTheme
  }, [themePack.data])

  return { organizationId, organization, themePack }
}

export function FeatureLayout({
  user,
  title,
  subtitle,
  children,
}: PhaseSixPageProps & {
  title: string
  subtitle: string
  children: ReactNode
}) {
  const { organizationId, organization, themePack } = usePhaseSixContext()

  return (
    <div className="min-h-screen bg-[var(--theme-background)] text-[var(--theme-text)]">
      <div className="industrial-grid pointer-events-none fixed inset-0 opacity-40" />
      <header className="relative border-b border-white/10 bg-black/20">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-4 px-5 py-4">
          <Link
            to={`/organizations/${organizationId}`}
            className="font-black text-white"
          >
            {organization.data?.name ?? 'Community Intranet'}
          </Link>
          <div className="flex items-center gap-3">
            <NotificationBell organizationId={organizationId} />
            <span className="text-sm text-[var(--theme-muted)]">
              {user.displayName}
            </span>
          </div>
        </div>
      </header>
      <div className="relative mx-auto grid max-w-7xl gap-8 px-5 py-8 lg:grid-cols-[220px_1fr]">
        <nav className="flex gap-2 overflow-x-auto lg:flex-col">
          <FeatureLink to="" label="Dashboard" icon={<Gauge size={17} />} />
          <FeatureLink
            to="projects"
            label="Projekte"
            icon={<FolderKanban size={17} />}
          />
          <FeatureLink
            to="tasks"
            label="Aufgaben"
            icon={<ClipboardCheck size={17} />}
          />
          <FeatureLink
            to="incidents"
            label="Incidents"
            icon={<AlertTriangle size={17} />}
          />
          <FeatureLink
            to="awards"
            label="Auszeichnungen"
            icon={<AwardIcon size={17} />}
          />
          <FeatureLink
            to="activities"
            label="Aktivitäten"
            icon={<ScrollText size={17} />}
          />
          <FeatureLink
            to="server"
            label="Gameserver"
            icon={<Server size={17} />}
          />
          <FeatureLink
            to="members"
            label="Mitglieder"
            icon={<Users size={17} />}
          />
        </nav>
        <main>
          <p className="text-xs font-bold tracking-[0.16em] text-[var(--theme-primary)] uppercase">
            Eure gemeinsame Werkbank
          </p>
          <h1 className="mt-2 text-4xl font-black tracking-tight text-white">
            {title}
          </h1>
          <p className="mt-3 text-[var(--theme-muted)]">{subtitle}</p>
          <div className="mt-8">{children}</div>
        </main>
      </div>
      <AiAssistantPanel
        key={organizationId}
        organizationId={organizationId}
        themeName={themePack.data?.name}
      />
    </div>
  )
}

function FeatureLink({
  to,
  label,
  icon,
}: {
  to: string
  label: string
  icon: ReactNode
}) {
  const { organizationId = '' } = useParams()
  const target = `/organizations/${organizationId}${to ? `/${to}` : ''}`
  return (
    <Link
      to={target}
      className="inline-flex shrink-0 items-center gap-2 rounded-xl border border-white/10 bg-white/[0.03] px-4 py-3 text-sm font-semibold text-white hover:border-[var(--theme-primary)]/50"
    >
      {icon}
      {label}
    </Link>
  )
}

export function PhaseSixDashboard({ user }: PhaseSixPageProps) {
  const { organizationId, themePack } = usePhaseSixContext()
  const dashboard = useQuery({
    queryKey: ['dashboard', organizationId],
    queryFn: () => getDashboard(organizationId),
    enabled: Boolean(organizationId),
  })
  const terminology = themePack.data?.configuration.terminology

  return (
    <FeatureLayout
      user={user}
      title="Übersicht"
      subtitle={
        themePack.data?.configuration.messages.welcome ??
        'Alle Fachbereiche melden ihre aktuellen Kennzahlen.'
      }
    >
      {dashboard.error && <ErrorBox error={dashboard.error} />}
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Metric
          label="Mitglieder"
          value={dashboard.data?.memberCount ?? 0}
          to={`/organizations/${organizationId}/members`}
        />
        <Metric
          label={`Offene ${terminology?.task ?? 'Aufgaben'}`}
          value={dashboard.data?.openTaskCount ?? 0}
          to={`/organizations/${organizationId}/tasks`}
        />
        <Metric
          label={`Aktive ${terminology?.project ?? 'Projekte'}`}
          value={dashboard.data?.activeProjectCount ?? 0}
          to={`/organizations/${organizationId}/projects`}
        />
        <Metric
          label={`Offene ${terminology?.incident ?? 'Incidents'}`}
          value={dashboard.data?.openIncidentCount ?? 0}
          to={`/organizations/${organizationId}/incidents`}
        />
      </div>
      <div className="mt-6 grid gap-6 xl:grid-cols-2">
        <Panel title="Was gerade wichtig ist">
          <p className="text-lg font-bold text-white">
            {dashboard.data?.systemMessage ?? 'Lagebild wird erstellt …'}
          </p>
        </Panel>
        <Panel title="Aktuelle Auszeichnung">
          {dashboard.data?.currentAward ? (
            <>
              <p className="text-lg font-bold text-white">
                {dashboard.data.currentAward.name}
              </p>
              <p className="mt-2 text-sm text-[var(--theme-muted)]">
                {dashboard.data.currentAward.awardedToDisplayName} ·{' '}
                {dashboard.data.currentAward.description}
              </p>
            </>
          ) : (
            <Empty text="Noch niemand wurde offiziell übermäßig gewürdigt." />
          )}
        </Panel>
      </div>
      <Panel title="Schnellaktionen" className="mt-6">
        <div className="flex flex-wrap gap-3">
          <QuickLink organizationId={organizationId} path="tasks">
            Aufgabe erstellen
          </QuickLink>
          <QuickLink organizationId={organizationId} path="projects">
            Projekt erstellen
          </QuickLink>
          <QuickLink organizationId={organizationId} path="incidents">
            Incident melden
          </QuickLink>
          <QuickLink organizationId={organizationId} path="awards">
            Auszeichnung vergeben
          </QuickLink>
          <QuickLink organizationId={organizationId} path="members">
            Person einladen
          </QuickLink>
        </div>
      </Panel>
      <Panel title="Was zuletzt passiert ist" className="mt-6">
        <ActivityList activities={dashboard.data?.recentActivities ?? []} />
      </Panel>
    </FeatureLayout>
  )
}

export function ProjectsPage({ user }: PhaseSixPageProps) {
  const { organizationId } = usePhaseSixContext()
  const queryClient = useQueryClient()
  const [selectedProjectId, setSelectedProjectId] = useState<string>()
  const [selectedTaskId, setSelectedTaskId] = useState<string>()
  const projects = useQuery({
    queryKey: ['projects', organizationId],
    queryFn: () => listProjects(organizationId),
    enabled: Boolean(organizationId),
  })
  const members = useQuery({
    queryKey: ['members', organizationId],
    queryFn: () => listMembers(organizationId),
    enabled: Boolean(organizationId),
  })
  const tasks = useQuery({
    queryKey: ['tasks', organizationId],
    queryFn: () => listTasks(organizationId),
    enabled: Boolean(organizationId),
  })
  const form = useForm<SaveProjectInput>({
    defaultValues: {
      name: '',
      description: '',
      status: 'Idea',
      priority: 'Normal',
      ownerMemberId: '',
      startDate: '',
      dueDate: '',
    },
  })
  const createMutation = useMutation({
    mutationFn: (input: SaveProjectInput) =>
      createProject(organizationId, normalizeProject(input)),
    onSuccess: async () => {
      form.reset()
      await invalidate(queryClient, organizationId, 'projects')
    },
  })
  const statusMutation = useMutation({
    mutationFn: ({
      projectId,
      input,
    }: {
      projectId: string
      input: SaveProjectInput
    }) => updateProject(organizationId, projectId, input),
    onSuccess: async () => invalidate(queryClient, organizationId, 'projects'),
  })

  return (
    <FeatureLayout
      user={user}
      title="Projekte"
      subtitle="Alles, was aus mehreren Aufgaben besteht. Klick ein Projekt an und du siehst sofort, was dazugehört."
    >
      <CreatePanel title="Neues Projekt">
        <form
          className="feature-form"
          onSubmit={form.handleSubmit((input) => createMutation.mutate(input))}
        >
          <input
            placeholder="Projektname"
            {...form.register('name')}
            required
          />
          <textarea
            placeholder="Was soll am Ende erreicht sein?"
            {...form.register('description')}
          />
          <select {...form.register('status')}>
            {projectStatuses.map((status) => (
              <option key={status} value={status}>
                {projectStatusLabels[status]}
              </option>
            ))}
          </select>
          <select {...form.register('priority')}>
            {['Low', 'Normal', 'High', 'Critical'].map((priority) => (
              <option key={priority} value={priority}>
                {priorityLabels[priority as Priority]}
              </option>
            ))}
          </select>
          <select {...form.register('ownerMemberId')}>
            <option value="">Keine verantwortliche Person</option>
            {members.data?.map((member) => (
              <option key={member.id} value={member.id}>
                {member.displayName}
              </option>
            ))}
          </select>
          <input type="date" {...form.register('startDate')} />
          <input type="date" {...form.register('dueDate')} />
          <SubmitButton pending={createMutation.isPending}>
            Projekt anlegen
          </SubmitButton>
        </form>
        {createMutation.error && <ErrorBox error={createMutation.error} />}
      </CreatePanel>
      <CardGrid>
        {projects.data?.map((project) => (
          <article key={project.id} className="feature-card interactive-card">
            <button
              type="button"
              className="grid w-full gap-3 text-left"
              onClick={() => setSelectedProjectId(project.id)}
            >
              <StatusLine status={project.status} priority={project.priority} />
              <span className="flex items-center justify-between gap-3">
                <span className="text-lg font-extrabold text-white">
                  {project.name}
                </span>
                <ChevronRight
                  size={19}
                  className="text-[var(--theme-primary)]"
                />
              </span>
              <span className="line-clamp-3 text-sm leading-6 text-[var(--theme-muted)]">
                {project.description ??
                  'Noch kein Ziel beschrieben. Klick hinein, um die zugehörigen Aufgaben zu sehen.'}
              </span>
              <span className="text-xs font-semibold text-[var(--theme-text)]">
                {
                  tasks.data?.filter((task) => task.projectId === project.id)
                    .length
                }{' '}
                Aufgaben
              </span>
            </button>
            <select
              aria-label={`Status von ${project.name}`}
              value={project.status}
              onChange={(event) =>
                statusMutation.mutate({
                  projectId: project.id,
                  input: {
                    ...project,
                    status: event.target.value as ProjectStatus,
                  },
                })
              }
            >
              {projectStatuses.map((status) => (
                <option key={status} value={status}>
                  {projectStatusLabels[status]}
                </option>
              ))}
            </select>
          </article>
        ))}
      </CardGrid>
      {!projects.isPending && projects.data?.length === 0 && (
        <Empty text="Noch keine Projekte. Leg eins an, sobald mehrere Aufgaben zusammengehören." />
      )}
      {selectedProjectId && (
        <ProjectDetail
          project={projects.data?.find(
            (project) => project.id === selectedProjectId,
          )}
          tasks={
            tasks.data?.filter(
              (task) =>
                task.projectId === selectedProjectId && !task.parentTaskId,
            ) ?? []
          }
          onClose={() => setSelectedProjectId(undefined)}
          onSelectTask={setSelectedTaskId}
        />
      )}
      {selectedTaskId && (
        <TaskDetailDrawer
          organizationId={organizationId}
          taskId={selectedTaskId}
          projects={projects.data ?? []}
          members={members.data ?? []}
          onSelectTask={setSelectedTaskId}
          onClose={() => setSelectedTaskId(undefined)}
        />
      )}
    </FeatureLayout>
  )
}

export function TasksPage({ user }: PhaseSixPageProps) {
  const { organizationId } = usePhaseSixContext()
  const queryClient = useQueryClient()
  const [searchParams, setSearchParams] = useSearchParams()
  const [selectedTaskId, setSelectedTaskId] = useState<string | undefined>(
    searchParams.get('task') ?? undefined,
  )
  const [draggedTaskId, setDraggedTaskId] = useState<string>()
  const tasks = useQuery({
    queryKey: ['tasks', organizationId],
    queryFn: () => listTasks(organizationId),
    enabled: Boolean(organizationId),
  })
  const projects = useQuery({
    queryKey: ['projects', organizationId],
    queryFn: () => listProjects(organizationId),
    enabled: Boolean(organizationId),
  })
  const members = useQuery({
    queryKey: ['members', organizationId],
    queryFn: () => listMembers(organizationId),
    enabled: Boolean(organizationId),
  })
  const form = useForm<SaveTaskInput>({
    defaultValues: {
      title: '',
      description: '',
      status: 'Open',
      priority: 'Normal',
      projectId: '',
      assignedMemberId: '',
      dueDate: '',
    },
  })
  const createMutation = useMutation({
    mutationFn: (input: SaveTaskInput) =>
      createTask(organizationId, normalizeTask(input)),
    onSuccess: async () => {
      form.reset()
      await invalidate(queryClient, organizationId, 'tasks')
    },
  })
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
    onSuccess: async () => invalidate(queryClient, organizationId, 'tasks'),
  })
  const selectTask = (taskId?: string) => {
    setSelectedTaskId(taskId)
    setSearchParams(taskId ? { task: taskId } : {}, { replace: true })
  }
  const topLevelTasks = tasks.data?.filter((task) => !task.parentTaskId) ?? []
  const boardStatuses: TaskStatus[] = ['Open', 'InProgress', 'Blocked', 'Done']

  return (
    <FeatureLayout
      user={user}
      title="Aufgaben"
      subtitle="Konkrete Dinge, die jemand erledigen kann. Klick eine Aufgabe für Subtasks, Kommentare und Screenshots an."
    >
      <CreatePanel title="Neue Aufgabe">
        <form
          className="feature-form"
          onSubmit={form.handleSubmit((input) => createMutation.mutate(input))}
        >
          <input placeholder="Titel" {...form.register('title')} required />
          <textarea
            placeholder="Was genau soll gemacht werden – und wann ist es fertig?"
            {...form.register('description')}
          />
          <select {...form.register('projectId')}>
            <option value="">Ohne Projekt</option>
            {projects.data?.map((project) => (
              <option key={project.id} value={project.id}>
                {project.name}
              </option>
            ))}
          </select>
          <select {...form.register('assignedMemberId')}>
            <option value="">Nicht zugewiesen</option>
            {members.data?.map((member) => (
              <option key={member.id} value={member.id}>
                {member.displayName}
              </option>
            ))}
          </select>
          <select {...form.register('priority')}>
            {['Low', 'Normal', 'High', 'Critical'].map((priority) => (
              <option key={priority} value={priority}>
                {priorityLabels[priority as Priority]}
              </option>
            ))}
          </select>
          <input type="date" {...form.register('dueDate')} />
          <SubmitButton pending={createMutation.isPending}>
            Aufgabe anlegen
          </SubmitButton>
        </form>
      </CreatePanel>
      <div className="kanban-board" aria-label="Aufgabenboard">
        {boardStatuses.map((status) => (
          <section
            key={status}
            className={`kanban-column kanban-${status.toLowerCase()}`}
            onDragOver={(event) => event.preventDefault()}
            onDrop={(event) => {
              event.preventDefault()
              const task = topLevelTasks.find(
                (item) =>
                  item.id ===
                  (draggedTaskId || event.dataTransfer.getData('text/plain')),
              )
              setDraggedTaskId(undefined)
              if (task && task.status !== status) {
                statusMutation.mutate({
                  id: task.id,
                  status,
                  token: task.concurrencyToken,
                })
              }
            }}
          >
            <header>
              <span>{taskStatusLabels[status]}</span>
              <strong>
                {topLevelTasks.filter((task) => task.status === status).length}
              </strong>
            </header>
            <div className="kanban-stack">
              {topLevelTasks
                .filter((task) => task.status === status)
                .map((task) => (
                  <article
                    key={task.id}
                    draggable
                    className="kanban-card"
                    onDragStart={(event) => {
                      setDraggedTaskId(task.id)
                      event.dataTransfer.setData('text/plain', task.id)
                      event.dataTransfer.effectAllowed = 'move'
                    }}
                    onDragEnd={() => setDraggedTaskId(undefined)}
                  >
                    <button type="button" onClick={() => selectTask(task.id)}>
                      <StatusLine
                        status={task.status}
                        priority={task.priority}
                      />
                      <span className="kanban-title">
                        {task.title}
                        <ChevronRight size={17} />
                      </span>
                      <span className="kanban-description">
                        {task.description ??
                          'Noch nicht beschrieben – klick rein und macht klar, was fertig bedeutet.'}
                      </span>
                      <span className="kanban-meta">
                        {
                          tasks.data?.filter(
                            (subtask) => subtask.parentTaskId === task.id,
                          ).length
                        }{' '}
                        Schritte
                        {task.dueDate
                          ? ` · bis ${new Date(`${task.dueDate}T00:00:00`).toLocaleDateString('de-DE')}`
                          : ''}
                      </span>
                    </button>
                  </article>
                ))}
              {topLevelTasks.every((task) => task.status !== status) && (
                <p className="kanban-empty">Hier ist gerade Luft.</p>
              )}
            </div>
          </section>
        ))}
      </div>
      {topLevelTasks.some((task) => task.status === 'Cancelled') && (
        <p className="mt-4 text-sm text-[var(--theme-muted)]">
          {topLevelTasks.filter((task) => task.status === 'Cancelled').length}{' '}
          abgebrochene Aufgabe(n) liegen im Archiv.
        </p>
      )}
      {!tasks.isPending &&
        tasks.data?.filter((task) => !task.parentTaskId).length === 0 && (
          <Empty text="Noch keine Aufgaben. Frag den Chat oder leg die erste direkt hier an." />
        )}
      {selectedTaskId && (
        <TaskDetailDrawer
          organizationId={organizationId}
          taskId={selectedTaskId}
          projects={projects.data ?? []}
          members={members.data ?? []}
          onSelectTask={selectTask}
          onClose={() => selectTask(undefined)}
        />
      )}
    </FeatureLayout>
  )
}

export function IncidentsPage({ user }: PhaseSixPageProps) {
  const { organizationId, themePack } = usePhaseSixContext()
  const queryClient = useQueryClient()
  const incidents = useQuery({
    queryKey: ['incidents', organizationId],
    queryFn: () => listIncidents(organizationId),
    enabled: Boolean(organizationId),
  })
  const form = useForm<SaveIncidentInput>({
    defaultValues: {
      title: '',
      description: '',
      category:
        themePack.data?.configuration.incidentCategories[0] ?? 'Allgemein',
      severity: 'Medium',
      status: 'Reported',
      responsibleMemberId: '',
      occurredAt: new Date().toISOString().slice(0, 16),
    },
  })
  const createMutation = useMutation({
    mutationFn: (input: SaveIncidentInput) =>
      createIncident(organizationId, {
        ...input,
        responsibleMemberId: input.responsibleMemberId || undefined,
        occurredAt: new Date(input.occurredAt).toISOString(),
      }),
    onSuccess: async () => {
      form.reset()
      await invalidate(queryClient, organizationId, 'incidents')
    },
  })
  const resolveMutation = useMutation({
    mutationFn: ({ id, token }: { id: string; token: string }) =>
      resolveIncident(
        organizationId,
        id,
        'Ursache dokumentiert und Lage offiziell für beherrschbar erklärt.',
        token,
      ),
    onSuccess: async () => invalidate(queryClient, organizationId, 'incidents'),
  })

  return (
    <FeatureLayout
      user={user}
      title="Incident Reports"
      subtitle="Echte und humorvolle Betriebsstörungen inklusive Schweregrad und Lösung."
    >
      <CreatePanel title="Incident melden">
        <form
          className="feature-form"
          onSubmit={form.handleSubmit((input) => createMutation.mutate(input))}
        >
          <input placeholder="Titel" {...form.register('title')} required />
          <textarea
            placeholder="Was ist passiert?"
            {...form.register('description')}
            required
          />
          <input
            list="incident-categories"
            placeholder="Kategorie"
            {...form.register('category')}
            required
          />
          <datalist id="incident-categories">
            {themePack.data?.configuration.incidentCategories.map(
              (category) => (
                <option key={category} value={category} />
              ),
            )}
          </datalist>
          <select {...form.register('severity')}>
            {incidentSeverities.map((severity) => (
              <option key={severity}>{severity}</option>
            ))}
          </select>
          <input type="datetime-local" {...form.register('occurredAt')} />
          <SubmitButton pending={createMutation.isPending}>
            Incident registrieren
          </SubmitButton>
        </form>
      </CreatePanel>
      <CardGrid>
        {incidents.data?.map((incident) => (
          <article key={incident.id} className="feature-card">
            <StatusLine status={incident.status} priority={incident.severity} />
            <h2>{incident.title}</h2>
            <p>{incident.description}</p>
            <p className="text-xs font-bold text-[var(--theme-primary)]">
              {incident.category}
            </p>
            {incident.status !== 'Resolved' &&
              incident.status !== 'Rejected' && (
                <button
                  type="button"
                  className="feature-button"
                  onClick={() =>
                    resolveMutation.mutate({
                      id: incident.id,
                      token: incident.concurrencyToken,
                    })
                  }
                >
                  <CheckCircle2 size={16} />
                  Als gelöst dokumentieren
                </button>
              )}
          </article>
        ))}
      </CardGrid>
    </FeatureLayout>
  )
}

export function AwardsPage({ user }: PhaseSixPageProps) {
  const { organizationId, organization, themePack } = usePhaseSixContext()
  const queryClient = useQueryClient()
  const awards = useQuery({
    queryKey: ['awards', organizationId],
    queryFn: () => listAwards(organizationId),
    enabled: Boolean(organizationId),
  })
  const templates = useQuery({
    queryKey: [
      'award-templates',
      organizationId,
      organization.data?.themePackKey,
    ],
    queryFn: () =>
      listAwardTemplates(organizationId, organization.data!.themePackKey),
    enabled: Boolean(organizationId && organization.data?.themePackKey),
  })
  const members = useQuery({
    queryKey: ['members', organizationId],
    queryFn: () => listMembers(organizationId),
    enabled: Boolean(organizationId),
  })
  const form = useForm<GrantAwardInput>({
    defaultValues: {
      name: '',
      description: '',
      awardedToMemberId: '',
      icon: 'award',
      category: 'Besondere Verdienste',
      isPublic: true,
    },
  })
  const grantMutation = useMutation({
    mutationFn: (input: GrantAwardInput) => grantAward(organizationId, input),
    onSuccess: async () => {
      form.reset()
      await invalidate(queryClient, organizationId, 'awards')
    },
  })

  return (
    <FeatureLayout
      user={user}
      title={
        themePack.data?.configuration.terminology.award ?? 'Auszeichnungen'
      }
      subtitle="Offizielle Anerkennung für inoffizielle Höchstleistungen."
    >
      <CreatePanel title="Auszeichnung vergeben">
        <div className="mb-4 flex flex-wrap gap-2">
          {templates.data?.map((template) => (
            <button
              key={template.name}
              type="button"
              className="rounded-lg border border-white/10 px-3 py-2 text-xs text-white"
              onClick={() => {
                form.setValue('name', template.name)
                form.setValue(
                  'description',
                  template.descriptionTemplate.replace(
                    '{reason}',
                    'außergewöhnlicher Gemeinschaftsleistungen',
                  ),
                )
              }}
            >
              {template.name}
            </button>
          ))}
        </div>
        <form
          className="feature-form"
          onSubmit={form.handleSubmit((input) => grantMutation.mutate(input))}
        >
          <input placeholder="Name" {...form.register('name')} required />
          <textarea
            placeholder="Begründung"
            {...form.register('description')}
            required
          />
          <select {...form.register('awardedToMemberId')} required>
            <option value="">Mitglied auswählen</option>
            {members.data?.map((member) => (
              <option key={member.id} value={member.id}>
                {member.displayName}
              </option>
            ))}
          </select>
          <input placeholder="Kategorie" {...form.register('category')} />
          <SubmitButton pending={grantMutation.isPending}>
            Auszeichnung verleihen
          </SubmitButton>
        </form>
      </CreatePanel>
      <CardGrid>
        {awards.data?.map((award) => (
          <article key={award.id} className="feature-card">
            <AwardIcon className="text-[var(--theme-primary)]" />
            <h2>{award.name}</h2>
            <p>{award.description}</p>
            <p className="text-xs text-[var(--theme-muted)]">
              {memberName(members.data, award.awardedToMemberId)} ·{' '}
              {new Date(award.awardedAt).toLocaleDateString('de-DE')}
            </p>
          </article>
        ))}
      </CardGrid>
    </FeatureLayout>
  )
}

export function ActivitiesPage({ user }: PhaseSixPageProps) {
  const { organizationId } = usePhaseSixContext()
  const activities = useQuery({
    queryKey: ['activities', organizationId],
    queryFn: () => listActivities(organizationId),
    enabled: Boolean(organizationId),
  })

  return (
    <FeatureLayout
      user={user}
      title="Aktivitäten"
      subtitle="Was eure Community zuletzt erledigt, geändert oder gemeldet hat."
    >
      <Panel title="Chronik">
        <ActivityList activities={activities.data ?? []} />
      </Panel>
    </FeatureLayout>
  )
}

function ProjectDetail({
  project,
  tasks,
  onClose,
  onSelectTask,
}: {
  project?: Project
  tasks: WorkTask[]
  onClose: () => void
  onSelectTask: (taskId: string) => void
}) {
  if (!project) return null

  return (
    <div
      className="fixed inset-0 z-30 grid place-items-center bg-black/65 p-4 backdrop-blur-sm"
      onMouseDown={(event) => {
        if (event.currentTarget === event.target) onClose()
      }}
    >
      <section className="max-h-[88vh] w-full max-w-3xl overflow-y-auto rounded-3xl border border-white/10 bg-[var(--theme-background)] p-6 shadow-2xl sm:p-8">
        <div className="flex items-start justify-between gap-5">
          <div>
            <StatusLine status={project.status} priority={project.priority} />
            <h2 className="mt-3 text-3xl font-black text-white">
              {project.name}
            </h2>
          </div>
          <button
            type="button"
            aria-label="Projekt schließen"
            className="rounded-xl border border-white/10 p-2 text-[var(--theme-muted)] hover:text-white"
            onClick={onClose}
          >
            <X size={20} />
          </button>
        </div>
        <div className="mt-6 rounded-2xl bg-white/[0.04] p-5">
          <h3 className="font-bold text-white">Was wollen wir erreichen?</h3>
          <p className="mt-2 text-sm leading-6 whitespace-pre-wrap text-[var(--theme-muted)]">
            {project.description ??
              'Für dieses Projekt fehlt noch ein verständliches Ziel.'}
          </p>
        </div>
        <div className="mt-7 flex items-end justify-between gap-4">
          <div>
            <h3 className="text-lg font-black text-white">Aufgaben</h3>
            <p className="mt-1 text-sm text-[var(--theme-muted)]">
              Klick eine Aufgabe an, um Details und Fortschritt zu sehen.
            </p>
          </div>
          <span className="task-count">{tasks.length}</span>
        </div>
        <div className="mt-4 grid gap-3">
          {tasks.map((task) => (
            <button
              key={task.id}
              type="button"
              className="flex items-center gap-4 rounded-2xl border border-white/10 bg-white/[0.03] p-4 text-left transition hover:border-[var(--theme-primary)]/50 hover:bg-[var(--theme-primary)]/[0.05]"
              onClick={() => onSelectTask(task.id)}
            >
              <span
                className={`subtask-check ${
                  task.status === 'Done' ? 'is-done' : ''
                }`}
              >
                {task.status === 'Done' && <CheckCircle2 size={14} />}
              </span>
              <span>
                <span className="block font-bold text-white">{task.title}</span>
                <span className="mt-1 block text-xs text-[var(--theme-muted)]">
                  {task.status} · {task.priority}
                </span>
              </span>
              <ChevronRight
                size={18}
                className="ml-auto text-[var(--theme-primary)]"
              />
            </button>
          ))}
          {tasks.length === 0 && (
            <Empty text="Noch keine Aufgaben in diesem Projekt." />
          )}
        </div>
      </section>
    </div>
  )
}

function ActivityList({ activities }: { activities: Activity[] }) {
  if (activities.length === 0) {
    return <Empty text="Die Chronik ist noch verdächtig leer." />
  }

  return (
    <div className="grid gap-3">
      {activities.map((activity) => (
        <div
          key={activity.id}
          className="rounded-xl border border-white/10 bg-black/10 p-4"
        >
          <p className="font-semibold text-white">{renderActivity(activity)}</p>
          <p className="mt-1 text-xs text-[var(--theme-muted)]">
            {activity.actorDisplayName ?? 'System'} ·{' '}
            {new Date(activity.createdAt).toLocaleString('de-DE')}
          </p>
        </div>
      ))}
    </div>
  )
}

function renderActivity(activity: Activity) {
  switch (activity.activityType) {
    case 'organization.created':
      return `${activity.data.organizationName ?? 'Organisation'} wurde gegründet.`
    case 'member.joined':
      return `${activity.data.memberName ?? 'Ein Mitglied'} ist beigetreten.`
    case 'member.title-changed':
      return `Ein sichtbarer Titel wurde auf „${activity.data.visibleTitle ?? 'ohne Titel'}“ geändert.`
    case 'project.created':
      return `Projekt „${activity.data.projectName ?? 'Unbenannt'}“ wurde angelegt.`
    case 'project.completed':
      return `Projekt „${activity.data.projectName ?? 'Unbenannt'}“ wurde abgeschlossen.`
    case 'task.created':
      return `Aufgabe „${activity.data.taskTitle ?? 'Unbenannt'}“ wurde angelegt.`
    case 'task.completed':
      return `Aufgabe „${activity.data.taskTitle ?? 'Unbenannt'}“ wurde erledigt.`
    case 'incident.reported':
      return `Incident „${activity.data.incidentTitle ?? 'Unbenannt'}“ wurde gemeldet.`
    case 'incident.resolved':
      return `Incident „${activity.data.incidentTitle ?? 'Unbenannt'}“ wurde gelöst.`
    case 'award.granted':
      return `${activity.data.targetMemberName ?? 'Ein Mitglied'} erhielt „${activity.data.awardName ?? 'eine Auszeichnung'}“.`
    case 'assistant.work-plan-confirmed':
      return `KI-Entwurf „${activity.data.projectName ?? 'Unbenannt'}“ wurde mit ${activity.data.taskCount ?? 'mehreren'} Aufgaben bestätigt.`
    default:
      return `Aktivität ${activity.activityType}`
  }
}

function normalizeProject(input: SaveProjectInput): SaveProjectInput {
  return {
    ...input,
    description: input.description || undefined,
    ownerMemberId: input.ownerMemberId || undefined,
    startDate: input.startDate || undefined,
    dueDate: input.dueDate || undefined,
  }
}

function normalizeTask(input: SaveTaskInput): SaveTaskInput {
  return {
    ...input,
    description: input.description || undefined,
    projectId: input.projectId || undefined,
    parentTaskId: input.parentTaskId || undefined,
    assignedMemberId: input.assignedMemberId || undefined,
    dueDate: input.dueDate || undefined,
  }
}

function memberName(
  members: Awaited<ReturnType<typeof listMembers>> | undefined,
  memberId: string,
) {
  return (
    members?.find((member) => member.id === memberId)?.displayName ??
    'Unbekanntes Mitglied'
  )
}

async function invalidate(
  queryClient: ReturnType<typeof useQueryClient>,
  organizationId: string,
  key: string,
) {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: [key, organizationId] }),
    queryClient.invalidateQueries({
      queryKey: ['dashboard', organizationId],
    }),
    queryClient.invalidateQueries({
      queryKey: ['activities', organizationId],
    }),
  ])
}

function Metric({
  label,
  value,
  to,
}: {
  label: string
  value: number
  to?: string
}) {
  const content = (
    <>
      <p className="text-sm text-[var(--theme-muted)]">{label}</p>
      <p className="mt-2 text-4xl font-black text-white">{value}</p>
    </>
  )

  if (to) {
    return (
      <Link
        to={to}
        className="block rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5 transition hover:border-[var(--theme-primary)]/50 hover:bg-white/[0.05]"
      >
        {content}
      </Link>
    )
  }

  return (
    <div className="rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5">
      {content}
    </div>
  )
}

function Panel({
  title,
  children,
  className = '',
}: {
  title: string
  children: ReactNode
  className?: string
}) {
  return (
    <section
      className={`rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-6 ${className}`}
    >
      <h2 className="mb-4 text-lg font-bold text-white">{title}</h2>
      {children}
    </section>
  )
}

function CreatePanel({
  title,
  children,
}: {
  title: string
  children: ReactNode
}) {
  return (
    <details className="rounded-2xl border border-[var(--theme-primary)]/30 bg-[var(--theme-primary)]/[0.06] p-5">
      <summary className="flex cursor-pointer list-none items-center gap-2 font-bold text-white">
        <Plus size={18} />
        {title}
      </summary>
      <div className="mt-5">{children}</div>
    </details>
  )
}

function CardGrid({ children }: { children: ReactNode }) {
  return <div className="mt-6 grid gap-4 xl:grid-cols-2">{children}</div>
}

function StatusLine({
  status,
  priority,
}: {
  status: string
  priority: string
}) {
  return (
    <div className="flex items-center justify-between gap-3 text-xs font-bold tracking-wide uppercase">
      <span className="text-[var(--theme-primary)]">{status}</span>
      <span className="text-[var(--theme-muted)]">{priority}</span>
    </div>
  )
}

function SubmitButton({
  pending,
  children,
}: {
  pending: boolean
  children: ReactNode
}) {
  return (
    <button type="submit" className="feature-button" disabled={pending}>
      <Plus size={16} />
      {pending ? 'Wird gespeichert …' : children}
    </button>
  )
}

function QuickLink({
  organizationId,
  path,
  children,
}: {
  organizationId: string
  path: string
  children: ReactNode
}) {
  return (
    <Link
      to={`/organizations/${organizationId}/${path}`}
      className="feature-button"
    >
      {children}
    </Link>
  )
}

function Empty({ text }: { text: string }) {
  return <p className="text-sm text-[var(--theme-muted)]">{text}</p>
}

function ErrorBox({ error }: { error: Error }) {
  return (
    <div className="mt-4 rounded-xl border border-[var(--theme-danger)]/30 bg-[var(--theme-danger)]/10 p-4 text-sm text-[var(--theme-danger)]">
      {error.message}
    </div>
  )
}
