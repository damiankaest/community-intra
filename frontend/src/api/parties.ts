import { apiFetch, apiProblem, apiRequest } from './client'

export interface PartyOrderItem {
  id: string
  name: string
  icon?: string
  sortOrder: number
  isActive: boolean
}

export interface Party {
  id: string
  name: string
  slug: string
  description?: string
  type: string
  location?: string
  startAt: string
  endAt?: string
  welcomeText?: string
  isActive: boolean
  guestsCanViewGallery: boolean
  guestsCanViewGuestbook: boolean
  createdAt: string
  updatedAt: string
  guestCount: number
  openOrderCount: number
  orderItems: PartyOrderItem[]
}

export type PublicParty = Omit<
  Party,
  'id' | 'createdAt' | 'updatedAt' | 'guestCount' | 'openOrderCount'
>

export interface PartyGuestSession {
  guestId: string
  name: string
  sessionToken: string
}

export interface PartyGuest {
  id: string
  name: string
  firstSeenAt: string
  lastSeenAt: string
}

export interface PartyOrder {
  id: string
  guestId: string
  guestName: string
  claimedByGuestId?: string
  claimedByGuestName?: string
  orderItemId?: string
  itemName?: string
  icon?: string
  customText?: string
  status: 'Open' | 'Done' | 'Cancelled'
  createdAt: string
  claimedAt?: string
  completedAt?: string
}

export interface PartyPulse {
  guestCount: number
  openOrderCount: number
  unclaimedOrderCount: number
  mediaCount: number
  openMusicRequestCount: number
  guestbookEntryCount: number
  topDrinkName?: string
  topDrinkCount: number
}

export interface PartyFeedItem {
  type: 'order' | 'claim' | 'media' | 'music' | 'guestbook'
  emoji: string
  text: string
  createdAt: string
}

export interface PartyMedia {
  id: string
  guestId: string
  guestName: string
  mediaType: 'image' | 'video'
  fileName: string
  mimeType: string
  size: number
  caption?: string
  createdAt: string
  contentUrl: string
}

export interface PartyGuestbookEntry {
  id: string
  guestId: string
  guestName: string
  message: string
  createdAt: string
}

export interface PartyMusicRequest {
  id: string
  guestId: string
  guestName: string
  song: string
  artist?: string
  comment?: string
  status: 'Open' | 'Played' | 'Rejected'
  createdAt: string
  spotifyTrackId?: string
  spotifyUri?: string
  spotifyAlbumImageUrl?: string
  durationMs?: number
  spotifyQueuedAt?: string
  voteCount: number
  hasVoted: boolean
}

export interface SpotifyTrack {
  id: string
  uri: string
  name: string
  artist: string
  albumImageUrl?: string
  durationMs: number
}

export interface SpotifyNowPlaying extends SpotifyTrack {
  isPlaying: boolean
  progressMs: number
}

export interface PartySpotifyAdminStatus {
  isConfigured: boolean
  isConnected: boolean
  accountName?: string
  autoQueue: boolean
  nowPlaying?: SpotifyNowPlaying
}

export interface PartySpotifyPublicStatus {
  isConnected: boolean
  autoQueue: boolean
  nowPlaying?: SpotifyNowPlaying
}

export interface PartyInput {
  name: string
  description?: string
  type: string
  location?: string
  startAt: string
  endAt?: string
  welcomeText?: string
  isActive: boolean
  guestsCanViewGallery: boolean
  guestsCanViewGuestbook: boolean
}

export function listParties() {
  return apiRequest<Party[]>('/api/parties')
}

export function getParty(id: string) {
  return apiRequest<Party>(`/api/parties/${id}`)
}

export function createParty(input: PartyInput) {
  return apiRequest<Party>('/api/parties', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function updateParty(id: string, input: PartyInput) {
  return apiRequest<Party>(`/api/parties/${id}`, {
    method: 'PUT',
    body: JSON.stringify(input),
  })
}

export function archiveParty(id: string) {
  return apiRequest<void>(`/api/parties/${id}`, { method: 'DELETE' })
}

export function addPartyOrderItem(
  partyId: string,
  input: Omit<PartyOrderItem, 'id'>,
) {
  return apiRequest<PartyOrderItem>(`/api/parties/${partyId}/order-items`, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function updatePartyOrderItem(
  partyId: string,
  itemId: string,
  input: Omit<PartyOrderItem, 'id'>,
) {
  return apiRequest<PartyOrderItem>(
    `/api/parties/${partyId}/order-items/${itemId}`,
    { method: 'PUT', body: JSON.stringify(input) },
  )
}

export function listPartyOrders(partyId: string) {
  return apiRequest<PartyOrder[]>(`/api/parties/${partyId}/orders`)
}

export function setPartyOrderStatus(
  partyId: string,
  orderId: string,
  status: PartyOrder['status'],
) {
  return apiRequest<void>(`/api/parties/${partyId}/orders/${orderId}`, {
    method: 'PATCH',
    body: JSON.stringify({ status }),
  })
}

export function listAdminPartyMedia(partyId: string) {
  return apiRequest<PartyMedia[]>(`/api/parties/${partyId}/media`)
}

export function deleteAdminPartyMedia(partyId: string, mediaId: string) {
  return apiRequest<void>(`/api/parties/${partyId}/media/${mediaId}`, {
    method: 'DELETE',
  })
}

export function listPartyGuests(partyId: string) {
  return apiRequest<PartyGuest[]>(`/api/parties/${partyId}/guests`)
}

export async function downloadAdminPartyMedia(contentUrl: string) {
  const response = await apiFetch(contentUrl)
  if (!response.ok) throw await apiProblem(response)
  return response.blob()
}

export function listPartyMusic(partyId: string) {
  return apiRequest<PartyMusicRequest[]>(
    `/api/parties/${partyId}/music-requests`,
  )
}

export function setPartyMusicStatus(
  partyId: string,
  requestId: string,
  status: PartyMusicRequest['status'],
) {
  return apiRequest<void>(
    `/api/parties/${partyId}/music-requests/${requestId}`,
    {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    },
  )
}

export function getPartySpotifyStatus(partyId: string) {
  return apiRequest<PartySpotifyAdminStatus>(`/api/parties/${partyId}/spotify`)
}

export function connectPartySpotify(partyId: string) {
  return apiRequest<{ authorizeUrl: string }>(
    `/api/parties/${partyId}/spotify/connect`,
    { method: 'POST' },
  )
}

export function disconnectPartySpotify(partyId: string) {
  return apiRequest<void>(`/api/parties/${partyId}/spotify/disconnect`, {
    method: 'POST',
  })
}

export function updatePartySpotify(partyId: string, autoQueue: boolean) {
  return apiRequest<void>(`/api/parties/${partyId}/spotify`, {
    method: 'PATCH',
    body: JSON.stringify({ autoQueue }),
  })
}

export function queuePartyMusicRequest(partyId: string, requestId: string) {
  return apiRequest<void>(
    `/api/parties/${partyId}/spotify/queue/${requestId}`,
    { method: 'POST' },
  )
}

export function listAdminGuestbook(partyId: string) {
  return apiRequest<PartyGuestbookEntry[]>(`/api/parties/${partyId}/guestbook`)
}

export function deleteAdminGuestbookEntry(partyId: string, entryId: string) {
  return apiRequest<void>(`/api/parties/${partyId}/guestbook/${entryId}`, {
    method: 'DELETE',
  })
}

export function getPublicParty(slug: string) {
  return apiRequest<PublicParty>(
    `/api/parties/public/${encodeURIComponent(slug)}`,
  )
}

export function registerPartyGuest(slug: string, name: string) {
  return apiRequest<PartyGuestSession>(
    `/api/parties/public/${encodeURIComponent(slug)}/guests`,
    { method: 'POST', body: JSON.stringify({ name }) },
  )
}

export function updatePartyGuest(slug: string, token: string, name: string) {
  return guestRequest<PartyGuest>(slug, token, '/guests/me', {
    method: 'PATCH',
    body: JSON.stringify({ name }),
  })
}

export function getPartyGuest(slug: string, token: string) {
  return guestRequest<PartyGuest>(slug, token, '/guests/me')
}

export function guestRequest<T>(
  slug: string,
  token: string,
  path: string,
  init: RequestInit = {},
) {
  const headers = new Headers(init.headers)
  headers.set('X-Party-Session', token)
  return apiRequest<T>(
    `/api/parties/public/${encodeURIComponent(slug)}${path}`,
    { ...init, headers },
  )
}

export function createGuestOrder(
  slug: string,
  token: string,
  orderItemId?: string,
  customText?: string,
) {
  return guestRequest<{ id: string; item: string }>(slug, token, '/orders', {
    method: 'POST',
    body: JSON.stringify({ orderItemId, customText }),
  })
}

export function listGuestOrders(slug: string, token: string) {
  return guestRequest<PartyOrder[]>(slug, token, '/orders')
}

export function claimGuestOrders(
  slug: string,
  token: string,
  orderIds: string[],
) {
  return guestRequest<{ claimed: number }>(slug, token, '/orders/claim', {
    method: 'POST',
    body: JSON.stringify({ orderIds }),
  })
}

export function releaseGuestOrder(slug: string, token: string, orderId: string) {
  return guestRequest<void>(slug, token, `/orders/${orderId}/release`, {
    method: 'POST',
  })
}

export function completeGuestOrder(slug: string, token: string, orderId: string) {
  return guestRequest<void>(slug, token, `/orders/${orderId}/done`, {
    method: 'POST',
  })
}

export function cancelGuestOrder(slug: string, token: string, orderId: string) {
  return guestRequest<void>(slug, token, `/orders/${orderId}`, {
    method: 'DELETE',
  })
}

export function getPartyPulse(slug: string, token: string) {
  return guestRequest<PartyPulse>(slug, token, '/pulse')
}

export function getPartyFeed(slug: string, token: string) {
  return guestRequest<PartyFeedItem[]>(slug, token, '/feed')
}

export function uploadGuestMedia(
  slug: string,
  token: string,
  file: File,
  caption?: string,
  onProgress?: (progress: number) => void,
) {
  const form = new FormData()
  form.append('file', file)
  if (caption) form.append('caption', caption)
  return new Promise<PartyMedia>((resolve, reject) => {
    const request = new XMLHttpRequest()
    request.open(
      'POST',
      `/api/parties/public/${encodeURIComponent(slug)}/media`,
    )
    request.withCredentials = true
    request.setRequestHeader('X-Party-Session', token)
    request.upload.onprogress = (event) => {
      if (event.lengthComputable) {
        onProgress?.(Math.round((event.loaded / event.total) * 100))
      }
    }
    request.onerror = () =>
      reject(new Error('Netzwerkfehler beim Upload. Bitte versuche es erneut.'))
    request.onabort = () => reject(new Error('Upload wurde abgebrochen.'))
    request.onload = () => {
      if (request.status >= 200 && request.status < 300) {
        onProgress?.(100)
        resolve(JSON.parse(request.responseText) as PartyMedia)
        return
      }
      let message = 'Upload fehlgeschlagen. Bitte versuche es erneut.'
      try {
        const problem = JSON.parse(request.responseText) as {
          title?: string
          detail?: string
          errors?: Record<string, string[]>
        }
        message =
          problem.detail ??
          Object.values(problem.errors ?? {})[0]?.[0] ??
          problem.title ??
          message
      } catch {
        // The generic message is intentionally used for non-JSON failures.
      }
      reject(new Error(message))
    }
    request.send(form)
  })
}

export function listGuestMedia(slug: string, token: string) {
  return guestRequest<PartyMedia[]>(slug, token, '/media')
}

export function listOwnGuestMedia(slug: string, token: string) {
  return guestRequest<PartyMedia[]>(slug, token, '/media/mine')
}

export async function downloadGuestMedia(contentUrl: string, token: string) {
  const response = await apiFetch(contentUrl, {
    headers: { 'X-Party-Session': token },
  })
  if (!response.ok) throw await apiProblem(response)
  return response.blob()
}

export function listGuestbook(slug: string, token: string) {
  return guestRequest<PartyGuestbookEntry[]>(slug, token, '/guestbook')
}

export function addGuestbookEntry(
  slug: string,
  token: string,
  message: string,
) {
  return guestRequest<PartyGuestbookEntry>(slug, token, '/guestbook', {
    method: 'POST',
    body: JSON.stringify({ message }),
  })
}

export function addMusicRequest(
  slug: string,
  token: string,
  input: {
    song: string
    artist?: string
    comment?: string
    spotifyTrackId?: string
  },
) {
  return guestRequest<PartyMusicRequest>(slug, token, '/music-requests', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function listGuestMusicRequests(slug: string, token: string) {
  return guestRequest<PartyMusicRequest[]>(slug, token, '/music-requests')
}

export function toggleGuestMusicVote(
  slug: string,
  token: string,
  requestId: string,
) {
  return guestRequest<{ voteCount: number; hasVoted: boolean }>(
    slug,
    token,
    `/music-requests/${requestId}/vote`,
    { method: 'POST' },
  )
}

export function getGuestSpotifyStatus(slug: string, token: string) {
  return guestRequest<PartySpotifyPublicStatus>(slug, token, '/spotify')
}

export function searchPartySpotify(
  slug: string,
  token: string,
  query: string,
) {
  return guestRequest<SpotifyTrack[]>(
    slug,
    token,
    `/spotify/search?q=${encodeURIComponent(query)}`,
  )
}

export function listOwnMusicRequests(slug: string, token: string) {
  return guestRequest<PartyMusicRequest[]>(slug, token, '/music-requests/mine')
}
