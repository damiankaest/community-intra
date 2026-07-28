export interface SystemInfo {
  name: string
  version: string
  environment: string
  status: string
}

export interface HealthCheck {
  status: string
  description: string | null
  durationMilliseconds: number
}

export interface HealthResponse {
  status: string
  checkedAt: string
  checks: Record<string, HealthCheck>
}

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(path, {
    headers: {
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(`Request to ${path} failed with status ${response.status}`)
  }

  return (await response.json()) as T
}

export function getSystemInfo(): Promise<SystemInfo> {
  return getJson<SystemInfo>('/api/system/info')
}

export function getHealth(): Promise<HealthResponse> {
  return getJson<HealthResponse>('/api/health')
}
