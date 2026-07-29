import { afterEach, describe, expect, it, vi } from 'vitest'
import type { WorkPlanDraft } from '../api/assistant'
import { registerAssistantTools } from './assistantTools'

const draft: WorkPlanDraft = {
  id: 'draft-id',
  tone: 'Theme',
  prompt: 'Aluminiumproduktion bauen',
  proposal: {
    title: 'Aluminiumversorgung',
    executiveSummary: 'Versorgung aufbauen.',
    managementMessage: 'Synergien zeitnah materialisieren.',
    materials: [],
    tasks: [
      {
        title: 'Raffinerie bauen',
        description: 'Produktionslinie errichten.',
        priority: 'High',
        acceptanceCriteria: ['Aluminiumoxid wird produziert.'],
      },
    ],
  },
  model: 'gpt-5.6',
  createdAt: '2026-07-29T07:00:00Z',
  expiresAt: '2026-07-29T07:30:00Z',
  concurrencyToken: 'draft-token',
}

describe('WebMCP assistant tools', () => {
  afterEach(() => {
    delete document.modelContext
    vi.restoreAllMocks()
  })

  it('registers prepare and confirmation as effectful tools', () => {
    const tools: WebMcpTool[] = []
    document.modelContext = {
      registerTool: vi.fn((tool: WebMcpTool) => {
        tools.push(tool)
      }),
    }

    const registration = registerAssistantTools({
      organizationId: 'organization-id',
      currentDraft: draft,
      onDraft: vi.fn(),
      onConfirmed: vi.fn(),
    })

    expect(registration.isSupported).toBe(true)
    expect(tools.map((tool) => tool.name)).toEqual([
      'prepare_work_plan',
      'confirm_current_work_plan',
    ])
    expect(
      tools.every((tool) => tool.annotations?.readOnlyHint === false),
    ).toBe(true)
  })

  it('does not register tools in browsers without WebMCP', () => {
    const registration = registerAssistantTools({
      organizationId: 'organization-id',
      onDraft: vi.fn(),
      onConfirmed: vi.fn(),
    })

    expect(registration.isSupported).toBe(false)
  })
})
