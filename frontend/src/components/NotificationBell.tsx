import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Bell, CheckCheck } from 'lucide-react'
import { Link } from 'react-router-dom'
import {
  getNotificationSummary,
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  type MemberNotification,
} from '../api/notifications'

export function NotificationBell({
  organizationId,
}: {
  organizationId: string
}) {
  const queryClient = useQueryClient()
  const [isOpen, setIsOpen] = useState(false)
  const summary = useQuery({
    queryKey: ['notification-summary', organizationId],
    queryFn: () => getNotificationSummary(organizationId),
    enabled: Boolean(organizationId),
    refetchInterval: 30_000,
  })
  const notifications = useQuery({
    queryKey: ['notifications', organizationId],
    queryFn: () => listNotifications(organizationId),
    enabled: Boolean(organizationId) && isOpen,
    refetchInterval: isOpen ? 30_000 : false,
  })
  const refresh = async () => {
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: ['notification-summary', organizationId],
      }),
      queryClient.invalidateQueries({
        queryKey: ['notifications', organizationId],
      }),
    ])
  }
  const readMutation = useMutation({
    mutationFn: (notificationId: string) =>
      markNotificationRead(organizationId, notificationId),
    onSuccess: refresh,
  })
  const readAllMutation = useMutation({
    mutationFn: () => markAllNotificationsRead(organizationId),
    onSuccess: refresh,
  })
  const unreadCount = summary.data?.unreadCount ?? 0

  return (
    <div className="notification-bell">
      <button
        type="button"
        className="notification-trigger"
        aria-label={`${unreadCount} ungelesene Benachrichtigungen`}
        aria-expanded={isOpen}
        onClick={() => setIsOpen((current) => !current)}
      >
        <Bell size={19} />
        {unreadCount > 0 && (
          <span>{unreadCount > 99 ? '99+' : unreadCount}</span>
        )}
      </button>
      {isOpen && (
        <div className="notification-popover">
          <header>
            <div>
              <strong>Für dich</strong>
              <p>{unreadCount} noch ungelesen</p>
            </div>
            {unreadCount > 0 && (
              <button
                type="button"
                title="Alle als gelesen markieren"
                onClick={() => readAllMutation.mutate()}
              >
                <CheckCheck size={17} />
              </button>
            )}
          </header>
          <div className="notification-list">
            {notifications.data?.map((notification) => (
              <NotificationRow
                key={notification.id}
                notification={notification}
                organizationId={organizationId}
                onOpen={() => {
                  if (!notification.readAt) {
                    readMutation.mutate(notification.id)
                  }
                  setIsOpen(false)
                }}
              />
            ))}
            {!notifications.isPending && notifications.data?.length === 0 && (
              <p className="notification-empty">
                Gerade nichts Neues. Verdächtig ruhig.
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

function NotificationRow({
  notification,
  organizationId,
  onOpen,
}: {
  notification: MemberNotification
  organizationId: string
  onOpen: () => void
}) {
  const target =
    notification.entityType === 'task'
      ? `/organizations/${organizationId}/tasks?task=${notification.entityId}`
      : `/organizations/${organizationId}`
  return (
    <Link
      to={target}
      className={notification.readAt ? '' : 'is-unread'}
      onClick={onOpen}
    >
      <span className="notification-dot" />
      <span>
        <strong>{notification.title}</strong>
        <small>{notification.body}</small>
        <time>{relativeTime(notification.createdAt)}</time>
      </span>
    </Link>
  )
}

function relativeTime(value: string) {
  const minutes = Math.max(
    0,
    Math.round((Date.now() - new Date(value).getTime()) / 60_000),
  )
  if (minutes < 1) return 'gerade eben'
  if (minutes < 60) return `vor ${minutes} Min.`
  const hours = Math.round(minutes / 60)
  if (hours < 24) return `vor ${hours} Std.`
  return new Date(value).toLocaleDateString('de-DE')
}
