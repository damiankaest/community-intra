import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Bolt,
  Clock3,
  Hammer,
  LogIn,
  LogOut,
  RotateCw,
  Trash2,
  Users,
  Wrench,
} from 'lucide-react'
import { useParams } from 'react-router-dom'
import type { CurrentUser } from '../api/auth'
import {
  clockIn,
  clockOut,
  getTimeClockOverview,
  logWork,
  type WorkLogKind,
} from '../api/timeTracking'
import { FeatureLayout } from './PhaseSixPages'

const workKinds: Array<{
  kind: WorkLogKind
  label: string
  prompt: string
  icon: ReactNode
}> = [
  {
    kind: 'Built',
    label: 'Gebaut',
    prompt: 'Was steht jetzt, was vorher noch nicht stand?',
    icon: <Hammer size={22} />,
  },
  {
    kind: 'Fixed',
    label: 'Gefixt',
    prompt: 'Welches Problem wurde aus der Welt geschraubt?',
    icon: <Wrench size={22} />,
  },
  {
    kind: 'Optimized',
    label: 'Optimiert',
    prompt: 'Was läuft jetzt schneller, schöner oder weniger chaotisch?',
    icon: <Bolt size={22} />,
  },
  {
    kind: 'Destroyed',
    label: 'Zerstört',
    prompt: 'Was musste der strategischen Neuausrichtung weichen?',
    icon: <Trash2 size={22} />,
  },
]

const kindLabels: Record<WorkLogKind, string> = {
  Built: 'Gebaut',
  Fixed: 'Gefixt',
  Optimized: 'Optimiert',
  Destroyed: 'Zerstört',
}

export function TimeClockPage({ user }: { user: CurrentUser }) {
  const { organizationId = '' } = useParams()
  const queryClient = useQueryClient()
  const [selectedKind, setSelectedKind] = useState<WorkLogKind>()
  const [note, setNote] = useState('')
  const [now, setNow] = useState(() => Date.now())
  const overview = useQuery({
    queryKey: ['time-clock', organizationId],
    queryFn: () => getTimeClockOverview(organizationId),
    enabled: Boolean(organizationId),
    refetchInterval: 30_000,
  })
  const activeShift = overview.data?.activeShift

  useEffect(() => {
    if (!activeShift) return
    const interval = window.setInterval(() => setNow(Date.now()), 1000)
    return () => window.clearInterval(interval)
  }, [activeShift])

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: ['time-clock', organizationId],
      }),
      queryClient.invalidateQueries({
        queryKey: ['activities', organizationId],
      }),
      queryClient.invalidateQueries({
        queryKey: ['dashboard', organizationId],
      }),
    ])
  }
  const clockInMutation = useMutation({
    mutationFn: () => clockIn(organizationId),
    onSuccess: invalidate,
  })
  const clockOutMutation = useMutation({
    mutationFn: () => clockOut(organizationId),
    onSuccess: async () => {
      setSelectedKind(undefined)
      setNote('')
      await invalidate()
    },
  })
  const logMutation = useMutation({
    mutationFn: ({ kind, body }: { kind: WorkLogKind; body: string }) =>
      logWork(organizationId, kind, body),
    onSuccess: async () => {
      setSelectedKind(undefined)
      setNote('')
      await invalidate()
    },
  })
  const runningSeconds = useMemo(() => {
    if (!activeShift || !overview.data) return 0
    const sinceCheck = Math.max(
      0,
      Math.floor((now - new Date(overview.data.checkedAt).getTime()) / 1000),
    )
    return activeShift.elapsedSeconds + sinceCheck
  }, [activeShift, now, overview.data])
  const selected = workKinds.find((item) => item.kind === selectedKind)
  const error =
    overview.error ??
    clockInMutation.error ??
    clockOutMutation.error ??
    logMutation.error
  const maximumLeaderboardSeconds = Math.max(
    1,
    ...(overview.data?.weeklyLeaderboard.map((item) => item.elapsedSeconds) ??
      []),
  )

  return (
    <FeatureLayout
      user={user}
      title="FICSIT-Stechuhr"
      subtitle="Einstempeln, Fortschritt festhalten und am Ende wenigstens wissen, wo der Abend geblieben ist."
    >
      {error && <TimeClockError error={error} />}

      <section className={`time-clock-hero ${activeShift ? 'is-running' : ''}`}>
        <div className="time-clock-status">
          <span className="time-clock-status-light" />
          {activeShift ? 'Schicht läuft' : 'Noch nicht im Dienst'}
        </div>
        <div className="time-clock-display">
          {formatDuration(activeShift ? runningSeconds : 0)}
        </div>
        <p>
          {activeShift
            ? `Eingestempelt seit ${formatTime(activeShift.startedAt)} Uhr`
            : 'Die Produktivität wartet überraschend geduldig auf dich.'}
        </p>
        <button
          type="button"
          className={`time-clock-punch-button ${
            activeShift ? 'is-clock-out' : ''
          }`}
          disabled={clockInMutation.isPending || clockOutMutation.isPending}
          onClick={() =>
            activeShift ? clockOutMutation.mutate() : clockInMutation.mutate()
          }
        >
          {activeShift ? <LogOut size={20} /> : <LogIn size={20} />}
          {activeShift
            ? clockOutMutation.isPending
              ? 'Schicht wird beendet …'
              : 'Feierabend'
            : clockInMutation.isPending
              ? 'Stempel läuft …'
              : 'Schicht beginnen'}
        </button>
        <div className="time-clock-totals">
          <div>
            <strong>
              {formatCompactDuration(overview.data?.todaySeconds)}
            </strong>
            <span>heute</span>
          </div>
          <div>
            <strong>{formatCompactDuration(overview.data?.weekSeconds)}</strong>
            <span>diese Woche</span>
          </div>
          <div>
            <strong>{overview.data?.recentEntries.length ?? 0}</strong>
            <span>Logbucheinträge</span>
          </div>
        </div>
      </section>

      <section className="mt-6">
        <div className="mb-3 flex items-end justify-between gap-4">
          <div>
            <h2 className="text-lg font-black text-white">Was ist passiert?</h2>
            <p className="mt-1 text-sm text-[var(--theme-muted)]">
              Ein Klick, ein verständlicher Satz, fertig.
            </p>
          </div>
          {!activeShift && (
            <span className="text-xs text-[var(--theme-warning)]">
              Erst einstempeln
            </span>
          )}
        </div>
        <div className="time-clock-quick-grid">
          {workKinds.map((item) => (
            <button
              key={item.kind}
              type="button"
              className={`time-clock-quick-action ${
                selectedKind === item.kind ? 'is-selected' : ''
              }`}
              disabled={!activeShift}
              onClick={() => {
                setSelectedKind(item.kind)
                setNote('')
              }}
            >
              {item.icon}
              <span>{item.label}</span>
            </button>
          ))}
        </div>
        {selected && (
          <form
            className="time-clock-composer"
            onSubmit={(event) => {
              event.preventDefault()
              if (note.trim()) {
                logMutation.mutate({
                  kind: selected.kind,
                  body: note.trim(),
                })
              }
            }}
          >
            <div>
              <strong>{selected.label}</strong>
              <span>{selected.prompt}</span>
            </div>
            <input
              autoFocus
              value={note}
              maxLength={240}
              aria-label={`${selected.label} beschreiben`}
              placeholder="Kurz und konkret …"
              onChange={(event) => setNote(event.target.value)}
            />
            <button
              type="submit"
              disabled={!note.trim() || logMutation.isPending}
            >
              {logMutation.isPending ? 'Wird verbucht …' : 'Ins Logbuch'}
            </button>
          </form>
        )}
      </section>

      <div className="mt-6 grid gap-6 xl:grid-cols-[1.15fr_0.85fr]">
        <section className="time-clock-panel">
          <div className="time-clock-panel-title">
            <div>
              <h2>Wer ist im Dienst?</h2>
              <p>Live aus der hochseriösen Personaldisposition.</p>
            </div>
            <span>
              <Users size={15} />
              {overview.data?.activeMembers.length ?? 0}
            </span>
          </div>
          <div className="time-clock-presence-list">
            {overview.data?.activeMembers.map((member) => (
              <div key={member.memberId}>
                <span className="time-clock-avatar">
                  {initials(member.displayName)}
                </span>
                <div>
                  <strong>
                    {member.displayName ?? 'Unbekanntes Mitglied'}
                  </strong>
                  <small>seit {formatTime(member.startedAt)} Uhr</small>
                </div>
                <b>{formatCompactDuration(member.elapsedSeconds)}</b>
              </div>
            ))}
            {overview.data?.activeMembers.length === 0 && (
              <p className="time-clock-empty">
                Aktuell ist niemand eingestempelt. Die Fabrik behauptet, das sei
                geplant.
              </p>
            )}
          </div>
        </section>

        <section className="time-clock-panel">
          <div className="time-clock-panel-title">
            <div>
              <h2>Wochenleistung</h2>
              <p>Zeit, nicht Selbstdarstellung.</p>
            </div>
            <Clock3 size={18} />
          </div>
          <div className="time-clock-leaderboard">
            {overview.data?.weeklyLeaderboard.map((member, index) => (
              <div key={member.memberId}>
                <span>{index + 1}</span>
                <div>
                  <div>
                    <strong>{member.displayName ?? 'Unbekannt'}</strong>
                    <b>{formatCompactDuration(member.elapsedSeconds)}</b>
                  </div>
                  <i>
                    <em
                      style={{
                        width: `${Math.max(
                          4,
                          Math.round(
                            (member.elapsedSeconds /
                              maximumLeaderboardSeconds) *
                              100,
                          ),
                        )}%`,
                      }}
                    />
                  </i>
                </div>
              </div>
            ))}
            {overview.data?.weeklyLeaderboard.length === 0 && (
              <p className="time-clock-empty">
                Diese Woche wurde noch keine Zeit verbucht.
              </p>
            )}
          </div>
        </section>
      </div>

      <div className="mt-6 grid gap-6 xl:grid-cols-[1.35fr_0.65fr]">
        <section className="time-clock-panel">
          <div className="time-clock-panel-title">
            <div>
              <h2>Team-Logbuch</h2>
              <p>Was wirklich gebaut, repariert oder geopfert wurde.</p>
            </div>
            <RotateCw size={18} />
          </div>
          <div className="time-clock-log-list">
            {overview.data?.recentEntries.map((entry) => (
              <article key={entry.id}>
                <span className={`is-${entry.kind.toLowerCase()}`}>
                  {kindLabels[entry.kind]}
                </span>
                <div>
                  <strong>{entry.note}</strong>
                  <small>
                    {entry.memberDisplayName ?? 'Unbekannt'} ·{' '}
                    {formatDateTime(entry.createdAt)}
                  </small>
                </div>
              </article>
            ))}
            {overview.data?.recentEntries.length === 0 && (
              <p className="time-clock-empty">
                Noch nichts im Logbuch. Entweder lief alles perfekt oder niemand
                möchte verantwortlich sein.
              </p>
            )}
          </div>
        </section>

        <section className="time-clock-panel">
          <div className="time-clock-panel-title">
            <div>
              <h2>Meine letzten Schichten</h2>
              <p>Für die persönliche Beweisführung.</p>
            </div>
          </div>
          <div className="time-clock-shift-list">
            {overview.data?.recentShifts.map((shift) => (
              <div key={shift.id}>
                <span>{formatDate(shift.startedAt)}</span>
                <strong>{formatCompactDuration(shift.elapsedSeconds)}</strong>
                <small>
                  {formatTime(shift.startedAt)}–
                  {shift.endedAt ? formatTime(shift.endedAt) : 'läuft'}
                </small>
              </div>
            ))}
            {overview.data?.recentShifts.length === 0 && (
              <p className="time-clock-empty">Noch keine Schicht erfasst.</p>
            )}
          </div>
        </section>
      </div>
    </FeatureLayout>
  )
}

function TimeClockError({ error }: { error: Error }) {
  return (
    <div className="mb-5 rounded-2xl border border-[var(--theme-danger)]/35 bg-[var(--theme-danger)]/10 p-4 text-sm text-[var(--theme-danger)]">
      {error.message}
    </div>
  )
}

function formatDuration(totalSeconds: number) {
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60
  return [hours, minutes, seconds]
    .map((value) => String(value).padStart(2, '0'))
    .join(':')
}

function formatCompactDuration(totalSeconds = 0) {
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat('de-DE', {
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('de-DE', {
    day: '2-digit',
    month: '2-digit',
  }).format(new Date(value))
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('de-DE', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

function initials(name?: string) {
  if (!name) return '?'
  return name
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase()
}
