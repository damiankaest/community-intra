import { useMemo, useState } from 'react'
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
  const [selectedBlockId, setSelectedBlockId] = useState<string>()
  const [draft, setDraft] = useState<FootballTrainingCoachTaskInput[]>()
  const [setupAndFlow, setSetupAndFlow] = useState<string>()
  const [coachingPoints, setCoachingPoints] = useState<string>()

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

  const blockId = selectedBlockId ?? session.data?.blocks[0]?.id ?? ''
  const activeBlock = session.data?.blocks.find((block) => block.id === blockId) ?? session.data?.blocks[0]
  const persistedDraft = useMemo(
    () => (tasks.data ?? [])
      .filter((task) => task.trainingBlockId === blockId)
      .map((task) => ({ memberId: task.memberId, role: task.role, task: task.task })),
    [blockId, tasks.data],
  )
  const effectiveDraft = draft ?? persistedDraft
  const effectiveSetupAndFlow = setupAndFlow ?? activeBlock?.description ?? ''
  const effectiveCoachingPoints = coachingPoints ?? activeBlock?.coachingPoints ?? ''

  const selectBlock = (nextBlockId: string) => {
    setSelectedBlockId(nextBlockId)
    setDraft(undefined)
    setSetupAndFlow(undefined)
    setCoachingPoints(undefined)
  }

  const saveTasks = useMutation({
    mutationFn: () => replaceFootballTrainingCoachTasks(organizationId!, nextTraining!.id, blockId, effectiveDraft),
    onSuccess: async () => {
      setDraft(undefined)
      await queryClient.invalidateQueries({ queryKey: ['football-coach-tasks', organizationId, nextTraining?.id] })
    },
  })

  const saveBriefing = useMutation({
    mutationFn: () => updateFootballTrainingBriefing(organizationId!, nextTraining!.id, blockId, effectiveSetupAndFlow, effectiveCoachingPoints),
    onSuccess: async () => {
      setSetupAndFlow(undefined)
      setCoachingPoints(undefined)
      await queryClient.invalidateQueries({ queryKey: ['football-session', organizationId, nextTraining?.id] })
      await queryClient.invalidateQueries({ queryKey: ['football-live-training', organizationId, nextTraining?.id] })
    },
  })

  if (!organizationId || !isTrainingPage || !isCoach || !nextTraining || !session.data?.blocks.length || !activeBlock) return null

  const addTask = () => setDraft((current) => [...(current ?? persistedDraft), { memberId: eligibleMembers[0]?.id ?? '', role: 'Übung leiten', task: '' }])
  const updateTask = (index: number, patch: Partial<FootballTrainingCoachTaskInput>) => setDraft((current) => (current ?? persistedDraft).map((item, itemIndex) => itemIndex === index ? { ...item, ...patch } : item))
  const removeTask = (index: number) => setDraft((current) => (current ?? persistedDraft).filter((_, itemIndex) => itemIndex !== index))

  return (
    <aside className="fixed inset-x-0 bottom-[5.75rem] z-50 mx-2 rounded-3xl border border-white/10 bg-slate-950/98 shadow-2xl backdrop-blur sm:inset-x-auto sm:bottom-4 sm:left-4 sm:mx-0 sm:w-[min(560px,calc(100vw-2rem))] sm:rounded-2xl">
      <button type="button" onClick={() => setOpen((value) => !value)} className="flex min-h-14 w-full items-center gap-3 px-4 py-3 text-left active:bg-white/[0.04]">
        <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-sky-400/15 text-sky-300"><Users size={21} /></div>
        <div className="min-w-0 flex-1"><p className="text-sm font-black text-white">Trainer-Regie</p><p className="truncate text-xs text-slate-400">Aufbau, Rollen und Aufgaben</p></div>
        <span className="text-xs font-bold text-slate-400">{open ? 'Schließen' : 'Öffnen'}</span>
      </button>

      {open && (
        <div className="max-h-[78dvh] overflow-y-auto overscroll-contain border-t border-white/10 px-4 pt-4 pb-[max(1rem,env(safe-area-inset-bottom))] sm:max-h-[75vh] sm:pb-4">
          <label className="text-xs font-bold uppercase tracking-wider text-slate-500">Trainingsblock</label>
          <select value={blockId} onChange={(event) => selectBlock(event.target.value)} className="mt-2 min-h-12 w-full rounded-xl border border-white/10 bg-slate-900 px-3 py-2 text-base text-white sm:text-sm">
            {session.data.blocks.map((block, index) => <option key={block.id} value={block.id}>{index + 1}. {block.title}</option>)}
          </select>

          <div className="mt-4 rounded-2xl border border-emerald-400/20 bg-emerald-400/[0.07] p-4">
            <div className="flex items-center gap-2 text-emerald-300"><ClipboardList size={16} /><p className="text-xs font-black uppercase tracking-wider">Aufbau & Ablauf</p></div>
            <p className="mt-2 text-xs leading-5 text-slate-400">So konkret schreiben, dass ein anderer Trainer die Übung ohne Rückfrage aufbauen und starten kann.</p>
            <textarea value={effectiveSetupAndFlow} onChange={(event) => setSetupAndFlow(event.target.value)} placeholder={setupPlaceholder} rows={9} className="mt-3 w-full resize-y rounded-xl border border-white/10 bg-slate-900 px-3 py-3 text-base leading-6 text-white sm:text-sm" />
            <p className="mt-4 text-xs font-black uppercase tracking-wider text-violet-300">Coaching Points</p>
            <textarea value={effectiveCoachingPoints} onChange={(event) => setCoachingPoints(event.target.value)} placeholder="3–5 klare Beobachtungspunkte, z. B. Körperstellung offen, erster Kontakt nach vorne, nach Ballverlust sofort Druck auf den Ball …" rows={4} className="mt-2 w-full resize-y rounded-xl border border-white/10 bg-slate-900 px-3 py-3 text-base leading-6 text-white sm:text-sm" />
            <button type="button" onClick={() => saveBriefing.mutate()} disabled={saveBriefing.isPending} className="football-button mt-3 min-h-12 w-full"><Save size={16} /> {saveBriefing.isPending ? 'Speichert …' : 'Aufbau speichern'}</button>
            {saveBriefing.isError && <p className="mt-2 text-sm text-rose-300">{saveBriefing.error.message}</p>}
          </div>

          <div className="mt-5 flex items-center justify-between gap-3">
            <div><p className="text-sm font-black text-white">Trainer-Aufgaben</p><p className="text-xs text-slate-500">Mehrere Trainer können parallel unterschiedliche Rollen übernehmen.</p></div>
            <button type="button" onClick={addTask} className="football-button min-h-11 shrink-0"><Plus size={16} /> Aufgabe</button>
          </div>

          <div className="mt-3 space-y-3">
            {effectiveDraft.map((item, index) => (
              <div key={`${index}-${item.memberId}`} className="rounded-2xl border border-white/10 bg-white/[0.03] p-3">
                <div className="flex gap-2">
                  <select value={item.memberId} onChange={(event) => updateTask(index, { memberId: event.target.value })} className="min-h-12 min-w-0 flex-1 rounded-xl border border-white/10 bg-slate-900 px-3 py-2 text-base text-white sm:text-sm">
                    <option value="">Trainer wählen</option>
                    {eligibleMembers.map((member) => <option key={member.id} value={member.id}>{member.displayName}</option>)}
                  </select>
                  <button type="button" aria-label="Aufgabe löschen" onClick={() => removeTask(index)} className="flex min-h-12 min-w-12 items-center justify-center rounded-xl border border-rose-400/20 text-rose-300 active:bg-rose-400/10"><Trash2 size={18} /></button>
                </div>
                <input list="football-coach-task-roles" value={item.role} onChange={(event) => updateTask(index, { role: event.target.value })} placeholder="Rolle, z. B. Gruppe coachen" className="mt-2 min-h-12 w-full rounded-xl border border-white/10 bg-slate-900 px-3 py-2 text-base text-white sm:text-sm" />
                <textarea value={item.task} onChange={(event) => updateTask(index, { task: event.target.value })} placeholder="Konkrete Aufgabe: rechte Gruppe betreuen, Abstände korrigieren und nach Ballverlust sofort Gegenpressing coachen …" rows={3} className="mt-2 w-full resize-none rounded-xl border border-white/10 bg-slate-900 px-3 py-3 text-base leading-6 text-white sm:text-sm" />
              </div>
            ))}
          </div>

          <datalist id="football-coach-task-roles">{roleSuggestions.map((role) => <option key={role} value={role} />)}</datalist>
          <button type="button" disabled={saveTasks.isPending || effectiveDraft.some((item) => !item.memberId || !item.role.trim() || !item.task.trim())} onClick={() => saveTasks.mutate()} className="football-button mt-3 min-h-12 w-full"><Save size={16} /> {saveTasks.isPending ? 'Speichert …' : 'Trainer-Aufgaben speichern'}</button>
          {saveTasks.isError && <p className="mt-2 text-sm text-rose-300">{saveTasks.error.message}</p>}
        </div>
      )}
    </aside>
  )
}
