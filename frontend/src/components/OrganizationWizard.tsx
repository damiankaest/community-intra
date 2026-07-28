import { useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { ArrowLeft, ArrowRight, Check, Plus } from 'lucide-react'
import {
  useForm,
  useWatch,
  type UseFormRegister,
  type UseFormReturn,
} from 'react-hook-form'
import {
  createOrganization,
  type CreateOrganizationInput,
} from '../api/organizations'
import { listThemePacks, type ThemePack } from '../api/themePacks'
import { ApiError } from '../api/client'
import { ThemePreview } from './ThemePreview'
import { ThemeIcon } from './ThemeIcon'

const steps = ['Grundlagen', 'Theme', 'Module', 'Einrichtung'] as const

const moduleOptions = [
  { key: 'projects', label: 'Projekte' },
  { key: 'tasks', label: 'Aufgaben' },
  { key: 'incidents', label: 'Incidents' },
  { key: 'awards', label: 'Auszeichnungen' },
  { key: 'activity-feed', label: 'Activity Feed' },
] as const

export function OrganizationWizard({
  onCreated,
}: {
  onCreated: (organizationId: string) => void
}) {
  const [step, setStep] = useState(0)
  const themePacks = useQuery({
    queryKey: ['theme-packs'],
    queryFn: listThemePacks,
  })
  const form = useForm<CreateOrganizationInput>({
    defaultValues: {
      name: '',
      description: '',
      language: 'de',
      timeZone: 'Europe/Berlin',
      visibleTitle: '',
      themePackKey: 'generic-corporate',
      enabledModules: moduleOptions.map((module) => module.key),
    },
  })
  form.register('enabledModules', {
    validate: (modules) =>
      modules.length > 0 || 'Bitte wähle mindestens ein Modul aus.',
  })
  const selectedThemeKey = useWatch({
    control: form.control,
    name: 'themePackKey',
  })
  const selectedModules = useWatch({
    control: form.control,
    name: 'enabledModules',
  })
  const visibleTitle = useWatch({
    control: form.control,
    name: 'visibleTitle',
  })
  const organizationName = useWatch({
    control: form.control,
    name: 'name',
  })
  const selectedTheme = themePacks.data?.find(
    (theme) => theme.key === selectedThemeKey,
  )
  const mutation = useMutation({
    mutationFn: createOrganization,
    onSuccess: (organization) => onCreated(organization.id),
  })

  const next = async () => {
    const fields =
      step === 0
        ? (['name', 'description', 'language', 'timeZone'] as const)
        : step === 1
          ? (['themePackKey'] as const)
          : step === 2
            ? (['enabledModules'] as const)
            : (['visibleTitle'] as const)
    const isValid = await form.trigger(fields)
    if (isValid) {
      setStep((current) => Math.min(current + 1, steps.length - 1))
    }
  }

  const submit = form.handleSubmit((values) => mutation.mutate(values))

  return (
    <form
      onSubmit={submit}
      className="mt-9 overflow-hidden rounded-2xl border border-white/10 bg-white/[0.035]"
    >
      <ol className="grid grid-cols-4 border-b border-white/10 bg-black/15">
        {steps.map((label, index) => (
          <li
            key={label}
            aria-current={step === index ? 'step' : undefined}
            className={`border-r border-white/10 px-2 py-4 text-center text-xs last:border-r-0 sm:px-4 ${
              step === index
                ? 'bg-[var(--theme-primary)]/10 font-bold text-[var(--theme-primary)]'
                : index < step
                  ? 'text-[var(--theme-success)]'
                  : 'text-[var(--theme-muted)]'
            }`}
          >
            <span className="mx-auto mb-1 flex size-6 items-center justify-center rounded-full border border-current">
              {index < step ? <Check size={13} /> : index + 1}
            </span>
            <span className="hidden sm:inline">{label}</span>
          </li>
        ))}
      </ol>

      <div className="p-6 sm:p-8">
        {step === 0 && <BasicsStep form={form} />}
        {step === 1 && (
          <ThemeStep
            themes={themePacks.data}
            selectedThemeKey={selectedThemeKey}
            isPending={themePacks.isPending}
            error={themePacks.error}
            selectTheme={(key) => form.setValue('themePackKey', key)}
          />
        )}
        {step === 2 && (
          <ModulesStep
            selectedModules={selectedModules}
            toggleModule={(moduleKey) => {
              const nextModules = selectedModules.includes(moduleKey)
                ? selectedModules.filter((key) => key !== moduleKey)
                : [...selectedModules, moduleKey]
              form.setValue('enabledModules', nextModules, {
                shouldValidate: true,
              })
            }}
            error={form.formState.errors.enabledModules?.message}
          />
        )}
        {step === 3 && (
          <SetupStep
            theme={selectedTheme}
            visibleTitle={visibleTitle}
            setVisibleTitle={(title) => form.setValue('visibleTitle', title)}
            register={form.register}
            name={organizationName}
            selectedModules={selectedModules}
          />
        )}

        {mutation.error && <ErrorNotice error={mutation.error} />}

        <div className="mt-8 flex flex-col-reverse gap-3 border-t border-white/10 pt-6 sm:flex-row sm:justify-between">
          <button
            type="button"
            onClick={() => setStep((current) => Math.max(current - 1, 0))}
            disabled={step === 0}
            className="flex h-11 items-center justify-center gap-2 rounded-xl border border-white/10 bg-white/5 px-5 text-sm font-semibold text-white disabled:invisible"
          >
            <ArrowLeft size={16} />
            Zurück
          </button>
          {step < steps.length - 1 ? (
            <button
              type="button"
              onClick={next}
              className="flex h-11 items-center justify-center gap-2 rounded-xl bg-[var(--theme-primary)] px-5 font-bold text-black"
            >
              Weiter
              <ArrowRight size={16} />
            </button>
          ) : (
            <button
              type="submit"
              disabled={mutation.isPending}
              className="flex h-11 items-center justify-center gap-2 rounded-xl bg-[var(--theme-primary)] px-5 font-bold text-black disabled:opacity-60"
            >
              <Plus size={17} />
              {mutation.isPending ? 'Wird gegründet …' : 'Organisation gründen'}
            </button>
          )}
        </div>
      </div>
    </form>
  )
}

function BasicsStep({
  form,
}: {
  form: UseFormReturn<CreateOrganizationInput>
}) {
  return (
    <div>
      <StepHeading
        title="Die Grundlagen"
        description="Name, Sprache und Zeitzone bleiben unabhängig vom gewählten Theme."
      />
      <div className="mt-6 space-y-5">
        <Field
          label="Name der Organisation"
          error={form.formState.errors.name?.message}
        >
          <input
            {...form.register('name', {
              required: 'Bitte gib einen Namen ein.',
              minLength: { value: 2, message: 'Mindestens 2 Zeichen.' },
              maxLength: 120,
            })}
            placeholder="Rheinische Community-Zentrale"
          />
        </Field>
        <Field
          label="Beschreibung"
          error={form.formState.errors.description?.message}
        >
          <textarea
            rows={4}
            {...form.register('description', { maxLength: 1000 })}
            placeholder="Wofür ist diese außerordentlich wichtige Organisation zuständig?"
          />
        </Field>
        <div className="grid gap-5 sm:grid-cols-2">
          <Field
            label="Sprache"
            error={form.formState.errors.language?.message}
          >
            <input
              {...form.register('language', {
                required: 'Bitte gib eine Sprache an.',
                pattern: {
                  value: /^[a-z]{2}(-[A-Z]{2})?$/,
                  message: 'Zum Beispiel de oder de-DE.',
                },
              })}
            />
          </Field>
          <Field
            label="Zeitzone"
            error={form.formState.errors.timeZone?.message}
          >
            <input
              {...form.register('timeZone', {
                required: 'Bitte gib eine Zeitzone an.',
                maxLength: 100,
              })}
            />
          </Field>
        </div>
      </div>
    </div>
  )
}

function ThemeStep({
  themes,
  selectedThemeKey,
  selectTheme,
  isPending,
  error,
}: {
  themes?: ThemePack[]
  selectedThemeKey: string
  selectTheme: (key: string) => void
  isPending: boolean
  error: Error | null
}) {
  if (isPending) {
    return (
      <p className="text-sm text-[var(--theme-muted)]">
        Themes werden geladen …
      </p>
    )
  }

  if (error) {
    return <ErrorNotice error={error} />
  }

  return (
    <div>
      <StepHeading
        title="Welches Theme passt?"
        description="Farben, Begriffe und Humor ändern sich – technische Rechte und Datenmodell bleiben gleich."
      />
      <div className="mt-6 grid gap-5 lg:grid-cols-2">
        {themes?.map((theme) => {
          const selected = selectedThemeKey === theme.key
          return (
            <button
              type="button"
              key={theme.id}
              onClick={() => selectTheme(theme.key)}
              aria-pressed={selected}
              className={`rounded-2xl border p-1 text-left transition ${
                selected
                  ? 'border-[var(--theme-primary)] ring-2 ring-[var(--theme-primary)]/20'
                  : 'border-white/10 hover:border-white/25'
              }`}
            >
              <ThemePreview theme={theme} />
            </button>
          )
        })}
      </div>
    </div>
  )
}

function ModulesStep({
  selectedModules,
  toggleModule,
  error,
}: {
  selectedModules: string[]
  toggleModule: (moduleKey: string) => void
  error?: string
}) {
  return (
    <div>
      <StepHeading
        title="Module vormerken"
        description="Die Auswahl wird bereits an der Organisation gespeichert. Die fachlichen Funktionen folgen in Phase 6."
      />
      <div className="mt-6 grid gap-3 sm:grid-cols-2">
        {moduleOptions.map((module) => {
          const selected = selectedModules.includes(module.key)
          return (
            <button
              type="button"
              key={module.key}
              onClick={() => toggleModule(module.key)}
              aria-pressed={selected}
              className={`flex items-center justify-between rounded-xl border p-4 text-left ${
                selected
                  ? 'border-[var(--theme-primary)]/40 bg-[var(--theme-primary)]/10 text-white'
                  : 'border-white/10 text-[var(--theme-muted)]'
              }`}
            >
              <span className="font-semibold">{module.label}</span>
              <span className="flex size-6 items-center justify-center rounded-full border border-current">
                {selected && <Check size={14} />}
              </span>
            </button>
          )
        })}
      </div>
      {error && (
        <p className="mt-3 text-xs text-[var(--theme-danger)]">{error}</p>
      )}
      {selectedModules.length === 0 && (
        <p className="mt-3 text-xs text-[var(--theme-danger)]">
          Bitte wähle mindestens ein Modul aus.
        </p>
      )}
    </div>
  )
}

function SetupStep({
  theme,
  visibleTitle,
  setVisibleTitle,
  register,
  name,
  selectedModules,
}: {
  theme?: ThemePack
  visibleTitle?: string
  setVisibleTitle: (title: string) => void
  register: UseFormRegister<CreateOrganizationInput>
  name: string
  selectedModules: string[]
}) {
  return (
    <div>
      <StepHeading
        title="Einrichtung prüfen"
        description="Du wirst automatisch Owner. Der sichtbare Titel ist nur Darstellung und verleiht keine Rechte."
      />
      <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_0.9fr]">
        <div>
          <Field label="Dein sichtbarer Titel">
            <input
              {...register('visibleTitle', { maxLength: 100 })}
              placeholder={theme?.configuration.suggestedTitles[0]}
            />
          </Field>
          {theme && (
            <div className="mt-3 flex flex-wrap gap-2">
              {theme.configuration.suggestedTitles.map((title) => (
                <button
                  type="button"
                  key={title}
                  onClick={() => setVisibleTitle(title)}
                  className={`rounded-full border px-2.5 py-1 text-xs ${
                    visibleTitle === title
                      ? 'border-[var(--theme-primary)] bg-[var(--theme-primary)]/10 text-[var(--theme-primary)]'
                      : 'border-white/10 text-[var(--theme-muted)]'
                  }`}
                >
                  {title}
                </button>
              ))}
            </div>
          )}

          {theme && (
            <div className="mt-6">
              <p className="text-sm font-semibold text-white">
                Vorgeschlagene {theme.configuration.terminology.department}
              </p>
              <p className="mt-1 text-xs text-[var(--theme-muted)]">
                Werden in Phase 5 bei der Mitgliederverwaltung als Vorlagen
                angeboten.
              </p>
              <div className="mt-3 flex flex-wrap gap-2">
                {theme.configuration.suggestedDepartments.map((department) => (
                  <span
                    key={department.name}
                    className="inline-flex items-center gap-1.5 rounded-lg border border-white/10 px-2.5 py-1.5 text-xs text-white"
                  >
                    <ThemeIcon name={department.icon} size={14} />
                    {department.name}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>

        <div className="rounded-2xl border border-white/10 bg-black/15 p-5">
          <p className="text-xs font-bold tracking-[0.14em] text-[var(--theme-primary)] uppercase">
            Zusammenfassung
          </p>
          <dl className="mt-4 space-y-3 text-sm">
            <SummaryRow label="Name" value={name || 'Noch nicht gesetzt'} />
            <SummaryRow label="Theme" value={theme?.name ?? 'Wird geladen'} />
            <SummaryRow
              label="Module"
              value={`${selectedModules.length} ausgewählt`}
            />
            <SummaryRow
              label="Owner-Titel"
              value={visibleTitle || 'Kein Titel'}
            />
          </dl>
        </div>
      </div>
    </div>
  )
}

function Field({
  label,
  error,
  children,
}: {
  label: string
  error?: string
  children: React.ReactElement
}) {
  return (
    <label className="wizard-field block">
      <span className="mb-2 block text-sm font-semibold text-white">
        {label}
      </span>
      {children}
      {error && (
        <span className="mt-1.5 block text-xs text-[var(--theme-danger)]">
          {error}
        </span>
      )}
    </label>
  )
}

function StepHeading({
  title,
  description,
}: {
  title: string
  description: string
}) {
  return (
    <div>
      <h2 className="text-2xl font-black text-white">{title}</h2>
      <p className="mt-2 max-w-2xl text-sm leading-6 text-[var(--theme-muted)]">
        {description}
      </p>
    </div>
  )
}

function SummaryRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 border-b border-white/[0.07] pb-3 last:border-0 last:pb-0">
      <dt className="text-[var(--theme-muted)]">{label}</dt>
      <dd className="text-right font-semibold text-white">{value}</dd>
    </div>
  )
}

function ErrorNotice({ error }: { error: Error }) {
  const message =
    error instanceof ApiError && error.status === 401
      ? 'Deine Sitzung ist abgelaufen. Bitte melde dich erneut an.'
      : error.message
  return (
    <div
      role="alert"
      className="mt-5 rounded-xl border border-rose-400/20 bg-rose-400/10 px-4 py-3 text-sm text-rose-200"
    >
      {message}
    </div>
  )
}
