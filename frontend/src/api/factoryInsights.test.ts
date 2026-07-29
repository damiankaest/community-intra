import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  createFactory,
  getFactoryInsights,
  importServerSave,
  uploadSaveFile,
} from './factoryInsights'

describe('factory insights API', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('uses the organization-scoped import and factory endpoints', async () => {
    const fetchMock = vi
      .fn()
      .mockImplementation(() =>
        Promise.resolve(Response.json({ id: 'result-id' })),
      )
    vi.stubGlobal('fetch', fetchMock)
    const save = new File(['save'], 'friend-group.sav')

    await getFactoryInsights('organization-id')
    await uploadSaveFile('organization-id', save)
    await importServerSave('organization-id')
    await createFactory('organization-id', {
      name: 'Aluminium',
      centerX: 100,
      centerY: 200,
      radiusMeters: 500,
    })

    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      '/api/organizations/organization-id/factory-insights',
      expect.any(Object),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      '/api/organizations/organization-id/factory-insights/imports/manual',
      expect.objectContaining({ method: 'POST', body: expect.any(FormData) }),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(
      3,
      '/api/organizations/organization-id/factory-insights/imports/server',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({}),
      }),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(
      4,
      '/api/organizations/organization-id/factory-insights/factories',
      expect.objectContaining({ method: 'POST' }),
    )
  })
})
