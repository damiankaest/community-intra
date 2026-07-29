import { apiRequest } from './client'

export type LiveServerConnectionState =
  | 'NotConfigured'
  | 'Disabled'
  | 'Online'
  | 'Reachable'
  | 'Offline'
  | 'AuthenticationFailed'
  | 'UntrustedCertificate'
  | 'CertificateChanged'
  | 'ConfigurationError'

export interface GameServerConfiguration {
  id: string
  displayName: string
  host: string
  port: number
  hasApiToken: boolean
  certificateFingerprint?: string
  isEnabled: boolean
  updatedAt: string
  concurrencyToken: string
}

export interface SaveGameServerConfiguration {
  displayName: string
  host: string
  port: number
  apiToken?: string
  certificateFingerprint?: string
  isEnabled: boolean
  concurrencyToken?: string
}

export interface LiveServerStatus {
  state: LiveServerConnectionState
  displayName?: string
  host?: string
  port?: number
  health?: string
  activeSessionName?: string
  connectedPlayers?: number
  playerLimit?: number
  techTier?: number
  activeSchematic?: string
  gamePhase?: string
  isGameRunning?: boolean
  isGamePaused?: boolean
  totalGameDurationSeconds?: number
  averageTickRate?: number
  checkedAt: string
  message: string
  presentedCertificateFingerprint?: string
}

const base = (organizationId: string) =>
  `/api/organizations/${organizationId}/live-operations/server`

export function getLiveServerStatus(
  organizationId: string,
  forceRefresh = false,
) {
  return apiRequest<LiveServerStatus>(
    `${base(organizationId)}/status?forceRefresh=${forceRefresh}`,
  )
}

export function getGameServerConfiguration(organizationId: string) {
  return apiRequest<GameServerConfiguration | undefined>(
    `${base(organizationId)}/configuration`,
  )
}

export function saveGameServerConfiguration(
  organizationId: string,
  input: SaveGameServerConfiguration,
) {
  return apiRequest<GameServerConfiguration>(
    `${base(organizationId)}/configuration`,
    {
      method: 'PUT',
      body: JSON.stringify(input),
    },
  )
}

export function testGameServerConnection(
  organizationId: string,
  input: Omit<SaveGameServerConfiguration, 'isEnabled' | 'concurrencyToken'>,
) {
  return apiRequest<LiveServerStatus>(`${base(organizationId)}/test`, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function disconnectGameServer(organizationId: string) {
  return apiRequest<void>(`${base(organizationId)}/configuration`, {
    method: 'DELETE',
  })
}
