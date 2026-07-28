export interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly errors?: Record<string, string[]>

  constructor(status: number, problem?: ProblemDetails) {
    super(
      problem?.detail ?? problem?.title ?? 'Die Anfrage ist fehlgeschlagen.',
    )
    this.name = 'ApiError'
    this.status = status
    this.errors = problem?.errors
  }
}

interface RefreshResponse {
  accessToken: string
}

let accessToken = readStoredToken()
let refreshRequest: Promise<boolean> | undefined

export function setAccessToken(token: string | undefined) {
  accessToken = token

  if (token) {
    sessionStorage.setItem('community-access-token', token)
  } else {
    sessionStorage.removeItem('community-access-token')
  }
}

export function getAccessToken() {
  return accessToken
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
  retryAuthentication = true,
): Promise<T> {
  const response = await fetch(path, createRequestInit(init))

  if (
    response.status === 401 &&
    retryAuthentication &&
    path !== '/api/auth/refresh'
  ) {
    const refreshed = await refreshAccessToken()
    if (refreshed) {
      return apiRequest<T>(path, init, false)
    }
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readProblemDetails(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

function createRequestInit(init: RequestInit): RequestInit {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }

  return {
    ...init,
    headers,
    credentials: 'include',
  }
}

async function refreshAccessToken(): Promise<boolean> {
  refreshRequest ??= performRefresh()

  try {
    return await refreshRequest
  } finally {
    refreshRequest = undefined
  }
}

async function performRefresh(): Promise<boolean> {
  const response = await fetch('/api/auth/refresh', {
    method: 'POST',
    credentials: 'include',
  })
  if (!response.ok) {
    setAccessToken(undefined)
    return false
  }

  const result = (await response.json()) as RefreshResponse
  setAccessToken(result.accessToken)
  return true
}

async function readProblemDetails(
  response: Response,
): Promise<ProblemDetails | undefined> {
  const contentType = response.headers.get('content-type')
  if (!contentType?.includes('json')) {
    return undefined
  }

  return (await response.json()) as ProblemDetails
}

function readStoredToken() {
  return sessionStorage.getItem('community-access-token') ?? undefined
}
