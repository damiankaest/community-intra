import type { CSSProperties } from 'react'
import type { ThemePack, ThemeVisuals } from './api/themePacks'

export type ThemeCssVariables = CSSProperties &
  Record<`--theme-${string}`, string>

export const defaultThemeVisuals: ThemeVisuals = {
  primaryColor: '#F59E0B',
  secondaryColor: '#252A33',
  accentColor: '#F97316',
  backgroundColor: '#0B0D10',
  surfaceColor: '#15181E',
  textColor: '#F5F7FA',
  mutedColor: '#99A1AD',
  dangerColor: '#FB7185',
  warningColor: '#FBBF24',
  successColor: '#34D399',
  logoIcon: 'factory',
  style: 'industrial-corporate',
}

export function getThemeCssVariables(visuals: ThemeVisuals): ThemeCssVariables {
  return {
    '--theme-primary': visuals.primaryColor,
    '--theme-secondary': visuals.secondaryColor,
    '--theme-accent': visuals.accentColor,
    '--theme-background': visuals.backgroundColor,
    '--theme-surface': visuals.surfaceColor,
    '--theme-text': visuals.textColor,
    '--theme-muted': visuals.mutedColor,
    '--theme-danger': visuals.dangerColor,
    '--theme-warning': visuals.warningColor,
    '--theme-success': visuals.successColor,
  }
}

export function applyTheme(theme?: ThemePack) {
  const variables = getThemeCssVariables(
    theme?.configuration.visuals ?? defaultThemeVisuals,
  )

  for (const [property, value] of Object.entries(variables)) {
    document.documentElement.style.setProperty(property, value)
  }

  document.documentElement.dataset.themeStyle =
    theme?.configuration.visuals.style ?? defaultThemeVisuals.style
}

export function resetTheme() {
  applyTheme()
}
