import { apiRequest, setAccessToken } from './client'

export interface CurrentUser {
  id: string
  email: string
  displayName: string
  avatarUrl?: string
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAt: string
  user: CurrentUser
}

export interface RegisterInput {
  email: string
  password: string
  displayName: string
}

export interface LoginInput {
  email: string
  password: string
}

export interface AuthProviders {
  google: boolean
  discord: boolean
  steam: boolean
}

export interface ConnectedAccount {
  connected: boolean
  displayName?: string
  avatarUrl?: string
  steamId64?: string
  linkedAt?: string
}

export interface ConnectedAccounts {
  google: ConnectedAccount
  discord: ConnectedAccount
  steam: ConnectedAccount
}

export async function register(input: RegisterInput) {
  const response = await apiRequest<AuthResponse>(
    '/api/auth/register',
    {
      method: 'POST',
      body: JSON.stringify(input),
    },
    false,
  )
  setAccessToken(response.accessToken)
  return response
}

export async function login(input: LoginInput) {
  const response = await apiRequest<AuthResponse>(
    '/api/auth/login',
    {
      method: 'POST',
      body: JSON.stringify(input),
    },
    false,
  )
  setAccessToken(response.accessToken)
  return response
}

export async function logout() {
  try {
    await apiRequest<void>(
      '/api/auth/logout',
      {
        method: 'POST',
      },
      false,
    )
  } finally {
    setAccessToken(undefined)
  }
}

export function getCurrentUser() {
  return apiRequest<CurrentUser>('/api/auth/me')
}

export const getAuthProviders = () =>
  apiRequest<AuthProviders>('/api/auth/providers', {}, false)

export const forgotPassword = (email: string) =>
  apiRequest<{ message: string }>(
    '/api/auth/forgot-password',
    { method: 'POST', body: JSON.stringify({ email }) },
    false,
  )

export const resetPassword = (
  email: string,
  token: string,
  newPassword: string,
) =>
  apiRequest<void>(
    '/api/auth/reset-password',
    {
      method: 'POST',
      body: JSON.stringify({ email, token, newPassword }),
    },
    false,
  )

export const getConnectedAccounts = () =>
  apiRequest<ConnectedAccounts>('/api/auth/connections')

export const createConnectedAccountLink = (
  provider: 'google' | 'discord' | 'steam',
  returnUrl = '/account',
) =>
  apiRequest<{ url: string }>(
    `/api/auth/connections/${provider}?returnUrl=${encodeURIComponent(returnUrl)}`,
    { method: 'POST' },
  )

export const disconnectAccount = (
  provider: 'google' | 'discord' | 'steam',
) =>
  apiRequest<void>(`/api/auth/connections/${provider}`, { method: 'DELETE' })
