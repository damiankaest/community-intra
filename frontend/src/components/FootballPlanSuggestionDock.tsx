import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, BrainCircuit, ChevronDown, ChevronUp, Save, Sparkles } from 'lucide-react'
import { useLocation } from 'react-router-dom'
import { getCurrentUser } from '../api/auth'
import {
  listFootballProfiles,
  listFootballSessions,
  replaceFootballTrainingBlocks,
  suggestFootballTrainingPlan,
  type FootballTrainingPlanSuggestion,
} from '../api/football'
import { listMembers } from '../api/members'

const planningFrom = new Date().toISOString()

function organizationIdFromPath(pathname: string) {
  return pathname.match(/^\/football\/([0-9a-f-]{36})(?:\/|$)/i)?.[1]
}

export function FootballPlanSuggestionDock() {
  const location = useLocation()
  const organizationId = organizationIdFromPath(location.pathname)
  const isTrainingPage = /\/football\/[0-9a-f-]{36}\/training(?:\/|$)/i.test(location.pathname)
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const [suggestion, setSuggestion] = useState<FootballTrainingPlanSuggestion>()

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
    queryKey: ['football-sessions', organizationId, 'planning-suggestion'],
    queryFn: () => listFootballSessions(organizationId!, planningFrom),
    enabled: Boolean(organizationId && isTrainingPage && me.data),
  })

  const currentMemberId = members.data?.find((member) => member.userId === me.data?.id)?.id
  const isCoach = profiles.data?.some((profile) => profile.memberId === currentMemberId && profile.teamRole === 'Coach') ?? false
  const nextTraining = sessions.data?.find((session) => session.kind === 'Training')

  const suggest = useMutation({
    mutationFn: () => suggestFootballTrainingPlan(organizationId!, nextTraining!.id),
    onSuccess: (result) => {
      setSuggestion(result)
      setOpen(true)
    },
  })

  const save = useMutation({
    mutationFn: () =>
      replaceFootballTrainingBlocks(
        organizationId!,
        nextTraining!.id,
        suggestion!.blocks.map((block) => ({
          exerciseId: block.exerciseId,
          title: block.title,
          description: block.description,
          coachingPoints: block.coachingPoints,
          durationMinutes: block.durationMinutes,
          responsibleMemberId: block.responsibleMemberId,
          aiReason: block.reason,
        })),
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['football-session', organizationId, nextTraining?.id] })
      setSuggestion(undefined)
      setOpen(false)
    },
  })

  if (!organizationId || !isTrainingPage || !isCoach || !nextTraining) return null

  return (
    <aside className="fixed bottom-24 left-4 z-50 w-[min(560px,calc(100vw-2rem))] rounded-2xl border border-white/10 bg-slate-950/95 shadow-2xl backdrop-blur">
      <div className="flex items-center justify-between gap-3 p-4">
        <button type="button" onClick={() => setOpen((value) => !value)} className="flex min-w-0 flex-1 items-center gap-3 text-left">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-violet-400/15 text-violet-300">
            <BrainCircuit size={20} />
          </div>
          <div className="min-w-0">
            <p className="truncate text-sm font-black text-white">Server-Trainingsplan</p>
            <p className="truncate text-xs text-slate-400">{nextTraining.title} · Readiness, Load & Feedback</p>
          </div>
          {open ? <ChevronDown size={18} className="ml-auto text-slate-400" /> : <ChevronUp size={18} className="ml-auto text-slate-400" />}
        </button>
        <button type="button" onClick={() => suggest.mutate()} disabled={suggest.isPending} className="football-button shrink-0">
          <Sparkles size={15} /> {suggest.isPending ? 'Plant …' : 'Vorschlagen'}
        </button>
      </div>

      {open && (
        <div className="max-h-[65vh] overflow-y-auto border-t border-white/10 p-4">
          {!suggestion && !suggest.isPending && (
            <p className="text-sm text-slate-500">
              Der Vorschlag wird serverseitig aus Zusagen, Positionen, Bereitschaft, letzter RPE-Belastung, Playbook und bisherigen Übungsbewertungen erstellt. Er verändert den gespeicherten Plan erst nach „Übernehmen“.
            </p>
          )}
          {suggest.isError && <p className="text-sm text-rose-300">{suggest.error.message}</p>}

          {suggestion && (
            <>
              <div className="flex flex-wrap gap-2 text-xs">
                <span className="football-chip">{suggestion.playerCount} aktive Spieler</span>
                <span className="football-chip">Fokus: {suggestion.focus}</span>
              </div>

              {suggestion.warnings.length > 0 && (
                <div className="mt-3 space-y-2">
                  {suggestion.warnings.map((warning) => (
                    <div key={warning} className="flex gap-2 rounded-xl border border-amber-400/20 bg-amber-400/10 p-3 text-xs text-amber-100">
                      <AlertTriangle size={15} className="mt-0.5 shrink-0" /> {warning}
                    </div>
                  ))}
                </div>
              )}

              <div className="mt-4 space-y-2">
                {suggestion.blocks.map((block, index) => (
                  <div key={`${block.title}-${index}`} className="rounded-xl border border-white/10 bg-white/[0.04] p-3">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-sm font-bold text-slate-100">{block.title}</p>
                        <p className="mt-1 text-xs text-slate-500">{block.durationMinutes} min · {block.intensity}{block.exerciseId ? ' · Playbook' : ''}</p>
                      </div>
                    </div>
                    {block.description && <p className="mt-2 text-xs leading-5 text-slate-400">{block.description}</p>}
                    <p className="mt-2 text-xs leading-5 text-violet-200">Warum: {block.reason}</p>
                  </div>
                ))}
              </div>

              <button type="button" onClick={() => save.mutate()} disabled={save.isPending} className="football-button mt-4">
                <Save size={15} /> {save.isPending ? 'Übernimmt …' : 'Vorschlag übernehmen'}
              </button>
              {save.isError && <p className="mt-2 text-sm text-rose-300">{save.error.message}</p>}
            </>
          )}
        </div>
      )}
    </aside>
  )
}
