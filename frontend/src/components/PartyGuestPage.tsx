import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  BookHeart,
  Camera,
  CheckCircle2,
  GlassWater,
  ImagePlus,
  Music2,
  PartyPopper,
  Send,
  Upload,
} from 'lucide-react'
import { useParams } from 'react-router-dom'
import {
  addGuestbookEntry,
  addMusicRequest,
  createGuestOrder,
  downloadGuestMedia,
  getPublicParty,
  listGuestbook,
  listGuestMedia,
  registerPartyGuest,
  uploadGuestMedia,
  type PartyMedia,
} from '../api/parties'
import { prepareScreenshot } from '../imageProcessing'

type View = 'home' | 'media' | 'order' | 'music' | 'guestbook'

interface StoredGuest {
  name: string
  token: string
}

export function PartyGuestPage() {
  const { slug = '' } = useParams()
  const [guest, setGuest] = useState<StoredGuest | undefined>(() =>
    readGuest(slug),
  )
  const [view, setView] = useState<View>('home')
  const party = useQuery({
    queryKey: ['public-party', slug],
    queryFn: () => getPublicParty(slug),
    enabled: Boolean(slug),
    retry: false,
  })

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

  if (party.isPending) return <PartyMessage text="Party wird geladen …" />
  if (party.error)
    return <PartyMessage text="Diese Party wurde nicht gefunden." />
  if (!party.data.isActive)
    return <PartyMessage text="Diese Party ist aktuell nicht aktiv. 🎈" />
  if (!guest) {
    return (
      <GuestWelcome
        partyName={party.data.name}
        welcomeText={party.data.welcomeText}
        onRegistered={(next) => {
          storeGuest(slug, next)
          setGuest(next)
        }}
        slug={slug}
      />
    )
  }

  return (
    <PartyFrame>
      {view === 'home' ? (
        <PartyHome
          partyName={party.data.name}
          name={guest.name}
          welcomeText={party.data.welcomeText}
          setView={setView}
        />
      ) : (
        <div>
          <button
            type="button"
            className="party-back"
            onClick={() => setView('home')}
          >
            <ArrowLeft size={18} /> Zurück
          </button>
          {view === 'order' && (
            <OrderView
              slug={slug}
              token={guest.token}
              items={party.data.orderItems}
            />
          )}
          {view === 'media' && (
            <MediaView
              slug={slug}
              token={guest.token}
              canViewGallery={party.data.guestsCanViewGallery}
            />
          )}
          {view === 'music' && <MusicView slug={slug} token={guest.token} />}
          {view === 'guestbook' && (
            <GuestbookView
              slug={slug}
              token={guest.token}
              canRead={party.data.guestsCanViewGuestbook}
            />
          )}
        </div>
      )}
      <p className="mt-10 text-center text-[11px] leading-5 text-white/45">
        Mit dem Upload erklärst du dich damit einverstanden, dass deine Medien
        im privaten Rahmen dieser Feier gespeichert und mit den Teilnehmern
        geteilt werden können.
      </p>
    </PartyFrame>
  )
}

function GuestWelcome({
  partyName,
  welcomeText,
  slug,
  onRegistered,
}: {
  partyName: string
  welcomeText?: string
  slug: string
  onRegistered: (guest: StoredGuest) => void
}) {
  const [name, setName] = useState('')
  const mutation = useMutation({
    mutationFn: () => registerPartyGuest(slug, name),
    onSuccess: (session) =>
      onRegistered({ name: session.name, token: session.sessionToken }),
  })
  return (
    <PartyFrame>
      <div className="py-10 text-center">
        <span className="mx-auto flex size-20 items-center justify-center rounded-full bg-white/15 text-4xl shadow-xl">
          🎉
        </span>
        <p className="mt-8 text-sm font-bold tracking-[0.18em] text-amber-200 uppercase">
          Willkommen bei
        </p>
        <h1 className="mt-2 text-4xl font-black tracking-tight text-white">
          {partyName}
        </h1>
        {welcomeText && (
          <p className="mx-auto mt-4 max-w-sm text-sm leading-6 text-white/70">
            {welcomeText}
          </p>
        )}
        <form
          className="mx-auto mt-10 max-w-sm text-left"
          onSubmit={(event) => {
            event.preventDefault()
            mutation.mutate()
          }}
        >
          <label
            className="text-sm font-semibold text-white"
            htmlFor="party-name"
          >
            Wie heißt du?
          </label>
          <input
            id="party-name"
            autoFocus
            maxLength={100}
            value={name}
            onChange={(event) => setName(event.target.value)}
            className="party-input mt-2"
            placeholder="Dein Name"
          />
          {mutation.error && (
            <p className="mt-2 text-sm text-rose-200">
              {mutation.error.message}
            </p>
          )}
          <button
            className="party-primary mt-4 w-full"
            disabled={!name.trim() || mutation.isPending}
          >
            {mutation.isPending ? 'Einen Moment …' : 'Party öffnen 🎈'}
          </button>
        </form>
      </div>
    </PartyFrame>
  )
}

function PartyHome({
  partyName,
  name,
  welcomeText,
  setView,
}: {
  partyName: string
  name: string
  welcomeText?: string
  setView: (view: View) => void
}) {
  return (
    <div className="py-5">
      <div className="text-center">
        <PartyPopper className="mx-auto text-amber-200" size={36} />
        <h1 className="mt-4 text-3xl font-black tracking-tight text-white">
          {partyName}
        </h1>
        <p className="mt-2 text-white/75">Schön, dass du da bist, {name}!</p>
        {welcomeText && (
          <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-white/55">
            {welcomeText}
          </p>
        )}
      </div>
      <div className="mt-8 grid gap-3">
        <PartyTile
          icon={<Camera />}
          label="Fotos & Videos"
          onClick={() => setView('media')}
        />
        <PartyTile
          icon={<GlassWater />}
          label="Getränk bestellen"
          onClick={() => setView('order')}
        />
        <PartyTile
          icon={<Music2 />}
          label="Musikwunsch"
          onClick={() => setView('music')}
        />
        <PartyTile
          icon={<BookHeart />}
          label="Gästebuch"
          onClick={() => setView('guestbook')}
        />
      </div>
    </div>
  )
}

function OrderView({
  slug,
  token,
  items,
}: {
  slug: string
  token: string
  items: { id: string; name: string; icon?: string; isActive: boolean }[]
}) {
  const [custom, setCustom] = useState('')
  const [confirmation, setConfirmation] = useState<string>()
  const mutation = useMutation({
    mutationFn: ({ id, customText }: { id?: string; customText?: string }) =>
      createGuestOrder(slug, token, id, customText),
    onSuccess: (result) => {
      setConfirmation(`Alles klar, ${result.item} ist unterwegs!`)
      setCustom('')
      window.setTimeout(() => setConfirmation(undefined), 3500)
    },
  })
  return (
    <section>
      <PartyTitle
        icon="🍹"
        title="Was darf's sein?"
        text="Ein Tipp = ein Wunsch. Kein Warenkorb, kein Stress."
      />
      {confirmation && (
        <div className="party-success">
          <CheckCircle2 size={20} /> {confirmation}
        </div>
      )}
      <div className="grid grid-cols-2 gap-3">
        {items
          .filter((item) => item.isActive)
          .map((item) => (
            <button
              key={item.id}
              type="button"
              className="party-choice"
              disabled={mutation.isPending}
              onClick={() => mutation.mutate({ id: item.id })}
            >
              <span className="text-3xl">{item.icon ?? '🥤'}</span>
              <span>{item.name}</span>
            </button>
          ))}
      </div>
      <div className="mt-5 flex gap-2">
        <input
          className="party-input"
          maxLength={160}
          placeholder="Sonstiges …"
          value={custom}
          onChange={(event) => setCustom(event.target.value)}
        />
        <button
          className="party-icon-button"
          type="button"
          aria-label="Wunsch senden"
          disabled={!custom.trim() || mutation.isPending}
          onClick={() => mutation.mutate({ customText: custom })}
        >
          <Send size={20} />
        </button>
      </div>
      {mutation.error && <PartyError error={mutation.error} />}
    </section>
  )
}

function MediaView({
  slug,
  token,
  canViewGallery,
}: {
  slug: string
  token: string
  canViewGallery: boolean
}) {
  const queryClient = useQueryClient()
  const [file, setFile] = useState<File>()
  const [caption, setCaption] = useState('')
  const gallery = useQuery({
    queryKey: ['party-media', slug],
    queryFn: () => listGuestMedia(slug, token),
    enabled: canViewGallery,
    retry: false,
  })
  const upload = useMutation({
    mutationFn: async () => {
      if (!file) throw new Error('Bitte wähle zuerst eine Datei aus.')
      const prepared = file.type.startsWith('image/')
        ? (await prepareScreenshot(file)).file
        : file
      return uploadGuestMedia(slug, token, prepared, caption)
    },
    onSuccess: async () => {
      setFile(undefined)
      setCaption('')
      await queryClient.invalidateQueries({ queryKey: ['party-media', slug] })
    },
  })
  return (
    <section>
      <PartyTitle
        icon="📸"
        title="Fotos & Videos"
        text="Lad deinen besten Schnappschuss oder ein kurzes Video hoch."
      />
      <div className="party-card">
        <label className="flex min-h-28 cursor-pointer flex-col items-center justify-center rounded-2xl border border-dashed border-white/25 bg-white/5 p-4 text-center">
          <ImagePlus size={28} className="text-amber-200" />
          <span className="mt-2 text-sm font-semibold text-white">
            {file ? file.name : 'Foto oder Video auswählen'}
          </span>
          <span className="mt-1 text-xs text-white/45">
            Fotos bis 12 MB · Videos bis 100 MB
          </span>
          <input
            type="file"
            className="hidden"
            accept="image/jpeg,image/png,image/webp,image/gif,video/mp4,video/quicktime,video/webm"
            onChange={(event) => setFile(event.target.files?.[0])}
          />
        </label>
        <input
          className="party-input mt-3"
          maxLength={500}
          placeholder="Kommentar (optional)"
          value={caption}
          onChange={(event) => setCaption(event.target.value)}
        />
        <button
          className="party-primary mt-3 w-full"
          disabled={!file || upload.isPending}
          onClick={() => upload.mutate()}
        >
          <Upload size={18} />{' '}
          {upload.isPending ? 'Wird hochgeladen …' : 'Hochladen'}
        </button>
        {upload.error && <PartyError error={upload.error} />}
      </div>
      {canViewGallery && (
        <div className="mt-8">
          <h2 className="mb-3 text-lg font-bold text-white">Galerie</h2>
          {gallery.isPending && (
            <p className="text-sm text-white/50">Galerie wird geladen …</p>
          )}
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
            {gallery.data?.map((media) => (
              <PartyMediaPreview key={media.id} media={media} token={token} />
            ))}
          </div>
        </div>
      )}
    </section>
  )
}

function PartyMediaPreview({
  media,
  token,
}: {
  media: PartyMedia
  token: string
}) {
  const blob = useQuery({
    queryKey: ['party-media-blob', media.id],
    queryFn: async () =>
      URL.createObjectURL(await downloadGuestMedia(media.contentUrl, token)),
    staleTime: Number.POSITIVE_INFINITY,
  })
  if (!blob.data)
    return (
      <div className="aspect-square animate-pulse rounded-xl bg-white/10" />
    )
  return (
    <div className="overflow-hidden rounded-xl bg-black/30">
      {media.mediaType === 'video' ? (
        <video
          controls
          playsInline
          className="aspect-square h-full w-full object-cover"
          src={blob.data}
        />
      ) : (
        <img
          className="aspect-square h-full w-full object-cover"
          src={blob.data}
          alt={media.caption ?? `Foto von ${media.guestName}`}
        />
      )}
    </div>
  )
}

function MusicView({ slug, token }: { slug: string; token: string }) {
  const [song, setSong] = useState('')
  const [artist, setArtist] = useState('')
  const [sent, setSent] = useState(false)
  const mutation = useMutation({
    mutationFn: () =>
      addMusicRequest(slug, token, { song, artist: artist || undefined }),
    onSuccess: () => {
      setSong('')
      setArtist('')
      setSent(true)
    },
  })
  return (
    <section>
      <PartyTitle
        icon="🎵"
        title="Musikwunsch"
        text="Welcher Song fehlt heute noch?"
      />
      {sent && (
        <div className="party-success">
          <CheckCircle2 size={20} /> Wunsch ist notiert!
        </div>
      )}
      <div className="party-card grid gap-3">
        <input
          className="party-input"
          maxLength={200}
          placeholder="Song *"
          value={song}
          onChange={(event) => setSong(event.target.value)}
        />
        <input
          className="party-input"
          maxLength={200}
          placeholder="Künstler (optional)"
          value={artist}
          onChange={(event) => setArtist(event.target.value)}
        />
        <button
          className="party-primary"
          disabled={!song.trim() || mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          <Music2 size={18} /> Wunsch senden
        </button>
      </div>
      {mutation.error && <PartyError error={mutation.error} />}
    </section>
  )
}

function GuestbookView({
  slug,
  token,
  canRead,
}: {
  slug: string
  token: string
  canRead: boolean
}) {
  const queryClient = useQueryClient()
  const [message, setMessage] = useState('')
  const entries = useQuery({
    queryKey: ['party-guestbook', slug],
    queryFn: () => listGuestbook(slug, token),
    enabled: canRead,
  })
  const mutation = useMutation({
    mutationFn: () => addGuestbookEntry(slug, token, message),
    onSuccess: async () => {
      setMessage('')
      await queryClient.invalidateQueries({
        queryKey: ['party-guestbook', slug],
      })
    },
  })
  return (
    <section>
      <PartyTitle
        icon="💌"
        title="Gästebuch"
        text="Lass ein paar Worte für später da."
      />
      <div className="party-card">
        <textarea
          className="party-input min-h-28 resize-y py-3"
          maxLength={1000}
          placeholder="Deine Nachricht …"
          value={message}
          onChange={(event) => setMessage(event.target.value)}
        />
        <button
          className="party-primary mt-3 w-full"
          disabled={!message.trim() || mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          <Send size={18} /> Eintragen
        </button>
      </div>
      {canRead && (
        <div className="mt-6 grid gap-3">
          {entries.data?.map((entry) => (
            <div key={entry.id} className="party-card">
              <p className="text-sm leading-6 text-white/85">{entry.message}</p>
              <p className="mt-2 text-xs font-semibold text-amber-200">
                {entry.guestName}
              </p>
            </div>
          ))}
        </div>
      )}
    </section>
  )
}

function PartyFrame({ children }: { children: React.ReactNode }) {
  return (
    <div className="party-surface min-h-screen px-4 py-6 text-white">
      <main className="mx-auto max-w-lg">{children}</main>
    </div>
  )
}

function PartyTile({
  icon,
  label,
  onClick,
}: {
  icon: React.ReactNode
  label: string
  onClick: () => void
}) {
  return (
    <button type="button" className="party-tile" onClick={onClick}>
      <span className="party-tile-icon">{icon}</span>
      <span>{label}</span>
    </button>
  )
}

function PartyTitle({
  icon,
  title,
  text,
}: {
  icon: string
  title: string
  text: string
}) {
  return (
    <div className="mb-6 text-center">
      <span className="text-4xl">{icon}</span>
      <h1 className="mt-3 text-3xl font-black text-white">{title}</h1>
      <p className="mt-2 text-sm leading-6 text-white/60">{text}</p>
    </div>
  )
}

function PartyError({ error }: { error: Error }) {
  return (
    <p className="mt-3 rounded-xl bg-rose-500/15 p-3 text-sm text-rose-100">
      {error.message}
    </p>
  )
}

function PartyMessage({ text }: { text: string }) {
  return (
    <PartyFrame>
      <div className="flex min-h-[75vh] flex-col items-center justify-center text-center">
        <PartyPopper size={42} className="text-amber-200" />
        <p className="mt-5 text-lg font-bold">{text}</p>
      </div>
    </PartyFrame>
  )
}

function storageKey(slug: string) {
  return `community-party-guest:${slug}`
}
function readGuest(slug: string): StoredGuest | undefined {
  if (!slug) return undefined
  try {
    return JSON.parse(
      localStorage.getItem(storageKey(slug)) ?? '',
    ) as StoredGuest
  } catch {
    return undefined
  }
}
function storeGuest(slug: string, guest: StoredGuest) {
  localStorage.setItem(storageKey(slug), JSON.stringify(guest))
}
