import { apiRequest } from './client'
import type { LiveServerConnectionState } from './liveOperations'

export type SaveImportSource = 'ManualUpload' | 'ServerApi'

export interface SaveTotals {
  objects: number
  buildables: number
  productionMachines: number
  extractors: number
  powerBuildings: number
  logistics: number
  storageBuildings: number
  transportBuildings: number
  foundations: number
  otherBuildables: number
}

export interface BuildingTypeSummary {
  typePath: string
  className: string
  displayName: string
  category: string
  count: number
}

export interface DetectedFactoryArea {
  key: string
  suggestedName: string
  centerX: number
  centerY: number
  radiusMeters: number
  machineCount: number
  buildableCount: number
  topBuildingTypes: Array<{ displayName: string; count: number }>
}

export interface SaveAnalysis {
  parserVersion: string
  saveName?: string
  sessionName?: string
  mapName?: string
  saveVersion?: number
  buildVersion?: number
  playDurationSeconds?: number
  savedAt?: string
  isModdedSave?: boolean
  totals: SaveTotals
  buildingTypes: BuildingTypeSummary[]
  detectedAreas: DetectedFactoryArea[]
}

export interface SaveSnapshot {
  id: string
  source: SaveImportSource
  originalFileName: string
  contentSha256: string
  fileSizeBytes: number
  saveName?: string
  sessionName?: string
  mapName?: string
  saveVersion?: number
  buildVersion?: number
  playDurationSeconds?: number
  savedAt?: string
  isModdedSave?: boolean
  parserVersion: string
  importedAt: string
  analysis?: SaveAnalysis
}

export interface FactorySite {
  id: string
  name: string
  description?: string
  centerX?: number
  centerY?: number
  radiusMeters?: number
  machineCount?: number
  buildableCount?: number
  updatedAt: string
  concurrencyToken: string
}

export interface FactoryInsightsOverview {
  factories: FactorySite[]
  latestSnapshot?: SaveSnapshot
  recentSnapshots: SaveSnapshot[]
  saveParserAvailable: boolean
  serverState: LiveServerConnectionState
  serverMessage: string
}

export interface SaveFactoryInput {
  name: string
  description?: string
  centerX?: number
  centerY?: number
  radiusMeters?: number
}

const base = (organizationId: string) =>
  `/api/organizations/${organizationId}/factory-insights`

export function getFactoryInsights(organizationId: string) {
  return apiRequest<FactoryInsightsOverview>(base(organizationId))
}

export function uploadSaveFile(organizationId: string, file: File) {
  const body = new FormData()
  body.append('file', file)
  return apiRequest<SaveSnapshot>(`${base(organizationId)}/imports/manual`, {
    method: 'POST',
    body,
  })
}

export function importServerSave(
  organizationId: string,
  saveName?: string,
) {
  return apiRequest<SaveSnapshot>(`${base(organizationId)}/imports/server`, {
    method: 'POST',
    body: JSON.stringify({ saveName }),
  })
}

export function createFactory(
  organizationId: string,
  input: SaveFactoryInput,
) {
  return apiRequest<FactorySite>(`${base(organizationId)}/factories`, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function deleteFactory(organizationId: string, factoryId: string) {
  return apiRequest<void>(
    `${base(organizationId)}/factories/${factoryId}`,
    { method: 'DELETE' },
  )
}
