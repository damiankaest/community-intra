import {
  confirmWorkPlan,
  prepareWorkPlan,
  type AssistantTone,
  type ConfirmedWorkPlan,
  type WorkPlanDraft,
} from '../api/assistant'

interface RegisterAssistantToolsOptions {
  organizationId: string
  currentDraft?: WorkPlanDraft
  onDraft: (draft: WorkPlanDraft) => void
  onConfirmed: (result: ConfirmedWorkPlan) => void
}

export function registerAssistantTools({
  organizationId,
  currentDraft,
  onDraft,
  onConfirmed,
}: RegisterAssistantToolsOptions) {
  if (!document.modelContext) {
    return { isSupported: false, unregister: () => undefined }
  }

  const controller = new AbortController()
  void document.modelContext.registerTool(
    {
      name: 'prepare_work_plan',
      description:
        'Erstellt einen unverbindlichen Projektentwurf mit Ressourcen und Aufgaben. Speichert noch kein Projekt.',
      inputSchema: {
        type: 'object',
        additionalProperties: false,
        properties: {
          prompt: {
            type: 'string',
            description: 'Das Ziel oder Vorhaben in natürlicher Sprache.',
          },
          tone: {
            type: 'string',
            enum: ['Theme', 'Neutral'],
            description: 'Theme für humorvoll, Neutral für sachlich.',
          },
        },
        required: ['prompt', 'tone'],
      },
      annotations: {
        readOnlyHint: false,
        untrustedContentHint: true,
      },
      execute: async (input) => {
        const prompt = String(input.prompt ?? '')
        const tone: AssistantTone =
          input.tone === 'Neutral' ? 'Neutral' : 'Theme'
        const draft = await prepareWorkPlan(organizationId, prompt, tone)
        onDraft(draft)
        return {
          draftId: draft.id,
          title: draft.proposal.title,
          taskCount: draft.proposal.tasks.length,
          requiresUserConfirmation: true,
        }
      },
    },
    { signal: controller.signal },
  )

  if (currentDraft && !currentDraft.confirmedAt) {
    void document.modelContext.registerTool(
      {
        name: 'confirm_current_work_plan',
        description:
          'Legt den aktuell sichtbaren Entwurf als Projekt und Aufgaben an. Verlangt eine sichtbare Bestätigung.',
        inputSchema: {
          type: 'object',
          additionalProperties: false,
          properties: {},
        },
        annotations: {
          readOnlyHint: false,
          untrustedContentHint: false,
        },
        execute: async () => {
          const confirmed = window.confirm(
            `Projekt „${currentDraft.proposal.title}“ mit ${currentDraft.proposal.tasks.length} Aufgaben wirklich anlegen?`,
          )
          if (!confirmed) {
            return { confirmed: false, reason: 'User cancelled' }
          }

          const result = await confirmWorkPlan(
            organizationId,
            currentDraft.id,
            currentDraft.concurrencyToken,
          )
          onConfirmed(result)
          return {
            confirmed: true,
            projectId: result.projectId,
            taskCount: result.taskIds.length,
          }
        },
      },
      { signal: controller.signal },
    )
  }

  return {
    isSupported: true,
    unregister: () => controller.abort(),
  }
}
