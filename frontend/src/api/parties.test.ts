import { afterEach, describe, expect, it, vi } from 'vitest'
import { uploadGuestMedia } from './parties'

class FakeUploadRequest {
  static last: FakeUploadRequest | undefined
  readonly upload = {
    onprogress: null as ((event: ProgressEvent) => void) | null,
  }
  readonly headers = new Map<string, string>()
  method = ''
  url = ''
  body: FormData | undefined
  status = 0
  responseText = ''
  withCredentials = false
  onload: (() => void) | null = null
  onerror: (() => void) | null = null
  onabort: (() => void) | null = null

  constructor() {
    FakeUploadRequest.last = this
  }

  open(method: string, url: string) {
    this.method = method
    this.url = url
  }

  setRequestHeader(name: string, value: string) {
    this.headers.set(name, value)
  }

  send(body: FormData) {
    this.body = body
    this.upload.onprogress?.({
      lengthComputable: true,
      loaded: 5,
      total: 10,
    } as ProgressEvent)
    this.status = 201
    this.responseText = JSON.stringify({
      id: 'media-id',
      guestId: 'guest-id',
      guestName: 'Damian',
      mediaType: 'image',
      fileName: 'party.jpg',
      mimeType: 'image/jpeg',
      size: 4,
      createdAt: '2026-08-07T13:00:00Z',
      contentUrl: '/api/parties/public/test/media/media-id/content',
    })
    this.onload?.()
  }
}

describe('party media upload', () => {
  afterEach(() => {
    FakeUploadRequest.last = undefined
    vi.unstubAllGlobals()
  })

  it('uses the party session, multipart data and reports real upload progress', async () => {
    vi.stubGlobal('XMLHttpRequest', FakeUploadRequest)
    const progress = vi.fn()
    const file = new File([new Uint8Array([0xff, 0xd8, 0xff, 0xe0])], 'party.jpg', {
      type: 'image/jpeg',
    })

    const result = await uploadGuestMedia(
      'annas-party-12345',
      'opaque-token',
      file,
      'Bester Abend',
      progress,
    )

    const request = FakeUploadRequest.last
    expect(request?.method).toBe('POST')
    expect(request?.url).toBe(
      '/api/parties/public/annas-party-12345/media',
    )
    expect(request?.headers.get('X-Party-Session')).toBe('opaque-token')
    expect(request?.withCredentials).toBe(true)
    expect(request?.body?.get('file')).toBe(file)
    expect(request?.body?.get('caption')).toBe('Bester Abend')
    expect(progress).toHaveBeenCalledWith(50)
    expect(progress).toHaveBeenCalledWith(100)
    expect(result.id).toBe('media-id')
  })
})
