import {
  useEffect,
  useState,
  type Dispatch,
  type ReactNode,
  type SetStateAction,
} from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  ArrowRight,
  BookOpen,
  Brain,
  Check,
  ChevronRight,
  Clipboard,
  Copy,
  DoorOpen,
  Fingerprint,
  KeyRound,
  Lightbulb,
  LoaderCircle,
  MapPin,
  MessageCircleQuestion,
  NotebookPen,
  Plus,
  RotateCcw,
  Search,
  Send,
  Sparkles,
  Trash2,
  TriangleAlert,
  Users,
} from 'lucide-react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiError } from '../api/client'
import {
  advanceMysterySession,
  askMysteryQuestion,
  createMysterySession,
  getMysterySession,
  getMysterySessionByCode,
  requestMysteryHint,
  submitMysteryDecision,
  submitMysteryFinale,
  submitMysteryPuzzle,
  updateMysteryNotes,
  type MysteryDifficulty,
  type MysterySession,
} from '../api/mystery'

const sessionKey = (id: string) => ['mystery-session', id] as const

export function MysteryPage() {
  const { sessionId } = useParams()

  useEffect(() => {
    const existing = document.querySelector<HTMLMetaElement>(
      'meta[name="robots"]',
    )
    const previous = existing?.content
    const meta = existing ?? document.createElement('meta')
    meta.name = 'robots'
    meta.content = 'noindex,nofollow'
    if (!existing) document.head.append(meta)
    return () => {
      if (existing) existing.content = previous ?? ''
      else meta.remove()
    }
  }, [])

  return sessionId ? <MysteryGame sessionId={sessionId} /> : <MysteryStart />
}

interface LocationDraft {
  description: string
  availableFromProgress: number
  preferredUse: string
}

function MysteryStart() {
  const navigate = useNavigate()
  const [players, setPlayers] = useState('Damian\n')
  const [durationMinutes, setDurationMinutes] = useState(75)
  const [difficulty, setDifficulty] = useState<MysteryDifficulty>('Medium')
  const [genre, setGenre] = useState('Klassischer Whodunit')
  const [atmosphere, setAtmosphere] = useState(
    'Düster, spannend, aber nicht brutal',
  )
  const [items, setItems] = useState('Papier, Stifte')
  const [locations, setLocations] = useState<LocationDraft[]>([])
  const [joinCode, setJoinCode] = useState('')
  const [generationLine, setGenerationLine] = useState(0)

  const create = useMutation({
    mutationFn: () =>
      createMysterySession({
        players: splitValues(players),
        durationMinutes,
        difficulty,
        genre,
        atmosphere,
        availableItems: splitValues(items),
        locations: locations
          .filter((location) => location.description.trim())
          .map((location, index) => ({
            id: `LOCATION_${index + 1}`,
            description: location.description.trim(),
            availableFromProgress: location.availableFromProgress,
            preferredUse: location.preferredUse,
          })),
      }),
    onSuccess: (session) => navigate(`/mistery/${session.id}`),
  })
  const join = useMutation({
    mutationFn: () => getMysterySessionByCode(joinCode.trim()),
    onSuccess: (session) => navigate(`/mistery/${session.id}`),
  })

  useEffect(() => {
    if (!create.isPending) return
    const timer = window.setInterval(
      () => setGenerationLine((current) => (current + 1) % 4),
      2_600,
    )
    return () => window.clearInterval(timer)
  }, [create.isPending])

  if (create.isPending) {
    const lines = [
      'Der Tatort wird versiegelt …',
      'Verdächtige erhalten Motive und Geheimnisse …',
      'Hinweise und falsche Fährten werden geprüft …',
      'Der Game Master vernichtet seine sichtbaren Notizen …',
    ]
    return (
      <MysteryShell>
        <div className="mx-auto flex min-h-[75vh] max-w-xl flex-col items-center justify-center text-center">
          <div className="mystery-seal grid size-32 place-items-center rounded-full">
            <LoaderCircle className="animate-spin text-[#e7bd71]" size={52} />
          </div>
          <p className="mt-10 text-xs font-bold tracking-[0.28em] text-[#b4935d] uppercase">
            Geheime Fallakte wird erstellt
          </p>
          <h1 className="mt-4 font-serif text-4xl text-[#f4ead7]">
            Niemand am Tisch kennt die Wahrheit.
          </h1>
          <p className="mt-5 min-h-14 text-base text-[#b8afa2]">
            {lines[generationLine]}
          </p>
          <p className="mt-8 text-xs text-white/35">
            Ein vollständig neuer Fall kann ungefähr eine Minute benötigen.
          </p>
        </div>
      </MysteryShell>
    )
  }

  return (
    <MysteryShell>
      <header className="mx-auto flex max-w-6xl items-center justify-between py-2">
        <Link
          to="/"
          className="inline-flex items-center gap-2 text-sm text-[#b8afa2] transition hover:text-white"
        >
          <ArrowLeft size={16} /> CouchClash
        </Link>
        <span className="text-xs font-bold tracking-[0.22em] text-[#b4935d] uppercase">
          Local Mystery
        </span>
      </header>

      <main className="mx-auto grid max-w-6xl gap-8 py-10 lg:grid-cols-[1.08fr_0.92fr] lg:py-20">
        <section className="self-center">
          <div className="inline-flex items-center gap-2 rounded-full border border-[#b4935d]/30 bg-[#b4935d]/10 px-3 py-1.5 text-xs font-bold tracking-[0.16em] text-[#e7bd71] uppercase">
            <Fingerprint size={14} /> KI-geführter Krimiabend
          </div>
          <h1 className="mt-7 max-w-2xl font-serif text-5xl leading-[0.98] text-[#f4ead7] sm:text-7xl">
            Der Fall, den selbst der Host nicht kennt.
          </h1>
          <p className="mt-7 max-w-xl text-base leading-7 text-[#b8afa2] sm:text-lg">
            Legt nur Rahmen, Personen und vorbereitete Orte fest. Täter,
            Wendungen und Lösungen bleiben versiegelt, bis ihr eure gemeinsame
            Theorie abgebt.
          </p>

          <form
            className="mystery-join mt-10 max-w-xl rounded-2xl p-4"
            onSubmit={(event) => {
              event.preventDefault()
              if (joinCode.trim()) join.mutate()
            }}
          >
            <p className="text-xs font-bold tracking-[0.18em] text-[#b4935d] uppercase">
              Laufendem Fall beitreten
            </p>
            <div className="mt-3 flex gap-2">
              <input
                className="mystery-input font-mono tracking-[0.3em] uppercase"
                value={joinCode}
                maxLength={8}
                placeholder="CODE"
                onChange={(event) => setJoinCode(event.target.value)}
              />
              <button
                className="mystery-button-secondary shrink-0"
                disabled={join.isPending || !joinCode.trim()}
              >
                <DoorOpen size={18} /> Beitreten
              </button>
            </div>
            {join.error && <ErrorText error={join.error} />}
          </form>
        </section>

        <section className="mystery-dossier rounded-[1.75rem] p-5 sm:p-7">
          <div className="flex items-center justify-between border-b border-black/15 pb-5">
            <div>
              <p className="text-[11px] font-black tracking-[0.25em] text-[#765f3c] uppercase">
                Neue Fallakte
              </p>
              <h2 className="mt-1 font-serif text-3xl text-[#241c16]">
                Einsatzbriefing
              </h2>
            </div>
            <span className="rotate-3 rounded border-2 border-[#8f2d2d]/55 px-2 py-1 text-xs font-black tracking-[0.18em] text-[#8f2d2d]/70 uppercase">
              Geheim
            </span>
          </div>

          <form
            className="mt-6 grid gap-5"
            onSubmit={(event) => {
              event.preventDefault()
              create.mutate()
            }}
          >
            <DossierField label="Wer ermittelt?" hint="Ein Name pro Zeile">
              <textarea
                className="mystery-paper-input min-h-24"
                value={players}
                onChange={(event) => setPlayers(event.target.value)}
                required
              />
            </DossierField>

            <div className="grid gap-4 sm:grid-cols-2">
              <DossierField label="Dauer">
                <select
                  className="mystery-paper-input"
                  value={durationMinutes}
                  onChange={(event) =>
                    setDurationMinutes(Number(event.target.value))
                  }
                >
                  <option value={45}>Kurz · ca. 45 Min.</option>
                  <option value={75}>Abend · ca. 75 Min.</option>
                  <option value={120}>Lang · ca. 2 Std.</option>
                </select>
              </DossierField>
              <DossierField label="Schwierigkeit">
                <select
                  className="mystery-paper-input"
                  value={difficulty}
                  onChange={(event) =>
                    setDifficulty(event.target.value as MysteryDifficulty)
                  }
                >
                  <option value="Easy">Leicht</option>
                  <option value="Medium">Mittel</option>
                  <option value="Hard">Schwer</option>
                </select>
              </DossierField>
            </div>

            <DossierField label="Genre">
              <input
                className="mystery-paper-input"
                value={genre}
                onChange={(event) => setGenre(event.target.value)}
                required
              />
            </DossierField>
            <DossierField label="Atmosphäre">
              <input
                className="mystery-paper-input"
                value={atmosphere}
                onChange={(event) => setAtmosphere(event.target.value)}
                required
              />
            </DossierField>
            <DossierField
              label="Verfügbare Gegenstände"
              hint="Komma oder Zeilenumbruch"
            >
              <input
                className="mystery-paper-input"
                value={items}
                onChange={(event) => setItems(event.target.value)}
              />
            </DossierField>

            <div>
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-black text-[#3f3125]">
                    Reale geheime Schauplätze
                  </p>
                  <p className="text-xs text-[#765f3c]">
                    Zeitpunkt und Storygrund bleiben für euch verborgen.
                  </p>
                </div>
                <button
                  type="button"
                  className="inline-flex items-center gap-1 rounded-lg border border-black/15 px-2.5 py-1.5 text-xs font-bold text-[#4b392a] hover:bg-black/5"
                  onClick={() =>
                    setLocations((current) => [
                      ...current,
                      {
                        description: '',
                        availableFromProgress: 0.5,
                        preferredUse: 'mid_game',
                      },
                    ])
                  }
                >
                  <Plus size={14} /> Ort
                </button>
              </div>
              <div className="mt-3 grid gap-3">
                {locations.map((location, index) => (
                  <div
                    className="grid gap-2 rounded-xl border border-black/10 bg-black/[0.035] p-3 sm:grid-cols-[1fr_8rem_auto]"
                    key={index}
                  >
                    <input
                      className="mystery-paper-input"
                      placeholder="z. B. vorbereiteter Partykeller"
                      value={location.description}
                      onChange={(event) =>
                        updateLocation(setLocations, index, {
                          description: event.target.value,
                        })
                      }
                    />
                    <select
                      className="mystery-paper-input"
                      value={location.availableFromProgress}
                      onChange={(event) =>
                        updateLocation(setLocations, index, {
                          availableFromProgress: Number(event.target.value),
                          preferredUse:
                            Number(event.target.value) >= 0.7
                              ? 'late_game'
                              : 'mid_game',
                        })
                      }
                    >
                      <option value={0}>Ab Beginn</option>
                      <option value={0.5}>Ab Mitte</option>
                      <option value={0.75}>Erst spät</option>
                    </select>
                    <button
                      type="button"
                      aria-label="Ort entfernen"
                      className="grid size-11 place-items-center rounded-lg text-[#8f2d2d] hover:bg-[#8f2d2d]/10"
                      onClick={() =>
                        setLocations((current) =>
                          current.filter((_, itemIndex) => itemIndex !== index),
                        )
                      }
                    >
                      <Trash2 size={17} />
                    </button>
                  </div>
                ))}
              </div>
            </div>

            {create.error && <ErrorText error={create.error} dark />}
            <button
              className="mystery-button-primary mt-2 w-full justify-center py-3.5"
              disabled={splitValues(players).length === 0}
            >
              <Sparkles size={19} /> Unbekannten Fall erzeugen
            </button>
          </form>
        </section>
      </main>
    </MysteryShell>
  )
}

function MysteryGame({ sessionId }: { sessionId: string }) {
  const queryClient = useQueryClient()
  const session = useQuery({
    queryKey: sessionKey(sessionId),
    queryFn: () => getMysterySession(sessionId),
    retry: false,
    refetchInterval: 5_000,
  })
  const [hint, setHint] = useState<{ level: number; text: string }>()
  const [puzzleAnswer, setPuzzleAnswer] = useState('')
  const [puzzleMessage, setPuzzleMessage] = useState<string>()
  const [question, setQuestion] = useState('')
  const [notesText, setNotesText] = useState<string>()

  const updateSession = (next: MysterySession) =>
    queryClient.setQueryData(sessionKey(sessionId), next)

  const advance = useMutation({
    mutationFn: () => advanceMysterySession(sessionId, session.data!.version),
    onSuccess: (next) => {
      setHint(undefined)
      setPuzzleMessage(undefined)
      setPuzzleAnswer('')
      updateSession(next)
      window.scrollTo({ top: 0, behavior: 'smooth' })
    },
    onError: () => void session.refetch(),
  })
  const solvePuzzle = useMutation({
    mutationFn: () =>
      submitMysteryPuzzle(sessionId, puzzleAnswer, session.data!.version),
    onSuccess: (result) => {
      setPuzzleMessage(result.message)
      updateSession(result.session)
    },
    onError: () => void session.refetch(),
  })
  const decide = useMutation({
    mutationFn: (choiceId: string) =>
      submitMysteryDecision(sessionId, choiceId, session.data!.version),
    onSuccess: updateSession,
    onError: () => void session.refetch(),
  })
  const getHint = useMutation({
    mutationFn: (level: number) =>
      requestMysteryHint(sessionId, level, session.data!.version),
    onSuccess: (result) => {
      setHint({ level: result.level, text: result.hint })
      updateSession(result.session)
    },
    onError: () => void session.refetch(),
  })
  const ask = useMutation({
    mutationFn: () =>
      askMysteryQuestion(sessionId, question, session.data!.version),
    onSuccess: (result) => {
      setQuestion('')
      updateSession(result.session)
    },
    onError: () => void session.refetch(),
  })
  const saveNotes = useMutation({
    mutationFn: () =>
      updateMysteryNotes(
        sessionId,
        splitLines(notesText ?? session.data!.notes.join('\n')).slice(0, 30),
        session.data!.version,
      ),
    onSuccess: (next) => {
      setNotesText(undefined)
      updateSession(next)
    },
    onError: () => void session.refetch(),
  })

  if (session.isPending) {
    return <MysteryMessage text="Die versiegelte Fallakte wird geladen …" />
  }
  if (session.error || !session.data) {
    return <MysteryMessage text="Diese Fallakte wurde nicht gefunden." error />
  }

  const game = session.data
  const currentDecision = game.currentScene
    ? game.decisions.find(
        (decision) => decision.sceneId === game.currentScene?.id,
      )
    : undefined
  const mutationError =
    advance.error ??
    solvePuzzle.error ??
    decide.error ??
    getHint.error ??
    ask.error ??
    saveNotes.error

  return (
    <MysteryShell>
      <header className="mx-auto max-w-7xl border-b border-white/10 pb-5">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            <Link
              to="/mistery"
              aria-label="Zur Mystery-Startseite"
              className="grid size-10 place-items-center rounded-full border border-white/10 text-[#b8afa2] hover:border-white/25 hover:text-white"
            >
              <ArrowLeft size={17} />
            </Link>
            <div>
              <p className="text-[10px] font-black tracking-[0.24em] text-[#b4935d] uppercase">
                Kapitel {game.chapter} · {game.phase}
              </p>
              <h1 className="mt-1 font-serif text-2xl text-[#f4ead7] sm:text-3xl">
                {game.title}
              </h1>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button
              type="button"
              className="mystery-code"
              title="Einladungslink kopieren"
              onClick={() =>
                void navigator.clipboard.writeText(window.location.href)
              }
            >
              <Copy size={14} /> Code <strong>{game.joinCode}</strong>
            </button>
            <span className="hidden rounded-full border border-white/10 px-3 py-2 text-xs text-white/50 sm:inline">
              {game.gameMaster}
            </span>
          </div>
        </div>
        <div className="mt-5 flex flex-wrap gap-2">
          {game.players.map((player) => (
            <span
              key={player}
              className="rounded-full bg-white/[0.06] px-3 py-1 text-xs text-white/60"
            >
              {player}
            </span>
          ))}
        </div>
      </header>

      {game.notice && (
        <div className="mx-auto mt-5 flex max-w-7xl items-start gap-3 rounded-xl border border-amber-300/20 bg-amber-300/[0.07] p-4 text-sm text-amber-100/80">
          <TriangleAlert className="mt-0.5 shrink-0" size={17} /> {game.notice}
        </div>
      )}

      <main className="mx-auto grid max-w-7xl gap-6 py-7 lg:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="grid min-w-0 gap-6">
          {game.status === 'Active' && game.currentScene && (
            <SceneCard
              game={game}
              puzzleAnswer={puzzleAnswer}
              setPuzzleAnswer={setPuzzleAnswer}
              puzzleMessage={puzzleMessage}
              solvePuzzle={() => solvePuzzle.mutate()}
              isSolving={solvePuzzle.isPending}
              currentChoice={currentDecision?.choiceId}
              choose={(choiceId) => decide.mutate(choiceId)}
              isChoosing={decide.isPending}
              advance={() => advance.mutate()}
              isAdvancing={advance.isPending}
            />
          )}

          {game.status === 'ReadyForFinale' && (
            <FinalTheory game={game} updateSession={updateSession} />
          )}

          {game.status === 'Completed' && game.finale && (
            <Resolution game={game} />
          )}

          {game.status === 'Active' && (
            <div className="grid gap-6 md:grid-cols-2">
              <section className="mystery-panel rounded-2xl p-5">
                <div className="flex items-center gap-3">
                  <span className="grid size-10 place-items-center rounded-xl bg-[#b4935d]/10 text-[#e7bd71]">
                    <Lightbulb size={19} />
                  </span>
                  <div>
                    <h2 className="font-bold text-[#f4ead7]">
                      Hinweis anfordern
                    </h2>
                    <p className="text-xs text-white/40">
                      Bisher {game.usedHintCount} Hinweise verwendet
                    </p>
                  </div>
                </div>
                <div className="mt-4 grid grid-cols-3 gap-2">
                  {[
                    ['1', 'Denkanstoß'],
                    ['2', 'Deutlich'],
                    ['3', 'Fast gelöst'],
                  ].map(([level, label]) => (
                    <button
                      type="button"
                      key={level}
                      className="rounded-xl border border-white/10 bg-white/[0.04] px-2 py-3 text-xs text-white/65 hover:border-[#b4935d]/50 hover:text-white disabled:opacity-50"
                      disabled={getHint.isPending}
                      onClick={() => getHint.mutate(Number(level))}
                    >
                      <strong className="block text-base text-[#e7bd71]">
                        {level}
                      </strong>
                      {label}
                    </button>
                  ))}
                </div>
                {hint && (
                  <div className="mt-4 rounded-xl border-l-2 border-[#e7bd71] bg-[#e7bd71]/[0.06] p-4 text-sm leading-6 text-[#d8cfc0]">
                    <strong className="text-[#e7bd71]">
                      Stufe {hint.level}:
                    </strong>{' '}
                    {hint.text}
                  </div>
                )}
              </section>

              <section className="mystery-panel rounded-2xl p-5">
                <div className="flex items-center gap-3">
                  <span className="grid size-10 place-items-center rounded-xl bg-[#8f2d2d]/15 text-[#d98170]">
                    <MessageCircleQuestion size={19} />
                  </span>
                  <div>
                    <h2 className="font-bold text-[#f4ead7]">KI fragen</h2>
                    <p className="text-xs text-white/40">
                      Der Game Master kennt euren aktuellen Stand
                    </p>
                  </div>
                </div>
                <form
                  className="mt-4 flex gap-2"
                  onSubmit={(event) => {
                    event.preventDefault()
                    if (question.trim()) ask.mutate()
                  }}
                >
                  <input
                    className="mystery-input"
                    value={question}
                    maxLength={500}
                    placeholder="Können wir die Uhr genauer untersuchen?"
                    onChange={(event) => setQuestion(event.target.value)}
                  />
                  <button
                    className="mystery-icon-button"
                    disabled={ask.isPending || !question.trim()}
                    aria-label="Frage absenden"
                  >
                    {ask.isPending ? (
                      <LoaderCircle className="animate-spin" size={18} />
                    ) : (
                      <Send size={18} />
                    )}
                  </button>
                </form>
                {game.questions.length > 0 && (
                  <div className="mt-4 max-h-52 space-y-3 overflow-auto pr-1">
                    {game.questions
                      .slice()
                      .reverse()
                      .map((entry) => (
                        <div
                          key={`${entry.askedAt}-${entry.question}`}
                          className="rounded-xl bg-white/[0.035] p-3 text-sm"
                        >
                          <p className="font-semibold text-white/70">
                            {entry.question}
                          </p>
                          <p className="mt-1 leading-5 text-[#b8afa2]">
                            {entry.answer}
                          </p>
                        </div>
                      ))}
                  </div>
                )}
              </section>
            </div>
          )}

          {mutationError && <ErrorText error={mutationError} />}
        </div>

        <aside className="grid content-start gap-5">
          <EvidenceBoard game={game} />
          <section className="mystery-panel rounded-2xl p-5">
            <div className="flex items-center justify-between">
              <h2 className="flex items-center gap-2 font-bold text-[#f4ead7]">
                <NotebookPen size={17} className="text-[#e7bd71]" /> Notizen
              </h2>
              {saveNotes.isSuccess && notesText === undefined && (
                <span className="flex items-center gap-1 text-[11px] text-emerald-300/70">
                  <Check size={12} /> gespeichert
                </span>
              )}
            </div>
            <textarea
              className="mystery-notes mt-4 min-h-40 w-full"
              value={notesText ?? game.notes.join('\n')}
              placeholder="Eine Beobachtung pro Zeile …"
              onChange={(event) => {
                setNotesText(event.target.value)
              }}
            />
            <button
              type="button"
              className="mystery-button-secondary mt-3 w-full justify-center"
              disabled={notesText === undefined || saveNotes.isPending}
              onClick={() => saveNotes.mutate()}
            >
              <Clipboard size={16} /> Notizen speichern
            </button>
          </section>
        </aside>
      </main>
    </MysteryShell>
  )
}

function SceneCard({
  game,
  puzzleAnswer,
  setPuzzleAnswer,
  puzzleMessage,
  solvePuzzle,
  isSolving,
  currentChoice,
  choose,
  isChoosing,
  advance,
  isAdvancing,
}: {
  game: MysterySession
  puzzleAnswer: string
  setPuzzleAnswer: (value: string) => void
  puzzleMessage?: string
  solvePuzzle: () => void
  isSolving: boolean
  currentChoice?: string
  choose: (choiceId: string) => void
  isChoosing: boolean
  advance: () => void
  isAdvancing: boolean
}) {
  const scene = game.currentScene!
  return (
    <article className="mystery-scene overflow-hidden rounded-[1.75rem]">
      <div className="border-b border-black/10 px-5 py-4 sm:px-8">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <p className="flex items-center gap-2 text-[11px] font-black tracking-[0.22em] text-[#765f3c] uppercase">
            <SceneIcon kind={scene.kind} /> {sceneKindLabel(scene.kind)}
          </p>
          {scene.locationId && (
            <span className="flex items-center gap-1.5 rounded-full bg-[#8f2d2d]/10 px-3 py-1 text-xs font-bold text-[#8f2d2d]">
              <MapPin size={13} /> Ortswechsel
            </span>
          )}
        </div>
      </div>
      <div className="px-5 py-7 sm:px-8 sm:py-9">
        <h2 className="font-serif text-4xl text-[#241c16] sm:text-5xl">
          {scene.title}
        </h2>
        <div className="mystery-story mt-6 text-base leading-8 whitespace-pre-line text-[#483a2d] sm:text-lg">
          {scene.narrative}
        </div>
        {scene.prompt && (
          <div className="mt-7 border-l-2 border-[#8f2d2d]/60 pl-4 text-sm leading-6 font-semibold text-[#5f392d]">
            {scene.prompt}
          </div>
        )}

        {scene.puzzle && (
          <form
            className="mt-8 rounded-2xl bg-[#241c16] p-5 text-[#f4ead7] sm:p-6"
            onSubmit={(event) => {
              event.preventDefault()
              if (!scene.puzzle?.isSolved && puzzleAnswer.trim()) solvePuzzle()
            }}
          >
            <div className="flex gap-3">
              <span className="grid size-11 shrink-0 place-items-center rounded-xl bg-[#e7bd71]/10 text-[#e7bd71]">
                <KeyRound size={20} />
              </span>
              <div>
                <p className="text-[10px] font-black tracking-[0.2em] text-[#b4935d] uppercase">
                  Rätsel
                </p>
                <p className="mt-1 text-sm leading-6 text-[#d8cfc0]">
                  {scene.puzzle.prompt}
                </p>
              </div>
            </div>
            {scene.puzzle.isSolved ? (
              <div className="mt-5 flex items-center gap-2 rounded-xl bg-emerald-400/10 p-3 text-sm font-bold text-emerald-200">
                <Check size={17} /> Gelöst
              </div>
            ) : (
              <div className="mt-5 flex gap-2">
                <input
                  className="mystery-input"
                  value={puzzleAnswer}
                  inputMode={
                    scene.puzzle.inputType === 'code' ? 'numeric' : 'text'
                  }
                  placeholder={
                    scene.puzzle.inputType === 'code'
                      ? 'Code eingeben'
                      : 'Lösung eingeben'
                  }
                  onChange={(event) => setPuzzleAnswer(event.target.value)}
                />
                <button
                  className="mystery-button-primary shrink-0"
                  disabled={isSolving || !puzzleAnswer.trim()}
                >
                  Prüfen
                </button>
              </div>
            )}
            {puzzleMessage && (
              <p className="mt-3 text-sm text-[#d8cfc0]">{puzzleMessage}</p>
            )}
          </form>
        )}

        {scene.choices.length > 0 && (
          <div className="mt-8 grid gap-3">
            {scene.choices.map((choice) => {
              const selected = currentChoice === choice.id
              return (
                <button
                  type="button"
                  key={choice.id}
                  className={`group flex items-center justify-between rounded-xl border p-4 text-left text-sm font-bold transition ${
                    selected
                      ? 'border-[#8f2d2d]/50 bg-[#8f2d2d]/10 text-[#5e211f]'
                      : 'border-black/10 bg-black/[0.025] text-[#4b392a] hover:border-[#8f2d2d]/30'
                  }`}
                  disabled={isChoosing}
                  onClick={() => choose(choice.id)}
                >
                  <span>{choice.label}</span>
                  {selected ? (
                    <Check size={17} />
                  ) : (
                    <ChevronRight
                      className="opacity-40 transition group-hover:translate-x-0.5 group-hover:opacity-80"
                      size={17}
                    />
                  )}
                </button>
              )
            })}
          </div>
        )}

        <div className="mt-9 flex justify-end">
          <button
            type="button"
            className="mystery-button-dark"
            disabled={!scene.canAdvance || isAdvancing}
            onClick={advance}
          >
            {isAdvancing ? (
              <LoaderCircle className="animate-spin" size={18} />
            ) : (
              <ArrowRight size={18} />
            )}
            Szene abschließen
          </button>
        </div>
      </div>
    </article>
  )
}

function EvidenceBoard({ game }: { game: MysterySession }) {
  return (
    <section className="mystery-panel rounded-2xl p-5">
      <div className="flex items-center justify-between">
        <h2 className="flex items-center gap-2 font-bold text-[#f4ead7]">
          <Search size={17} className="text-[#e7bd71]" /> Ermittlungswand
        </h2>
        <span className="text-xs text-white/35">
          {game.evidence.length} Spuren
        </span>
      </div>
      <div className="mt-4 grid gap-3">
        {game.evidence.length === 0 && (
          <p className="rounded-xl border border-dashed border-white/10 p-4 text-sm text-white/35">
            Noch keine gesicherten Beweise.
          </p>
        )}
        {game.evidence.map((evidence, index) => (
          <article
            className={`mystery-evidence-note rounded-sm p-4 ${index % 2 ? '-rotate-[0.4deg]' : 'rotate-[0.35deg]'}`}
            key={evidence.id}
          >
            <p className="text-xs font-black tracking-wide text-[#493729] uppercase">
              {evidence.title}
            </p>
            <p className="mt-2 text-xs leading-5 text-[#675443]">
              {evidence.description}
            </p>
          </article>
        ))}
      </div>

      <h3 className="mt-6 flex items-center gap-2 text-xs font-black tracking-[0.15em] text-white/45 uppercase">
        <Users size={14} /> Bekannte Personen
      </h3>
      <div className="mt-3 grid gap-2">
        {game.characters.map((character) => (
          <details
            key={character.id}
            className="rounded-xl border border-white/[0.07] bg-white/[0.025] px-3 py-2.5 text-sm"
          >
            <summary className="cursor-pointer font-bold text-white/75">
              {character.name}{' '}
              <span className="font-normal text-white/35">
                · {character.role}
              </span>
            </summary>
            <p className="mt-2 text-xs leading-5 text-white/45">
              {character.description}
            </p>
          </details>
        ))}
      </div>
    </section>
  )
}

function FinalTheory({
  game,
  updateSession,
}: {
  game: MysterySession
  updateSession: (session: MysterySession) => void
}) {
  const [culpritId, setCulpritId] = useState(game.characters[0]?.id ?? '')
  const [motive, setMotive] = useState('')
  const [sequence, setSequence] = useState('')
  const submit = useMutation({
    mutationFn: () =>
      submitMysteryFinale(game.id, {
        culpritId,
        motive,
        sequence: sequence || undefined,
        version: game.version,
      }),
    onSuccess: updateSession,
  })

  return (
    <section className="mystery-scene rounded-[1.75rem] p-6 sm:p-10">
      <div className="mx-auto max-w-2xl text-center">
        <span className="mx-auto grid size-16 place-items-center rounded-full border border-[#8f2d2d]/30 bg-[#8f2d2d]/10 text-[#8f2d2d]">
          <Brain size={28} />
        </span>
        <p className="mt-6 text-xs font-black tracking-[0.25em] text-[#8f2d2d] uppercase">
          Finale
        </p>
        <h2 className="mt-2 font-serif text-5xl text-[#241c16]">
          Was ist eure Theorie?
        </h2>
        <p className="mt-4 text-sm leading-6 text-[#675443]">
          Erst nach dem Absenden öffnet der Game Master die versiegelte
          Auflösung. Sprecht euch vorher gemeinsam ab.
        </p>
      </div>
      <form
        className="mx-auto mt-9 grid max-w-2xl gap-5"
        onSubmit={(event) => {
          event.preventDefault()
          submit.mutate()
        }}
      >
        <DossierField label="Wer war es?">
          <select
            className="mystery-paper-input"
            value={culpritId}
            onChange={(event) => setCulpritId(event.target.value)}
          >
            {game.characters.map((character) => (
              <option value={character.id} key={character.id}>
                {character.name} · {character.role}
              </option>
            ))}
          </select>
        </DossierField>
        <DossierField label="Was war das Motiv?">
          <textarea
            className="mystery-paper-input min-h-28"
            value={motive}
            maxLength={1200}
            onChange={(event) => setMotive(event.target.value)}
            required
          />
        </DossierField>
        <DossierField label="Wie lief die Tat ab?" hint="Optional">
          <textarea
            className="mystery-paper-input min-h-28"
            value={sequence}
            maxLength={1800}
            onChange={(event) => setSequence(event.target.value)}
          />
        </DossierField>
        {submit.error && <ErrorText error={submit.error} dark />}
        <button
          className="mystery-button-dark mt-2 justify-center py-4"
          disabled={!culpritId || !motive.trim() || submit.isPending}
        >
          {submit.isPending ? (
            <LoaderCircle className="animate-spin" size={19} />
          ) : (
            <Fingerprint size={19} />
          )}
          Versiegelte Auflösung öffnen
        </button>
      </form>
    </section>
  )
}

function Resolution({ game }: { game: MysterySession }) {
  const finale = game.finale!
  return (
    <section className="mystery-resolution overflow-hidden rounded-[1.75rem]">
      <div className="border-b border-white/10 p-7 text-center sm:p-10">
        <p className="text-xs font-black tracking-[0.28em] text-[#b4935d] uppercase">
          Fall geschlossen
        </p>
        <h2 className="mt-3 font-serif text-5xl text-[#f4ead7] sm:text-6xl">
          {finale.culpritName}
        </h2>
        <p
          className={`mt-5 inline-flex rounded-full px-4 py-2 text-sm font-bold ${
            finale.correctCulprit
              ? 'bg-emerald-400/10 text-emerald-200'
              : 'bg-[#8f2d2d]/20 text-[#efb1a5]'
          }`}
        >
          {finale.correctCulprit
            ? 'Eure Tätertheorie war richtig.'
            : 'Der Täter war jemand anderes.'}
        </p>
      </div>
      <div className="grid gap-7 p-6 sm:p-10 lg:grid-cols-[1fr_15rem]">
        <div className="space-y-7 text-[#d8cfc0]">
          <RevealSection title="Motiv" text={finale.motive} />
          <RevealSection title="Tathergang" text={finale.timeline} />
          <RevealSection title="Auflösung" text={finale.resolution} />
          {finale.redHerrings.length > 0 && (
            <div>
              <h3 className="text-xs font-black tracking-[0.2em] text-[#b4935d] uppercase">
                Falsche Fährten
              </h3>
              <div className="mt-3 flex flex-wrap gap-2">
                {finale.redHerrings.map((item) => (
                  <span
                    key={item}
                    className="rounded-full border border-white/10 px-3 py-1.5 text-xs text-white/55"
                  >
                    {item}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>
        <aside className="rounded-2xl border border-[#b4935d]/25 bg-[#b4935d]/[0.06] p-5 text-center">
          <p className="text-[10px] font-black tracking-[0.2em] text-[#b4935d] uppercase">
            Ermittlungs-Score
          </p>
          <p className="mt-3 font-serif text-6xl text-[#f4ead7]">
            {finale.score}
          </p>
          <p className="mt-2 text-xs text-white/40">
            {finale.usedHints} Hinweise verwendet
          </p>
          <Link
            to="/mistery"
            className="mystery-button-secondary mt-6 w-full justify-center"
          >
            <RotateCcw size={16} /> Neuer Fall
          </Link>
        </aside>
      </div>
    </section>
  )
}

function RevealSection({ title, text }: { title: string; text: string }) {
  return (
    <div>
      <h3 className="text-xs font-black tracking-[0.2em] text-[#b4935d] uppercase">
        {title}
      </h3>
      <p className="mt-2 text-sm leading-7 whitespace-pre-line">{text}</p>
    </div>
  )
}

function MysteryShell({ children }: { children: ReactNode }) {
  return (
    <div className="mystery-surface min-h-screen px-4 py-5 text-[#f4ead7] sm:px-7">
      <div className="mystery-grain pointer-events-none fixed inset-0" />
      <div className="relative">{children}</div>
    </div>
  )
}

function MysteryMessage({ text, error }: { text: string; error?: boolean }) {
  return (
    <MysteryShell>
      <div className="grid min-h-[85vh] place-items-center text-center">
        <div>
          <span className="mx-auto grid size-20 place-items-center rounded-full border border-white/10 bg-white/[0.04]">
            {error ? (
              <TriangleAlert className="text-[#d98170]" size={30} />
            ) : (
              <LoaderCircle className="animate-spin text-[#e7bd71]" size={30} />
            )}
          </span>
          <p className="mt-5 text-sm text-[#b8afa2]">{text}</p>
          {error && (
            <Link
              to="/mistery"
              className="mt-5 inline-flex items-center gap-2 text-sm font-bold text-[#e7bd71]"
            >
              <ArrowLeft size={15} /> Zur Startseite
            </Link>
          )}
        </div>
      </div>
    </MysteryShell>
  )
}

function DossierField({
  label,
  hint,
  children,
}: {
  label: string
  hint?: string
  children: ReactNode
}) {
  return (
    <label className="block">
      <span className="flex items-center justify-between text-sm font-black text-[#3f3125]">
        {label}
        {hint && (
          <span className="text-xs font-normal text-[#806b55]">{hint}</span>
        )}
      </span>
      <span className="mt-2 block">{children}</span>
    </label>
  )
}

function ErrorText({ error, dark }: { error: Error; dark?: boolean }) {
  const message =
    error instanceof ApiError && error.status === 409
      ? 'Der Spielstand hat sich geändert. Der aktuelle Stand wird neu geladen.'
      : error.message
  return (
    <p
      className={`mt-3 flex items-start gap-2 text-sm ${dark ? 'text-[#8f2d2d]' : 'text-[#efb1a5]'}`}
    >
      <TriangleAlert className="mt-0.5 shrink-0" size={15} /> {message}
    </p>
  )
}

function SceneIcon({ kind }: { kind: string }) {
  if (kind === 'Puzzle') return <KeyRound size={14} />
  if (kind === 'Decision') return <Brain size={14} />
  if (kind === 'LocationChange') return <MapPin size={14} />
  if (kind === 'Dialogue') return <MessageCircleQuestion size={14} />
  if (kind === 'Evidence') return <Search size={14} />
  if (kind === 'RealTask') return <Fingerprint size={14} />
  return <BookOpen size={14} />
}

function sceneKindLabel(kind: string) {
  const labels: Record<string, string> = {
    Story: 'Szene',
    Dialogue: 'Befragung',
    Evidence: 'Neue Beweise',
    Puzzle: 'Rätsel',
    Decision: 'Entscheidung',
    RealTask: 'Reale Aufgabe',
    LocationChange: 'Geheimer Ortswechsel',
  }
  return labels[kind] ?? kind
}

function splitValues(value: string) {
  return value
    .split(/[\n,]/)
    .map((item) => item.trim())
    .filter(Boolean)
}

function updateLocation(
  setLocations: Dispatch<SetStateAction<LocationDraft[]>>,
  index: number,
  patch: Partial<LocationDraft>,
) {
  setLocations((current) =>
    current.map((location, itemIndex) =>
      itemIndex === index ? { ...location, ...patch } : location,
    ),
  )
}

function splitLines(value: string) {
  return value
    .split('\n')
    .map((item) => item.trim())
    .filter(Boolean)
}
