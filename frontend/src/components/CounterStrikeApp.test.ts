import { describe, expect, it } from 'vitest'
import { cs2Path } from './counterStrikeRoutes'

describe('cs2Path', () => {
  const organizationId = '4e07bbb0-2816-411f-b897-368ea453ee11'

  it('builds navigation targets from the CS2 root', () => {
    expect(cs2Path(organizationId)).toBe(`/cs2/${organizationId}`)
    expect(cs2Path(organizationId, 'matches')).toBe(
      `/cs2/${organizationId}/matches`,
    )
    expect(cs2Path(organizationId, 'season/recap')).toBe(
      `/cs2/${organizationId}/season/recap`,
    )
  })

  it('keeps training query parameters on the absolute target', () => {
    expect(cs2Path(organizationId, 'training/aim?mode=flick')).toBe(
      `/cs2/${organizationId}/training/aim?mode=flick`,
    )
  })
})
