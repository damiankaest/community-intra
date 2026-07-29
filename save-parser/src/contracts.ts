export interface SaveAnalysis {
  parserVersion: string;
  saveName?: string;
  sessionName?: string;
  mapName?: string;
  saveVersion?: number;
  buildVersion?: number;
  playDurationSeconds?: number;
  savedAt?: string;
  isModdedSave?: boolean;
  totals: SaveTotals;
  bounds?: WorldBounds;
  buildingTypes: BuildingTypeSummary[];
  detectedAreas: DetectedFactoryArea[];
}

export interface SaveTotals {
  objects: number;
  buildables: number;
  productionMachines: number;
  extractors: number;
  powerBuildings: number;
  logistics: number;
  storageBuildings: number;
  transportBuildings: number;
  foundations: number;
  otherBuildables: number;
}

export interface WorldBounds {
  minimumX: number;
  minimumY: number;
  maximumX: number;
  maximumY: number;
}

export interface BuildingTypeSummary {
  typePath: string;
  className: string;
  displayName: string;
  category: BuildingCategory;
  count: number;
}

export interface DetectedFactoryArea {
  key: string;
  suggestedName: string;
  centerX: number;
  centerY: number;
  radiusMeters: number;
  machineCount: number;
  buildableCount: number;
  topBuildingTypes: Array<{
    displayName: string;
    count: number;
  }>;
}

export type BuildingCategory =
  | "Production"
  | "Extraction"
  | "Power"
  | "Logistics"
  | "Storage"
  | "Transport"
  | "Infrastructure"
  | "Other";
