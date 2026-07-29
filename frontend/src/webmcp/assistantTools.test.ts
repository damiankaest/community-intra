import { afterEach, describe, expect, it, vi } from 'vitest'
import { registerAssistantTools } from './assistantTools'

describe('WebMCP assistant tools', () => {
  afterEach(() => {
    delete document.modelContext
    vi.restoreAllMocks()
  })

  it('registers read and confirmed write tools', () => {
    const tools: WebMcpTool[] = []
    document.modelContext = {
      registerTool: vi.fn((tool: WebMcpTool) => {
        tools.push(tool)
      }),
    }

    const registration = registerAssistantTools({
      organizationId: 'organization-id',
      onChanged: vi.fn(),
      onOpenChat: vi.fn(),
    })

    expect(registration.isSupported).toBe(true)
    expect(tools.map((tool) => tool.name)).toEqual([
      'community_list_projects',
      'community_list_tasks',
      'community_list_members',
      'community_get_task',
      'community_get_live_server_status',
      'community_create_task',
      'community_change_task_status',
      'community_set_task_material_state',
      'community_assign_task',
      'community_add_task_comment',
    ])
    expect(
      tools.slice(0, 5).every((tool) => tool.annotations?.readOnlyHint),
    ).toBe(true)
    expect(
      tools.slice(5).every((tool) => tool.annotations?.readOnlyHint === false),
    ).toBe(true)
  })

  it('does not register tools in browsers without WebMCP', () => {
    const registration = registerAssistantTools({
      organizationId: 'organization-id',
      onChanged: vi.fn(),
      onOpenChat: vi.fn(),
    })

    expect(registration.isSupported).toBe(false)
  })
})
