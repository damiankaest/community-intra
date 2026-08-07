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
  orderItemId?: string
  itemName?: string
  icon?: string
  customText?: string
  status: 'Open' | 'Done' | 'Cancelled'
  createdAt: string
  completedAt?: string
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

export function uploadGuestMedia(
  slug: string,
  token: string,
  file: File,
  caption?: string,
) {
  const form = new FormData()
  form.append('file', file)
  if (caption) form.append('caption', caption)
  return guestRequest<PartyMedia>(slug, token, '/media', {
    method: 'POST',
    body: form,
  })
}

export function listGuestMedia(slug: string, token: string) {
  return guestRequest<PartyMedia[]>(slug, token, '/media')
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
  input: { song: string; artist?: string; comment?: string },
) {
  return guestRequest<PartyMusicRequest>(slug, token, '/music-requests', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}
