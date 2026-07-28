import type { ThemePack } from '../api/themePacks'
import { getThemeCssVariables } from '../theme'
import { ThemeIcon } from './ThemeIcon'

export function ThemePreview({
  theme,
  compact = false,
}: {
  theme: ThemePack
  compact?: boolean
}) {
  const configuration = theme.configuration
  const terminology = configuration.terminology

  return (
    <section
      aria-label={`Vorschau für ${theme.name}`}
      style={getThemeCssVariables(configuration.visuals)}
      className="overflow-hidden rounded-2xl border border-white/10 bg-[var(--theme-background)] text-[var(--theme-text)] shadow-xl shadow-black/20"
    >
      <div className="theme-pattern border-b border-white/10 p-5 sm:p-6">
        <div className="flex items-start justify-between gap-4">
          <span className="flex size-11 items-center justify-center rounded-xl bg-[var(--theme-primary)] text-black">
            <ThemeIcon name={configuration.visuals.logoIcon} />
          </span>
          <span className="rounded-full border border-white/10 bg-black/20 px-2.5 py-1 text-xs text-[var(--theme-muted)]">
            v{theme.version}
          </span>
        </div>
        <p className="mt-5 text-xs font-bold tracking-[0.15em] text-[var(--theme-primary)] uppercase">
          {terminology.organization}
        </p>
        <h3 className="mt-1 text-xl font-black">{theme.name}</h3>
        <p className="mt-2 text-sm leading-6 text-[var(--theme-muted)]">
          {configuration.messages.welcome}
        </p>
      </div>

      <div className="p-5 sm:p-6">
        <div className="flex gap-2" aria-label="Theme-Farben">
          {[
            configuration.visuals.primaryColor,
            configuration.visuals.secondaryColor,
            configuration.visuals.accentColor,
            configuration.visuals.surfaceColor,
          ].map((color) => (
            <span
              key={color}
              className="h-2.5 flex-1 rounded-full border border-white/10"
              style={{ backgroundColor: color }}
            />
          ))}
        </div>

        {!compact && (
          <>
            <div className="mt-5 grid grid-cols-2 gap-2 text-xs">
              {[
                terminology.project,
                terminology.task,
                terminology.incident,
                terminology.activityFeed,
              ].map((term) => (
                <span
                  key={term}
                  className="rounded-lg bg-[var(--theme-surface)] px-3 py-2 text-[var(--theme-muted)]"
                >
                  {term}
                </span>
              ))}
            </div>

            <div className="mt-5">
              <p className="text-xs font-semibold text-[var(--theme-muted)]">
                Beispielrollen
              </p>
              <div className="mt-2 flex flex-wrap gap-2">
                {configuration.suggestedTitles.slice(0, 3).map((title) => (
                  <span
                    key={title}
                    className="rounded-full border border-[var(--theme-primary)]/25 bg-[var(--theme-primary)]/10 px-2.5 py-1 text-xs text-[var(--theme-primary)]"
                  >
                    {title}
                  </span>
                ))}
              </div>
            </div>
          </>
        )}
      </div>
    </section>
  )
}
