import { describe, expect, it } from 'vitest'
import {
  demoFileKey,
  formatDemoSize,
  mergeDemoFiles,
} from './counterStrikeImports'

describe('counter-strike demo selection', () => {
  const demo = (name: string, content = 'PBDEMS2') =>
    new File([content], name, { lastModified: 1 })

  it('keeps valid demos and rejects unrelated files', () => {
    const result = mergeDemoFiles([], [demo('match.dem'), demo('notes.txt')])

    expect(result.files.map((file) => file.name)).toEqual(['match.dem'])
    expect(result.rejectedCount).toBe(1)
  })

  it('does not enqueue the same file twice', () => {
    const file = demo('match.dem')
    const result = mergeDemoFiles([file], [file])

    expect(result.files).toHaveLength(1)
    expect(result.duplicateCount).toBe(1)
    expect(demoFileKey(result.files[0])).toBe(demoFileKey(file))
  })

  it('formats compact file sizes', () => {
    expect(formatDemoSize(512)).toBe('1 KB')
    expect(formatDemoSize(5 * 1024 * 1024)).toBe('5.0 MB')
  })
})
