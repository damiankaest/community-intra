import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  clockIn,
  clockOut,
  getTimeClockOverview,
  logWork,
} from './timeTracking'

describe('time tracking API', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('uses the organization time-clock endpoints', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        Response.json({
          checkedAt: '2026-07-29T12:00:00Z',
          todaySeconds: 0,
          weekSeconds: 0,
          activeMembers: [],
          weeklyLeaderboard: [],
          recentEntries: [],
          recentShifts: [],
        }),
      )
      .mockResolvedValueOnce(
        Response.json({
          shift: {
            id: 'shift-id',
            memberId: 'member-id',
            startedAt: '2026-07-29T12:00:00Z',
            elapsedSeconds: 0,
          },
          alreadyActive: false,
        }),
      )
      .mockResolvedValueOnce(
        Response.json({
          id: 'entry-id',
          memberId: 'member-id',
          kind: 'Built',
          note: 'Aluminiumzufuhr gebaut',
          createdAt: '2026-07-29T12:01:00Z',
        }),
      )
      .mockResolvedValueOnce(
        Response.json({
          id: 'shift-id',
          memberId: 'member-id',
          startedAt: '2026-07-29T12:00:00Z',
          endedAt: '2026-07-29T13:00:00Z',
          elapsedSeconds: 3600,
        }),
      )
    vi.stubGlobal('fetch', fetchMock)

    await getTimeClockOverview('organization-id')
    await clockIn('organization-id')
    await logWork('organization-id', 'Built', 'Aluminiumzufuhr gebaut')
    await clockOut('organization-id')

    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      '/api/organizations/organization-id/time-clock',
      expect.any(Object),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      '/api/organizations/organization-id/time-clock/clock-in',
      expect.objectContaining({ method: 'POST' }),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(
      3,
      '/api/organizations/organization-id/time-clock/entries',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          kind: 'Built',
          note: 'Aluminiumzufuhr gebaut',
        }),
      }),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(
      4,
      '/api/organizations/organization-id/time-clock/clock-out',
      expect.objectContaining({ method: 'POST' }),
    )
  })
})
