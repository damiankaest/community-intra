import type {
  BuildingCategory,
  BuildingTypeSummary,
  DetectedFactoryArea,
  SaveAnalysis,
  WorldBounds,
} from "./contracts.js";

interface SaveObject {
  typePath?: unknown;
  instanceName?: unknown;
  needTransform?: unknown;
  transform?: {
    translation?: {
      x?: unknown;
      y?: unknown;
      z?: unknown;
    };
  };
}

interface PositionedBuildable {
  typePath: string;
  displayName: string;
  category: BuildingCategory;
  x: number;
  y: number;
}

const productionMarkers = [
  "constructor",
  "assembler",
  "manufacturer",
  "smelter",
  "foundry",
  "refinery",
  "blender",
  "packager",
  "hadroncollider",
  "converter",
  "quantumencoder",
  "biochemicalsculptor",
];
const extractionMarkers = [
  "miner",
  "oilpump",
  "waterpump",
  "frackingextractor",
  "frackingactivator",
];
const powerMarkers = [
  "generator",
  "powerpole",
  "powerstorage",
  "geothermal",
  "nuclear",
  "coal",
];
const logisticsMarkers = [
  "conveyor",
  "pipeline",
  "pipe",
  "merger",
  "splitter",
  "pump",
  "lift",
];
const storageMarkers = [
  "storagecontainer",
  "industrialstorage",
  "fluidbuffer",
  "trainplatformcargo",
];
const transportMarkers = [
  "trainstation",
  "railroad",
  "drone",
  "truckstation",
  "vehicle",
  "hypertube",
];
const infrastructureMarkers = [
  "foundation",
  "wall",
  "roof",
  "ramp",
  "walkway",
  "ladder",
  "pillar",
  "frame",
];

export function analyzeParsedSave(
  input: unknown,
  fallbackName: string,
): SaveAnalysis {
  const save = asRecord(input);
  const header = asRecord(save.header);
  const objects = objectValues(save.levels).flatMap((level) => {
    const value = asRecord(level);
    return Array.isArray(value.objects) ? value.objects : [];
  });
  const buildables = objects
    .map(toBuildable)
    .filter((item): item is PositionedBuildable => item !== undefined);
  const groupedTypes = new Map<string, BuildingTypeSummary>();
  for (const buildable of buildables) {
    const existing = groupedTypes.get(buildable.typePath);
    if (existing) {
      existing.count += 1;
    } else {
      groupedTypes.set(buildable.typePath, {
        typePath: buildable.typePath,
        className: className(buildable.typePath),
        displayName: buildable.displayName,
        category: buildable.category,
        count: 1,
      });
    }
  }

  const buildingTypes = [...groupedTypes.values()].sort(
    (left, right) => right.count - left.count,
  );
  const countCategory = (category: BuildingCategory) =>
    buildables.filter((item) => item.category === category).length;
  const infrastructure = buildables.filter(
    (item) => item.category === "Infrastructure",
  );
  const foundations = infrastructure.filter((item) =>
    item.typePath.toLowerCase().includes("foundation"),
  ).length;
  const knownCount = [
    "Production",
    "Extraction",
    "Power",
    "Logistics",
    "Storage",
    "Transport",
    "Infrastructure",
  ].reduce(
    (total, category) => total + countCategory(category as BuildingCategory),
    0,
  );

  return {
    parserVersion: "4.1.2",
    saveName: stringValue(header.saveName) ?? fallbackName,
    sessionName: stringValue(header.sessionName),
    mapName: stringValue(header.mapName),
    saveVersion: numberValue(header.saveVersion),
    buildVersion: numberValue(header.buildVersion),
    playDurationSeconds: numberValue(header.playDurationSeconds),
    savedAt: dateValue(header.saveDateTime),
    isModdedSave: booleanValue(header.isModdedSave),
    totals: {
      objects: objects.length,
      buildables: buildables.length,
      productionMachines: countCategory("Production"),
      extractors: countCategory("Extraction"),
      powerBuildings: countCategory("Power"),
      logistics: countCategory("Logistics"),
      storageBuildings: countCategory("Storage"),
      transportBuildings: countCategory("Transport"),
      foundations,
      otherBuildables: buildables.length - knownCount,
    },
    bounds: calculateBounds(buildables),
    buildingTypes,
    detectedAreas: detectFactoryAreas(buildables),
  };
}

function toBuildable(input: unknown): PositionedBuildable | undefined {
  const object = input as SaveObject;
  const typePath = stringValue(object?.typePath);
  const translation = object?.transform?.translation;
  const x = numberValue(translation?.x);
  const y = numberValue(translation?.y);
  if (
    !typePath ||
    !typePath.toLowerCase().includes("build_") ||
    x === undefined ||
    y === undefined
  ) {
    return undefined;
  }

  return {
    typePath,
    displayName: displayName(typePath),
    category: categorize(typePath),
    x,
    y,
  };
}

function categorize(typePath: string): BuildingCategory {
  const value = typePath.toLowerCase().replaceAll("_", "");
  if (matches(value, productionMarkers)) return "Production";
  if (matches(value, extractionMarkers)) return "Extraction";
  if (matches(value, storageMarkers)) return "Storage";
  if (matches(value, transportMarkers)) return "Transport";
  if (matches(value, powerMarkers)) return "Power";
  if (matches(value, logisticsMarkers)) return "Logistics";
  if (matches(value, infrastructureMarkers)) return "Infrastructure";
  return "Other";
}

function detectFactoryAreas(
  buildables: PositionedBuildable[],
): DetectedFactoryArea[] {
  const anchors = buildables.filter((item) =>
    ["Production", "Extraction", "Power"].includes(item.category),
  );
  if (anchors.length === 0) return [];

  const maximumDistance = 15_000;
  const cells = new Map<string, number[]>();
  anchors.forEach((item, index) => {
    const key = cellKey(item.x, item.y, maximumDistance);
    const indices = cells.get(key) ?? [];
    indices.push(index);
    cells.set(key, indices);
  });
  const visited = new Set<number>();
  const clusters: PositionedBuildable[][] = [];

  for (let start = 0; start < anchors.length; start += 1) {
    if (visited.has(start)) continue;
    const cluster: PositionedBuildable[] = [];
    const queue = [start];
    visited.add(start);
    while (queue.length > 0) {
      const currentIndex = queue.shift();
      if (currentIndex === undefined) break;
      const current = anchors[currentIndex];
      if (!current) continue;
      cluster.push(current);
      const cellX = Math.floor(current.x / maximumDistance);
      const cellY = Math.floor(current.y / maximumDistance);
      for (let offsetX = -1; offsetX <= 1; offsetX += 1) {
        for (let offsetY = -1; offsetY <= 1; offsetY += 1) {
          const neighbors =
            cells.get(`${cellX + offsetX}:${cellY + offsetY}`) ?? [];
          for (const neighborIndex of neighbors) {
            if (visited.has(neighborIndex)) continue;
            const neighbor = anchors[neighborIndex];
            if (
              neighbor &&
              squaredDistance(current, neighbor) <= maximumDistance ** 2
            ) {
              visited.add(neighborIndex);
              queue.push(neighborIndex);
            }
          }
        }
      }
    }
    if (cluster.length >= 3) clusters.push(cluster);
  }

  return clusters
    .map((cluster) => summarizeArea(cluster, buildables))
    .sort((left, right) => right.machineCount - left.machineCount)
    .slice(0, 24)
    .map((area, index) => ({
      ...area,
      key: `factory-area-${index + 1}`,
      suggestedName: `Fabrikbereich ${index + 1}`,
    }));
}

function summarizeArea(
  anchors: PositionedBuildable[],
  allBuildables: PositionedBuildable[],
): Omit<DetectedFactoryArea, "key" | "suggestedName"> {
  const centerX =
    anchors.reduce((total, item) => total + item.x, 0) / anchors.length;
  const centerY =
    anchors.reduce((total, item) => total + item.y, 0) / anchors.length;
  const anchorRadius = Math.max(
    5_000,
    ...anchors.map((item) =>
      Math.sqrt((item.x - centerX) ** 2 + (item.y - centerY) ** 2),
    ),
  );
  const radius = anchorRadius + 5_000;
  const members = allBuildables.filter(
    (item) => (item.x - centerX) ** 2 + (item.y - centerY) ** 2 <= radius ** 2,
  );
  const typeCounts = new Map<string, number>();
  for (const item of members) {
    typeCounts.set(
      item.displayName,
      (typeCounts.get(item.displayName) ?? 0) + 1,
    );
  }

  return {
    centerX: Math.round(centerX),
    centerY: Math.round(centerY),
    radiusMeters: Math.ceil(radius / 100),
    machineCount: anchors.length,
    buildableCount: members.length,
    topBuildingTypes: [...typeCounts.entries()]
      .map(([name, count]) => ({ displayName: name, count }))
      .sort((left, right) => right.count - left.count)
      .slice(0, 6),
  };
}

function calculateBounds(
  buildables: PositionedBuildable[],
): WorldBounds | undefined {
  if (buildables.length === 0) return undefined;
  return {
    minimumX: Math.min(...buildables.map((item) => item.x)),
    minimumY: Math.min(...buildables.map((item) => item.y)),
    maximumX: Math.max(...buildables.map((item) => item.x)),
    maximumY: Math.max(...buildables.map((item) => item.y)),
  };
}

function displayName(typePath: string) {
  const value = className(typePath)
    .replace(/^Build_/, "")
    .replace(/_C$/, "")
    .replace(/Mk(\d+)/g, " Mk.$1")
    .replaceAll("_", " ")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .trim();
  return value || "Unbekanntes Gebäude";
}

function className(typePath: string) {
  const separator = typePath.lastIndexOf(".");
  return separator >= 0 ? typePath.slice(separator + 1) : typePath;
}

function matches(value: string, markers: string[]) {
  return markers.some((marker) => value.includes(marker));
}

function cellKey(x: number, y: number, size: number) {
  return `${Math.floor(x / size)}:${Math.floor(y / size)}`;
}

function squaredDistance(
  left: PositionedBuildable,
  right: PositionedBuildable,
) {
  return (left.x - right.x) ** 2 + (left.y - right.y) ** 2;
}

function objectValues(input: unknown): unknown[] {
  if (Array.isArray(input)) return input;
  return input && typeof input === "object" ? Object.values(input) : [];
}

function asRecord(input: unknown): Record<string, unknown> {
  return input && typeof input === "object"
    ? (input as Record<string, unknown>)
    : {};
}

function stringValue(input: unknown) {
  return typeof input === "string" && input.trim() ? input.trim() : undefined;
}

function numberValue(input: unknown) {
  return typeof input === "number" && Number.isFinite(input)
    ? input
    : undefined;
}

function booleanValue(input: unknown) {
  if (typeof input === "boolean") return input;
  return typeof input === "number" && Number.isFinite(input)
    ? input !== 0
    : undefined;
}

function dateValue(input: unknown) {
  if (typeof input === "string") return input;
  if (typeof input === "number" && Number.isFinite(input)) {
    return new Date(input).toISOString();
  }
  return undefined;
}
