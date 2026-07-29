import { describe, expect, it } from 'vitest'
import { fitImage } from './imageProcessing'

describe('fitImage', () => {
  it('keeps small screenshots unchanged', () => {
    expect(fitImage(800, 600, 1920)).toEqual({ width: 800, height: 600 })
  })

  it('scales landscape and portrait images proportionally', () => {
    expect(fitImage(3840, 2160, 1920)).toEqual({
      width: 1920,
      height: 1080,
    })
    expect(fitImage(1200, 2400, 480)).toEqual({
      width: 240,
      height: 480,
    })
  })
})
