import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Bot,
  Check,
  CircleStop,
  Factory,
  MessageCircleMore,
  Send,
  Sparkles,
  Wrench,
  X,
} from 'lucide-react'
import {
  confirmAssistantAction,
  getAiAssistantAvailability,
  getAssistantChat,
  streamAssistantMessage,
  type AssistantAction,
  type AssistantMessage,
  type AssistantStreamEvent,
  type AssistantTone,
} from '../api/assistant'
import { registerAssistantTools } from '../webmcp/assistantTools'

interface AiAssistantPanelProps {
  organizationId: string
  themeName?: string
}

const suggestions = [
  'Woran sollten wir als Nächstes arbeiten?',
  'Zeig mir die offenen Aufgaben.',
  'Erstelle eine Aufgabe für einen Screenshot-Test.',
]

export function AiAssistantPanel({
  organizationId,
  themeName,
}: AiAssistantPanelProps) {
  const queryClient = useQueryClient()
  const [isOpen, setIsOpen] = useState(false)
  const [prompt, setPrompt] = useState('')
  const [tone, setTone] = useState<AssistantTone>('Theme')
  const [messages, setMessages] = useState<AssistantMessage[]>([])
  const [actions, setActions] = useState<AssistantAction[]>([])
  const [isStreaming, setIsStreaming] = useState(false)
  const [streamError, setStreamError] = useState<string>()
  const abortRef = useRef<AbortController | undefined>(undefined)
  const endRef = useRef<HTMLDivElement>(null)
  const initializedRef = useRef(false)
  const webMcpSupported = Boolean(document.modelContext)

  const availability = useQuery({
    queryKey: ['assistant-availability', organizationId],
    queryFn: () => getAiAssistantAvailability(organizationId),
    enabled: Boolean(organizationId),
    retry: false,
  })
  const chat = useQuery({
    queryKey: ['assistant-chat', organizationId],
    queryFn: () => getAssistantChat(organizationId),
    enabled: Boolean(organizationId),
  })

  useEffect(() => {
    if (chat.data && !initializedRef.current) {
      initializedRef.current = true
      setMessages(chat.data.messages)
      setActions(chat.data.actions)
      setTone(chat.data.tone)
    }
  }, [chat.data])

  useEffect(() => {
    if (isOpen) {
      endRef.current?.scrollIntoView({ behavior: 'smooth' })
    }
  }, [actions, isOpen, messages])

  const invalidateWorkspace = useCallback(async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['projects', organizationId] }),
      queryClient.invalidateQueries({ queryKey: ['tasks', organizationId] }),
      queryClient.invalidateQueries({
        queryKey: ['dashboard', organizationId],
      }),
      queryClient.invalidateQueries({
        queryKey: ['activities', organizationId],
      }),
    ])
  }, [organizationId, queryClient])

  useEffect(() => {
    const registration = registerAssistantTools({
      organizationId,
      onChanged: () => void invalidateWorkspace(),
      onOpenChat: () => setIsOpen(true),
    })
    return registration.unregister
  }, [invalidateWorkspace, organizationId])

  const confirmMutation = useMutation({
    mutationFn: (action: AssistantAction) =>
      confirmAssistantAction(
        organizationId,
        action.id,
        action.concurrencyToken,
      ),
    onSuccess: async (result) => {
      setActions((current) =>
        current.map((action) =>
          action.id === result.actionId
            ? {
                ...action,
                status: 'Confirmed',
                resultEntityId: result.resultEntityId,
              }
            : action,
        ),
      )
      await invalidateWorkspace()
    },
  })

  const handleStreamEvent = (
    event: AssistantStreamEvent,
    userMessageId: string,
    assistantMessageId: string,
  ) => {
    switch (event.type) {
      case 'message_ack':
        setMessages((current) =>
          current.map((message) =>
            message.id === userMessageId ? event.message : message,
          ),
        )
        break
      case 'delta':
        setMessages((current) =>
          current.map((message) =>
            message.id === assistantMessageId
              ? { ...message, content: message.content + event.delta }
              : message,
          ),
        )
        break
      case 'action':
        setActions((current) => [
          ...current.filter((action) => action.id !== event.action.id),
          event.action,
        ])
        break
      case 'done':
        setMessages((current) =>
          current.map((message) =>
            message.id === assistantMessageId ? event.message : message,
          ),
        )
        break
      case 'error':
        setStreamError(event.message)
        break
    }
  }

  const submit = async (suggestedPrompt?: string) => {
    const content = (suggestedPrompt ?? prompt).trim()
    if (content.length < 1 || isStreaming || !availability.data?.isConfigured) {
      return
    }

    const createdAt = new Date().toISOString()
    const userMessageId = `local-user-${crypto.randomUUID()}`
    const assistantMessageId = `local-assistant-${crypto.randomUUID()}`
    setPrompt('')
    setStreamError(undefined)
    setMessages((current) => [
      ...current,
      {
        id: userMessageId,
        role: 'User',
        content,
        createdAt,
      },
      {
        id: assistantMessageId,
        role: 'Assistant',
        content: '',
        createdAt,
      },
    ])
    setIsStreaming(true)
    const controller = new AbortController()
    abortRef.current = controller
    try {
      await streamAssistantMessage(
        organizationId,
        content,
        tone,
        (event) => handleStreamEvent(event, userMessageId, assistantMessageId),
        controller.signal,
      )
    } catch (error) {
      if (!controller.signal.aborted) {
        setStreamError(
          error instanceof Error
            ? error.message
            : 'Die Antwort ist fehlgeschlagen.',
        )
      }
    } finally {
      setIsStreaming(false)
      abortRef.current = undefined
    }
  }

  if (!isOpen) {
    return (
      <button
        type="button"
        className="fixed right-5 bottom-5 z-50 flex items-center gap-2 rounded-full bg-[var(--theme-primary)] px-5 py-3 font-bold text-black shadow-2xl shadow-black/40 transition hover:-translate-y-0.5"
        onClick={() => setIsOpen(true)}
      >
        <MessageCircleMore size={19} />
        Frag den Chat
      </button>
    )
  }

  return (
    <aside
      aria-label="Community-Chat"
      className="fixed right-3 bottom-3 z-50 flex h-[min(760px,calc(100vh-1.5rem))] w-[calc(100vw-1.5rem)] max-w-[460px] flex-col overflow-hidden rounded-[28px] border border-white/15 bg-[color-mix(in_srgb,var(--theme-surface)_94%,black)] shadow-2xl shadow-black/60 backdrop-blur-xl"
    >
      <header className="relative overflow-hidden border-b border-white/10 px-5 py-4">
        <div className="absolute inset-0 bg-gradient-to-r from-[var(--theme-primary)]/15 to-transparent" />
        <div className="relative flex items-center justify-between gap-4">
          <div className="flex min-w-0 items-center gap-3">
            <div className="rounded-2xl bg-[var(--theme-primary)] p-2.5 text-black shadow-lg">
              <Bot size={20} />
            </div>
            <div className="min-w-0">
              <p className="truncate font-black text-white">Community-Chat</p>
              <p className="truncate text-xs text-[var(--theme-muted)]">
                Fragt nach · findet Arbeit · bereitet Änderungen vor
              </p>
            </div>
          </div>
          <button
            type="button"
            aria-label="Chat schließen"
            className="rounded-xl p-2 text-[var(--theme-muted)] hover:bg-white/10 hover:text-white"
            onClick={() => setIsOpen(false)}
          >
            <X size={19} />
          </button>
        </div>
      </header>

      <div className="flex items-center gap-2 border-b border-white/10 px-4 py-3">
        <ToneButton
          active={tone === 'Theme'}
          onClick={() => setTone('Theme')}
          icon={<Factory size={14} />}
        >
          {themeName ? `${themeName}-Stil` : 'Mit Theme-Humor'}
        </ToneButton>
        <ToneButton
          active={tone === 'Neutral'}
          onClick={() => setTone('Neutral')}
          icon={<MessageCircleMore size={14} />}
        >
          Klar & normal
        </ToneButton>
      </div>

      <div aria-live="polite" className="flex-1 overflow-y-auto px-4 py-5">
        {messages.length === 0 && (
          <div className="grid place-items-center py-8 text-center">
            <div className="rounded-3xl bg-[var(--theme-primary)]/10 p-4 text-[var(--theme-primary)]">
              <Sparkles size={27} />
            </div>
            <h2 className="mt-4 text-xl font-black text-white">
              Was wollt ihr machen?
            </h2>
            <p className="mt-2 max-w-xs text-sm leading-6 text-[var(--theme-muted)]">
              Frag kurz nach dem Stand oder lass genau die Aufgabe vorbereiten,
              die du gerade brauchst.
            </p>
            <div className="mt-6 grid w-full gap-2">
              {suggestions.map((suggestion) => (
                <button
                  key={suggestion}
                  type="button"
                  className="rounded-2xl border border-white/10 bg-white/[0.04] px-4 py-3 text-left text-sm text-[var(--theme-text)] transition hover:border-[var(--theme-primary)]/50 hover:bg-[var(--theme-primary)]/[0.06]"
                  onClick={() => void submit(suggestion)}
                >
                  {suggestion}
                </button>
              ))}
            </div>
          </div>
        )}

        <div className="grid gap-4">
          {messages.map((message) => (
            <ChatBubble
              key={message.id}
              message={message}
              isStreaming={
                isStreaming &&
                message.role === 'Assistant' &&
                message === messages.at(-1)
              }
            />
          ))}
          {actions
            .filter((action) => action.status === 'Pending')
            .map((action) => (
              <ActionCard
                key={action.id}
                action={action}
                pending={
                  confirmMutation.isPending &&
                  confirmMutation.variables?.id === action.id
                }
                onConfirm={() => confirmMutation.mutate(action)}
              />
            ))}
        </div>

        {(streamError || chat.error || confirmMutation.error) && (
          <div className="mt-4 rounded-2xl border border-[var(--theme-danger)]/30 bg-[var(--theme-danger)]/10 p-3 text-sm text-[var(--theme-danger)]">
            {streamError ??
              chat.error?.message ??
              confirmMutation.error?.message}
          </div>
        )}
        {!availability.isPending && !availability.data?.isConfigured && (
          <div className="mt-4 rounded-2xl border border-[var(--theme-warning)]/30 bg-[var(--theme-warning)]/10 p-4 text-sm text-[var(--theme-warning)]">
            Auf dem Server fehlt noch <code>OPENAI_API_KEY</code>.
          </div>
        )}
        <div ref={endRef} />
      </div>

      <footer className="border-t border-white/10 bg-black/20 p-3">
        <div className="rounded-2xl border border-white/10 bg-black/20 p-2 focus-within:border-[var(--theme-primary)]/60">
          <textarea
            aria-label="Nachricht"
            value={prompt}
            onChange={(event) => setPrompt(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter' && !event.shiftKey) {
                event.preventDefault()
                void submit()
              }
            }}
            rows={2}
            maxLength={2000}
            placeholder="Schreib einfach, was du wissen oder ändern willst …"
            className="max-h-36 min-h-14 w-full resize-none bg-transparent px-2 py-1 text-sm leading-6 text-white outline-none placeholder:text-[var(--theme-muted)]"
          />
          <div className="flex items-center justify-between gap-3">
            <span
              className="inline-flex items-center gap-1.5 px-2 text-[11px] text-[var(--theme-muted)]"
              title={
                webMcpSupported
                  ? 'Zusätzliche Browser-Agenten können die freigegebenen Werkzeuge nutzen.'
                  : 'Der eingebaute Chat funktioniert unabhängig von der experimentellen Browser-API.'
              }
            >
              <Wrench size={12} />
              Chat-Steuerung aktiv
              {webMcpSupported ? ' · Browser-Tools bereit' : ''}
            </span>
            <button
              type="button"
              aria-label={isStreaming ? 'Antwort stoppen' : 'Nachricht senden'}
              className="grid h-10 w-10 place-items-center rounded-xl bg-[var(--theme-primary)] text-black transition hover:scale-105 disabled:cursor-not-allowed disabled:opacity-40"
              disabled={
                !isStreaming &&
                (!availability.data?.isConfigured || !prompt.trim())
              }
              onClick={() =>
                isStreaming ? abortRef.current?.abort() : void submit()
              }
            >
              {isStreaming ? <CircleStop size={18} /> : <Send size={18} />}
            </button>
          </div>
        </div>
      </footer>
    </aside>
  )
}

function ChatBubble({
  message,
  isStreaming,
}: {
  message: AssistantMessage
  isStreaming: boolean
}) {
  const isUser = message.role === 'User'
  if (!message.content && !isStreaming) {
    return null
  }

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[88%] rounded-2xl px-4 py-3 text-sm leading-6 ${
          isUser
            ? 'rounded-br-md bg-[var(--theme-primary)] text-black'
            : 'rounded-bl-md border border-white/10 bg-white/[0.055] text-[var(--theme-text)]'
        }`}
      >
        {message.content ? (
          <p className="whitespace-pre-wrap">{message.content}</p>
        ) : (
          <span className="inline-flex gap-1 py-2">
            <i className="h-1.5 w-1.5 animate-pulse rounded-full bg-[var(--theme-primary)]" />
            <i className="h-1.5 w-1.5 animate-pulse rounded-full bg-[var(--theme-primary)] [animation-delay:120ms]" />
            <i className="h-1.5 w-1.5 animate-pulse rounded-full bg-[var(--theme-primary)] [animation-delay:240ms]" />
          </span>
        )}
      </div>
    </div>
  )
}

function ActionCard({
  action,
  pending,
  onConfirm,
}: {
  action: AssistantAction
  pending: boolean
  onConfirm: () => void
}) {
  const title =
    action.payload.title ??
    action.payload.name ??
    (action.kind === 'UpdateTask' ? 'Aufgabe anpassen' : 'Änderung vorbereiten')
  const kindLabel = {
    CreateTask: 'Neue Aufgabe',
    UpdateTask: 'Aufgabe ändern',
    CreateProject: 'Neues Projekt',
  }[action.kind]

  return (
    <article className="ml-2 rounded-2xl border border-[var(--theme-primary)]/35 bg-[var(--theme-primary)]/[0.07] p-4">
      <p className="text-[11px] font-black tracking-wider text-[var(--theme-primary)] uppercase">
        {kindLabel} · erst nach Bestätigung
      </p>
      <h3 className="mt-2 font-black text-white">{title}</h3>
      {action.payload.description && (
        <p className="mt-2 text-xs leading-5 whitespace-pre-wrap text-[var(--theme-muted)]">
          {action.payload.description}
        </p>
      )}
      <div className="mt-3 flex flex-wrap gap-2 text-[11px] text-[var(--theme-text)]">
        {action.payload.status && <Chip>{action.payload.status}</Chip>}
        {action.payload.priority && <Chip>{action.payload.priority}</Chip>}
        {action.payload.dueDate && (
          <Chip>bis {formatDate(action.payload.dueDate)}</Chip>
        )}
      </div>
      <button
        type="button"
        className="mt-4 flex w-full items-center justify-center gap-2 rounded-xl bg-[var(--theme-primary)] px-4 py-2.5 text-sm font-black text-black disabled:opacity-50"
        disabled={pending}
        onClick={onConfirm}
      >
        <Check size={16} />
        {pending ? 'Wird gespeichert …' : 'Ja, so speichern'}
      </button>
    </article>
  )
}

function Chip({ children }: { children: ReactNode }) {
  return (
    <span className="rounded-full border border-white/10 bg-black/20 px-2.5 py-1">
      {children}
    </span>
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
      className={`flex flex-1 items-center justify-center gap-1.5 rounded-xl px-3 py-2 text-xs font-bold transition ${
        active
          ? 'bg-[var(--theme-primary)] text-black'
          : 'bg-white/[0.04] text-[var(--theme-muted)] hover:text-white'
      }`}
      onClick={onClick}
    >
      {icon}
      {children}
    </button>
  )
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('de-DE').format(new Date(`${value}T00:00:00`))
}
