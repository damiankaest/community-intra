import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ClipboardList, Plus, Save, Trash2, Users } from 'lucide-react'
import { useLocation } from 'react-router-dom'
import { getCurrentUser } from '../api/auth'
import { getFootballSession, listFootballProfiles, listFootballSessions } from '../api/football'
import {
  listFootballTrainingCoachTasks,
  replaceFootballTrainingCoachTasks,
  type FootballTrainingCoachTaskInput,
} from '../api/footballLive'
import { listMembers } from '../api/members'

const operationsFrom = new Date().toISOString()

function organizationIdFromPath(pathname: string) {
  return pathname.match(/^\/football\/([0-9a-f-]{36})(?:\/|$)/i)?.[1]
}

const roleSuggestions = ['Übung leiten', 'Aufbau & Material', 'Gruppe coachen', 'Torhüter', 'Beobachtung']

export function FootballTrainingOperationsDock() {
  const location = useLocation()
  const organizationId = organizationIdFromPath(location.pathname)
  const isTrainingPage = /\/football\/[0-9a-f-]{36}\/training(?:\/|$)/i.test(location.pathname)
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const [blockId, setBlockId] = useState('')
  const [draft, setDraft] = useState<FootballTrainingCoachTaskInput[]>([])

  const me = useQuery({ queryKey: ['football-current-user'], queryFn: getCurrentUser, retry: false, enabled: Boolean(organizationId && isTrainingPage) })
  const members = useQuery({ queryKey: ['football-members', organizationId], queryFn: () => listMembers(organizationId!), enabled: Boolean(organizationId && me.data) })
  const profiles = useQuery({ queryKey: ['football-profiles', organizationId], queryFn: () => listFootballProfiles(organizationId!), enabled: Boolean(organizationId && me.data) })
  const sessions = useQuery({ queryKey: ['football-sessions', organizationId, 'operations'], queryFn: () => listFootballSessions(organizationId!, operationsFrom), enabled: Boolean(organizationId && me.data) })
  const nextTraining = sessions.data?.find((session) => session.kind === 'Training')
  const session = useQuery({ queryKey: ['football-session', organizationId, nextTraining?.id], queryFn: () => getFootballSession(organizationId!, nextTraining!.id), enabled: Boolean(organizationId && nextTraining?.id) })
  const tasks = useQuery({ queryKey: ['football-coach-tasks', organizationId, nextTraining?.id], queryFn: () => listFootballTrainingCoachTasks(organizationId!, nextTraining!.id), enabled: Boolean(organizationId && nextTraining?.id) })

  const currentMemberId = members.data?.find((member) => member.userId === me.data?.id)?.id
  const isCoach = profiles.data?.some((profile) => profile.memberId === currentMemberId && profile.teamRole === 'Coach') ?? false
  const eligibleMemberIds = useMemo(() => new Set(profiles.data?.filter((profile) => profile.teamRole === 'Coach' || profile.teamRole === 'Staff').map((profile) => profile.memberId) ?? []), [profiles.data])
  const eligibleMembers = members.data?.filter((member) => eligibleMemberIds.has(member.id)) ?? []

  useEffect(() => {
    if (!blockId && session.data?.blocks[0]) setBlockId(session.data.blocks[0].id)
  }, [blockId, session.data?.blocks])

  useEffect(() => {
    if (!blockId) return
    setDraft((tasks.data ?? []).filter((task) => task.trainingBlockId === blockId).map((task) => ({ memberId: task.memberId, role: task.role, task: task.task })))
  }, [blockId, tasks.data])

  const save = useMutation({
    mutationFn: () => replaceFootballTrainingCoachTasks(organizationId!, nextTraining!.id, blockId, draft),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['football-coach-tasks', organizationId, nextTraining?.id] })
    },
  })

  if (!organizationId || !isTrainingPage || !isCoach || !nextTraining || !session.data?.blocks.length) return null
  const activeBlock = session.data.blocks.find((block) => block.id === blockId) ?? session.data.blocks[0]

  const addTask = () => setDraft((current) => [...current, { memberId: eligibleMembers[0]?.id ?? '', role: 'Übung leiten', task: '' }])
  const updateTask = (index: number, patch: Partial<FootballTrainingCoachTaskInput>) => setDraft((current) => current.map((item, itemIndex) => itemIndex === index ? { ...item, ...patch } : item))

  return (
    <aside className="fixed bottom-4 left-4 z-50 w-[min(520px,calc(100vw-2rem))] rounded-2xl border border-white/10 bg-slate-950/95 shadow-2xl backdrop-blur">
      <button type="button" onClick={() => setOpen((value) => !value)} className="flex w-full items-center gap-3 p-4 text-left">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-sky-400/15 text-sky-300"><Users size={20} /></div>
        <div className="min-w-0 flex-1"><p className="text-sm font-black text-white">Trainer-Regie</p><p className="truncate text-xs text-slate-400">Aufbau, Rollen und Aufgaben pro Trainingsblock</p></div>
        <span className="text-xs font-bold text-slate-500">{open ? 'Schließen' : 'Öffnen'}</span>
      </button>
      {open && (
        <div className="max-h-[72vh] overflow-y-auto border-t border-white/10 p-4">
          <label className="text-xs font-bold uppercase tracking-wider text-slate-500">Trainingsblock</label>
          <select value={blockId} onChange={(event) => setBlockId(event.target.value)} className="mt-2 w-full rounded-xl border border-white/10 bg-slate-900 px-3 py-2 text-sm text-white">
            {session.data.blocks.map((block, index) => <option key={block.id} value={block.id}>{index + 1}. {block.title}</option>)}
          </select>

          <div className="mt-4 rounded-2xl border border-emerald-400/20 bg-emerald-400/[0.07] p-4">
            <div className="flex items-center gap-2 text-emerald-300"><ClipboardList size={16} /><p className="text-xs font-black uppercase tracking-wider">Aufbau & Ablauf</p></div>
            <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-200">{activeBlock.description || 'Für diesen Block ist noch kein Aufbau beschrieben. Ergänze ihn im Trainingsplan bzw. lass den KI-Plan neu erzeugen.'}</p>
            {activeBlock.coachingPoints && <><p className="mt-4 text-xs font-black uppercase tracking-wider text-violet-300">Coaching Points</p><p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-300">{activeBlock.coachingPoints}</p></>}
          </div>

          <div className="mt-4 space-y-3">
            {draft.map((item, index) => (
              <div key={`${index}-${item.memberId}`} className="rounded-2xl border border-white/10 bg-white/[0.03] p-3">
                <div className="flex gap-2">
                  <select value={item.memberId} onChange={(event) => updateTask(index, { memberId: event.target.value })} className="min-w-0 flex-1 rounded-xl border border-white/10 bg-slate-900 px-3 py-2 text-sm text-white">
                    <option value="">Trainer wählen</option>
                    {eligibleMembers.map((member) => <option key={member.id} value={member.id}>{member.displayName}</option>)}
                  </select>
                  <button type="button" onClick={() => setDraft((current) => current.filter((_, itemIndex) => itemIndex !== index))} className="rounded-xl border border-rose-400/20 p-2 text-rose-300"><Trash2 size={16} /></button>
                </div>
                <input list="football-coach-task-roles" value={item.role} onChange={(event) => updateTask(index, { role: event.target.value })} placeholder="Rolle, z. B. Gruppe coachen" className="mt-2 w-full rounded-xl border border-white/10 bg-slate-900 px-3 py-2 text-sm text-white" />
                <textarea value={item.task} onChange={(event) => updateTask(index, { task: event.target.value })} placeholder="Konkrete Aufgabe: rechte Gruppe betreuen, Abstände korrigieren und nach Ballverlust sofort Gegenpressing coachen …" rows={2} className="mt-2 w-full resize-none rounded-xl border border-white/10 bg-slate-900 px-3 py-2 text-sm text-white" />
              </div>
            ))}
          </div>
          <datalist id="football-coach-task-roles">{roleSuggestions.map((role) => <option key={role} value={role} />)}</datalist>
          <div className="mt-3 flex gap-2">
            <button type="button" onClick={addTask} className="football-button flex-1"><Plus size={15} /> Aufgabe</button>
            <button type="button" disabled={save.isPending || draft.some((item) => !item.memberId || !item.role.trim() || !item.task.trim())} onClick={() => save.mutate()} className="football-button flex-1"><Save size={15} /> {save.isPending ? 'Speichert …' : 'Speichern'}</button>
          </div>
          {save.isError && <p className="mt-2 text-sm text-rose-300">{save.error.message}</p>}
        </div>
      )}
    </aside>
  )
}
