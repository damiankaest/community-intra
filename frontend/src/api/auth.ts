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
