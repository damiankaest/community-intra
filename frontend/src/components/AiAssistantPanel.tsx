import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Bot,
  Check,
  ChevronDown,
  Factory,
  MessageSquareText,
  Send,
  ShieldCheck,
  Sparkles,
  X,
} from 'lucide-react'
import {
  confirmWorkPlan,
  getAiAssistantAvailability,
  prepareWorkPlan,
  type AssistantTone,
  type ConfirmedWorkPlan,
  type WorkPlanDraft,
} from '../api/assistant'
import { registerAssistantTools } from '../webmcp/assistantTools'

interface AiAssistantPanelProps {
  organizationId: string
  themeName?: string
}

export function AiAssistantPanel({
  organizationId,
  themeName,
}: AiAssistantPanelProps) {
  const queryClient = useQueryClient()
  const [isOpen, setIsOpen] = useState(false)
  const [prompt, setPrompt] = useState('')
  const [tone, setTone] = useState<AssistantTone>('Theme')
  const [draft, setDraft] = useState<WorkPlanDraft>()
  const [confirmed, setConfirmed] = useState<ConfirmedWorkPlan>()
  const [webMcpSupported] = useState(() => Boolean(document.modelContext))

  const availability = useQuery({
    queryKey: ['assistant-availability', organizationId],
    queryFn: () => getAiAssistantAvailability(organizationId),
    enabled: Boolean(organizationId),
    retry: false,
  })

  const prepareMutation = useMutation({
    mutationFn: () => prepareWorkPlan(organizationId, prompt, tone),
    onSuccess: (result) => {
      setDraft(result)
      setConfirmed(undefined)
    },
  })

  const handleConfirmed = useCallback(
    async (result: ConfirmedWorkPlan) => {
      setConfirmed(result)
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ['projects', organizationId],
        }),
        queryClient.invalidateQueries({ queryKey: ['tasks', organizationId] }),
        queryClient.invalidateQueries({
          queryKey: ['dashboard', organizationId],
        }),
        queryClient.invalidateQueries({
          queryKey: ['activities', organizationId],
        }),
      ])
    },
    [organizationId, queryClient],
  )

  const confirmMutation = useMutation({
    mutationFn: () =>
      confirmWorkPlan(organizationId, draft!.id, draft!.concurrencyToken),
    onSuccess: handleConfirmed,
  })

  useEffect(() => {
    const registration = registerAssistantTools({
      organizationId,
      currentDraft: draft,
      onDraft: (result) => {
        setDraft(result)
        setConfirmed(undefined)
        setIsOpen(true)
      },
      onConfirmed: (result) => {
        void handleConfirmed(result)
        setIsOpen(true)
      },
    })
    return registration.unregister
  }, [draft, handleConfirmed, organizationId])

  const submit = () => {
    if (prompt.trim().length >= 3) {
      prepareMutation.mutate()
    }
  }

  if (!isOpen) {
    return (
      <button
        type="button"
        className="fixed right-5 bottom-5 z-50 flex items-center gap-2 rounded-full bg-[var(--theme-primary)] px-5 py-3 font-bold text-black shadow-2xl shadow-black/40 transition hover:-translate-y-0.5"
        onClick={() => setIsOpen(true)}
      >
        <Sparkles size={18} />
        Arbeitsplanung
      </button>
    )
  }

  return (
    <aside
      aria-label="KI-Arbeitsplanung"
      className="fixed right-4 bottom-4 z-50 flex max-h-[calc(100vh-2rem)] w-[calc(100vw-2rem)] max-w-xl flex-col overflow-hidden rounded-3xl border border-white/15 bg-[var(--theme-surface)] shadow-2xl shadow-black/60"
    >
      <header className="flex items-center justify-between gap-4 border-b border-white/10 bg-black/20 px-5 py-4">
        <div className="flex min-w-0 items-center gap-3">
          <div className="rounded-xl bg-[var(--theme-primary)]/15 p-2 text-[var(--theme-primary)]">
            <Bot size={20} />
          </div>
          <div className="min-w-0">
            <p className="truncate font-black text-white">Arbeitsplanung</p>
            <p className="text-xs text-[var(--theme-muted)]">
              Entwurf → Prüfung → Bestätigung
            </p>
          </div>
        </div>
        <button
          type="button"
          aria-label="Chat schließen"
          className="rounded-lg p-2 text-[var(--theme-muted)] hover:bg-white/10 hover:text-white"
          onClick={() => setIsOpen(false)}
        >
          <X size={19} />
        </button>
      </header>

      <div className="flex-1 overflow-y-auto p-5">
        <div className="rounded-2xl border border-white/10 bg-black/15 p-4">
          <div className="flex items-start gap-3">
            <MessageSquareText
              size={18}
              className="mt-0.5 shrink-0 text-[var(--theme-primary)]"
            />
            <p className="text-sm leading-6 text-[var(--theme-text)]">
              Sag mir, was als Nächstes gebaut oder organisiert werden soll. Ich
              erstelle einen prüfbaren Projektentwurf mit Material und
              abhakbaren Aufgaben.
            </p>
          </div>
        </div>

        <div className="mt-4 flex gap-2 rounded-xl bg-black/20 p-1">
          <ToneButton
            active={tone === 'Theme'}
            onClick={() => setTone('Theme')}
            icon={<Factory size={15} />}
          >
            {themeName ? `${themeName}-Stil` : 'Theme-Stil'}
          </ToneButton>
          <ToneButton
            active={tone === 'Neutral'}
            onClick={() => setTone('Neutral')}
            icon={<ChevronDown size={15} />}
          >
            Normal
          </ToneButton>
        </div>

        {!availability.isPending && !availability.data?.isConfigured && (
          <div className="mt-4 rounded-xl border border-[var(--theme-warning)]/30 bg-[var(--theme-warning)]/10 p-4 text-sm text-[var(--theme-warning)]">
            Der Chat ist vorbereitet, aber auf dem Server fehlt noch
            <code className="mx-1">OPENAI_API_KEY</code>.
          </div>
        )}

        {draft && (
          <WorkPlanCard
            draft={draft}
            confirmed={confirmed}
            confirming={confirmMutation.isPending}
            onConfirm={() => confirmMutation.mutate()}
          />
        )}

        {(prepareMutation.error || confirmMutation.error) && (
          <div className="mt-4 rounded-xl border border-[var(--theme-danger)]/30 bg-[var(--theme-danger)]/10 p-4 text-sm text-[var(--theme-danger)]">
            {(prepareMutation.error ?? confirmMutation.error)?.message}
          </div>
        )}
      </div>

      <footer className="border-t border-white/10 bg-black/20 p-4">
        <div className="flex items-end gap-2">
          <textarea
            aria-label="Vorhaben beschreiben"
            value={prompt}
            onChange={(event) => setPrompt(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter' && !event.shiftKey) {
                event.preventDefault()
                submit()
              }
            }}
            rows={3}
            maxLength={2000}
            placeholder="Hey, wir brauchen als Nächstes eine Aluminiumproduktion …"
            className="min-h-20 flex-1 resize-none rounded-xl border border-white/10 bg-black/20 px-4 py-3 text-sm text-white outline-none placeholder:text-[var(--theme-muted)] focus:border-[var(--theme-primary)]"
          />
          <button
            type="button"
            aria-label="Entwurf erstellen"
            className="rounded-xl bg-[var(--theme-primary)] p-3 text-black disabled:cursor-not-allowed disabled:opacity-40"
            disabled={
              prepareMutation.isPending ||
              !availability.data?.isConfigured ||
              prompt.trim().length < 3
            }
            onClick={submit}
          >
            <Send size={19} />
          </button>
        </div>
        <div className="mt-3 flex flex-wrap items-center justify-between gap-2 text-[11px] text-[var(--theme-muted)]">
          <span className="inline-flex items-center gap-1">
            <ShieldCheck size={13} />
            Änderungen erst nach Bestätigung
          </span>
          <span>
            WebMCP {webMcpSupported ? 'aktiv' : 'nicht unterstützt'} ·{' '}
            {availability.data?.model ?? 'Modell wird geprüft'}
          </span>
        </div>
      </footer>
    </aside>
  )
}

function ToneButton({
  active,
  onClick,
  icon,
  children,
}: {
  active: boolean
  onClick: () => void
  icon: ReactNode
  children: ReactNode
}) {
  return (
    <button
      type="button"
      className={`flex flex-1 items-center justify-center gap-2 rounded-lg px-3 py-2 text-xs font-bold transition ${
        active
          ? 'bg-[var(--theme-primary)] text-black'
          : 'text-[var(--theme-muted)] hover:text-white'
      }`}
      onClick={onClick}
    >
      {icon}
      {children}
    </button>
  )
}

function WorkPlanCard({
  draft,
  confirmed,
  confirming,
  onConfirm,
}: {
  draft: WorkPlanDraft
  confirmed?: ConfirmedWorkPlan
  confirming: boolean
  onConfirm: () => void
}) {
  return (
    <article className="mt-5 rounded-2xl border border-[var(--theme-primary)]/30 bg-[var(--theme-primary)]/[0.05] p-5">
      <p className="text-xs font-bold tracking-wider text-[var(--theme-primary)] uppercase">
        Unverbindlicher Entwurf
      </p>
      <h2 className="mt-2 text-xl font-black text-white">
        {draft.proposal.title}
      </h2>
      <p className="mt-3 text-sm leading-6 text-[var(--theme-text)]">
        {draft.proposal.executiveSummary}
      </p>
      <blockquote className="mt-4 border-l-2 border-[var(--theme-primary)] pl-4 text-sm text-[var(--theme-muted)] italic">
        {draft.proposal.managementMessage}
      </blockquote>

      <h3 className="mt-5 font-bold text-white">Ressourcen / Material</h3>
      {draft.proposal.materials.length > 0 ? (
        <ul className="mt-2 grid gap-2 text-sm text-[var(--theme-text)]">
          {draft.proposal.materials.map((material, index) => (
            <li
              key={`${material.name}-${index}`}
              className="rounded-lg bg-black/15 px-3 py-2"
            >
              <strong>{material.quantity}</strong> × {material.name}
              {material.notes ? ` – ${material.notes}` : ''}
            </li>
          ))}
        </ul>
      ) : (
        <p className="mt-2 text-sm text-[var(--theme-muted)]">
          Keine gesonderten Ressourcen erforderlich.
        </p>
      )}

      <h3 className="mt-5 font-bold text-white">
        Aufgaben ({draft.proposal.tasks.length})
      </h3>
      <ol className="mt-2 grid gap-3">
        {draft.proposal.tasks.map((task, index) => (
          <li
            key={`${task.title}-${index}`}
            className="rounded-xl border border-white/10 bg-black/15 p-3"
          >
            <div className="flex items-start gap-2">
              <span className="mt-0.5 rounded-md bg-white/10 px-2 py-1 text-xs font-bold text-[var(--theme-primary)]">
                {index + 1}
              </span>
              <div>
                <p className="font-bold text-white">{task.title}</p>
                <p className="mt-1 text-xs leading-5 text-[var(--theme-muted)]">
                  {task.description}
                </p>
                {task.acceptanceCriteria.map((criterion) => (
                  <p
                    key={criterion}
                    className="mt-1 flex items-start gap-1 text-xs text-[var(--theme-text)]"
                  >
                    <span>□</span>
                    {criterion}
                  </p>
                ))}
              </div>
            </div>
          </li>
        ))}
      </ol>

      {confirmed ? (
        <div className="mt-5 flex items-center gap-2 rounded-xl bg-[var(--theme-success)]/10 p-4 text-sm font-bold text-[var(--theme-success)]">
          <Check size={18} />
          Projekt und {confirmed.taskIds.length} Aufgaben wurden angelegt.
        </div>
      ) : (
        <button
          type="button"
          className="mt-5 flex w-full items-center justify-center gap-2 rounded-xl bg-[var(--theme-primary)] px-4 py-3 font-black text-black disabled:opacity-50"
          disabled={confirming}
          onClick={onConfirm}
        >
          <Check size={18} />
          {confirming
            ? 'Wird verbindlich angelegt …'
            : 'Projekt und Aufgaben verbindlich anlegen'}
        </button>
      )}
    </article>
  )
}
