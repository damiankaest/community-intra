import { describe, expect, it } from 'vitest'
import { fitImage, imageBlobToFile } from './imageProcessing'

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

describe('imageBlobToFile', () => {
  it('keeps the actual canvas MIME type when WebP falls back to PNG', () => {
    const blob = new Blob(['png-data'], { type: 'image/png' })

    const file = imageBlobToFile(blob, 'party-photo')

    expect(file.type).toBe('image/png')
    expect(file.name).toBe('party-photo.png')
  })

  it('uses WebP when the browser really produced WebP', () => {
    const blob = new Blob(['webp-data'], { type: 'image/webp' })

    const file = imageBlobToFile(blob, 'party-photo')

    expect(file.type).toBe('image/webp')
    expect(file.name).toBe('party-photo.webp')
  })
})
