import { apiRequest } from './client'

export interface MemberNotification {
  id: string
  notificationType: string
  title: string
  body: string
  entityType: string
  entityId: string
  actorMemberId?: string
  createdAt: string
  readAt?: string
}

export interface NotificationSummary {
  unreadCount: number
}

const base = (organizationId: string) =>
  `/api/organizations/${organizationId}/notifications`

export function listNotifications(organizationId: string, unreadOnly = false) {
  return apiRequest<MemberNotification[]>(
    `${base(organizationId)}?unreadOnly=${unreadOnly}&limit=40`,
  )
}

export function getNotificationSummary(organizationId: string) {
  return apiRequest<NotificationSummary>(`${base(organizationId)}/summary`)
}

export function markNotificationRead(
  organizationId: string,
  notificationId: string,
) {
  return apiRequest<MemberNotification>(
    `${base(organizationId)}/${notificationId}/read`,
    { method: 'POST' },
  )
}

export function markAllNotificationsRead(organizationId: string) {
  return apiRequest<void>(`${base(organizationId)}/read-all`, {
    method: 'POST',
  })
}
