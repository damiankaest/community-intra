import { afterEach, describe, expect, it, vi } from 'vitest'
import { getSystemInfo } from './system'

describe('getSystemInfo', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns the typed backend response', async () => {
    const payload = {
      name: 'Community Intranet',
      version: '0.1.0',
      environment: 'Development',
      status: 'Operational',
    }

    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify(payload), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
      ),
    )

    await expect(getSystemInfo()).resolves.toEqual(payload)
  })

  it('rejects unsuccessful responses', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(null, { status: 503 })),
    )

    await expect(getSystemInfo()).rejects.toThrow('status 503')
  })
})
