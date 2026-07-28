import { afterEach, describe, expect, it, vi } from 'vitest'
import { login, logout } from './auth'
import { getAccessToken, setAccessToken } from './client'

describe('auth session', () => {
  afterEach(() => {
    setAccessToken(undefined)
    vi.unstubAllGlobals()
  })

  it('keeps the short-lived access token after login', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      Response.json({
        accessToken: 'access-token',
        accessTokenExpiresAt: '2026-07-28T12:00:00Z',
        user: {
          id: '6bedf99f-3475-4bff-ab81-0c70b35fd681',
          email: 'damian@example.com',
          displayName: 'Damian',
        },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    const response = await login({
      email: 'damian@example.com',
      password: 'ReallySecure123',
    })

    expect(response.user.displayName).toBe('Damian')
    expect(getAccessToken()).toBe('access-token')
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/auth/login',
      expect.objectContaining({
        method: 'POST',
        credentials: 'include',
      }),
    )
  })

  it('clears the access token on logout', async () => {
    setAccessToken('access-token')
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(undefined, { status: 204 })),
    )

    await logout()

    expect(getAccessToken()).toBeUndefined()
  })
})
