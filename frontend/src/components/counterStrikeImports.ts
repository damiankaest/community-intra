export interface DemoFileSelection {
  files: File[]
  rejectedCount: number
  duplicateCount: number
}

export function demoFileKey(file: File) {
  return `${file.name}:${file.size}:${file.lastModified}`
}

export function mergeDemoFiles(
  existing: File[],
  candidates: Iterable<File>,
): DemoFileSelection {
  const files = [...existing]
  const known = new Set(files.map(demoFileKey))
  let rejectedCount = 0
  let duplicateCount = 0

  for (const file of candidates) {
    if (!file.name.toLowerCase().endsWith('.dem') || file.size === 0) {
      rejectedCount += 1
      continue
    }

    const key = demoFileKey(file)
    if (known.has(key)) {
      duplicateCount += 1
      continue
    }

    known.add(key)
    files.push(file)
  }

  return { files, rejectedCount, duplicateCount }
}

export function formatDemoSize(bytes: number) {
  if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
