import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ClipboardList, Plus, Save, Trash2, Users } from 'lucide-react'
import { useLocation } from 'react-router-dom'
import { getCurrentUser } from '../api/auth'
import { getFootballSession, listFootballProfiles, listFootballSessions } from '../api/football'
import {
  listFootballTrainingCoachTasks,
  replaceFootballTrainingCoachTasks,
  updateFootballTrainingBriefing,
  type FootballTrainingCoachTaskInput,
} from '../api/footballLive'
import { listMembers } from '../api/members'

const operationsFrom = new Date().toISOString()

function organizationIdFromPath(pathname: string) {
  return pathname.match(/^\/football\/([0-9a-f-]{36})(?:\/|$)/i)?.[1]
}

const roleSuggestions = ['Übung leiten', 'Aufbau & Material', 'Gruppe coachen', 'Torhüter', 'Beobachtung']
const setupPlaceholder = `Material: z. B. 12 Hütchen, 8 Bälle, 2 Minitore\nFeld: z. B. 25 x 20 m\nGruppen: z. B. 2 Teams à 5 + 2 Joker\nAufbau: Tore/Hütchen positionieren, Startpositionen festlegen\nAblauf: Wer startet wo? Was ist das Ziel? Wann wird gewechselt?\nVariation: Wie wird die Übung leichter oder schwerer?`

export function FootballTrainingOperationsDock() {
  const location = useLocation()
  const organizationId = organizationIdFromPath(location.pathname)
  const isTrainingPage = /\/football\/[0-9a-f-]{36}\/training(?:\/|$)/i.test(location.pathname)
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const [blockId, setBlockId] = useState('')
  const [draft, setDraft] = useState<FootballTrainingCoachTaskInput[]>([])
  const [setupAndFlow, setSetupAndFlow] = useState('')
  const [coachingPoints, setCoachingPoints] = useState('')

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
  const activeBlock = session.data?.blocks.find((block) => block.id === blockId) ?? session.data?.blocks[0]

  useEffect(() => {
    if (!blockId && session.data?.blocks[0]) setBlockId(session.data.blocks[0].id)
  }, [blockId, session.data?.blocks])

  useEffect(() => {
    if (!blockId) return
    setDraft((tasks.data ?? []).filter((task) => task.trainingBlockId === blockId).map((task) => ({ memberId: task.memberId, role: task.role, task: task.task })))
  }, [blockId, tasks.data])

  useEffect(() => {
    setSetupAndFlow(activeBlock?.description ?? '')
    setCoachingPoints(activeBlock?.coachingPoints ?? '')
  }, [activeBlock?.id, activeBlock?.description, activeBlock?.coachingPoints])

  const saveTasks = useMutation({
    mutationFn: () => replaceFootballTrainingCoachTasks(organizationId!, nextTraining!.id, blockId, draft),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['football-coach-tasks', organizationId, nextTraining?.id] }),
  })

  const saveBriefing = useMutation({
    mutationFn: () => updateFootballTrainingBriefing(organizationId!, nextTraining!.id, blockId, setupAndFlow, coachingPoints),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['football-session', organizationId, nextTraining?.id] })
      await queryClient.invalidateQueries({ queryKey: ['football-live-training', organizationId, nextTraining?.id] })
    },
  })

  if (!organizationId || !isTrainingPage || !isCoach || !nextTraining || !session.data?.blocks.length || !activeBlock) return null

  const addTask = () => setDraft((current) => [...current, { memberId: eligibleMembers[0]?.id ?? '', role: 'Übung leiten', task: '' }])
  const updateTask = (index: number, patch: Partial<FootballTrainingCoachTaskInput>) => setDraft((current) => current.map((item, itemIndex) => itemIndex === index ? { ...item, ...patch } : item))

  return (
    <aside className="fixed bottom-4 left-4 z-50 w-[min(560px,calc(100vw-2rem))] rounded-2xl border border-white/10 bg-slate-950/95 shadow-2xl backdrop-blur">
      <button type="button" onClick={() => setOpen((value) => !value)} className="flex w-full items-center gap-3 p-4 text-left">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-sky-400/15 text-sky-300"><Users size={20} /></div>
        <div className="min-w-0 flex-1"><p className="text-sm font-black text-white">Trainer-Regie</p><p className="truncate text-xs text-slate-400">Aufbau, Rollen und Aufgaben pro Trainingsblock</p></div>
        <span className="text-xs font-bold text-slate-500">{open ? 'Schließen' : 'Öffnen'}</span>
      </button>
      {open && (
        <div className="max-h-[75vh] overflow-y-auto border-t border-white/10 p-4">
          <label className="text-xs font-bold uppercase tracking-wider text-slate-500">Trainingsblock</label>
          <select value={blockId} onChange={(event) => setBlockId(event.target.value)} className="mt-2 w-full rounded-xl border border-white/10 bg-slate-900 px-3 py-2 text-sm text-white">
            {session.data.blocks.map((block, index) => <option key={block.id} value={block.id}>{index + 1}. {block.title}</option>)}
          </select>

          <div className="mt-4 rounded-2xl border border-emerald-400/20 bg-emerald-400/[0.07] p-4">
            <div className="flex items-center gap-2 text-emerald-300"><ClipboardList size={16} /><p className="text-xs font-black uppercase tracking-wider">Aufbau & Ablauf</p></div>
            <p className="mt-2 text-xs leading-5 text-slate-400">So konkret schreiben, dass ein anderer Trainer die Übung ohne Rückfrage aufbauen und starten kann.</p>
            <textarea value={setupAndFlow} onChange={(event) => setSetupAndFlow(event.target.value)} placeholder={setupPlaceholder} rows={9} className="mt-3 w-full resize-y rounded-xl border border-white/10 bg-slate-900 px-3 py-2 text-sm leading-6 text-white" />
            <p className="mt-4 text-xs font-black uppercase tracking-wider text-violet-300">Coaching Points</p>
            <textarea value={coachingPoints} onChange={(event) => setCoachingPoints(event.target.value)} placeholder="3–5 klare Beobachtungspunkte, z. B. Körperstellung offen, erster Kontakt nach vorne, nach Ballverlust sofort Druck auf den Ball …" rows={4} className="mt-2 w-full resize-y rounded-xl border border-white/10 bg-slate-900 px-3 py-2 text-sm leading-6 text-white" />
            <button type="button" onClick={() => saveBriefing.mutate()} disabled={saveBriefing.isPending} className="football-button mt-3 w-full"><Save size={15} /> {saveBriefing.isPending ? 'Speichert …' : 'Aufbau speichern'}</button>
            {saveBriefing.isError && <p className="mt-2 text-sm text-rose-300">{saveBriefing.error.message}</p>}
          </div>

          <div className="mt-5 flex items-center justify-between gap-3">
            <div><p className="text-sm font-black text-white">Trainer-Aufgaben</p><p className="text-xs text-slate-500">Mehrere Trainer können parallel unterschiedliche Rollen übernehmen.</p></div>
            <button type="button" onClick={addTask} className="football-button shrink-0"><Plus size={15} /> Aufgabe</button>
          </div>
          <div className="mt-3 space-y-3">
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
          <button type="button" disabled={saveTasks.isPending || draft.some((item) => !item.memberId || !item.role.trim() || !item.task.trim())} onClick={() => saveTasks.mutate()} className="football-button mt-3 w-full"><Save size={15} /> {saveTasks.isPending ? 'Speichert …' : 'Trainer-Aufgaben speichern'}</button>
          {saveTasks.isError && <p className="mt-2 text-sm text-rose-300">{saveTasks.error.message}</p>}
        </div>
      )}
    </aside>
  )
}
