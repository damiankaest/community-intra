import { afterEach, describe, expect, it, vi } from 'vitest'
import { createInvitation, resolveInvitation } from './members'

describe('member invitations', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('resolves an invitation without putting the token in the request URL', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      Response.json({
        invitationId: 'invite-id',
        organizationId: 'organization-id',
        organizationName: 'Rheinische FICSIT-Niederlassung',
        themePackKey: 'satisfactory-ficsit',
        defaultPermissionRole: 'Member',
        expiresAt: '2026-08-04T12:00:00Z',
        remainingUses: 1,
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await resolveInvitation('secret-invitation-token')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/invitations/resolve',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ token: 'secret-invitation-token' }),
      }),
    )
  })

  it('creates an organization-scoped invitation', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      Response.json({
        id: 'invite-id',
        token: 'generated-token',
        defaultPermissionRole: 'Member',
        expiresAt: '2026-08-04T12:00:00Z',
        maximumUses: 1,
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await createInvitation('organization-id', {
      defaultPermissionRole: 'Member',
      expiresInDays: 7,
      maximumUses: 1,
    })

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/organizations/organization-id/invitations',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          defaultPermissionRole: 'Member',
          expiresInDays: 7,
          maximumUses: 1,
        }),
      }),
    )
  })
})
