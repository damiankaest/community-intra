import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronDown, ChevronUp, MessageSquareHeart, Save, Star } from 'lucide-react'
import { useLocation } from 'react-router-dom'
import { getCurrentUser } from '../api/auth'
import {
  getFootballSession,
  getFootballSessionFeedback,
  getFootballTrainingHistory,
  updateFootballExerciseFeedback,
  type FootballExerciseFeedback,
  type FootballTrainingBlock,
} from '../api/football'
import { listMembers } from '../api/members'

function organizationIdFromPath(pathname: string) {
  const match = pathname.match(/^\/football\/([0-9a-f-]{36})(?:\/|$)/i)
  return match?.[1]
}

export function FootballFeedbackDock() {
  const location = useLocation()
  const organizationId = organizationIdFromPath(location.pathname)
  const [open, setOpen] = useState(false)
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
  const memberId = members.data?.find((member) => member.userId === me.data?.id)?.id
  const history = useQuery({
    queryKey: ['football-history', organizationId, memberId, 'feedback'],
    queryFn: () => getFootballTrainingHistory(organizationId!, memberId!, 5),
    enabled: Boolean(organizationId && memberId),
  })
  const latestSession = history.data?.find((entry) => entry.session.kind === 'Training')?.session
  const detail = useQuery({
    queryKey: ['football-session', organizationId, latestSession?.id],
    queryFn: () => getFootballSession(organizationId!, latestSession!.id),
    enabled: Boolean(organizationId && latestSession?.id && open),
  })
  const feedback = useQuery({
    queryKey: ['football-feedback', organizationId, latestSession?.id],
    queryFn: () => getFootballSessionFeedback(organizationId!, latestSession!.id),
    enabled: Boolean(organizationId && latestSession?.id && open),
  })

  if (!organizationId || !memberId || !latestSession) return null

  const ownFeedback = feedback.data?.feedback.filter((item) => item.memberId === memberId) ?? []
  const ratedCount = ownFeedback.length
  const blockCount = detail.data?.blocks.length ?? 0

  return (
    <aside className="fixed bottom-24 right-4 z-40 w-[min(460px,calc(100vw-2rem))] rounded-2xl border border-white/10 bg-slate-950/95 shadow-2xl backdrop-blur">
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        className="flex w-full items-center justify-between gap-3 p-4 text-left"
      >
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-violet-400/15 text-violet-300">
            <MessageSquareHeart size={20} />
          </div>
          <div>
            <p className="text-sm font-black text-white">Training bewerten</p>
            <p className="text-xs text-slate-400">
              {latestSession.title}{blockCount > 0 ? ` · ${ratedCount}/${blockCount} bewertet` : ''}
            </p>
          </div>
        </div>
        {open ? <ChevronDown size={18} className="text-slate-400" /> : <ChevronUp size={18} className="text-slate-400" />}
      </button>

      {open && (
        <div className="max-h-[65vh] overflow-y-auto border-t border-white/10 p-4">
          <p className="text-xs leading-5 text-slate-500">
            Deine Bewertung hilft dem Trainerteam später zu erkennen, welche Blöcke gut funktionieren und welche angepasst werden sollten.
          </p>
          <div className="mt-4 space-y-3">
            {detail.data?.blocks.map((block) => {
              const existing = ownFeedback.find((item) => item.trainingBlockId === block.id)
              return (
                <FeedbackEditor
                  key={`${block.id}-${existing?.updatedAt ?? 'new'}`}
                  organizationId={organizationId}
                  sessionId={latestSession.id}
                  memberId={memberId}
                  block={block}
                  existing={existing}
                />
              )
            })}
            {!detail.isPending && !detail.data?.blocks.length && (
              <p className="rounded-xl bg-white/5 p-3 text-sm text-slate-500">
                Für diese Einheit ist kein gespeicherter Trainingsplan vorhanden.
              </p>
            )}
          </div>
        </div>
      )}
    </aside>
  )
}

function FeedbackEditor({
  organizationId,
  sessionId,
  memberId,
  block,
  existing,
}: {
  organizationId: string
  sessionId: string
  memberId: string
  block: FootballTrainingBlock
  existing?: FootballExerciseFeedback
}) {
  const queryClient = useQueryClient()
  const [fun, setFun] = useState(existing?.fun ?? 3)
  const [difficulty, setDifficulty] = useState(existing?.difficulty ?? 3)
  const [benefit, setBenefit] = useState(existing?.benefit ?? 3)
  const [comment, setComment] = useState(existing?.comment ?? '')
  const save = useMutation({
    mutationFn: () =>
      updateFootballExerciseFeedback(
        organizationId,
        sessionId,
        block.id,
        memberId,
        { fun, difficulty, benefit, comment: comment.trim() || undefined },
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['football-feedback', organizationId, sessionId] })
    },
  })

  return (
    <div className="rounded-xl border border-white/10 bg-white/[0.04] p-3">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm font-bold text-slate-100">{block.title}</p>
          <p className="mt-1 text-xs text-slate-500">{block.durationMinutes} min{block.exerciseId ? ' · Playbook' : ' · freier Block'}</p>
        </div>
        {existing && <span className="rounded-full bg-emerald-400/10 px-2 py-1 text-[11px] font-bold text-emerald-300">Gespeichert</span>}
      </div>

      <div className="mt-3 grid grid-cols-3 gap-2">
        <RatingField label="Spaß" value={fun} onChange={setFun} />
        <RatingField label="Schwierigkeit" value={difficulty} onChange={setDifficulty} />
        <RatingField label="Nutzen" value={benefit} onChange={setBenefit} />
      </div>
      <input
        className="football-input mt-3"
        value={comment}
        onChange={(event) => setComment(event.target.value)}
        placeholder="Optionaler Kommentar"
      />
      <button type="button" onClick={() => save.mutate()} disabled={save.isPending} className="football-button mt-3">
        <Save size={14} /> {save.isPending ? 'Speichert …' : existing ? 'Bewertung aktualisieren' : 'Bewertung speichern'}
      </button>
      {save.isError && <p className="mt-2 text-xs text-rose-300">{save.error.message}</p>}
    </div>
  )
}

function RatingField({ label, value, onChange }: { label: string; value: number; onChange: (value: number) => void }) {
  return (
    <label className="block">
      <span className="mb-1 flex items-center gap-1 text-[11px] font-bold uppercase tracking-wide text-slate-500">
        <Star size={11} /> {label}
      </span>
      <select className="football-input" value={value} onChange={(event) => onChange(Number(event.target.value))}>
        {[1, 2, 3, 4, 5].map((option) => <option key={option} value={option}>{option}</option>)}
      </select>
    </label>
  )
}
