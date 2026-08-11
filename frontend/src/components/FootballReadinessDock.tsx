import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Activity, ChevronDown, ChevronUp, HeartPulse, Save, ShieldAlert } from 'lucide-react'
import { getCurrentUser } from '../api/auth'
import {
  getFootballTrainingHistory,
  listFootballAvailability,
  updateFootballAvailability,
  updateFootballSessionLoad,
  type FootballAvailabilityStatus,
} from '../api/football'
import { listMembers } from '../api/members'

const statusLabels: Record<FootballAvailabilityStatus, string> = {
  Fit: 'Fit',
  Limited: 'Angeschlagen',
  ReturnToPlay: 'Return-to-Play',
  Injured: 'Verletzt',
}

function organizationIdFromPath() {
  const match = window.location.pathname.match(/^\/football\/([0-9a-f-]{36})(?:\/|$)/i)
  return match?.[1]
}

export function FootballReadinessDock() {
  const organizationId = organizationIdFromPath()
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const [draftStatus, setDraftStatus] = useState<FootballAvailabilityStatus>()
  const [draftLoad, setDraftLoad] = useState<number>()
  const [draftNote, setDraftNote] = useState<string>()
  const [rpe, setRpe] = useState(5)
  const [minutes, setMinutes] = useState<number>()

  const me = useQuery({
    queryKey: ['football-current-user'],
    queryFn: getCurrentUser,
    retry: false,
    enabled: Boolean(organizationId),
  })
  const members = useQuery({
    queryKey: ['football-members', organizationId],
    queryFn: () => listMembers(organizationId!),
    enabled: Boolean(organizationId && me.data),
  })
  const availability = useQuery({
    queryKey: ['football-availability', organizationId],
    queryFn: () => listFootballAvailability(organizationId!),
    enabled: Boolean(organizationId && me.data),
  })

  const memberId = members.data?.find((member) => member.userId === me.data?.id)?.id
  const own = availability.data?.find((item) => item.memberId === memberId)
  const status = draftStatus ?? own?.status ?? 'Fit'
  const maxLoadPercent = draftLoad ?? own?.maxLoadPercent ?? 100
  const note = draftNote ?? own?.note ?? ''

  const history = useQuery({
    queryKey: ['football-history', organizationId, memberId],
    queryFn: () => getFootballTrainingHistory(organizationId!, memberId!, 6),
    enabled: Boolean(organizationId && memberId && open),
  })

  const latestSession = history.data?.[0]
  const weeklyLoad = useMemo(
    () => history.data?.reduce((sum, entry) => sum + (entry.trainingLoad ?? 0), 0) ?? 0,
    [history.data],
  )

  const save = useMutation({
    mutationFn: () =>
      updateFootballAvailability(
        organizationId!,
        memberId!,
        status,
        maxLoadPercent,
        note || undefined,
      ),
    onSuccess: async () => {
      setDraftStatus(undefined)
      setDraftLoad(undefined)
      setDraftNote(undefined)
      await queryClient.invalidateQueries({ queryKey: ['football-availability', organizationId] })
      await queryClient.invalidateQueries({ queryKey: ['football-session', organizationId] })
    },
  })

  const saveRpe = useMutation({
    mutationFn: () =>
      updateFootballSessionLoad(
        organizationId!,
        latestSession!.session.id,
        memberId!,
        rpe,
        minutes ?? latestSession!.plannedMinutes,
      ),
    onSuccess: async () => {
      setMinutes(undefined)
      await queryClient.invalidateQueries({ queryKey: ['football-history', organizationId, memberId] })
      await queryClient.invalidateQueries({ queryKey: ['football-session', organizationId, latestSession?.session.id] })
    },
  })

  if (!organizationId || !me.data || !memberId) return null

  return (
    <aside className="fixed bottom-4 right-4 z-50 w-[min(420px,calc(100vw-2rem))] rounded-2xl border border-white/10 bg-slate-950/95 shadow-2xl backdrop-blur">
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        className="flex w-full items-center justify-between gap-3 p-4 text-left"
      >
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-400/15 text-emerald-300">
            <HeartPulse size={20} />
          </div>
          <div>
            <p className="text-sm font-black text-white">Meine Trainingsbereitschaft</p>
            <p className="text-xs text-slate-400">{statusLabels[status]} · max. {maxLoadPercent}% Belastung</p>
          </div>
        </div>
        {open ? <ChevronDown size={18} className="text-slate-400" /> : <ChevronUp size={18} className="text-slate-400" />}
      </button>

      {open && (
        <div className="max-h-[75vh] overflow-y-auto border-t border-white/10 p-4">
          <div className="grid gap-3 sm:grid-cols-2">
            <label className="block">
              <span className="mb-1.5 block text-xs font-bold uppercase tracking-wide text-slate-500">Status</span>
              <select
                className="football-input"
                value={status}
                onChange={(event) => setDraftStatus(event.target.value as FootballAvailabilityStatus)}
              >
                {Object.entries(statusLabels).map(([value, label]) => (
                  <option key={value} value={value}>{label}</option>
                ))}
              </select>
            </label>
            <label className="block">
              <span className="mb-1.5 block text-xs font-bold uppercase tracking-wide text-slate-500">Max. Belastung</span>
              <div className="flex items-center gap-3">
                <input
                  type="range"
                  min="0"
                  max="100"
                  step="5"
                  value={maxLoadPercent}
                  onChange={(event) => setDraftLoad(Number(event.target.value))}
                  className="min-w-0 flex-1"
                />
                <strong className="w-12 text-right text-sm text-white">{maxLoadPercent}%</strong>
              </div>
            </label>
          </div>

          <label className="mt-3 block">
            <span className="mb-1.5 block text-xs font-bold uppercase tracking-wide text-slate-500">Hinweis</span>
            <input
              className="football-input"
              value={note}
              onChange={(event) => setDraftNote(event.target.value)}
              placeholder="z. B. Adduktoren leicht gereizt"
            />
          </label>

          {status !== 'Fit' && (
            <div className="mt-3 flex gap-2 rounded-xl border border-amber-400/20 bg-amber-400/10 p-3 text-xs text-amber-100">
              <ShieldAlert size={16} className="mt-0.5 shrink-0" />
              Dieser Status steht dem Trainerteam in der Trainingsplanung zur Verfügung.
            </div>
          )}

          <button
            type="button"
            onClick={() => save.mutate()}
            disabled={save.isPending}
            className="football-button mt-4"
          >
            <Save size={15} /> {save.isPending ? 'Speichert …' : 'Status speichern'}
          </button>

          {latestSession && (
            <div className="mt-5 border-t border-white/10 pt-4">
              <p className="text-xs font-bold uppercase tracking-wide text-slate-500">Letzte Einheit bewerten</p>
              <p className="mt-1 text-sm font-semibold text-slate-200">{latestSession.session.title}</p>
              <div className="mt-3 grid grid-cols-2 gap-3">
                <label>
                  <span className="mb-1 block text-xs text-slate-500">RPE 1–10</span>
                  <input
                    className="football-input"
                    type="number"
                    min="1"
                    max="10"
                    value={rpe}
                    onChange={(event) => setRpe(Number(event.target.value))}
                  />
                </label>
                <label>
                  <span className="mb-1 block text-xs text-slate-500">Minuten</span>
                  <input
                    className="football-input"
                    type="number"
                    min="0"
                    max={latestSession.session.durationMinutes}
                    value={minutes ?? latestSession.load?.minutesCompleted ?? latestSession.plannedMinutes}
                    onChange={(event) => setMinutes(Number(event.target.value))}
                  />
                </label>
              </div>
              <button
                type="button"
                onClick={() => saveRpe.mutate()}
                disabled={saveRpe.isPending || rpe < 1 || rpe > 10}
                className="football-button-secondary mt-3"
              >
                <Activity size={15} /> {saveRpe.isPending ? 'Speichert …' : 'Belastung speichern'}
              </button>
            </div>
          )}

          <div className="mt-5 border-t border-white/10 pt-4">
            <div className="flex items-center justify-between gap-3">
              <p className="text-xs font-bold uppercase tracking-wide text-slate-500">Letzte Belastungen</p>
              <span className="flex items-center gap-1 text-xs text-slate-400"><Activity size={13} /> Summe {weeklyLoad}</span>
            </div>
            <div className="mt-3 space-y-2">
              {history.data?.map((entry) => (
                <div key={entry.session.id} className="flex items-center justify-between gap-3 rounded-xl bg-white/5 px-3 py-2 text-sm">
                  <div className="min-w-0">
                    <p className="truncate font-semibold text-slate-200">{entry.session.title}</p>
                    <p className="text-xs text-slate-500">{new Date(entry.session.startsAt).toLocaleDateString('de-DE')} · {entry.load?.minutesCompleted ?? entry.plannedMinutes} min</p>
                  </div>
                  <div className="text-right">
                    <p className="font-bold text-emerald-300">{entry.trainingLoad ?? '–'}</p>
                    <p className="text-xs text-slate-500">RPE {entry.load?.rpe ?? '–'}</p>
                  </div>
                </div>
              ))}
              {!history.isPending && !history.data?.length && (
                <p className="text-sm text-slate-500">Noch keine Belastungsdaten vorhanden.</p>
              )}
            </div>
          </div>
        </div>
      )}
    </aside>
  )
}
