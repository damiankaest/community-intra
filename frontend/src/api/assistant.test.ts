import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  confirmWorkPlan,
  prepareWorkPlan,
  streamAssistantMessage,
} from './assistant'

describe('AI assistant API', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('prepares a theme-based work plan without creating data', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      Response.json({
        id: 'draft-id',
        tone: 'Theme',
        prompt: 'Aluminiumproduktion bauen',
        proposal: {
          title: 'Aluminiumversorgung',
          executiveSummary: 'Versorgung aufbauen.',
          managementMessage: 'Synergien zeitnah materialisieren.',
          materials: [],
          tasks: [],
        },
        model: 'gpt-5.6',
        createdAt: '2026-07-29T07:00:00Z',
        expiresAt: '2026-07-29T07:30:00Z',
        concurrencyToken: 'token',
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await prepareWorkPlan(
      'organization-id',
      'Aluminiumproduktion bauen',
      'Theme',
    )

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/organizations/organization-id/assistant/work-plan-drafts',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          prompt: 'Aluminiumproduktion bauen',
          tone: 'Theme',
        }),
      }),
    )
  })

  it('sends the concurrency token when confirming a draft', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      Response.json({
        draftId: 'draft-id',
        projectId: 'project-id',
        taskIds: ['task-id'],
        alreadyConfirmed: false,
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await confirmWorkPlan('organization-id', 'draft-id', 'draft-token')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/organizations/organization-id/assistant/work-plan-drafts/draft-id/confirm',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ concurrencyToken: 'draft-token' }),
      }),
    )
  })

  it('delivers split chat events while the response streams', async () => {
    const encoder = new TextEncoder()
    const body = new ReadableStream<Uint8Array>({
      start(controller) {
        controller.enqueue(
          encoder.encode(
            '{"type":"message_ack","conversationId":"chat","message":{"id":"user","role":"User","content":"Hallo","createdAt":"2026-07-29T07:00:00Z"}}\n{"type":"del',
          ),
        )
        controller.enqueue(
          encoder.encode(
            'ta","delta":"Hi"}\n{"type":"done","message":{"id":"assistant","role":"Assistant","content":"Hi","createdAt":"2026-07-29T07:00:01Z"}}',
          ),
        )
        controller.close()
      },
    })
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(body, {
          headers: { 'Content-Type': 'application/x-ndjson' },
        }),
      ),
    )
    const events: string[] = []

    await streamAssistantMessage(
      'organization-id',
      'Hallo',
      'Neutral',
      (event) => events.push(event.type),
    )

    expect(events).toEqual(['message_ack', 'delta', 'done'])
  })
})
