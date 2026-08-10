import { describe, expect, it } from 'vitest'
import { invitationReturnPath } from './invitationRoutes'

describe('invitationReturnPath', () => {
  const organizationId = '4e07bbb0-2816-411f-b897-368ea453ee11'

  it('allows CS2 destinations inside the invited organization', () => {
    expect(invitationReturnPath(`/cs2/${organizationId}`, organizationId)).toBe(
      `/cs2/${organizationId}`,
    )
    expect(
      invitationReturnPath(`/cs2/${organizationId}/squad`, organizationId),
    ).toBe(`/cs2/${organizationId}/squad`)
  })

  it('rejects destinations outside the invited CS2 area', () => {
    expect(invitationReturnPath('//example.com', organizationId)).toBe(
      `/organizations/${organizationId}`,
    )
    expect(invitationReturnPath('/cs2/another-organization', organizationId)).toBe(
      `/organizations/${organizationId}`,
    )
  })
})
