import { apiRequest } from './client'

export type WorkLogKind = 'Built' | 'Fixed' | 'Optimized' | 'Destroyed'

export interface WorkShift {
  id: string
  memberId: string
  startedAt: string
  endedAt?: string
  elapsedSeconds: number
}

export interface WorkLogEntry {
  id: string
  memberId: string
  memberDisplayName?: string
  kind: WorkLogKind
  note: string
  createdAt: string
}

export interface ActiveMember {
  memberId: string
  displayName?: string
  startedAt: string
  elapsedSeconds: number
}

export interface MemberTimeSummary {
  memberId: string
  displayName?: string
  elapsedSeconds: number
}

export interface TimeClockOverview {
  checkedAt: string
  activeShift?: WorkShift
  todaySeconds: number
  weekSeconds: number
  activeMembers: ActiveMember[]
  weeklyLeaderboard: MemberTimeSummary[]
  recentEntries: WorkLogEntry[]
  recentShifts: WorkShift[]
}

export interface ClockInResult {
  shift: WorkShift
  alreadyActive: boolean
}

const base = (organizationId: string) =>
  `/api/organizations/${organizationId}/time-clock`

export function getTimeClockOverview(organizationId: string) {
  return apiRequest<TimeClockOverview>(base(organizationId))
}

export function clockIn(organizationId: string) {
  return apiRequest<ClockInResult>(`${base(organizationId)}/clock-in`, {
    method: 'POST',
  })
}

export function clockOut(organizationId: string) {
  return apiRequest<WorkShift>(`${base(organizationId)}/clock-out`, {
    method: 'POST',
  })
}

export function logWork(
  organizationId: string,
  kind: WorkLogKind,
  note: string,
) {
  return apiRequest<WorkLogEntry>(`${base(organizationId)}/entries`, {
    method: 'POST',
    body: JSON.stringify({ kind, note }),
  })
}
