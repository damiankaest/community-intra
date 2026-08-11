import { useQuery } from '@tanstack/react-query'
import { ClipboardList, Target, Users } from 'lucide-react'
import { useLocation } from 'react-router-dom'
import { listFootballSessions } from '../api/football'
import { getFootballLiveTraining, listFootballTrainingCoachTasks } from '../api/footballLive'
import { listMembers } from '../api/members'

const briefingFrom = new Date().toISOString()

function organizationIdFromPath(pathname: string) {
  return pathname.match(/^\/football\/([0-9a-f-]{36})(?:\/|$)/i)?.[1]
}

export function FootballLiveTrainingBriefing() {
  const location = useLocation()
  const organizationId = organizationIdFromPath(location.pathname)
  const isTrainingPage = /\/football\/[0-9a-f-]{36}\/training(?:\/|$)/i.test(location.pathname)
  const sessions = useQuery({
    queryKey: ['football-sessions', organizationId, 'live-briefing'],
    queryFn: () => listFootballSessions(organizationId!, briefingFrom),
    enabled: Boolean(organizationId && isTrainingPage),
  })
  const nextTraining = sessions.data?.find((session) => session.kind === 'Training')
  const live = useQuery({
    queryKey: ['football-live-training', organizationId, nextTraining?.id],
    queryFn: () => getFootballLiveTraining(organizationId!, nextTraining!.id),
    enabled: Boolean(organizationId && nextTraining?.id),
    refetchInterval: 15_000,
  })
  const tasks = useQuery({
    queryKey: ['football-coach-tasks', organizationId, nextTraining?.id],
    queryFn: () => listFootballTrainingCoachTasks(organizationId!, nextTraining!.id),
    enabled: Boolean(organizationId && nextTraining?.id),
    refetchInterval: 15_000,
  })
  const members = useQuery({
    queryKey: ['football-members', organizationId],
    queryFn: () => listMembers(organizationId!),
    enabled: Boolean(organizationId && isTrainingPage),
  })

  const activeBlockId = live.data?.run?.activeTrainingBlockId
  const activeBlock = live.data?.blocks.find((block) => block.id === activeBlockId)
  const activeTasks = (tasks.data ?? []).filter((task) => task.trainingBlockId === activeBlockId)

  if (!organizationId || !isTrainingPage || !live.data?.run?.startedAt || !activeBlock) return null

  return (
    <aside className="fixed right-4 top-20 z-[120] hidden w-[min(420px,calc(100vw-2rem))] max-h-[calc(100vh-6rem)] overflow-y-auto rounded-2xl border border-white/10 bg-slate-950/95 p-4 shadow-2xl backdrop-blur lg:block">
      <div className="flex items-center gap-2 text-emerald-300">
        <ClipboardList size={17} />
        <p className="text-xs font-black uppercase tracking-[0.16em]">Aufbau & Ablauf</p>
      </div>
      <p className="mt-2 text-base font-black text-white">{activeBlock.title}</p>
      <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-slate-200">
        {activeBlock.description || 'Für diesen Block ist noch kein genauer Aufbau hinterlegt.'}
      </p>

      {activeBlock.coachingPoints && (
        <div className="mt-4 rounded-xl border border-violet-400/20 bg-violet-400/10 p-3">
          <div className="flex items-center gap-2 text-violet-300"><Target size={15} /><p className="text-xs font-black uppercase tracking-wider">Coaching Points</p></div>
          <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-violet-50">{activeBlock.coachingPoints}</p>
        </div>
      )}

      <div className="mt-4">
        <div className="flex items-center gap-2 text-sky-300"><Users size={15} /><p className="text-xs font-black uppercase tracking-wider">Trainer-Regie</p></div>
        {activeTasks.length === 0 ? (
          <p className="mt-2 text-sm text-slate-500">Keine zusätzlichen Trainer-Aufgaben für diesen Block verteilt.</p>
        ) : (
          <div className="mt-2 space-y-2">
            {activeTasks.map((task) => {
              const member = members.data?.find((item) => item.id === task.memberId)
              return (
                <div key={task.id} className="rounded-xl border border-white/10 bg-white/[0.04] p-3">
                  <p className="text-sm font-black text-white">{member?.displayName ?? 'Trainer'} · {task.role}</p>
                  <p className="mt-1 text-sm leading-5 text-slate-300">{task.task}</p>
                </div>
              )
            })}
          </div>
        )}
      </div>
    </aside>
  )
}
