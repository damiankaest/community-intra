import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  Check,
  Copy,
  Download,
  ExternalLink,
  Image,
  Music2,
  PartyPopper,
  Plus,
  QrCode,
  Trash2,
  Users,
} from 'lucide-react'
import QRCode from 'qrcode'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  addPartyOrderItem,
  archiveParty,
  connectPartySpotify,
  createAdminPartyGuestSession,
  createParty,
  deleteAdminGuestbookEntry,
  deleteAdminPartyMedia,
  downloadAdminPartyMedia,
  getParty,
  getPartySpotifyStatus,
  listAdminGuestbook,
  listAdminPartyMedia,
  listParties,
  listPartyGuests,
  listPartyMusic,
  listPartyOrders,
  queuePartyMusicRequest,
  removePartyGuest,
  disconnectPartySpotify,
  setPartyMusicStatus,
  setPartyOrderStatus,
  updateParty,
  updatePartyOrderItem,
  updatePartySpotify,
  type Party,
  type PartyInput,
  type PartyMedia,
} from '../api/parties'

export function PartyListPage() {
  const navigate = useNavigate()
  const parties = useQuery({ queryKey: ['parties'], queryFn: listParties })
  const [showForm, setShowForm] = useState(false)
  const create = useMutation({
    mutationFn: createParty,
    onSuccess: (party) => navigate(`/parties/${party.id}`),
  })

  return (
    <AdminSurface>
      <AdminHeader title="Partys" />
      <div className="mx-auto max-w-5xl px-4 py-8 sm:px-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-xs font-bold tracking-[0.16em] text-amber-300 uppercase">
              Events
            </p>
            <h1 className="mt-1 text-3xl font-black text-white">
              Deine Partys
            </h1>
          </div>
          <button
            className="feature-button"
            onClick={() => setShowForm((value) => !value)}
          >
            <Plus size={18} /> Party erstellen
          </button>
        </div>

        {showForm && (
          <CreatePartyForm
            pending={create.isPending}
            error={create.error}
            onSubmit={(input) => create.mutate(input)}
          />
        )}

        <div className="mt-8 grid gap-4 sm:grid-cols-2">
          {parties.data?.map((party) => (
            <Link
              key={party.id}
              to={`/parties/${party.id}`}
              className="rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5 no-underline transition hover:border-amber-300/30"
            >
              <div className="flex items-start justify-between gap-3">
                <div>
                  <h2 className="text-lg font-bold text-white">{party.name}</h2>
                  <p className="mt-1 text-sm text-[var(--theme-muted)]">
                    {formatDate(party.startAt)}
                    {party.location ? ` · ${party.location}` : ''}
                  </p>
                </div>
                <span
                  className={`rounded-full px-2.5 py-1 text-xs font-bold ${party.isActive ? 'bg-emerald-400/15 text-emerald-300' : 'bg-white/10 text-white/50'}`}
                >
                  {party.isActive ? 'Aktiv' : 'Inaktiv'}
                </span>
              </div>
              <div className="mt-5 flex gap-4 text-xs text-[var(--theme-muted)]">
                <span>{party.guestCount} Gäste</span>
                <span>{party.openOrderCount} offen</span>
              </div>
            </Link>
          ))}
        </div>
        {parties.data?.length === 0 && (
          <p className="mt-10 text-sm text-[var(--theme-muted)]">
            Noch keine Party angelegt. Das lässt sich ändern. 🎉
          </p>
        )}
      </div>
    </AdminSurface>
  )
}

export function PartyAdminPage() {
  const { partyId = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const party = useQuery({
    queryKey: ['party', partyId],
    queryFn: () => getParty(partyId),
    enabled: Boolean(partyId),
  })
  const orders = useQuery({
    queryKey: ['party-orders', partyId],
    queryFn: () => listPartyOrders(partyId),
    enabled: Boolean(partyId),
    refetchInterval: 5000,
  })
  const media = useQuery({
    queryKey: ['party-admin-media', partyId],
    queryFn: () => listAdminPartyMedia(partyId),
    enabled: Boolean(partyId),
  })
  const guests = useQuery({
    queryKey: ['party-guests', partyId],
    queryFn: () => listPartyGuests(partyId),
    enabled: Boolean(partyId),
  })
  const guestbook = useQuery({
    queryKey: ['party-admin-guestbook', partyId],
    queryFn: () => listAdminGuestbook(partyId),
    enabled: Boolean(partyId),
  })
  const music = useQuery({
    queryKey: ['party-music', partyId],
    queryFn: () => listPartyMusic(partyId),
    enabled: Boolean(partyId),
  })
  const spotify = useQuery({
    queryKey: ['party-spotify', partyId],
    queryFn: () => getPartySpotifyStatus(partyId),
    enabled: Boolean(partyId),
    refetchInterval: 5000,
  })
  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['party', partyId] }),
      queryClient.invalidateQueries({ queryKey: ['parties'] }),
    ])
  }
  const statusMutation = useMutation({
    mutationFn: ({
      orderId,
      status,
    }: {
      orderId: string
      status: 'Open' | 'Done' | 'Cancelled'
    }) => setPartyOrderStatus(partyId, orderId, status),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ['party-orders', partyId] }),
  })
  const updateMutation = useMutation({
    mutationFn: (input: PartyInput) => updateParty(partyId, input),
    onSuccess: invalidate,
  })
  const archiveMutation = useMutation({
    mutationFn: () => archiveParty(partyId),
    onSuccess: () => navigate('/parties', { replace: true }),
  })
  const connectSpotify = useMutation({
    mutationFn: () => connectPartySpotify(partyId),
    onSuccess: ({ authorizeUrl }) => window.location.assign(authorizeUrl),
  })
  const disconnectSpotify = useMutation({
    mutationFn: () => disconnectPartySpotify(partyId),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ['party-spotify', partyId] }),
  })
  const updateSpotify = useMutation({
    mutationFn: (autoQueue: boolean) => updatePartySpotify(partyId, autoQueue),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ['party-spotify', partyId] }),
  })
  const queueMusic = useMutation({
    mutationFn: (requestId: string) => queuePartyMusicRequest(partyId, requestId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['party-music', partyId] }),
        queryClient.invalidateQueries({ queryKey: ['party-spotify', partyId] }),
      ])
    },
  })
  const openGuestView = useMutation({
    mutationFn: () => createAdminPartyGuestSession(partyId),
    onSuccess: (session) => {
      const slug = party.data?.slug
      if (!slug) return
      localStorage.setItem(
        `community-party-guest:${slug}`,
        JSON.stringify({
          name: session.name,
          token: session.sessionToken,
          isAdmin: true,
        }),
      )
      window.location.assign(`/party/${slug}`)
    },
  })
  const removeGuest = useMutation({
    mutationFn: (guestId: string) => removePartyGuest(partyId, guestId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['party-guests', partyId] }),
        queryClient.invalidateQueries({ queryKey: ['party', partyId] }),
        queryClient.invalidateQueries({ queryKey: ['parties'] }),
      ])
    },
  })

  if (party.isPending)
    return (
      <AdminSurface>
        <AdminHeader title="Party" />
        <p className="mx-auto max-w-5xl px-4 py-10 text-[var(--theme-muted)]">
          Party wird geladen …
        </p>
      </AdminSurface>
    )
  if (!party.data)
    return (
      <AdminSurface>
        <AdminHeader title="Party" />
        <p className="mx-auto max-w-5xl px-4 py-10 text-rose-200">
          Party konnte nicht geladen werden.
        </p>
      </AdminSurface>
    )

  const openOrders =
    orders.data?.filter((order) => order.status === 'Open') ?? []
  const doneOrders =
    orders.data?.filter((order) => order.status === 'Done') ?? []

  return (
    <AdminSurface>
      <AdminHeader title={party.data.name} />
      <main className="mx-auto max-w-5xl px-4 py-7 sm:px-6">
        <Link
          to="/parties"
          className="inline-flex items-center gap-2 text-sm font-semibold text-[var(--theme-muted)] hover:text-white"
        >
          <ArrowLeft size={16} /> Alle Partys
        </Link>
        <div className="mt-5 flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <div className="flex items-center gap-2">
              <span
                className={`size-2.5 rounded-full ${party.data.isActive ? 'bg-emerald-400' : 'bg-slate-500'}`}
              />
              <span className="text-xs font-bold text-[var(--theme-muted)] uppercase">
                {party.data.isActive ? 'Aktiv' : 'Inaktiv'}
              </span>
            </div>
            <h1 className="mt-2 text-3xl font-black text-white">
              {party.data.name}
            </h1>
            <p className="mt-2 text-sm text-[var(--theme-muted)]">
              {formatDate(party.data.startAt)}
              {party.data.location ? ` · ${party.data.location}` : ''}
            </p>
          </div>
          <button
            type="button"
            className="feature-button"
            disabled={openGuestView.isPending}
            onClick={() => openGuestView.mutate()}
          >
            <ExternalLink size={17} />
            {openGuestView.isPending ? 'Gastseite wird geöffnet …' : 'Gastseite öffnen'}
          </button>
        </div>
        {openGuestView.error && (
          <p className="mt-3 text-sm text-rose-300">
            {openGuestView.error.message}
          </p>
        )}

        <section className="mt-7 rounded-2xl border border-amber-300/20 bg-amber-300/[0.06] p-5 sm:p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.15em] text-amber-300 uppercase">
                Offen
              </p>
              <p className="mt-1 text-4xl font-black text-white">
                {openOrders.length}
              </p>
            </div>
            <span className="text-3xl">🍹</span>
          </div>
          <div className="mt-4 grid gap-3">
            {openOrders.map((order) => (
              <div
                key={order.id}
                className="flex items-center justify-between gap-3 rounded-xl bg-black/20 p-4"
              >
                <div>
                  <p className="font-bold text-white">{order.guestName}</p>
                  <p className="mt-0.5 text-sm text-white/70">
                    {order.icon} {order.itemName ?? order.customText} ·{' '}
                    {formatTime(order.createdAt)}
                  </p>
                  {order.claimedByGuestName && (
                    <p className="mt-1 text-xs font-semibold text-emerald-300">
                      🏃 {order.claimedByGuestName} bringt&apos;s
                    </p>
                  )}
                </div>
                <div className="flex shrink-0 gap-2">
                  <button
                    className="party-small-button"
                    title="Bestellung stornieren"
                    aria-label="Bestellung stornieren"
                    disabled={statusMutation.isPending}
                    onClick={() =>
                      statusMutation.mutate({
                        orderId: order.id,
                        status: 'Cancelled',
                      })
                    }
                  >
                    <Trash2 size={16} />
                  </button>
                  <button
                    className="party-done-button"
                    disabled={statusMutation.isPending}
                    onClick={() =>
                      statusMutation.mutate({ orderId: order.id, status: 'Done' })
                    }
                  >
                    <Check size={18} /> Erledigt
                  </button>
                </div>
              </div>
            ))}
            {openOrders.length === 0 && (
              <p className="text-sm text-white/45">
                Gerade alles erledigt. Stark. ✨
              </p>
            )}
          </div>
        </section>

        <section className="mt-6 rounded-2xl border border-emerald-400/20 bg-[var(--theme-surface)] p-5">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <div className="flex items-center gap-2">
                <Music2 size={19} className="text-emerald-300" />
                <h2 className="font-bold text-white">Spotify</h2>
              </div>
              {!spotify.data?.isConfigured ? (
                <p className="mt-2 text-sm text-white/50">
                  Spotify ist auf dem Server noch nicht konfiguriert.
                </p>
              ) : spotify.data.isConnected ? (
                <p className="mt-2 text-sm text-emerald-200">
                  Verbunden{spotify.data.accountName ? ` mit ${spotify.data.accountName}` : ''} ✓
                </p>
              ) : (
                <p className="mt-2 text-sm text-white/55">
                  Verbinde deinen Premium-Account. Gäste brauchen keinen Spotify-Login.
                </p>
              )}
            </div>
            {spotify.data?.isConfigured && !spotify.data.isConnected && (
              <button
                type="button"
                className="party-primary"
                disabled={connectSpotify.isPending}
                onClick={() => connectSpotify.mutate()}
              >
                <Music2 size={17} /> Spotify verbinden
              </button>
            )}
          </div>

          {spotify.data?.isConnected && (
            <>
              {spotify.data.nowPlaying && (
                <div className="mt-4 flex items-center gap-3 rounded-xl bg-emerald-400/[0.07] p-3">
                  {spotify.data.nowPlaying.albumImageUrl && (
                    <img
                      src={spotify.data.nowPlaying.albumImageUrl}
                      alt="Albumcover"
                      className="h-14 w-14 rounded-lg object-cover"
                    />
                  )}
                  <div className="min-w-0 flex-1">
                    <p className="text-[11px] font-bold tracking-wide text-emerald-300 uppercase">
                      Jetzt läuft
                    </p>
                    <p className="truncate text-sm font-bold text-white">
                      {spotify.data.nowPlaying.name}
                    </p>
                    <p className="truncate text-xs text-white/45">
                      {spotify.data.nowPlaying.artist}
                    </p>
                  </div>
                </div>
              )}
              <div className="mt-4 flex flex-wrap items-center gap-3">
                <label className="flex items-center gap-2 text-sm text-white/70">
                  <input
                    type="checkbox"
                    checked={spotify.data.autoQueue}
                    disabled={updateSpotify.isPending}
                    onChange={(event) => updateSpotify.mutate(event.target.checked)}
                  />
                  Wünsche automatisch in die Spotify-Queue
                </label>
                <button
                  type="button"
                  className="text-xs font-bold text-rose-300"
                  disabled={disconnectSpotify.isPending}
                  onClick={() => disconnectSpotify.mutate()}
                >
                  Verbindung trennen
                </button>
              </div>
            </>
          )}
          {(connectSpotify.error || disconnectSpotify.error || updateSpotify.error) && (
            <p className="mt-3 text-sm text-rose-300">
              {(connectSpotify.error ?? disconnectSpotify.error ?? updateSpotify.error)?.message}
            </p>
          )}
        </section>

        <div className="mt-6 grid gap-6 lg:grid-cols-2">
          <QrCard party={party.data} />
          <PartySettings
            party={party.data}
            pending={updateMutation.isPending}
            onSubmit={(input) => updateMutation.mutate(input)}
          />
        </div>

        <OrderItemsCard party={party.data} onChanged={invalidate} />

        <section className="mt-6 rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5">
          <div className="flex items-center gap-2">
            <Image size={19} className="text-amber-300" />
            <h2 className="font-bold text-white">
              Medien · {media.data?.length ?? 0}
            </h2>
          </div>
          <div className="mt-4 grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-4">
            {media.data?.map((item) => (
              <AdminMedia
                key={item.id}
                partyId={partyId}
                media={item}
                onDelete={() =>
                  queryClient.invalidateQueries({
                    queryKey: ['party-admin-media', partyId],
                  })
                }
              />
            ))}
          </div>
        </section>

        <div className="mt-6 grid gap-6 lg:grid-cols-2">
          <section className="rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5">
            <div className="flex items-center gap-2">
              <Users size={19} className="text-amber-300" />
              <h2 className="font-bold text-white">
                Gäste · {guests.data?.length ?? 0}
              </h2>
            </div>
            <div className="mt-4 grid gap-2">
              {guests.data?.map((guest) => (
                <div
                  key={guest.id}
                  className="flex items-center justify-between gap-3 rounded-xl bg-white/[0.05] px-3 py-2.5"
                >
                  <div className="min-w-0">
                    <p className="truncate text-sm font-bold text-white">
                      {guest.name}
                    </p>
                    <p className="mt-0.5 text-[11px] text-white/40">
                      zuletzt {formatTime(guest.lastSeenAt)}
                    </p>
                  </div>
                  <button
                    type="button"
                    className="party-small-button shrink-0 text-rose-200"
                    disabled={removeGuest.isPending}
                    onClick={() => {
                      if (
                        window.confirm(
                          `${guest.name} wirklich aus dieser Party entfernen?`,
                        )
                      ) {
                        removeGuest.mutate(guest.id)
                      }
                    }}
                  >
                    <Trash2 size={15} /> Rauswerfen
                  </button>
                </div>
              ))}
              {guests.data?.length === 0 && (
                <p className="text-sm text-white/45">Noch keine Gäste drin.</p>
              )}
            </div>
            {removeGuest.error && (
              <p className="mt-3 text-sm text-rose-300">
                {removeGuest.error.message}
              </p>
            )}
          </section>
          <section className="rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5">
            <div className="flex items-center gap-2">
              <Music2 size={19} className="text-amber-300" />
              <h2 className="font-bold text-white">Musikwünsche</h2>
            </div>
            <div className="mt-4 grid gap-2">
              {music.data?.map((request) => (
                <div
                  key={request.id}
                  className="flex gap-3 rounded-xl bg-white/[0.04] p-3"
                >
                  {request.spotifyAlbumImageUrl && (
                    <img
                      src={request.spotifyAlbumImageUrl}
                      alt="Albumcover"
                      className="h-12 w-12 shrink-0 rounded-lg object-cover"
                    />
                  )}
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-bold text-white">
                      {request.song}
                      {request.artist ? ` · ${request.artist}` : ''}
                    </p>
                    <p className="mt-1 text-xs text-white/45">
                      {request.guestName} · ❤️ {request.voteCount}
                      {request.spotifyQueuedAt ? ' · in Spotify-Queue ✓' : ''}
                    </p>
                    {request.status === 'Open' && (
                      <div className="mt-2 flex flex-wrap gap-3">
                        {spotify.data?.isConnected &&
                          request.spotifyUri &&
                          !request.spotifyQueuedAt && (
                          <button
                            className="text-xs font-bold text-emerald-300"
                            disabled={queueMusic.isPending}
                            onClick={() => queueMusic.mutate(request.id)}
                          >
                            ▶ In Spotify-Queue
                          </button>
                        )}
                        <button
                          className="text-xs font-bold text-white/60"
                          onClick={async () => {
                            await setPartyMusicStatus(partyId, request.id, 'Played')
                            await queryClient.invalidateQueries({
                              queryKey: ['party-music', partyId],
                            })
                          }}
                        >
                          Als gespielt markieren
                        </button>
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
            {queueMusic.error && (
              <p className="mt-3 text-sm text-rose-300">{queueMusic.error.message}</p>
            )}
          </section>
        </div>

        <section className="mt-6 rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5">
          <h2 className="font-bold text-white">💌 Gästebuch</h2>
          <div className="mt-4 grid gap-3">
            {guestbook.data?.map((entry) => (
              <div
                key={entry.id}
                className="flex items-start justify-between gap-3 rounded-xl bg-white/[0.04] p-3"
              >
                <div>
                  <p className="text-sm leading-6 text-white/80">
                    {entry.message}
                  </p>
                  <p className="mt-1 text-xs font-semibold text-amber-300">
                    {entry.guestName}
                  </p>
                </div>
                <button
                  className="text-white/35 hover:text-rose-300"
                  aria-label="Eintrag löschen"
                  onClick={async () => {
                    await deleteAdminGuestbookEntry(partyId, entry.id)
                    await queryClient.invalidateQueries({
                      queryKey: ['party-admin-guestbook', partyId],
                    })
                  }}
                >
                  <Trash2 size={16} />
                </button>
              </div>
            ))}
          </div>
        </section>

        {doneOrders.length > 0 && (
          <details className="mt-6 rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5">
            <summary className="cursor-pointer font-bold text-white">
              Erledigte Bestellungen · {doneOrders.length}
            </summary>
            <div className="mt-3 grid gap-2">
              {doneOrders.map((order) => (
                <p key={order.id} className="text-sm text-[var(--theme-muted)]">
                  {order.guestName} · {order.icon}{' '}
                  {order.itemName ?? order.customText}
                </p>
              ))}
            </div>
          </details>
        )}

        <div className="mt-10 border-t border-white/10 pt-6">
          <button
            className="inline-flex items-center gap-2 text-sm font-semibold text-rose-300"
            disabled={archiveMutation.isPending}
            onClick={() => {
              if (
                window.confirm(
                  'Party wirklich archivieren? Inhalte bleiben erhalten.',
                )
              )
                archiveMutation.mutate()
            }}
          >
            <Trash2 size={16} /> Party archivieren
          </button>
        </div>
      </main>
    </AdminSurface>
  )
}

function CreatePartyForm({
  onSubmit,
  pending,
  error,
}: {
  onSubmit: (input: PartyInput) => void
  pending: boolean
  error: Error | null
}) {
  return (
    <form
      className="mt-6 rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5"
      onSubmit={(event) => {
        event.preventDefault()
        onSubmit(partyInputFromForm(new FormData(event.currentTarget)))
      }}
    >
      <h2 className="font-bold text-white">Neue Party</h2>
      <div className="mt-4 grid gap-3 sm:grid-cols-2">
        <input
          required
          name="name"
          maxLength={160}
          className="feature-input"
          placeholder="Name der Party"
        />
        <select name="type" className="feature-input" defaultValue="Geburtstag">
          <option>Geburtstag</option>
          <option>Grillabend</option>
          <option>Feier</option>
          <option>Hochzeit</option>
          <option>Sonstiges</option>
        </select>
        <textarea
          name="description"
          maxLength={1200}
          className="feature-input min-h-20 sm:col-span-2"
          placeholder="Beschreibung (optional)"
        />
        <label className="grid gap-1 text-xs text-[var(--theme-muted)]">
          Datum
          <input required name="date" type="date" className="feature-input" />
        </label>
        <label className="grid gap-1 text-xs text-[var(--theme-muted)]">
          Startzeit (optional)
          <input name="startTime" type="time" className="feature-input" />
        </label>
        <input name="endAt" type="datetime-local" className="feature-input" />
        <input
          name="location"
          maxLength={240}
          className="feature-input sm:col-span-2"
          placeholder="Ort (optional)"
        />
        <textarea
          name="welcomeText"
          maxLength={1000}
          className="feature-input min-h-24 sm:col-span-2"
          placeholder="Begrüßungstext (optional)"
        />
      </div>
      <button className="feature-button mt-4" disabled={pending}>
        <PartyPopper size={17} />{' '}
        {pending ? 'Wird erstellt …' : 'Party erstellen'}
      </button>
      {error && <p className="mt-3 text-sm text-rose-300">{error.message}</p>}
    </form>
  )
}

function QrCard({ party }: { party: Party }) {
  const publicUrl = `${window.location.origin}/party/${party.slug}`
  const qr = useQuery({
    queryKey: ['party-qr', publicUrl],
    queryFn: () =>
      QRCode.toDataURL(publicUrl, {
        width: 1024,
        margin: 2,
        errorCorrectionLevel: 'M',
      }),
    staleTime: Number.POSITIVE_INFINITY,
  })
  return (
    <section className="rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5">
      <div className="flex items-center gap-2">
        <QrCode size={19} className="text-amber-300" />
        <h2 className="font-bold text-white">QR-Code & Link</h2>
      </div>
      <div className="mt-4 flex gap-4">
        {qr.data && (
          <img
            src={qr.data}
            alt="QR-Code zur Party"
            className="size-32 rounded-xl bg-white p-2"
          />
        )}
        <div className="min-w-0 flex-1">
          <p className="text-xs leading-5 break-all text-[var(--theme-muted)]">
            {publicUrl}
          </p>
          <div className="mt-3 flex flex-wrap gap-2">
            <button
              className="party-small-button"
              onClick={() => navigator.clipboard.writeText(publicUrl)}
            >
              <Copy size={14} /> Kopieren
            </button>
            {qr.data && (
              <a
                className="party-small-button"
                href={qr.data}
                download={`${party.slug}-qr.png`}
              >
                <Download size={14} /> PNG
              </a>
            )}
          </div>
        </div>
      </div>
    </section>
  )
}

function PartySettings({
  party,
  onSubmit,
  pending,
}: {
  party: Party
  onSubmit: (input: PartyInput) => void
  pending: boolean
}) {
  return (
    <form
      className="rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5"
      onSubmit={(event) => {
        event.preventDefault()
        onSubmit(partyInputFromForm(new FormData(event.currentTarget), party))
      }}
    >
      <h2 className="font-bold text-white">Einstellungen</h2>
      <div className="mt-4 grid gap-3">
        <input
          name="name"
          defaultValue={party.name}
          className="feature-input"
          required
        />
        <input
          name="startAt"
          type="datetime-local"
          defaultValue={toLocalInput(party.startAt)}
          className="feature-input"
          required
        />
        <input
          name="location"
          defaultValue={party.location}
          className="feature-input"
          placeholder="Ort"
        />
        <textarea
          name="description"
          defaultValue={party.description}
          className="feature-input min-h-20"
          placeholder="Beschreibung"
        />
        <textarea
          name="welcomeText"
          defaultValue={party.welcomeText}
          className="feature-input min-h-20"
          placeholder="Begrüßung"
        />
        <input type="hidden" name="type" value={party.type} />
        <label className="party-check">
          <input
            type="checkbox"
            name="isActive"
            defaultChecked={party.isActive}
          />{' '}
          Party aktiv
        </label>
        <label className="party-check">
          <input
            type="checkbox"
            name="gallery"
            defaultChecked={party.guestsCanViewGallery}
          />{' '}
          Gäste sehen Galerie
        </label>
        <label className="party-check">
          <input
            type="checkbox"
            name="guestbook"
            defaultChecked={party.guestsCanViewGuestbook}
          />{' '}
          Gäste sehen Gästebuch
        </label>
      </div>
      <button className="party-small-button mt-4" disabled={pending}>
        <Check size={14} /> Speichern
      </button>
    </form>
  )
}

function OrderItemsCard({
  party,
  onChanged,
}: {
  party: Party
  onChanged: () => Promise<void>
}) {
  const [name, setName] = useState('')
  const [icon, setIcon] = useState('🥤')
  const add = useMutation({
    mutationFn: () =>
      addPartyOrderItem(party.id, {
        name,
        icon,
        sortOrder: party.orderItems.length,
        isActive: true,
      }),
    onSuccess: async () => {
      setName('')
      await onChanged()
    },
  })
  return (
    <section className="mt-6 rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5">
      <h2 className="font-bold text-white">🍹 Getränkeoptionen</h2>
      <div className="mt-4 flex flex-wrap gap-2">
        {party.orderItems.map((item) => (
          <button
            key={item.id}
            className={`rounded-full border px-3 py-2 text-xs font-semibold ${item.isActive ? 'border-emerald-300/20 bg-emerald-300/10 text-emerald-200' : 'border-white/10 text-white/35'}`}
            onClick={async () => {
              await updatePartyOrderItem(party.id, item.id, {
                ...item,
                isActive: !item.isActive,
              })
              await onChanged()
            }}
          >
            {item.icon} {item.name}
          </button>
        ))}
      </div>
      <div className="mt-4 flex gap-2">
        <input
          className="feature-input w-16"
          value={icon}
          maxLength={20}
          onChange={(event) => setIcon(event.target.value)}
        />
        <input
          className="feature-input min-w-0 flex-1"
          value={name}
          maxLength={100}
          placeholder="Neue Option"
          onChange={(event) => setName(event.target.value)}
        />
        <button
          className="party-icon-button"
          disabled={!name.trim() || add.isPending}
          onClick={() => add.mutate()}
        >
          <Plus size={18} />
        </button>
      </div>
    </section>
  )
}

function AdminMedia({
  media,
  partyId,
  onDelete,
}: {
  media: PartyMedia
  partyId: string
  onDelete: () => void
}) {
  const blob = useQuery({
    queryKey: ['admin-party-media-blob', media.id],
    queryFn: async () =>
      URL.createObjectURL(await downloadAdminPartyMedia(media.contentUrl)),
    staleTime: Number.POSITIVE_INFINITY,
  })
  return (
    <div className="group relative overflow-hidden rounded-xl bg-black/30">
      {blob.data &&
        (media.mediaType === 'video' ? (
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
            alt={media.caption ?? media.fileName}
          />
        ))}
      <button
        className="absolute top-2 right-2 flex size-8 items-center justify-center rounded-full bg-black/65 text-white opacity-100 sm:opacity-0 sm:group-hover:opacity-100"
        aria-label="Medium löschen"
        onClick={async () => {
          await deleteAdminPartyMedia(partyId, media.id)
          onDelete()
        }}
      >
        <Trash2 size={14} />
      </button>
    </div>
  )
}

function AdminSurface({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-[var(--theme-background)] text-[var(--theme-text)]">
      {children}
    </div>
  )
}
function AdminHeader({ title }: { title: string }) {
  return (
    <header className="border-b border-white/10 bg-black/25">
      <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-4 sm:px-6">
        <Link
          to="/organizations"
          className="flex items-center gap-3 text-sm font-bold text-white"
        >
          <span className="flex size-9 items-center justify-center rounded-xl bg-amber-400 text-black">
            <PartyPopper size={18} />
          </span>{' '}
          Community Intranet
        </Link>
        <span className="max-w-36 truncate text-xs text-[var(--theme-muted)]">
          {title}
        </span>
      </div>
    </header>
  )
}

function partyInputFromForm(form: FormData, fallback?: Party): PartyInput {
  const date = String(form.get('date') ?? '')
  const startTime = String(form.get('startTime') ?? '')
  const start =
    String(form.get('startAt') ?? '') ||
    (date ? `${date}T${startTime || '00:00'}` : '')
  const end = String(form.get('endAt') ?? '')
  return {
    name: String(form.get('name') ?? ''),
    description:
      String(form.get('description') ?? fallback?.description ?? '') ||
      undefined,
    type: String(form.get('type') ?? fallback?.type ?? 'Sonstiges'),
    location: String(form.get('location') ?? '') || undefined,
    startAt: new Date(start).toISOString(),
    endAt: end ? new Date(end).toISOString() : fallback?.endAt,
    welcomeText: String(form.get('welcomeText') ?? '') || undefined,
    isActive: fallback ? form.has('isActive') : true,
    guestsCanViewGallery: fallback ? form.has('gallery') : true,
    guestsCanViewGuestbook: fallback ? form.has('guestbook') : true,
  }
}
function toLocalInput(value: string) {
  const date = new Date(value)
  const offset = date.getTimezoneOffset()
  return new Date(date.getTime() - offset * 60_000).toISOString().slice(0, 16)
}
function formatDate(value: string) {
  const date = new Date(value)
  return date.toLocaleString(
    'de-DE',
    date.getHours() === 0 && date.getMinutes() === 0
      ? { dateStyle: 'medium' }
      : { dateStyle: 'medium', timeStyle: 'short' },
  )
}
function formatTime(value: string) {
  return new Date(value).toLocaleTimeString('de-DE', {
    hour: '2-digit',
    minute: '2-digit',
  })
}
