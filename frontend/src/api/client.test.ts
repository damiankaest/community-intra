import { describe, expect, it } from 'vitest'
import { ApiError } from './client'

describe('ApiError', () => {
  it('uses simple API messages for conflict responses', () => {
    const error = new ApiError(409, {
      message: 'Diese Demo wurde bereits hochgeladen.',
    })

    expect(error.message).toBe('Diese Demo wurde bereits hochgeladen.')
    expect(error.status).toBe(409)
  })
})
