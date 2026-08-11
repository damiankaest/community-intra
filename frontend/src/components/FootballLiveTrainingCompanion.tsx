import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Clock3,
  Maximize2,
  Pause,
  Play,
  Square,
  TimerReset,
  X,
} from 'lucide-react'
import { useLocation } from 'react-router-dom'
import { getCurrentUser } from '../api/auth'
import { listFootballProfiles, listFootballSessions } from '../api/football'
import {
  activateFootballLiveTrainingBlock,
  completeFootballLiveTraining,
  completeFootballLiveTrainingBlock,
  getFootballLiveTraining,
  pauseFootballLiveTraining,
  pauseFootballLiveTrainingBlock,
  resetFootballLiveTrainingBlock,
  resumeFootballLiveTraining,
  resumeFootballLiveTrainingBlock,
  startFootballLiveTraining,
  type FootballLiveTrainingState,
} from '../api/footballLive'
import { listMembers } from '../api/members'

const liveFrom = new Date().toISOString()

function organizationIdFromPath(pathname: string) {
  return pathname.match(/^\/football\/([0-9a-f-]{36})(?:\/|$)/i)?.[1]
}

function formatSeconds(value: number) {
  const seconds = Math.max(0, Math.floor(value))
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const rest = seconds % 60
  return hours > 0
    ? `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(rest).padStart(2, '0')}`
    : `${String(minutes).padStart(2, '0')}:${String(rest).padStart(2, '0')}`
}

function totalElapsed(state: FootballLiveTrainingState, nowMs: number) {
  const run = state.run
  if (!run?.startedAt) return 0
  const end = run.completedAt
    ? new Date(run.completedAt).getTime()
    : run.pausedAt
      ? new Date(run.pausedAt).getTime()
      : nowMs
  return Math.max(0, Math.floor((end - new Date(run.startedAt).getTime()) / 1000) - run.accumulatedPausedSeconds)
}

function blockElapsed(state: FootballLiveTrainingState, blockId: string, nowMs: number) {
  const run = state.blockRuns.find((item) => item.trainingBlockId === blockId)
  if (!run) return 0
  const liveSeconds = run.startedAt && !run.pausedAt && !run.completedAt
    ? Math.max(0, Math.floor((nowMs - new Date(run.startedAt).getTime()) / 1000))
    : 0
  return run.accumulatedSeconds + liveSeconds
}

export function FootballLiveTrainingCompanion() {
  const location = useLocation()
  const organizationId = organizationIdFromPath(location.pathname)
  const isTrainingPage = /\/football\/[0-9a-f-]{36}\/training(?:\/|$)/i.test(location.pathname)
  const queryClient = useQueryClient()
  const [expanded, setExpanded] = useState(false)
  const [nowMs, setNowMs] = useState(() => Date.now())

  useEffect(() => {
    const handle = window.setInterval(() => setNowMs(Date.now()), 1000)
    return () => window.clearInterval(handle)
  }, [])

  const me = useQuery({
    queryKey: ['football-current-user'],
    queryFn: getCurrentUser,
    retry: false,
    enabled: Boolean(organizationId && isTrainingPage),
  })
  const members = useQuery({
    queryKey: ['football-members', organizationId],
    queryFn: () => listMembers(organizationId!),
    enabled: Boolean(organizationId && isTrainingPage && me.data),
  })
  const profiles = useQuery({
    queryKey: ['football-profiles', organizationId],
    queryFn: () => listFootballProfiles(organizationId!),
    enabled: Boolean(organizationId && isTrainingPage && me.data),
  })
  const sessions = useQuery({
    queryKey: ['football-sessions', organizationId, 'live-training'],
    queryFn: () => listFootballSessions(organizationId!, liveFrom),
    enabled: Boolean(organizationId && isTrainingPage && me.data),
  })

  const currentMemberId = members.data?.find((member) => member.userId === me.data?.id)?.id
  const isCoach = profiles.data?.some((profile) => profile.memberId === currentMemberId && profile.teamRole === 'Coach') ?? false
  const nextTraining = sessions.data?.find((session) => session.kind === 'Training')

  const live = useQuery({
    queryKey: ['football-live-training', organizationId, nextTraining?.id],
    queryFn: () => getFootballLiveTraining(organizationId!, nextTraining!.id),
    enabled: Boolean(organizationId && isTrainingPage && nextTraining?.id),
    refetchInterval: expanded ? 15_000 : 30_000,
  })

  const applyState = (state: FootballLiveTrainingState) => {
    queryClient.setQueryData(['football-live-training', organizationId, nextTraining?.id], state)
  }

  const action = useMutation({
    mutationFn: (fn: () => Promise<FootballLiveTrainingState>) => fn(),
    onSuccess: applyState,
  })

  const state = live.data
  const activeIndex = state?.blocks.findIndex((block) => block.id === state.run?.activeTrainingBlockId) ?? -1
  const activeBlock = activeIndex >= 0 ? state?.blocks[activeIndex] : state?.blocks[0]
  const activeRun = activeBlock ? state?.blockRuns.find((item) => item.trainingBlockId === activeBlock.id) : undefined
  const responsible = activeBlock?.responsibleMemberId
    ? members.data?.find((member) => member.id === activeBlock.responsibleMemberId)
    : undefined

  const completedCount = useMemo(
    () => state?.blockRuns.filter((item) => item.isCompleted).length ?? 0,
    [state?.blockRuns],
  )

  if (!organizationId || !isTrainingPage || !nextTraining || !state) return null

  const started = Boolean(state.run?.startedAt)
  const completed = state.run?.status === 'Completed'
  const running = state.run?.status === 'Running'
  const blockIsRunning = Boolean(activeRun?.startedAt && !activeRun.pausedAt && !activeRun.completedAt)

  const activateIndex = (index: number) => {
    const block = state.blocks[index]
    if (!block) return
    action.mutate(() => activateFootballLiveTrainingBlock(organizationId, nextTraining.id, block.id))
  }

  if (!expanded) {
    return (
      <aside className="fixed bottom-4 right-4 z-50 w-[min(390px,calc(100vw-2rem))] rounded-2xl border border-white/10 bg-slate-950/95 p-4 shadow-2xl backdrop-blur">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-emerald-400/15 text-emerald-300">
            <Clock3 size={20} />
          </div>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-black text-white">Live Training</p>
            <p className="truncate text-xs text-slate-400">{nextTraining.title}</p>
          </div>
          {started && !completed && (
            <span className="font-mono text-lg font-black text-white">{formatSeconds(totalElapsed(state, nowMs))}</span>
          )}
        </div>
        <div className="mt-3 flex gap-2">
          {!started && isCoach ? (
            <button type="button" className="football-button flex-1" disabled={action.isPending || state.blocks.length === 0} onClick={() => action.mutate(() => startFootballLiveTraining(organizationId, nextTraining.id))}>
              <Play size={15} /> Training starten
            </button>
          ) : (
            <button type="button" className="football-button flex-1" onClick={() => setExpanded(true)}>
              <Maximize2 size={15} /> {completed ? 'Training ansehen' : 'Live öffnen'}
            </button>
          )}
        </div>
      </aside>
    )
  }

  return (
    <div className="fixed inset-0 z-[100] overflow-y-auto bg-slate-950 text-white">
      <div className="mx-auto flex min-h-screen w-full max-w-5xl flex-col p-4 sm:p-6">
        <header className="flex items-start justify-between gap-4 border-b border-white/10 pb-4">
          <div>
            <p className="text-xs font-black uppercase tracking-[0.18em] text-emerald-300">Live Training Companion</p>
            <h1 className="mt-1 text-xl font-black sm:text-2xl">{nextTraining.title}</h1>
            <p className="mt-1 text-sm text-slate-400">{completedCount} / {state.blocks.length} Blöcke abgeschlossen</p>
          </div>
          <button type="button" className="rounded-xl border border-white/10 p-2 text-slate-300" onClick={() => setExpanded(false)}>
            <X size={20} />
          </button>
        </header>

        <section className="grid gap-3 py-4 sm:grid-cols-2">
          <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-4 text-center">
            <p className="text-xs font-bold uppercase tracking-wider text-slate-500">Training läuft</p>
            <p className="mt-2 font-mono text-4xl font-black sm:text-5xl">{formatSeconds(totalElapsed(state, nowMs))}</p>
          </div>
          <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-4 text-center">
            <p className="text-xs font-bold uppercase tracking-wider text-slate-500">Aktueller Block</p>
            <p className="mt-2 font-mono text-4xl font-black sm:text-5xl">{activeBlock ? formatSeconds(blockElapsed(state, activeBlock.id, nowMs)) : '00:00'}</p>
            {activeBlock && <p className="mt-1 text-xs text-slate-500">geplant {activeBlock.durationMinutes}:00</p>}
          </div>
        </section>

        {activeBlock ? (
          <main className="flex-1 rounded-3xl border border-white/10 bg-white/[0.03] p-5 sm:p-7">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <p className="text-sm font-bold text-emerald-300">Block {Math.max(1, activeIndex + 1)} von {state.blocks.length}</p>
                <h2 className="mt-1 text-2xl font-black sm:text-4xl">{activeBlock.title}</h2>
                {responsible && <p className="mt-2 text-sm font-bold text-violet-200">Verantwortlich: {responsible.displayName}</p>}
              </div>
              {activeRun?.isCompleted && (
                <span className="flex items-center gap-1 rounded-full bg-emerald-400/15 px-3 py-1.5 text-xs font-bold text-emerald-200">
                  <CheckCircle2 size={15} /> Erledigt
                </span>
              )}
            </div>

            {activeBlock.description && <p className="mt-5 max-w-3xl text-base leading-7 text-slate-300">{activeBlock.description}</p>}
            {activeBlock.coachingPoints && (
              <div className="mt-5 rounded-2xl border border-violet-400/20 bg-violet-400/10 p-4">
                <p className="text-xs font-black uppercase tracking-wider text-violet-300">Coaching Points</p>
                <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-violet-50">{activeBlock.coachingPoints}</p>
              </div>
            )}

            <div className="mt-6 grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
              {isCoach && (
                <>
                  <button type="button" className="football-button" disabled={action.isPending} onClick={() => action.mutate(() => blockIsRunning ? pauseFootballLiveTrainingBlock(organizationId, nextTraining.id, activeBlock.id) : resumeFootballLiveTrainingBlock(organizationId, nextTraining.id, activeBlock.id))}>
                    {blockIsRunning ? <Pause size={16} /> : <Play size={16} />} {blockIsRunning ? 'Block pausieren' : 'Block weiter'}
                  </button>
                  <button type="button" className="football-button" disabled={action.isPending} onClick={() => action.mutate(() => resetFootballLiveTrainingBlock(organizationId, nextTraining.id, activeBlock.id))}>
                    <TimerReset size={16} /> Block resetten
                  </button>
                  <button type="button" className="football-button" disabled={action.isPending} onClick={() => action.mutate(() => completeFootballLiveTrainingBlock(organizationId, nextTraining.id, activeBlock.id))}>
                    <CheckCircle2 size={16} /> Block erledigt
                  </button>
                </>
              )}
              <button type="button" className="football-button" disabled={activeIndex <= 0 || action.isPending} onClick={() => activateIndex(activeIndex - 1)}>
                <ChevronLeft size={16} /> Zurück
              </button>
            </div>

            <div className="mt-3 grid gap-2 sm:grid-cols-2">
              <button type="button" className="football-button" disabled={activeIndex >= state.blocks.length - 1 || action.isPending} onClick={() => activateIndex(activeIndex + 1)}>
                Nächster Block <ChevronRight size={16} />
              </button>
              {isCoach && !completed && (
                <button type="button" className="football-button" disabled={action.isPending} onClick={() => action.mutate(() => running ? pauseFootballLiveTraining(organizationId, nextTraining.id) : resumeFootballLiveTraining(organizationId, nextTraining.id))}>
                  {running ? <Pause size={16} /> : <Play size={16} />} {running ? 'Training pausieren' : 'Training fortsetzen'}
                </button>
              )}
            </div>
          </main>
        ) : (
          <div className="flex flex-1 items-center justify-center text-slate-500">Keine Trainingsblöcke vorhanden.</div>
        )}

        {isCoach && started && !completed && (
          <footer className="mt-4 border-t border-white/10 pt-4">
            <button type="button" disabled={action.isPending} className="flex w-full items-center justify-center gap-2 rounded-xl border border-rose-400/30 bg-rose-400/10 px-4 py-3 text-sm font-black text-rose-200" onClick={() => action.mutate(() => completeFootballLiveTraining(organizationId, nextTraining.id))}>
              <Square size={16} /> Training beenden
            </button>
          </footer>
        )}

        {action.isError && <p className="mt-3 text-center text-sm text-rose-300">{action.error.message}</p>}
      </div>
    </div>
  )
}
