import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  BookHeart,
  Camera,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Download,
  GlassWater,
  Hand,
  Heart,
  House,
  ImagePlus,
  Images,
  ListChecks,
  Music2,
  PartyPopper,
  Send,
  Trash2,
  Upload,
  UserRound,
  X,
} from 'lucide-react'
import { useParams } from 'react-router-dom'
import { ApiError } from '../api/client'
import {
  addGuestbookEntry,
  addMusicRequest,
  cancelGuestOrder,
  claimGuestOrders,
  completeGuestOrder,
  createGuestOrder,
  downloadGuestMedia,
  getPartyFeed,
  getPartyGuest,
  getPartyPulse,
  getGuestSpotifyStatus,
  getPublicParty,
  listGuestMusicRequests,
  listGuestOrders,
  listGuestbook,
  listGuestMedia,
  releaseGuestOrder,
  listOwnGuestMedia,
  listOwnMusicRequests,
  registerPartyGuest,
  searchPartySpotify,
  toggleGuestMusicVote,
  toggleGuestMediaLike,
  updatePartyGuest,
  uploadGuestMedia,
  type PartyFeedItem,
  type PartyMedia,
  type PartyOrder,
  type PartyPulse,
  type SpotifyTrack,
} from '../api/parties'
import { prepareScreenshot } from '../imageProcessing'

type View = 'home' | 'media' | 'order' | 'music' | 'guestbook' | 'me'

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
  const [seenMediaAt, setSeenMediaAt] = useState<string | undefined>(() =>
    readMediaSeenAt(slug),
  )
  const party = useQuery({
    queryKey: ['public-party', slug],
    queryFn: () => getPublicParty(slug),
    enabled: Boolean(slug),
    retry: false,
  })
  const me = useQuery({
    queryKey: ['party-me', slug, guest?.token],
    queryFn: () => getPartyGuest(slug, guest!.token),
    enabled: Boolean(slug && guest?.token),
    retry: false,
  })
  const orders = useQuery({
    queryKey: ['party-guest-orders', slug],
    queryFn: () => listGuestOrders(slug, guest!.token),
    enabled: Boolean(slug && guest?.token),
    refetchInterval: 3_500,
  })
  const pulse = useQuery({
    queryKey: ['party-pulse', slug],
    queryFn: () => getPartyPulse(slug, guest!.token),
    enabled: Boolean(slug && guest?.token),
    refetchInterval: 4_000,
  })
  const feed = useQuery({
    queryKey: ['party-feed', slug],
    queryFn: () => getPartyFeed(slug, guest!.token),
    enabled: Boolean(slug && guest?.token),
    refetchInterval: 7_000,
  })
  const media = useQuery({
    queryKey: ['party-media', slug],
    queryFn: () => listGuestMedia(slug, guest!.token),
    enabled: Boolean(slug && guest?.token && party.data?.guestsCanViewGallery),
    refetchInterval: 10_000,
  })

  const markMediaSeen = () => {
    const latest = media.data?.[0]?.createdAt
    if (!latest) return
    storeMediaSeenAt(slug, latest)
    setSeenMediaAt(latest)
  }
  const changeView = (next: View) => {
    if (next === 'media') markMediaSeen()
    setView(next)
  }
  const mediaBadge =
    view === 'media'
      ? 0
      : media.data
        ? seenMediaAt
          ? media.data.filter(
              (item) => new Date(item.createdAt) > new Date(seenMediaAt),
            ).length
          : media.data.length
        : seenMediaAt
          ? 0
          : pulse.data?.mediaCount

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

  useEffect(() => {
    if (!slug) return
    const manifest = document.querySelector<HTMLLinkElement>('link[rel="manifest"]')
    if (!manifest) return
    const previous = manifest.href
    manifest.href = `/api/parties/public/${encodeURIComponent(slug)}/manifest.webmanifest`
    return () => {
      manifest.href = previous
    }
  }, [slug])

  if (party.isPending) return <PartyMessage text="Party wird geladen …" />
  if (party.error)
    return <PartyMessage text="Diese Party wurde nicht gefunden." />
  if (!party.data.isActive)
    return <PartyMessage text="Diese Party ist aktuell nicht aktiv. 🎈" />
  if (guest && me.error instanceof ApiError && me.error.status === 401) {
    return (
      <GuestWelcome
        partyName={party.data.name}
        welcomeText="Deine alte Party-Session ist abgelaufen. Sag uns kurz nochmal, wer du bist."
        onRegistered={(next) => {
          storeGuest(slug, next)
          setGuest(next)
        }}
        slug={slug}
      />
    )
  }
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
      <div className="pb-24">
      {view === 'home' && (
        <PartyHome
          slug={slug}
          partyName={party.data.name}
          name={guest.name}
          token={guest.token}
          guestId={me.data?.id}
          welcomeText={party.data.welcomeText}
          setView={changeView}
          pulse={pulse.data}
          mediaBadge={mediaBadge}
          feed={feed.data ?? []}
          orders={orders.data ?? []}
          latestMedia={media.data?.[0]}
        />
      )}
      {view === 'order' && (
        <OrderView
          slug={slug}
          token={guest.token}
          guestId={me.data?.id}
          items={party.data.orderItems}
          orders={orders.data ?? []}
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
      {view === 'me' && (
        <MeView
          slug={slug}
          guest={guest}
          guestId={me.data?.id}
          firstSeenAt={me.data?.firstSeenAt}
          orders={orders.data ?? []}
          onRenamed={(name) => {
            const next = { ...guest, name }
            storeGuest(slug, next)
            setGuest(next)
          }}
        />
      )}
      <p className="mt-10 text-center text-[11px] leading-5 text-white/45">
        Mit dem Upload erklärst du dich damit einverstanden, dass deine Medien
        im privaten Rahmen dieser Feier gespeichert und mit den Teilnehmern
        geteilt werden können.
      </p>
      </div>
      <PartyBottomNavigation
        view={view}
        setView={changeView}
        pulse={pulse.data}
        mediaBadge={mediaBadge}
      />
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
  slug,
  partyName,
  name,
  token,
  guestId,
  welcomeText,
  setView,
  pulse,
  mediaBadge,
  feed,
  orders,
  latestMedia,
}: {
  slug: string
  partyName: string
  name: string
  token: string
  guestId?: string
  welcomeText?: string
  setView: (view: View) => void
  pulse?: PartyPulse
  mediaBadge?: number
  feed: PartyFeedItem[]
  orders: PartyOrder[]
  latestMedia?: PartyMedia
}) {
  const queryClient = useQueryClient()
  const openOrders = orders.filter((order) => order.status === 'Open')
  const activity = feed.filter(
    (item) => item.type !== 'order' && item.type !== 'claim',
  )
  const refreshLive = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['party-guest-orders', slug] }),
      queryClient.invalidateQueries({ queryKey: ['party-pulse', slug] }),
      queryClient.invalidateQueries({ queryKey: ['party-feed', slug] }),
    ])
  }
  const claim = useMutation({
    mutationFn: (orderId: string) =>
      claimGuestOrders(slug, token, [orderId]),
    onSuccess: refreshLive,
  })
  const complete = useMutation({
    mutationFn: (orderId: string) => completeGuestOrder(slug, token, orderId),
    onSuccess: refreshLive,
  })

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
      {pulse && (
        <div className="mt-6 grid grid-cols-3 gap-2">
          <PartyStat icon="👥" value={pulse.guestCount} label="Gäste" />
          <PartyStat icon="🍹" value={pulse.openOrderCount} label="offen" />
          <PartyStat icon="📸" value={pulse.mediaCount} label="Medien" />
        </div>
      )}
      {pulse?.topDrinkName && (
        <div className="party-card mt-3 flex items-center gap-3 py-3">
          <span className="text-2xl">🔥</span>
          <p className="text-sm text-white/65">
            Most wanted: <strong className="text-white">{pulse.topDrinkName}</strong>{' '}
            <span className="text-white/40">· {pulse.topDrinkCount}× bestellt</span>
          </p>
        </div>
      )}
      <div className="mt-8 grid gap-3">
        <PartyTile
          icon={<Camera />}
          label="Fotos & Videos"
          badge={mediaBadge}
          onClick={() => setView('media')}
        />
        <PartyTile
          icon={<GlassWater />}
          label="Drinks & Runde holen"
          badge={pulse?.unclaimedOrderCount}
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
      {latestMedia && (
        <section className="mt-8">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="font-black text-white">Neuester Moment</h2>
            <button
              type="button"
              className="text-xs font-bold text-amber-200"
              onClick={() => setView('media')}
            >
              Galerie öffnen
            </button>
          </div>
          <div className="mx-auto max-w-sm">
            <PartyMediaPreview media={latestMedia} token={token} featured />
          </div>
        </section>
      )}
      {(openOrders.length > 0 || activity.length > 0) && (
        <section className="mt-8">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="font-black text-white">Gerade auf der Party</h2>
            <span className="party-live-dot">LIVE</span>
          </div>
          <div className="party-card grid gap-3">
            {openOrders.slice(0, 6).map((order) => {
              const claimedByMe = order.claimedByGuestId === guestId
              const canClaim =
                Boolean(guestId) &&
                order.guestId !== guestId &&
                !order.claimedByGuestId
              return (
                <div
                  key={`live-order-${order.id}`}
                  className="flex items-center gap-3 text-sm"
                >
                  <span className="text-xl">{order.icon ?? '🍹'}</span>
                  <div className="min-w-0 flex-1">
                    <p className="text-white/80">
                      {order.claimedByGuestName
                        ? `${order.claimedByGuestName} bringt ${order.guestName} ${order.itemName ?? order.customText ?? 'einen Drink'}.`
                        : `${order.guestName} möchte ${order.itemName ?? order.customText ?? 'etwas zu trinken'}.`}
                    </p>
                    <p className="mt-0.5 text-[11px] text-white/35">
                      {claimedByMe ? 'Von dir übernommen · ' : ''}
                      {relativeTime(order.claimedAt ?? order.createdAt)}
                    </p>
                  </div>
                  {claimedByMe && (
                    <button
                      type="button"
                      className="party-done-button shrink-0"
                      disabled={complete.isPending}
                      onClick={() => complete.mutate(order.id)}
                    >
                      <CheckCircle2 size={15} /> Übergeben
                    </button>
                  )}
                  {canClaim && (
                    <button
                      type="button"
                      className="party-small-button shrink-0"
                      disabled={claim.isPending}
                      onClick={() => claim.mutate(order.id)}
                    >
                      <Hand size={15} /> Übernehmen
                    </button>
                  )}
                </div>
              )
            })}
            {activity.slice(0, Math.max(0, 6 - openOrders.length)).map((item, index) => (
              <div
                key={`${item.type}-${item.createdAt}-${index}`}
                className="flex gap-3 text-sm"
              >
                <span className="text-xl">{item.emoji}</span>
                <div className="min-w-0">
                  <p className="text-white/80">{item.text}</p>
                  <p className="mt-0.5 text-[11px] text-white/35">
                    {relativeTime(item.createdAt)}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}
    </div>
  )
}

function OrderView({
  slug,
  token,
  guestId,
  items,
  orders,
}: {
  slug: string
  token: string
  guestId?: string
  items: { id: string; name: string; icon?: string; isActive: boolean }[]
  orders: PartyOrder[]
}) {
  const queryClient = useQueryClient()
  const [custom, setCustom] = useState('')
  const [confirmation, setConfirmation] = useState<string>()
  const [roundIds, setRoundIds] = useState<string[]>([])
  const refreshOrders = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['party-guest-orders', slug] }),
      queryClient.invalidateQueries({ queryKey: ['party-pulse', slug] }),
      queryClient.invalidateQueries({ queryKey: ['party-feed', slug] }),
    ])
  }
  const create = useMutation({
    mutationFn: ({ id, customText }: { id?: string; customText?: string }) =>
      createGuestOrder(slug, token, id, customText),
    onSuccess: async (result) => {
      setConfirmation(`Alles klar, ${result.item} ist angefragt! 🍹`)
      setCustom('')
      celebrate()
      await refreshOrders()
      window.setTimeout(() => setConfirmation(undefined), 3500)
    },
  })
  const claim = useMutation({
    mutationFn: (orderIds: string[]) => claimGuestOrders(slug, token, orderIds),
    onSuccess: async (result) => {
      setRoundIds([])
      setConfirmation(
        result.claimed > 1
          ? `Runde übernommen: ${result.claimed} Drinks! 🍻`
          : `Du bringst den Drink! 🏃`,
      )
      celebrate()
      await refreshOrders()
    },
  })
  const change = useMutation({
    mutationFn: ({
      action,
      orderId,
    }: {
      action: 'release' | 'done' | 'cancel'
      orderId: string
    }) => {
      if (action === 'release') return releaseGuestOrder(slug, token, orderId)
      if (action === 'done') return completeGuestOrder(slug, token, orderId)
      return cancelGuestOrder(slug, token, orderId)
    },
    onSuccess: refreshOrders,
  })
  const myOrders = guestId
    ? orders.filter((order) => order.guestId === guestId).slice(0, 8)
    : []
  const available = guestId
    ? orders.filter(
        (order) =>
          order.status === 'Open' &&
          order.guestId !== guestId &&
          !order.claimedByGuestId,
      )
    : []
  const claimedByMe = guestId
    ? orders.filter(
        (order) =>
          order.status === 'Open' && order.claimedByGuestId === guestId,
      )
    : []
  return (
    <section>
      <PartyTitle
        icon="🍹"
        title="Drinks"
        text="Bestellen, Status sehen oder jemandem direkt einen Drink mitbringen."
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
              disabled={create.isPending}
              onClick={() => create.mutate({ id: item.id })}
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
          disabled={!custom.trim() || create.isPending}
          onClick={() => create.mutate({ customText: custom })}
        >
          <Send size={20} />
        </button>
      </div>
      {create.error && <PartyError error={create.error} />}

      <section className="mt-8">
        <h2 className="mb-3 flex items-center gap-2 text-lg font-black text-white">
          <ListChecks size={20} className="text-amber-200" /> Meine Bestellungen
        </h2>
        <div className="grid gap-2">
          {myOrders.map((order) => (
            <div key={order.id} className="party-card flex items-center gap-3">
              <span className="text-2xl">{order.icon ?? '🥤'}</span>
              <div className="min-w-0 flex-1">
                <p className="truncate font-bold text-white">{order.itemName ?? order.customText}</p>
                <p className="mt-0.5 text-xs text-white/55">
                  {orderStatusText(order)}
                </p>
              </div>
              {order.status === 'Open' && (
                <button
                  type="button"
                  className="party-small-button"
                  aria-label="Bestellung stornieren"
                  onClick={() => change.mutate({ action: 'cancel', orderId: order.id })}
                >
                  <Trash2 size={15} />
                </button>
              )}
            </div>
          ))}
          {myOrders.length === 0 && (
            <p className="party-card text-sm text-white/45">Noch nichts bestellt.</p>
          )}
        </div>
      </section>

      {claimedByMe.length > 0 && (
        <section className="mt-8">
          <h2 className="mb-3 flex items-center gap-2 text-lg font-black text-white">
            <Hand size={20} className="text-emerald-300" /> Du bringst
          </h2>
          <div className="grid gap-2">
            {claimedByMe.map((order) => (
              <div key={order.id} className="party-card">
                <p className="font-bold text-white">
                  {order.icon ?? '🥤'} {order.itemName ?? order.customText} für {order.guestName}
                </p>
                <div className="mt-3 flex gap-2">
                  <button
                    type="button"
                    className="party-done-button flex-1"
                    onClick={() => change.mutate({ action: 'done', orderId: order.id })}
                  >
                    <CheckCircle2 size={16} /> Übergeben
                  </button>
                  <button
                    type="button"
                    className="party-small-button"
                    onClick={() => change.mutate({ action: 'release', orderId: order.id })}
                  >
                    Freigeben
                  </button>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      <section className="mt-8">
        <div className="mb-3 flex items-end justify-between gap-3">
          <div>
            <h2 className="flex items-center gap-2 text-lg font-black text-white">
              🍻 Runde holen
            </h2>
            <p className="mt-1 text-xs text-white/45">Offene Wünsche auswählen und gemeinsam übernehmen.</p>
          </div>
          {roundIds.length > 0 && (
            <button
              type="button"
              className="party-primary shrink-0"
              disabled={claim.isPending}
              onClick={() => claim.mutate(roundIds)}
            >
              {roundIds.length} übernehmen
            </button>
          )}
        </div>
        <div className="grid gap-2">
          {available.map((order) => {
            const selected = roundIds.includes(order.id)
            return (
              <button
                key={order.id}
                type="button"
                className={`party-claim-card ${selected ? 'party-claim-card-selected' : ''}`}
                onClick={() =>
                  setRoundIds((current) =>
                    current.includes(order.id)
                      ? current.filter((id) => id !== order.id)
                      : [...current, order.id],
                  )
                }
              >
                <span className="text-2xl">{order.icon ?? '🥤'}</span>
                <span className="min-w-0 flex-1 text-left">
                  <strong className="block truncate text-white">{order.itemName ?? order.customText}</strong>
                  <span className="text-xs text-white/50">für {order.guestName}</span>
                </span>
                <span className="party-claim-check">{selected ? '✓' : '+'}</span>
              </button>
            )
          })}
          {available.length === 0 && (
            <p className="party-card text-sm text-white/45">Gerade wartet kein offener Drink. ✨</p>
          )}
        </div>
        {claim.error && <PartyError error={claim.error} />}
        {change.error && <PartyError error={change.error} />}
      </section>
    </section>
  )
}

interface PendingMediaUpload {
  id: string
  file: File
  previewUrl: string
  progress: number
  status: 'queued' | 'preparing' | 'uploading' | 'done' | 'error'
  error?: string
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
  const [uploads, setUploads] = useState<PendingMediaUpload[]>([])
  const [caption, setCaption] = useState('')
  const [isUploading, setIsUploading] = useState(false)
  const [selectedMediaId, setSelectedMediaId] = useState<string>()
  const gallery = useQuery({
    queryKey: ['party-media', slug],
    queryFn: () => listGuestMedia(slug, token),
    enabled: canViewGallery,
    retry: false,
    refetchInterval: 10_000,
  })
  const selectedIndex =
    gallery.data?.findIndex((item) => item.id === selectedMediaId) ?? -1
  const selectedMedia =
    selectedIndex >= 0 ? gallery.data?.[selectedIndex] : undefined
  const moveSelection = (offset: number) => {
    if (!gallery.data?.length || selectedIndex < 0) return
    const next =
      (selectedIndex + offset + gallery.data.length) % gallery.data.length
    setSelectedMediaId(gallery.data[next].id)
  }

  const addFiles = (files: FileList | null) => {
    if (!files) return
    const next = Array.from(files)
      .slice(0, Math.max(0, 10 - uploads.length))
      .map((file, index): PendingMediaUpload => ({
        id: `${Date.now()}-${index}-${file.name}`,
        file,
        previewUrl: URL.createObjectURL(file),
        progress: 0,
        status: 'queued',
      }))
    setUploads((current) => [...current, ...next])
  }

  const updateUpload = (id: string, patch: Partial<PendingMediaUpload>) =>
    setUploads((current) =>
      current.map((item) => (item.id === id ? { ...item, ...patch } : item)),
    )

  const removeUpload = (id: string) => {
    setUploads((current) => {
      const removed = current.find((item) => item.id === id)
      if (removed) URL.revokeObjectURL(removed.previewUrl)
      return current.filter((item) => item.id !== id)
    })
  }

  const uploadAll = async () => {
    const pending = uploads.filter((item) => item.status !== 'done')
    if (pending.length === 0) return
    setIsUploading(true)
    const completed = new Set<string>()
    for (const item of pending) {
      try {
        updateUpload(item.id, { status: 'preparing', progress: 0, error: undefined })
        let prepared = item.file
        if (isImageUpload(item.file) && !isGifUpload(item.file)) {
          try {
            prepared = (await prepareScreenshot(item.file)).file
          } catch {
            const directlySupported = [
              'image/jpeg',
              'image/png',
              'image/webp',
            ].includes(item.file.type)
            if (!directlySupported) {
              throw new Error(
                'Dieses Foto konnte nicht vorbereitet werden. Bitte öffne es kurz in Fotos und teile/speichere es als JPEG.',
              )
            }
            prepared = item.file
          }
        }
        const maximum = isVideoUpload(prepared)
          ? 100 * 1024 * 1024
          : 12 * 1024 * 1024
        if (prepared.size > maximum) {
          throw new Error(
            isVideoUpload(prepared)
              ? 'Video ist größer als 100 MB.'
              : 'Foto ist nach der Optimierung größer als 12 MB.',
          )
        }
        updateUpload(item.id, { status: 'uploading', progress: 1 })
        await uploadGuestMedia(slug, token, prepared, caption, (progress) =>
          updateUpload(item.id, { status: 'uploading', progress }),
        )
        updateUpload(item.id, { status: 'done', progress: 100 })
        completed.add(item.id)
      } catch (error) {
        updateUpload(item.id, {
          status: 'error',
          error: error instanceof Error ? error.message : 'Upload fehlgeschlagen.',
        })
      }
    }
    if (completed.size > 0) {
      celebrate()
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['party-media', slug] }),
        queryClient.invalidateQueries({ queryKey: ['party-my-media', slug] }),
        queryClient.invalidateQueries({ queryKey: ['party-pulse', slug] }),
        queryClient.invalidateQueries({ queryKey: ['party-feed', slug] }),
      ])
      setCaption('')
      setUploads((current) => {
        current
          .filter((item) => completed.has(item.id))
          .forEach((item) => URL.revokeObjectURL(item.previewUrl))
        return current.filter((item) => !completed.has(item.id))
      })
    }
    setIsUploading(false)
  }
  return (
    <section>
      <PartyTitle
        icon="📸"
        title="Fotos & Videos"
        text="Lad deinen besten Schnappschuss oder ein kurzes Video hoch."
      />
      <div className="party-card">
        <div className="grid grid-cols-2 gap-2">
          <label className="party-upload-button">
            <Camera size={22} />
            <span>Kamera</span>
            <input
              type="file"
              className="hidden"
              accept="image/*"
              capture="environment"
              onChange={(event) => {
                addFiles(event.target.files)
                event.target.value = ''
              }}
            />
          </label>
          <label className="party-upload-button">
            <ImagePlus size={22} />
            <span>Auswählen</span>
            <input
              type="file"
              className="hidden"
              multiple
              accept="image/*,video/mp4,video/quicktime,video/webm"
              onChange={(event) => {
                addFiles(event.target.files)
                event.target.value = ''
              }}
            />
          </label>
        </div>
        <p className="mt-2 text-center text-[11px] text-white/40">
          Bis zu 10 Dateien · Fotos 12 MB · Videos 100 MB
        </p>
        {uploads.length > 0 && (
          <div className="mt-4 grid grid-cols-2 gap-2">
            {uploads.map((item) => (
              <div key={item.id} className="party-upload-preview">
                {isVideoUpload(item.file) ? (
                  <video src={item.previewUrl} muted playsInline />
                ) : (
                  <img src={item.previewUrl} alt="Upload-Vorschau" />
                )}
                {!isUploading && (
                  <button
                    type="button"
                    className="party-upload-remove"
                    aria-label="Datei entfernen"
                    onClick={() => removeUpload(item.id)}
                  >
                    <X size={15} />
                  </button>
                )}
                <div className="party-upload-state">
                  <span>
                    {item.status === 'preparing'
                      ? 'Optimieren …'
                      : item.status === 'uploading'
                        ? `${item.progress}%`
                        : item.status === 'error'
                          ? 'Fehler'
                          : 'Bereit'}
                  </span>
                  <div className="party-upload-progress">
                    <span style={{ width: `${item.progress}%` }} />
                  </div>
                </div>
                {item.error && <p className="party-upload-error">{item.error}</p>}
              </div>
            ))}
          </div>
        )}
        <input
          className="party-input mt-3"
          maxLength={500}
          placeholder="Kommentar (optional)"
          value={caption}
          onChange={(event) => setCaption(event.target.value)}
        />
        <button
          className="party-primary mt-3 w-full"
          disabled={uploads.length === 0 || isUploading}
          onClick={() => void uploadAll()}
        >
          <Upload size={18} />{' '}
          {isUploading
            ? 'Uploads laufen …'
            : `${uploads.length || ''} ${uploads.length === 1 ? 'Datei' : 'Dateien'} hochladen`}
        </button>
      </div>
      {canViewGallery && (
        <div className="mt-8">
          <h2 className="mb-3 text-lg font-bold text-white">Galerie</h2>
          {gallery.isPending && (
            <p className="text-sm text-white/50">Galerie wird geladen …</p>
          )}
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
            {gallery.data?.map((media) => (
              <div key={media.id} className="relative">
                <PartyMediaPreview
                  media={media}
                  token={token}
                  onOpen={() => setSelectedMediaId(media.id)}
                />
                {media.likeCount > 0 && (
                  <span className="pointer-events-none absolute right-2 bottom-2 flex items-center gap-1 rounded-full bg-black/70 px-2 py-1 text-[11px] font-bold text-rose-200 backdrop-blur">
                    <Heart size={12} fill="currentColor" /> {media.likeCount}
                  </span>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
      {selectedMedia && (
        <PartyMediaLightbox
          media={selectedMedia}
          slug={slug}
          token={token}
          hasMultiple={(gallery.data?.length ?? 0) > 1}
          onClose={() => setSelectedMediaId(undefined)}
          onPrevious={() => moveSelection(-1)}
          onNext={() => moveSelection(1)}
        />
      )}
    </section>
  )
}

function isImageUpload(file: File) {
  return (
    file.type.startsWith('image/') ||
    /\.(?:jpe?g|png|webp|gif|heic|heif)$/i.test(file.name)
  )
}

function isGifUpload(file: File) {
  return file.type === 'image/gif' || /\.gif$/i.test(file.name)
}

function isVideoUpload(file: File) {
  return (
    file.type.startsWith('video/') || /\.(?:mp4|mov|webm)$/i.test(file.name)
  )
}

function PartyMediaPreview({
  media,
  token,
  featured = false,
  onOpen,
}: {
  media: PartyMedia
  token: string
  featured?: boolean
  onOpen?: () => void
}) {
  const [loadVideo, setLoadVideo] = useState(media.mediaType !== 'video')
  const blob = useQuery({
    queryKey: ['party-media-blob', media.id],
    queryFn: async () =>
      URL.createObjectURL(await downloadGuestMedia(media.contentUrl, token)),
    staleTime: Number.POSITIVE_INFINITY,
    enabled: loadVideo,
  })
  if (media.mediaType === 'video' && !loadVideo) {
    return (
      <button
        type="button"
        className={`party-video-placeholder ${featured ? 'h-64' : 'aspect-square'}`}
        onClick={() => (onOpen ? onOpen() : setLoadVideo(true))}
      >
        <span className="text-3xl">🎬</span>
        <strong>Video laden</strong>
        <small>{formatMegabytes(media.size)}</small>
      </button>
    )
  }
  if (!blob.data)
    return (
      <div className="aspect-square animate-pulse rounded-xl bg-white/10" />
    )
  if (onOpen) {
    return (
      <button
        type="button"
        className="block aspect-square w-full overflow-hidden rounded-xl bg-black/30"
        aria-label={`${media.mediaType === 'video' ? 'Video' : 'Foto'} von ${media.guestName} groß öffnen`}
        onClick={onOpen}
      >
        {media.mediaType === 'video' ? (
          <video
            muted
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
      </button>
    )
  }
  return (
    <div className="overflow-hidden rounded-xl bg-black/30">
      {media.mediaType === 'video' ? (
        <video
          controls
          playsInline
          className={featured ? 'h-64 w-full object-cover' : 'aspect-square h-full w-full object-cover'}
          src={blob.data}
        />
      ) : (
        <img
          className={featured ? 'h-64 w-full object-cover' : 'aspect-square h-full w-full object-cover'}
          src={blob.data}
          alt={media.caption ?? `Foto von ${media.guestName}`}
        />
      )}
      {featured && (
        <div className="px-3 py-2 text-xs text-white/55">
          <strong className="text-white/75">{media.guestName}</strong>
          {media.caption ? ` · ${media.caption}` : ''}
        </div>
      )}
    </div>
  )
}

function PartyMediaLightbox({
  media,
  slug,
  token,
  hasMultiple,
  onClose,
  onPrevious,
  onNext,
}: {
  media: PartyMedia
  slug: string
  token: string
  hasMultiple: boolean
  onClose: () => void
  onPrevious: () => void
  onNext: () => void
}) {
  const queryClient = useQueryClient()
  const [touchStart, setTouchStart] = useState<number>()
  const blob = useQuery({
    queryKey: ['party-media-blob', media.id],
    queryFn: async () =>
      URL.createObjectURL(await downloadGuestMedia(media.contentUrl, token)),
    staleTime: Number.POSITIVE_INFINITY,
  })
  const like = useMutation({
    mutationFn: () => toggleGuestMediaLike(slug, token, media.id),
    onSuccess: (result) => {
      const applyLike = (current: PartyMedia[] | undefined) =>
        current?.map((item) =>
          item.id === media.id
            ? { ...item, likeCount: result.likeCount, hasLiked: result.hasLiked }
            : item,
        )
      queryClient.setQueryData<PartyMedia[]>(['party-media', slug], applyLike)
      queryClient.setQueryData<PartyMedia[]>(['party-my-media', slug], applyLike)
    },
  })

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
      if (event.key === 'ArrowLeft' && hasMultiple) onPrevious()
      if (event.key === 'ArrowRight' && hasMultiple) onNext()
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [hasMultiple, onClose, onNext, onPrevious])

  return (
    <div
      className="fixed inset-0 z-[80] flex flex-col bg-black/95 p-3 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-label="Medienansicht"
      onClick={onClose}
      onTouchStart={(event) => setTouchStart(event.changedTouches[0]?.clientX)}
      onTouchEnd={(event) => {
        if (touchStart === undefined || !hasMultiple) return
        const touchEnd = event.changedTouches[0]?.clientX
        if (touchEnd === undefined) return
        const distance = touchEnd - touchStart
        setTouchStart(undefined)
        if (Math.abs(distance) < 60) return
        if (distance > 0) onPrevious()
        else onNext()
      }}
    >
      <div className="flex items-center justify-between gap-3 px-1 pb-3">
        <div className="min-w-0">
          <p className="truncate text-sm font-bold text-white">{media.guestName}</p>
          <p className="text-[11px] text-white/45">{relativeTime(media.createdAt)}</p>
        </div>
        <button
          type="button"
          className="flex size-10 items-center justify-center rounded-full bg-white/10 text-white"
          aria-label="Galerie schließen"
          onClick={onClose}
        >
          <X size={20} />
        </button>
      </div>

      <div
        className="relative flex min-h-0 flex-1 items-center justify-center"
        onClick={(event) => event.stopPropagation()}
      >
        {blob.data ? (
          media.mediaType === 'video' ? (
            <video
              src={blob.data}
              controls
              autoPlay
              playsInline
              className="max-h-full max-w-full rounded-xl object-contain"
            />
          ) : (
            <img
              src={blob.data}
              alt={media.caption ?? `Foto von ${media.guestName}`}
              className="max-h-full max-w-full rounded-xl object-contain"
            />
          )
        ) : (
          <div className="size-16 animate-pulse rounded-2xl bg-white/10" />
        )}
        {hasMultiple && (
          <>
            <button
              type="button"
              className="absolute left-1 flex size-10 items-center justify-center rounded-full bg-black/60 text-white sm:left-3"
              aria-label="Vorheriges Medium"
              onClick={onPrevious}
            >
              <ChevronLeft size={24} />
            </button>
            <button
              type="button"
              className="absolute right-1 flex size-10 items-center justify-center rounded-full bg-black/60 text-white sm:right-3"
              aria-label="Nächstes Medium"
              onClick={onNext}
            >
              <ChevronRight size={24} />
            </button>
          </>
        )}
      </div>

      <div
        className="mx-auto mt-3 w-full max-w-lg rounded-2xl bg-white/[0.08] p-3"
        onClick={(event) => event.stopPropagation()}
      >
        {media.caption && (
          <p className="mb-3 text-sm leading-5 text-white/75">{media.caption}</p>
        )}
        <div className="flex gap-2">
          <button
            type="button"
            className={`party-small-button flex-1 justify-center ${media.hasLiked ? 'text-rose-300' : ''}`}
            disabled={like.isPending}
            onClick={() => like.mutate()}
          >
            <Heart size={17} fill={media.hasLiked ? 'currentColor' : 'none'} />
            {media.likeCount > 0 ? media.likeCount : 'Gefällt mir'}
          </button>
          {blob.data && (
            <a
              href={blob.data}
              download={media.fileName}
              className="party-small-button flex-1 justify-center"
            >
              <Download size={17} /> Speichern
            </a>
          )}
        </div>
      </div>
    </div>
  )
}

function MusicView({ slug, token }: { slug: string; token: string }) {
  const queryClient = useQueryClient()
  const [song, setSong] = useState('')
  const [artist, setArtist] = useState('')
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [sent, setSent] = useState(false)
  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(search.trim()), 350)
    return () => window.clearTimeout(handle)
  }, [search])

  const spotify = useQuery({
    queryKey: ['party-spotify', slug],
    queryFn: () => getGuestSpotifyStatus(slug, token),
    refetchInterval: 5000,
    retry: false,
  })
  const requests = useQuery({
    queryKey: ['party-music', slug],
    queryFn: () => listGuestMusicRequests(slug, token),
    refetchInterval: 5000,
  })
  const searchResults = useQuery({
    queryKey: ['party-spotify-search', slug, debouncedSearch],
    queryFn: () => searchPartySpotify(slug, token, debouncedSearch),
    enabled: Boolean(spotify.data?.isConnected && debouncedSearch.length >= 2),
    staleTime: 30_000,
    retry: false,
  })
  const mutation = useMutation({
    mutationFn: (input: {
      song: string
      artist?: string
      spotifyTrackId?: string
    }) => addMusicRequest(slug, token, input),
    onSuccess: async () => {
      setSong('')
      setArtist('')
      setSearch('')
      setSent(true)
      celebrate()
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['party-my-music', slug] }),
        queryClient.invalidateQueries({ queryKey: ['party-music', slug] }),
        queryClient.invalidateQueries({ queryKey: ['party-spotify', slug] }),
        queryClient.invalidateQueries({ queryKey: ['party-pulse', slug] }),
        queryClient.invalidateQueries({ queryKey: ['party-feed', slug] }),
      ])
    },
  })
  const vote = useMutation({
    mutationFn: (requestId: string) => toggleGuestMusicVote(slug, token, requestId),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ['party-music', slug] }),
  })
  return (
    <section>
      <PartyTitle
        icon="🎵"
        title="Musikwunsch"
        text={spotify.data?.isConnected ? 'Spotify ist verbunden. Was soll als Nächstes laufen?' : 'Welcher Song fehlt heute noch?'}
      />
      {sent && (
        <div className="party-success">
          <CheckCircle2 size={20} /> Wunsch ist notiert!
        </div>
      )}

      {spotify.data?.isConnected && spotify.data.nowPlaying && (
        <div className="party-card mb-4 flex items-center gap-3 border-emerald-300/20">
          {spotify.data.nowPlaying.albumImageUrl && (
            <img
              src={spotify.data.nowPlaying.albumImageUrl}
              alt="Albumcover"
              className="h-16 w-16 rounded-xl object-cover"
            />
          )}
          <div className="min-w-0 flex-1">
            <p className="text-[10px] font-black tracking-[0.14em] text-emerald-300 uppercase">
              Jetzt läuft
            </p>
            <p className="truncate font-black text-white">{spotify.data.nowPlaying.name}</p>
            <p className="truncate text-xs text-white/50">{spotify.data.nowPlaying.artist}</p>
          </div>
          <span className="text-xl">{spotify.data.nowPlaying.isPlaying ? '🔊' : '⏸️'}</span>
        </div>
      )}

      {spotify.data?.isConnected ? (
        <div className="party-card">
          <div className="flex items-center gap-2">
            <Music2 size={18} className="text-emerald-300" />
            <strong className="text-sm text-white">Spotify durchsuchen</strong>
          </div>
          <input
            className="party-input mt-3"
            maxLength={80}
            placeholder="Song oder Künstler …"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
          {searchResults.isFetching && (
            <p className="mt-2 text-xs text-white/40">Spotify sucht …</p>
          )}
          <div className="mt-3 grid gap-2">
            {searchResults.data?.map((track) => (
              <SpotifySearchResult
                key={track.id}
                track={track}
                pending={mutation.isPending}
                onRequest={() =>
                  mutation.mutate({
                    song: track.name,
                    artist: track.artist,
                    spotifyTrackId: track.id,
                  })
                }
              />
            ))}
          </div>
          {searchResults.error && <PartyError error={searchResults.error} />}
          {spotify.data.autoQueue && (
            <p className="mt-3 text-[11px] text-emerald-200/70">
              ⚡ Auto-Queue aktiv: neue Spotify-Wünsche gehen direkt in die Warteschlange.
            </p>
          )}
        </div>
      ) : (
        <p className="party-card text-sm text-white/55">
          Spotify ist noch nicht verbunden. Du kannst trotzdem einen Wunsch eintragen.
        </p>
      )}

      <details className="party-card mt-3" open={!spotify.data?.isConnected}>
        <summary className="cursor-pointer text-sm font-bold text-white/70">
          Song manuell eintragen
        </summary>
        <div className="mt-3 grid gap-3">
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
            onClick={() => mutation.mutate({ song, artist: artist || undefined })}
          >
            <Music2 size={18} /> Wunsch senden
          </button>
        </div>
      </details>

      <section className="mt-7">
        <h2 className="mb-3 text-lg font-black text-white">🔥 Party-Wünsche</h2>
        <div className="grid gap-2">
          {requests.data?.map((request) => (
            <div key={request.id} className="party-card flex items-center gap-3">
              {request.spotifyAlbumImageUrl ? (
                <img
                  src={request.spotifyAlbumImageUrl}
                  alt="Albumcover"
                  className="h-12 w-12 shrink-0 rounded-lg object-cover"
                />
              ) : (
                <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-white/[0.06] text-xl">🎵</span>
              )}
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-bold text-white">{request.song}</p>
                <p className="truncate text-xs text-white/45">
                  {request.artist ?? request.guestName}
                  {request.spotifyQueuedAt ? ' · Queue ✓' : ''}
                </p>
              </div>
              {request.status === 'Open' && (
                <button
                  type="button"
                  className={`party-vote-button ${request.hasVoted ? 'party-vote-button-active' : ''}`}
                  disabled={vote.isPending}
                  onClick={() => vote.mutate(request.id)}
                  aria-label={`${request.song} unterstützen`}
                >
                  ❤️ {request.voteCount}
                </button>
              )}
            </div>
          ))}
          {!requests.isPending && (requests.data?.length ?? 0) === 0 && (
            <p className="party-card text-sm text-white/45">Noch keine Wünsche – du bist dran. 🎧</p>
          )}
        </div>
      </section>
      {mutation.error && <PartyError error={mutation.error} />}
      {vote.error && <PartyError error={vote.error} />}
    </section>
  )
}

function SpotifySearchResult({
  track,
  pending,
  onRequest,
}: {
  track: SpotifyTrack
  pending: boolean
  onRequest: () => void
}) {
  return (
    <button
      type="button"
      className="party-spotify-result"
      disabled={pending}
      onClick={onRequest}
    >
      {track.albumImageUrl ? (
        <img src={track.albumImageUrl} alt="Albumcover" />
      ) : (
        <span className="party-spotify-result-placeholder">🎵</span>
      )}
      <span className="min-w-0 flex-1 text-left">
        <strong className="block truncate text-sm text-white">{track.name}</strong>
        <small className="block truncate text-white/45">{track.artist}</small>
      </span>
      <span className="text-xs font-black text-emerald-300">+ Wunsch</span>
    </button>
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
      celebrate()
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['party-guestbook', slug] }),
        queryClient.invalidateQueries({ queryKey: ['party-pulse', slug] }),
        queryClient.invalidateQueries({ queryKey: ['party-feed', slug] }),
      ])
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

function MeView({
  slug,
  guest,
  guestId,
  firstSeenAt,
  orders,
  onRenamed,
}: {
  slug: string
  guest: StoredGuest
  guestId?: string
  firstSeenAt?: string
  orders: PartyOrder[]
  onRenamed: (name: string) => void
}) {
  const queryClient = useQueryClient()
  const [name, setName] = useState(guest.name)
  const myMedia = useQuery({
    queryKey: ['party-my-media', slug],
    queryFn: () => listOwnGuestMedia(slug, guest.token),
  })
  const myMusic = useQuery({
    queryKey: ['party-my-music', slug],
    queryFn: () => listOwnMusicRequests(slug, guest.token),
  })
  const rename = useMutation({
    mutationFn: () => updatePartyGuest(slug, guest.token, name),
    onSuccess: async (updated) => {
      onRenamed(updated.name)
      celebrate()
      await queryClient.invalidateQueries({ queryKey: ['party-me', slug] })
    },
  })
  const myOrders = guestId
    ? orders.filter((order) => order.guestId === guestId).slice(0, 10)
    : []
  return (
    <section>
      <PartyTitle icon="🙋" title="Ich" text="Dein kleiner Party-Hub." />
      <div className="party-card">
        <div className="flex items-center gap-3">
          <div className="flex size-12 items-center justify-center rounded-full bg-amber-300/15 text-xl">
            🎉
          </div>
          <div>
            <p className="font-black text-white">{guest.name}</p>
            {firstSeenAt && (
              <p className="text-xs text-white/45">
                Seit {formatTime(firstSeenAt)} dabei
              </p>
            )}
          </div>
        </div>
        <form
          className="mt-4 flex gap-2"
          onSubmit={(event) => {
            event.preventDefault()
            rename.mutate()
          }}
        >
          <input
            className="party-input"
            maxLength={100}
            value={name}
            aria-label="Gastname"
            onChange={(event) => setName(event.target.value)}
          />
          <button
            className="party-small-button"
            disabled={!name.trim() || rename.isPending}
          >
            Speichern
          </button>
        </form>
        {rename.error && <PartyError error={rename.error} />}
      </div>
      <div className="party-card mt-3 flex items-start gap-3">
        <span className="text-2xl">📲</span>
        <div>
          <p className="font-bold text-white">Party wie eine App öffnen</p>
          <p className="mt-1 text-xs leading-5 text-white/50">
            Im Browser „Zum Home-Bildschirm“ wählen. Der Shortcut startet direkt wieder diese Party.
          </p>
        </div>
      </div>
      <div className="mt-7">
        <h2 className="mb-3 text-lg font-black text-white">Meine letzten Drinks</h2>
        <div className="grid gap-2">
          {myOrders.map((order) => (
            <div key={order.id} className="party-card flex items-center gap-3">
              <span className="text-2xl">{order.icon ?? '🥤'}</span>
              <div className="min-w-0">
                <p className="truncate font-bold text-white">{order.itemName ?? order.customText}</p>
                <p className="text-xs text-white/50">{orderStatusText(order)}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
      <div className="mt-7 grid gap-3">
        <div>
          <h2 className="mb-3 text-lg font-black text-white">
            Meine Uploads · {myMedia.data?.length ?? 0}
          </h2>
          <div className="grid grid-cols-3 gap-2">
            {myMedia.data?.slice(0, 6).map((media) => (
              <PartyMediaPreview key={media.id} media={media} token={guest.token} />
            ))}
          </div>
        </div>
        <div className="mt-3">
          <h2 className="mb-3 text-lg font-black text-white">
            Meine Musikwünsche · {myMusic.data?.length ?? 0}
          </h2>
          <div className="grid gap-2">
            {myMusic.data?.slice(0, 5).map((request) => (
              <div key={request.id} className="party-card flex items-center gap-3 py-3">
                <span className="text-xl">🎵</span>
                <div className="min-w-0">
                  <p className="truncate text-sm font-bold text-white">{request.song}</p>
                  <p className="text-xs text-white/45">
                    {request.artist ?? 'Ohne Künstler'} · {request.status}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}

function PartyBottomNavigation({
  view,
  setView,
  pulse,
  mediaBadge,
}: {
  view: View
  setView: (view: View) => void
  pulse?: PartyPulse
  mediaBadge?: number
}) {
  const items: { view: View; label: string; icon: React.ReactNode; badge?: number }[] = [
    { view: 'home', label: 'Party', icon: <House size={20} /> },
    {
      view: 'order',
      label: 'Drinks',
      icon: <GlassWater size={20} />,
      badge: pulse?.unclaimedOrderCount,
    },
    {
      view: 'media',
      label: 'Fotos',
      icon: <Images size={20} />,
      badge: mediaBadge,
    },
    {
      view: 'music',
      label: 'Musik',
      icon: <Music2 size={20} />,
      badge: pulse?.openMusicRequestCount,
    },
    { view: 'me', label: 'Ich', icon: <UserRound size={20} /> },
  ]
  return (
    <nav className="party-bottom-nav" aria-label="Party Navigation">
      {items.map((item) => (
        <button
          key={item.view}
          type="button"
          className={view === item.view ? 'party-nav-active' : ''}
          onClick={() => setView(item.view)}
        >
          <span className="relative">
            {item.icon}
            {!!item.badge && <span className="party-nav-badge">{item.badge > 99 ? '99+' : item.badge}</span>}
          </span>
          <span>{item.label}</span>
        </button>
      ))}
    </nav>
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
  badge,
  onClick,
}: {
  icon: React.ReactNode
  label: string
  badge?: number
  onClick: () => void
}) {
  return (
    <button type="button" className="party-tile" onClick={onClick}>
      <span className="party-tile-icon">{icon}</span>
      <span className="flex-1">{label}</span>
      {!!badge && <span className="party-tile-badge">{badge > 99 ? '99+' : badge}</span>}
    </button>
  )
}

function PartyStat({ icon, value, label }: { icon: string; value: number; label: string }) {
  return (
    <div className="party-stat">
      <span>{icon}</span>
      <strong>{value}</strong>
      <small>{label}</small>
    </div>
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

function mediaSeenKey(slug: string) {
  return `community-party-media-seen:${slug}`
}
function readMediaSeenAt(slug: string) {
  return slug ? localStorage.getItem(mediaSeenKey(slug)) ?? undefined : undefined
}
function storeMediaSeenAt(slug: string, createdAt: string) {
  localStorage.setItem(mediaSeenKey(slug), createdAt)
}

function orderStatusText(order: PartyOrder) {
  if (order.status === 'Done') return '✅ Erledigt'
  if (order.status === 'Cancelled') return '✕ Storniert'
  if (order.claimedByGuestName) return `🏃 ${order.claimedByGuestName} bringt's`
  return '🟡 Offen – wartet noch auf jemanden'
}

function formatTime(value: string) {
  return new Date(value).toLocaleTimeString('de-DE', {
    hour: '2-digit',
    minute: '2-digit',
  })
}

function formatMegabytes(bytes: number) {
  return `${Math.max(0.1, bytes / 1024 / 1024).toFixed(1)} MB`
}

function relativeTime(value: string) {
  const seconds = Math.max(0, Math.round((Date.now() - new Date(value).getTime()) / 1000))
  if (seconds < 60) return 'gerade eben'
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `vor ${minutes} Min.`
  return `vor ${Math.floor(minutes / 60)} Std.`
}

function celebrate() {
  if ('vibrate' in navigator) navigator.vibrate(35)
  const burst = document.createElement('div')
  burst.className = 'party-confetti'
  ;['🎉', '✨', '🎊', '🍻', '✨', '🎉'].forEach((emoji) => {
    const piece = document.createElement('span')
    piece.textContent = emoji
    burst.append(piece)
  })
  document.body.append(burst)
  window.setTimeout(() => burst.remove(), 1_100)
}
