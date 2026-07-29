interface WebMcpToolAnnotations {
  readOnlyHint?: boolean
  untrustedContentHint?: boolean
}

interface WebMcpTool {
  name: string
  description: string
  inputSchema: Record<string, unknown>
  annotations?: WebMcpToolAnnotations
  execute: (input: Record<string, unknown>) => unknown | Promise<unknown>
}

interface WebMcpRegistrationOptions {
  signal?: AbortSignal
}

interface ModelContext {
  registerTool(
    tool: WebMcpTool,
    options?: WebMcpRegistrationOptions,
  ): void | Promise<void>
}

interface Document {
  modelContext?: ModelContext
}
