import { useMemo, useState } from 'react'
import {
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import {
  Activity,
  ArrowLeft,
  BarChart3,
  BookOpen,
  BrainCircuit,
  CalendarDays,
  Check,
  ChevronRight,
  ClipboardList,
  Gauge,
  HeartPulse,
  Home,
  Plus,
  Save,
  Shield,
  Sparkles,
  Target,
  UserRound,
  UserRoundPlus,
  Users,
  X,
  Zap,
} from 'lucide-react'
import {
  Link,
  Navigate,
  Route,
  Routes,
  useLocation,
  useParams,
} from 'react-router-dom'
import { getCurrentUser } from '../api/auth'
import {
  createFootballExercise,
  createFootballSession,
  getFootballSession,
  listFootballExercises,
  listFootballProfiles,
  listFootballSessions,
  replaceFootballTrainingBlocks,
  updateFootballAttendance,
  upsertFootballProfile,
  type FootballAttendanceStatus,
  type FootballExercise,
  type FootballExerciseCategory,
  type FootballExerciseLocation,
  type FootballIntensity,
  type FootballMemberProfile,
  type FootballPosition,
  type FootballSession,
  type FootballSessionKind,
  type FootballTeamRole,
  type FootballTrainingBlockInput,
  type UpsertFootballProfileInput,
} from '../api/football'
import { listMembers, type Member } from '../api/members'
import {
  listOrganizations,
  type OrganizationSummary,
} from '../api/organizations'

const navItems = [
  { to: '', label: 'Home', icon: Home },
  { to: 'squad', label: 'Mannschaft', icon: Users },
  { to: 'playbook', label: 'Playbook', icon: BookOpen },
  { to: 'training', label: 'Training', icon: ClipboardList },
  { to: 'individual', label: 'Individual', icon: Target },
  { to: 'performance', label: 'Leistung', icon: BarChart3 },
]

const defaultSessionStart = localDateTimeValue(
  new Date(Date.now() + 24 * 60 * 60 * 1000),
)

const roleLabels: Record<FootballTeamRole, string> = {
  Player: 'Spieler',
  Coach: 'Trainer',
  Staff: 'Betreuer',
}

const categoryLabels: Record<FootballExerciseCategory, string> = {
  Stability: 'Stabilität',
  Strength: 'Kraft',
  Mobility: 'Mobilität',
  Endurance: 'Ausdauer',
  Speed: 'Schnelligkeit',
  Technique: 'Technik',
  Tactics: 'Taktik',
}

const locationLabels: Record<FootballExerciseLocation, string> = {
  Pitch: 'Platz',
  Home: 'Zuhause',
  Gym: 'Gym',
  Anywhere: 'Überall',
}

const intensityLabels: Record<FootballIntensity, string> = {
  Low: 'Niedrig',
  Medium: 'Mittel',
  High: 'Hoch',
}

export function FootballApp() {
  const me = useQuery({
    queryKey: ['football-current-user'],
    queryFn: getCurrentUser,
    retry: false,
  })

  if (me.isPending) return <FootballLoading text="Sitzung wird geprüft …" />
  if (!me.data) {
    window.location.replace('/login')
    return <FootballLoading text="Weiterleitung zur Anmeldung …" />
  }

  return (
    <Routes>
      <Route path="/football" element={<FootballEntry />} />
      <Route
        path="/football/:organizationId/*"
        element={<FootballWorkspace userId={me.data.id} />}
      />
      <Route path="*" element={<Navigate to="/football" replace />} />
    </Routes>
  )
}

function FootballEntry() {
  const organizations = useQuery({
    queryKey: ['football-organizations'],
    queryFn: listOrganizations,
  })

  return (
    <div className="min-h-screen bg-slate-950 px-5 py-10 text-slate-100">
      <div className="mx-auto max-w-5xl">
        <Link
          to="/organizations"
          className="inline-flex items-center gap-2 text-sm text-slate-400 hover:text-white"
        >
          <ArrowLeft size={16} /> Community Intranet
        </Link>
        <div className="mt-10 rounded-3xl border border-emerald-400/20 bg-gradient-to-br from-emerald-500/15 via-slate-900 to-slate-950 p-8">
          <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-emerald-400 text-slate-950">
            <Shield size={28} />
          </div>
          <p className="mt-6 text-xs font-black uppercase tracking-[0.2em] text-emerald-300">
            Football Operations
          </p>
          <h1 className="mt-2 text-4xl font-black tracking-tight">
            Mannschaft, Training und Entwicklung an einem Ort.
          </h1>
          <p className="mt-4 max-w-3xl text-slate-300">
            Gemeinsame Trainingsplanung, echte Zu- und Absagen und sportliche
            Profile statt lokaler Demo-Daten.
          </p>
        </div>
        <h2 className="mt-10 text-lg font-bold">Mannschaft auswählen</h2>
        <div className="mt-4 grid gap-4 md:grid-cols-2">
          {organizations.data?.map((organization) => (
            <OrganizationCard
              key={organization.id}
              organization={organization}
            />
          ))}
        </div>
      </div>
    </div>
  )
}

function OrganizationCard({
  organization,
}: {
  organization: OrganizationSummary
}) {
  return (
    <Link
      to={`/football/${organization.id}`}
      className="group rounded-2xl border border-white/10 bg-white/5 p-5 transition hover:border-emerald-400/50 hover:bg-emerald-400/5"
    >
      <div className="flex items-center justify-between gap-4">
        <div>
          <p className="font-bold text-white">{organization.name}</p>
          <p className="mt-1 text-sm text-slate-400">
            {organization.description || 'Als Fußballmannschaft öffnen'}
          </p>
        </div>
        <ChevronRight className="text-slate-500 transition group-hover:translate-x-1 group-hover:text-emerald-300" />
      </div>
    </Link>
  )
}

function FootballWorkspace({ userId }: { userId: string }) {
  const { organizationId = '' } = useParams()
  const location = useLocation()
  const members = useQuery({
    queryKey: ['football-members', organizationId],
    queryFn: () => listMembers(organizationId),
    enabled: Boolean(organizationId),
  })
  const organizations = useQuery({
    queryKey: ['football-organizations'],
    queryFn: listOrganizations,
  })
  const profiles = useQuery({
    queryKey: ['football-profiles', organizationId],
    queryFn: () => listFootballProfiles(organizationId),
    enabled: Boolean(organizationId),
  })
  const organization = organizations.data?.find(
    (item) => item.id === organizationId,
  )
  const currentMemberId = members.data?.find(
    (member) => member.userId === userId,
  )?.id

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <header className="sticky top-0 z-20 border-b border-white/10 bg-slate-950/90 backdrop-blur">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-4 px-4 py-3 sm:px-6">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-400 text-slate-950">
              <Shield size={21} />
            </div>
            <div>
              <p className="font-black">{organization?.name || 'Football'}</p>
              <p className="text-xs text-slate-500">Training & Performance</p>
            </div>
          </div>
          <Link
            to="/organizations"
            className="text-sm text-slate-400 hover:text-white"
          >
            Community Intranet
          </Link>
        </div>
      </header>
      <div className="mx-auto grid max-w-7xl gap-6 px-4 py-6 sm:px-6 lg:grid-cols-[210px_1fr]">
        <aside className="flex gap-2 overflow-x-auto lg:block lg:space-y-1">
          {navItems.map(({ to, label, icon: Icon }) => {
            const target = `/football/${organizationId}${to ? `/${to}` : ''}`
            const active =
              location.pathname === target ||
              Boolean(to && location.pathname.startsWith(`${target}/`))
            return (
              <Link
                key={label}
                to={target}
                className={`flex shrink-0 items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-semibold ${active ? 'bg-emerald-400 text-slate-950' : 'text-slate-400 hover:bg-white/5 hover:text-white'}`}
              >
                <Icon size={17} />
                {label}
              </Link>
            )
          })}
        </aside>
        <main className="min-w-0">
          <Routes>
            <Route
              index
              element={
                <Dashboard
                  organizationId={organizationId}
                  members={members.data || []}
                  currentMemberId={currentMemberId}
                />
              }
            />
            <Route
              path="squad"
              element={
                <SquadPage
                  members={members.data || []}
                  profiles={profiles.data || []}
                  organizationId={organizationId}
                />
              }
            />
            <Route
              path="playbook"
              element={<PlaybookPage organizationId={organizationId} />}
            />
            <Route
              path="training"
              element={
                <TrainingPage
                  organizationId={organizationId}
                  members={members.data || []}
                  profiles={profiles.data || []}
                />
              }
            />
            <Route
              path="individual"
              element={<IndividualPage members={members.data || []} />}
            />
            <Route
              path="performance"
              element={<PerformancePage members={members.data || []} />}
            />
          </Routes>
        </main>
      </div>
    </div>
  )
}

function Dashboard({
  organizationId,
  members,
  currentMemberId,
}: {
  organizationId: string
  members: Member[]
  currentMemberId?: string
}) {
  const sessions = useQuery({
    queryKey: ['football-sessions', organizationId, 'upcoming'],
    queryFn: () =>
      listFootballSessions(organizationId, new Date().toISOString()),
  })
  const nextTraining = sessions.data?.find((item) => item.kind === 'Training')
  const nextMatch = sessions.data?.find((item) => item.kind === 'Match')
  const trainingDetail = useQuery({
    queryKey: ['football-session', organizationId, nextTraining?.id],
    queryFn: () => getFootballSession(organizationId, nextTraining!.id),
    enabled: Boolean(nextTraining?.id),
  })

  const accepted =
    trainingDetail.data?.attendance.filter((item) => item.status === 'Accepted')
      .length ?? 0
  const pending = Math.max(0, members.length - (trainingDetail.data?.attendance.length ?? 0))

  return (
    <div className="space-y-6">
      <PageHeading
        eyebrow="Heute im Blick"
        title="Mannschaftszentrale"
        text="Die nächsten Termine und deine aktuelle Rückmeldung – direkt aus dem gemeinsamen Mannschaftsbereich."
      />
      <div className="grid gap-4 xl:grid-cols-2">
        <SessionCard
          organizationId={organizationId}
          session={nextMatch}
          currentMemberId={currentMemberId}
          emptyText="Noch kein Spiel geplant."
        />
        <SessionCard
          organizationId={organizationId}
          session={nextTraining}
          currentMemberId={currentMemberId}
          emptyText="Noch kein Training geplant."
        />
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        <Stat
          label="Zugesagt"
          value={String(accepted)}
          hint="für das nächste Training"
          icon={<Check />}
        />
        <Stat
          label="Noch offen"
          value={String(pending)}
          hint="ohne Rückmeldung"
          icon={<CalendarDays />}
        />
        <Stat
          label="Kader"
          value={String(members.length)}
          hint="aktive Mitglieder"
          icon={<Users />}
        />
      </div>
    </div>
  )
}

function SessionCard({
  organizationId,
  session,
  currentMemberId,
  emptyText,
}: {
  organizationId: string
  session?: FootballSession
  currentMemberId?: string
  emptyText: string
}) {
  const queryClient = useQueryClient()
  const detail = useQuery({
    queryKey: ['football-session', organizationId, session?.id],
    queryFn: () => getFootballSession(organizationId, session!.id),
    enabled: Boolean(session?.id),
  })
  const mutation = useMutation({
    mutationFn: (status: FootballAttendanceStatus) =>
      updateFootballAttendance(
        organizationId,
        session!.id,
        currentMemberId!,
        status,
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['football-session', organizationId, session?.id],
      })
    },
  })

  if (!session) {
    return (
      <div className="rounded-2xl border border-dashed border-white/10 bg-white/[0.03] p-6 text-slate-500">
        {emptyText}
      </div>
    )
  }

  const ownStatus = detail.data?.attendance.find(
    (item) => item.memberId === currentMemberId,
  )?.status
  const accepted =
    detail.data?.attendance.filter((item) => item.status === 'Accepted').length ?? 0

  return (
    <div className="rounded-2xl border border-white/10 bg-white/5 p-6">
      <span className="text-xs font-black uppercase tracking-[0.18em] text-emerald-300">
        {session.kind === 'Match' ? 'Spiel' : 'Training'}
      </span>
      <h2 className="mt-3 text-2xl font-black">{session.title}</h2>
      <p className="mt-2 text-slate-300">
        {formatDateTime(session.startsAt)} · {session.durationMinutes} min
      </p>
      <p className="mt-1 text-sm text-slate-500">
        {[session.location, session.opponent && `vs. ${session.opponent}`]
          .filter(Boolean)
          .join(' · ') || 'Ort noch offen'}
      </p>
      {session.focus && (
        <p className="mt-5 rounded-xl bg-black/20 p-3 text-sm text-slate-400">
          Fokus: {session.focus}
        </p>
      )}
      <div className="mt-4 text-sm text-slate-400">{accepted} Zusagen</div>
      {currentMemberId && (
        <div className="mt-4 flex flex-wrap gap-2">
          <button
            onClick={() => mutation.mutate('Accepted')}
            className={`football-button ${ownStatus === 'Accepted' ? 'ring-2 ring-white/30' : ''}`}
          >
            <Check size={15} /> Zusagen
          </button>
          <button
            onClick={() => mutation.mutate('Maybe')}
            className="football-button-secondary"
          >
            Vielleicht
          </button>
          <button
            onClick={() => mutation.mutate('Declined')}
            className="football-button-secondary"
          >
            <X size={15} /> Absagen
          </button>
        </div>
      )}
    </div>
  )
}

function SquadPage({
  members,
  profiles,
  organizationId,
}: {
  members: Member[]
  profiles: FootballMemberProfile[]
  organizationId: string
}) {
  return (
    <div className="space-y-6">
      <PageHeading
        eyebrow="Kader"
        title="Profile & Rollen"
        text="Fußballrolle, Position, Rückennummer, Stärken und Entwicklungsfelder werden gemeinsam im Backend gespeichert."
        action={
          <Link to={`/organizations/${organizationId}/members`} className="football-button">
            <UserRoundPlus size={16} /> Spieler einladen
          </Link>
        }
      />
      <div className="grid gap-4 xl:grid-cols-2">
        {members.map((member) => (
          <ProfileCard
            key={`${member.id}-${profiles.find((profile) => profile.memberId === member.id)?.updatedAt ?? 'new'}`}
            organizationId={organizationId}
            member={member}
            profile={profiles.find((item) => item.memberId === member.id)}
          />
        ))}
      </div>
    </div>
  )
}

function ProfileCard({
  organizationId,
  member,
  profile,
}: {
  organizationId: string
  member: Member
  profile?: FootballMemberProfile
}) {
  const queryClient = useQueryClient()
  const [form, setForm] = useState<UpsertFootballProfileInput>({
    teamRole: profile?.teamRole ?? 'Player',
    position: profile?.position ?? 'Midfielder',
    shirtNumber: profile?.shirtNumber,
    description: profile?.description ?? '',
    strengths: profile?.strengths ?? [],
    developmentAreas: profile?.developmentAreas ?? [],
    secondaryPositions: profile?.secondaryPositions ?? [],
  })
  const mutation = useMutation({
    mutationFn: () => upsertFootballProfile(organizationId, member.id, form),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['football-profiles', organizationId],
      })
    },
  })

  return (
    <div className="rounded-2xl border border-white/10 bg-white/5 p-5">
      <div className="flex items-start gap-4">
        <Avatar member={member} />
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="font-bold">{member.displayName}</h3>
            <span className="football-chip">{roleLabels[form.teamRole]}</span>
          </div>
          <p className="mt-1 text-sm text-slate-500">{member.email}</p>
        </div>
      </div>
      <div className="mt-5 grid gap-3 sm:grid-cols-3">
        <Select
          label="Teamrolle"
          value={form.teamRole}
          onChange={(value) =>
            setForm((current) => ({
              ...current,
              teamRole: value as FootballTeamRole,
            }))
          }
          options={[
            ['Player', 'Spieler'],
            ['Coach', 'Trainer'],
            ['Staff', 'Betreuer'],
          ]}
        />
        <Select
          label="Position"
          value={form.position ?? 'Midfielder'}
          onChange={(value) =>
            setForm((current) => ({
              ...current,
              position: value as FootballPosition,
            }))
          }
          options={[
            ['Goalkeeper', 'Tor'],
            ['Defender', 'Abwehr'],
            ['Midfielder', 'Mittelfeld'],
            ['Forward', 'Sturm'],
          ]}
        />
        <Field label="Rückennummer">
          <input
            className="football-input"
            type="number"
            min="1"
            max="99"
            value={form.shirtNumber ?? ''}
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                shirtNumber: event.target.value
                  ? Number(event.target.value)
                  : undefined,
              }))
            }
          />
        </Field>
      </div>
      <Field label="Kurzbeschreibung">
        <textarea
          className="football-input min-h-20"
          value={form.description ?? ''}
          onChange={(event) =>
            setForm((current) => ({
              ...current,
              description: event.target.value,
            }))
          }
        />
      </Field>
      <div className="mt-3 grid gap-3 sm:grid-cols-2">
        <TagEditor
          label="Stärken"
          values={form.strengths}
          onChange={(strengths) =>
            setForm((current) => ({ ...current, strengths }))
          }
        />
        <TagEditor
          label="Entwicklungsfelder"
          values={form.developmentAreas}
          onChange={(developmentAreas) =>
            setForm((current) => ({ ...current, developmentAreas }))
          }
        />
      </div>
      <button
        onClick={() => mutation.mutate()}
        disabled={mutation.isPending}
        className="football-button mt-4"
      >
        <Save size={16} /> {mutation.isPending ? 'Speichert …' : 'Profil speichern'}
      </button>
      {mutation.isError && (
        <p className="mt-2 text-sm text-rose-300">{mutation.error.message}</p>
      )}
    </div>
  )
}

function PlaybookPage({ organizationId }: { organizationId: string }) {
  const queryClient = useQueryClient()
  const [category, setCategory] = useState<FootballExerciseCategory | undefined>()
  const [players, setPlayers] = useState(8)
  const [showCreate, setShowCreate] = useState(false)
  const exercises = useQuery({
    queryKey: ['football-exercises', organizationId, category, players],
    queryFn: () => listFootballExercises(organizationId, category, players),
  })
  const [draft, setDraft] = useState({
    title: '',
    description: '',
    category: 'Stability' as FootballExerciseCategory,
    location: 'Pitch' as FootballExerciseLocation,
    intensity: 'Medium' as FootballIntensity,
    minPlayers: 1,
    defaultDurationMinutes: 10,
    focus: '',
  })
  const createMutation = useMutation({
    mutationFn: () =>
      createFootballExercise(organizationId, {
        ...draft,
        equipment: [],
        tags: [],
      }),
    onSuccess: async () => {
      setShowCreate(false)
      setDraft((current) => ({ ...current, title: '', description: '', focus: '' }))
      await queryClient.invalidateQueries({
        queryKey: ['football-exercises', organizationId],
      })
    },
  })

  return (
    <div className="space-y-6">
      <PageHeading
        eyebrow="Wissensbasis"
        title="Playbook"
        text="Übungen kommen jetzt aus der gemeinsamen Datenbank und können nach Kategorie und Teilnehmerzahl gefiltert werden."
        action={
          <button onClick={() => setShowCreate((value) => !value)} className="football-button">
            <Plus size={16} /> Übung anlegen
          </button>
        }
      />
      {showCreate && (
        <Panel title="Neue Übung" icon={<Plus />}>
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            <Field label="Titel">
              <input className="football-input" value={draft.title} onChange={(event) => setDraft((current) => ({ ...current, title: event.target.value }))} />
            </Field>
            <Select label="Kategorie" value={draft.category} onChange={(value) => setDraft((current) => ({ ...current, category: value as FootballExerciseCategory }))} options={Object.entries(categoryLabels)} />
            <Select label="Ort" value={draft.location} onChange={(value) => setDraft((current) => ({ ...current, location: value as FootballExerciseLocation }))} options={Object.entries(locationLabels)} />
            <Field label="Dauer">
              <input className="football-input" type="number" min="1" value={draft.defaultDurationMinutes} onChange={(event) => setDraft((current) => ({ ...current, defaultDurationMinutes: Number(event.target.value) }))} />
            </Field>
            <Field label="Mindestspieler">
              <input className="football-input" type="number" min="1" value={draft.minPlayers} onChange={(event) => setDraft((current) => ({ ...current, minPlayers: Number(event.target.value) }))} />
            </Field>
            <Select label="Intensität" value={draft.intensity} onChange={(value) => setDraft((current) => ({ ...current, intensity: value as FootballIntensity }))} options={Object.entries(intensityLabels)} />
            <Field label="Fokus">
              <input className="football-input" value={draft.focus} onChange={(event) => setDraft((current) => ({ ...current, focus: event.target.value }))} />
            </Field>
          </div>
          <button onClick={() => createMutation.mutate()} className="football-button mt-4" disabled={!draft.title.trim() || createMutation.isPending}>
            <Save size={16} /> Speichern
          </button>
        </Panel>
      )}
      <div className="rounded-2xl border border-white/10 bg-white/5 p-4">
        <div className="flex flex-wrap gap-2">
          <button onClick={() => setCategory(undefined)} className={`rounded-full px-3 py-1.5 text-sm font-semibold ${!category ? 'bg-emerald-400 text-slate-950' : 'bg-white/5 text-slate-300'}`}>Alle</button>
          {Object.entries(categoryLabels).map(([value, label]) => (
            <button key={value} onClick={() => setCategory(value as FootballExerciseCategory)} className={`rounded-full px-3 py-1.5 text-sm font-semibold ${category === value ? 'bg-emerald-400 text-slate-950' : 'bg-white/5 text-slate-300'}`}>{label}</button>
          ))}
        </div>
        <label className="mt-4 flex max-w-sm items-center gap-3 text-sm text-slate-400">
          Teilnehmer
          <input type="range" min="1" max="24" value={players} onChange={(event) => setPlayers(Number(event.target.value))} className="flex-1" />
          <strong className="text-white">{players}</strong>
        </label>
      </div>
      <div className="grid gap-4 xl:grid-cols-2">
        {exercises.data?.map((exercise) => (
          <ExerciseCard key={exercise.id} exercise={exercise} />
        ))}
      </div>
      {!exercises.isPending && !exercises.data?.length && (
        <p className="text-sm text-slate-500">Für diesen Filter gibt es noch keine Übung.</p>
      )}
    </div>
  )
}

function TrainingPage({
  organizationId,
  members,
  profiles,
}: {
  organizationId: string
  members: Member[]
  profiles: FootballMemberProfile[]
}) {
  const queryClient = useQueryClient()
  const [selectedSessionId, setSelectedSessionId] = useState<string>()
  const [showCreate, setShowCreate] = useState(false)
  const [sessionDraft, setSessionDraft] = useState({
    kind: 'Training' as FootballSessionKind,
    title: '',
    focus: '',
    location: '',
    opponent: '',
    startsAt: defaultSessionStart,
    durationMinutes: 90,
  })
  const sessions = useQuery({
    queryKey: ['football-sessions', organizationId, 'training-page'],
    queryFn: () => listFootballSessions(organizationId, new Date().toISOString()),
  })
  const selectedId = selectedSessionId ?? sessions.data?.find((item) => item.kind === 'Training')?.id
  const detail = useQuery({
    queryKey: ['football-session', organizationId, selectedId],
    queryFn: () => getFootballSession(organizationId, selectedId!),
    enabled: Boolean(selectedId),
  })
  const createMutation = useMutation({
    mutationFn: () =>
      createFootballSession(organizationId, {
        ...sessionDraft,
        startsAt: new Date(sessionDraft.startsAt).toISOString(),
      }),
    onSuccess: async (session) => {
      setShowCreate(false)
      setSelectedSessionId(session.id)
      await queryClient.invalidateQueries({
        queryKey: ['football-sessions', organizationId],
      })
    },
  })
  const attendanceMutation = useMutation({
    mutationFn: ({ memberId, status }: { memberId: string; status: FootballAttendanceStatus }) =>
      updateFootballAttendance(organizationId, selectedId!, memberId, status),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['football-session', organizationId, selectedId],
      })
    },
  })
  const [plan, setPlan] = useState<FootballTrainingBlockInput[]>([])
  const acceptedMembers = useMemo(() => {
    const ids = new Set(
      detail.data?.attendance
        .filter((item) => item.status === 'Accepted')
        .map((item) => item.memberId) ?? [],
    )
    return members.filter((member) => ids.has(member.id))
  }, [detail.data?.attendance, members])

  const generatePlan = () => {
    const positions = acceptedMembers
      .map((member) => profiles.find((profile) => profile.memberId === member.id)?.position)
      .filter(Boolean)
    const forwards = positions.filter((position) => position === 'Forward').length
    const defenders = positions.filter((position) => position === 'Defender').length
    const focus = detail.data?.session.focus || 'Spielfähigkeit'
    const mainTitle =
      forwards >= Math.max(3, positions.length / 2)
        ? 'Abschluss unter Druck + Gegenpressing'
        : defenders >= Math.max(3, positions.length / 2)
          ? 'Spielaufbau unter Druck + Restverteidigung'
          : focus

    setPlan([
      { title: 'Aktivierung & Mobilität', durationMinutes: 12, description: 'Dynamische Aktivierung mit Ball.', aiReason: 'Vorbereitung auf Belastung und saubere Bewegungsqualität.' },
      { title: 'Technisches Warm-up', durationMinutes: 15, description: 'Ballnahe Technik und Orientierung.', aiReason: `Vorbereitung auf den Schwerpunkt ${focus}.` },
      { title: mainTitle, durationMinutes: 30, description: 'Hauptteil auf Basis der tatsächlich zugesagten Positionsgruppen.', aiReason: `${acceptedMembers.length} Zusagen und ${positions.length} bekannte Positionsprofile berücksichtigt.` },
      { title: 'Spielform', durationMinutes: 25, description: 'Transfer in eine spielnahe Form.', aiReason: 'Schwerpunkt in Entscheidungen unter Gegnerdruck übertragen.' },
      { title: 'Cooldown & Feedback', durationMinutes: 8, description: 'Kurz herunterfahren und Feedback einsammeln.', aiReason: 'Basis für spätere Belastungs- und Übungsbewertung.' },
    ])
  }
  const savePlan = useMutation({
    mutationFn: () => replaceFootballTrainingBlocks(organizationId, selectedId!, plan),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['football-session', organizationId, selectedId],
      })
    },
  })

  return (
    <div className="space-y-6">
      <PageHeading
        eyebrow="Trainerteam"
        title="Training & Termine"
        text="Termine anlegen, Teilnehmerstatus verwalten und den Trainingsplan auf Basis der zugesagten Spieler vorbereiten."
        action={<button onClick={() => setShowCreate((value) => !value)} className="football-button"><Plus size={16} /> Termin anlegen</button>}
      />
      {showCreate && (
        <Panel title="Neuer Termin" icon={<CalendarDays />}>
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            <Select label="Art" value={sessionDraft.kind} onChange={(value) => setSessionDraft((current) => ({ ...current, kind: value as FootballSessionKind }))} options={[["Training","Training"],["Match","Spiel"],["Individual","Individual"],["PerformanceTest","Leistungstest"]]} />
            <Field label="Titel"><input className="football-input" value={sessionDraft.title} onChange={(event) => setSessionDraft((current) => ({ ...current, title: event.target.value }))} /></Field>
            <Field label="Start"><input className="football-input" type="datetime-local" value={sessionDraft.startsAt} onChange={(event) => setSessionDraft((current) => ({ ...current, startsAt: event.target.value }))} /></Field>
            <Field label="Dauer"><input className="football-input" type="number" min="5" value={sessionDraft.durationMinutes} onChange={(event) => setSessionDraft((current) => ({ ...current, durationMinutes: Number(event.target.value) }))} /></Field>
            <Field label="Ort"><input className="football-input" value={sessionDraft.location} onChange={(event) => setSessionDraft((current) => ({ ...current, location: event.target.value }))} /></Field>
            <Field label="Fokus"><input className="football-input" value={sessionDraft.focus} onChange={(event) => setSessionDraft((current) => ({ ...current, focus: event.target.value }))} /></Field>
            {sessionDraft.kind === 'Match' && <Field label="Gegner"><input className="football-input" value={sessionDraft.opponent} onChange={(event) => setSessionDraft((current) => ({ ...current, opponent: event.target.value }))} /></Field>}
          </div>
          <button onClick={() => createMutation.mutate()} disabled={!sessionDraft.title.trim() || createMutation.isPending} className="football-button mt-4"><Save size={16} /> Termin speichern</button>
          {createMutation.isError && <p className="mt-2 text-sm text-rose-300">{createMutation.error.message}</p>}
        </Panel>
      )}
      <div className="grid gap-4 xl:grid-cols-[320px_1fr]">
        <Panel title="Anstehende Termine" icon={<CalendarDays />}>
          <div className="space-y-2">
            {sessions.data?.map((session) => (
              <button key={session.id} onClick={() => setSelectedSessionId(session.id)} className={`w-full rounded-xl border p-3 text-left ${selectedId === session.id ? 'border-emerald-400/60 bg-emerald-400/10' : 'border-white/10 bg-black/20'}`}>
                <p className="text-xs font-bold uppercase text-emerald-300">{session.kind === 'Match' ? 'Spiel' : session.kind === 'Training' ? 'Training' : session.kind}</p>
                <p className="mt-1 font-bold">{session.title}</p>
                <p className="mt-1 text-xs text-slate-500">{formatDateTime(session.startsAt)}</p>
              </button>
            ))}
          </div>
        </Panel>
        <div className="space-y-4">
          {detail.data ? (
            <>
              <Panel title={detail.data.session.title} icon={<ClipboardList />}>
                <p className="text-sm text-slate-400">{formatDateTime(detail.data.session.startsAt)} · {detail.data.session.durationMinutes} min · {detail.data.session.location || 'Ort offen'}</p>
                {detail.data.session.focus && <p className="mt-2 text-sm text-slate-300">Fokus: {detail.data.session.focus}</p>}
                <div className="mt-5"><p className="text-xs font-bold uppercase tracking-wide text-slate-500">Teilnahme verwalten</p><div className="mt-3 flex flex-wrap gap-2">{members.map((member) => { const status = detail.data.attendance.find((item) => item.memberId === member.id)?.status ?? 'Pending'; return <button key={member.id} onClick={() => attendanceMutation.mutate({ memberId: member.id, status: status === 'Accepted' ? 'Pending' : 'Accepted' })} className={`rounded-full px-3 py-1.5 text-sm ${status === 'Accepted' ? 'bg-emerald-400 text-slate-950' : status === 'Declined' ? 'bg-rose-500/20 text-rose-200' : 'bg-white/5 text-slate-400'}`}>{member.displayName}</button> })}</div></div>
              </Panel>
              {detail.data.session.kind === 'Training' && (
                <Panel title="Trainingsplan" icon={<BrainCircuit />}>
                  <div className="flex flex-wrap items-center justify-between gap-3"><p className="text-sm text-slate-400">{acceptedMembers.length} zugesagte Spieler werden berücksichtigt.</p><button onClick={generatePlan} className="football-button"><Sparkles size={16} /> Entwurf generieren</button></div>
                  <div className="mt-4 space-y-3">{(plan.length ? plan : detail.data.blocks.map((block) => ({ title: block.title, durationMinutes: block.durationMinutes, description: block.description, coachingPoints: block.coachingPoints, responsibleMemberId: block.responsibleMemberId, exerciseId: block.exerciseId, aiReason: block.aiReason }))).map((block, index) => <div key={`${block.title}-${index}`} className="grid gap-3 rounded-xl border border-white/10 bg-black/20 p-4 sm:grid-cols-[70px_1fr]"><strong className="text-emerald-300">{block.durationMinutes} min</strong><div><p className="font-bold">{block.title}</p>{block.description && <p className="mt-1 text-sm text-slate-400">{block.description}</p>}{block.aiReason && <p className="mt-1 text-xs text-slate-500">Warum: {block.aiReason}</p>}</div></div>)}</div>
                  {plan.length > 0 && <button onClick={() => savePlan.mutate()} className="football-button mt-4" disabled={savePlan.isPending}><Save size={16} /> Plan speichern</button>}
                </Panel>
              )}
            </>
          ) : (
            <Panel title="Noch kein Termin gewählt" icon={<CalendarDays />}><p className="text-sm text-slate-500">Lege einen Termin an oder wähle links einen bestehenden aus.</p></Panel>
          )}
        </div>
      </div>
    </div>
  )
}

function IndividualPage({ members }: { members: Member[] }) {
  return <div className="space-y-6"><PageHeading eyebrow="Gezielte Entwicklung" title="Individualtraining" text="Die persistente Individualplanung kommt als nächste Backend-Schicht. Die Spielerprofile liefern bereits die Entwicklungsfelder dafür." /><div className="grid gap-4 xl:grid-cols-2">{members.slice(0, 6).map((member) => <div key={member.id} className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="flex items-center gap-3"><Avatar member={member} /><div><p className="font-bold">{member.displayName}</p><p className="text-sm text-slate-500">Individualblock aus Entwicklungsfeldern vorbereiten</p></div></div></div>)}</div></div>
}

function PerformancePage({ members }: { members: Member[] }) {
  return <div className="space-y-6"><PageHeading eyebrow="Fortschritt" title="Leistungstests" text="Die Terminart Leistungstest existiert bereits; Messdefinitionen und Ergebnis-Historie folgen als eigene Persistenzschicht." /><div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4"><TestCard icon={<Zap />} title="10 m Sprint" unit="s" /><TestCard icon={<Gauge />} title="30 m Sprint" unit="s" /><TestCard icon={<Activity />} title="5-10-5 Agility" unit="s" /><TestCard icon={<HeartPulse />} title="Yo-Yo / Ausdauer" unit="Level" /></div><Panel title="Kader" icon={<Users />}><p className="text-sm text-slate-400">{members.length} Spieler können später über wiederholbare Tests miteinander und mit ihrer eigenen Historie verglichen werden.</p></Panel></div>
}

function ExerciseCard({ exercise }: { exercise: FootballExercise }) {
  return <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="flex items-start justify-between gap-4"><div><span className="football-chip">{categoryLabels[exercise.category]}</span><h3 className="mt-3 text-lg font-black">{exercise.title}</h3></div><span className="text-xs text-slate-500">{intensityLabels[exercise.intensity]}</span></div><p className="mt-3 text-sm leading-6 text-slate-400">{exercise.description || 'Noch keine Beschreibung.'}</p><div className="mt-4 grid grid-cols-2 gap-2 text-xs text-slate-300"><span>👥 ab {exercise.minPlayers}</span><span>⏱ {exercise.defaultDurationMinutes} min</span><span>📍 {locationLabels[exercise.location]}</span><span>⚡ {intensityLabels[exercise.intensity]}</span></div>{exercise.focus && <p className="mt-4 text-sm text-slate-300"><strong>Fokus:</strong> {exercise.focus}</p>}</div>
}

function TestCard({ icon, title, unit }: { icon: React.ReactNode; title: string; unit: string }) {
  return <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="text-emerald-300">{icon}</div><p className="mt-4 font-black">{title}</p><p className="mt-1 text-sm text-slate-500">Messwert in {unit}</p></div>
}

function Stat({ label, value, hint, icon }: { label: string; value: string; hint: string; icon: React.ReactNode }) {
  return <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="text-emerald-300">{icon}</div><p className="mt-4 text-3xl font-black">{value}</p><p className="mt-1 font-semibold">{label}</p><p className="text-sm text-slate-500">{hint}</p></div>
}

function Panel({ title, icon, children }: { title: string; icon: React.ReactNode; children: React.ReactNode }) {
  return <section className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="mb-4 flex items-center gap-2 font-black"><span className="text-emerald-300">{icon}</span>{title}</div>{children}</section>
}

function PageHeading({ eyebrow, title, text, action }: { eyebrow: string; title: string; text: string; action?: React.ReactNode }) {
  return <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end"><div><p className="text-xs font-black uppercase tracking-[0.18em] text-emerald-300">{eyebrow}</p><h1 className="mt-2 text-3xl font-black tracking-tight">{title}</h1><p className="mt-2 max-w-3xl text-sm leading-6 text-slate-400">{text}</p></div>{action}</div>
}

function FootballLoading({ text }: { text: string }) {
  return <div className="flex min-h-screen items-center justify-center bg-slate-950 text-slate-400">{text}</div>
}

function Avatar({ member }: { member: Member }) {
  return member.avatarUrl ? <img src={member.avatarUrl} alt="" className="h-11 w-11 rounded-xl object-cover" /> : <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-white/10"><UserRound size={20} /></div>
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return <label className="mt-3 block"><span className="mb-2 block text-xs font-bold uppercase tracking-wide text-slate-500">{label}</span>{children}</label>
}

function Select({ label, value, onChange, options }: { label: string; value: string; onChange: (value: string) => void; options: Array<[string, string]> }) {
  return <Field label={label}><select className="football-input" value={value} onChange={(event) => onChange(event.target.value)}>{options.map(([optionValue, optionLabel]) => <option key={optionValue} value={optionValue}>{optionLabel}</option>)}</select></Field>
}

function TagEditor({ label, values, onChange }: { label: string; values: string[]; onChange: (values: string[]) => void }) {
  const [draft, setDraft] = useState('')
  return <div><p className="text-xs font-bold uppercase tracking-wide text-slate-500">{label}</p><div className="mt-2 flex flex-wrap gap-1">{values.map((value) => <button key={value} onClick={() => onChange(values.filter((item) => item !== value))} className="football-chip">{value} ×</button>)}</div><div className="mt-2 flex gap-2"><input className="football-input" value={draft} onChange={(event) => setDraft(event.target.value)} placeholder="z. B. Antritt" /><button className="football-button-secondary" onClick={() => { if (draft.trim()) { onChange([...values, draft.trim()]); setDraft('') } }}>+</button></div></div>
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('de-DE', {
    weekday: 'short',
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

function localDateTimeValue(date: Date) {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 16)
}
