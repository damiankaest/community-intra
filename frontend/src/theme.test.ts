import { describe, expect, it } from 'vitest'
import { applyTheme, getThemeCssVariables, resetTheme } from './theme'
import type { ThemePack } from './api/themePacks'

const themePack: ThemePack = {
  id: 'theme-id',
  key: 'test-theme',
  name: 'Test',
  description: 'Test theme',
  version: '1.0.0',
  author: 'Tests',
  isSystemTheme: true,
  configuration: {
    key: 'test-theme',
    name: 'Test',
    description: 'Test theme',
    version: '1.0.0',
    author: 'Tests',
    visuals: {
      primaryColor: '#112233',
      secondaryColor: '#223344',
      accentColor: '#334455',
      backgroundColor: '#445566',
      surfaceColor: '#556677',
      textColor: '#667788',
      mutedColor: '#778899',
      dangerColor: '#8899AA',
      warningColor: '#99AABB',
      successColor: '#AABBCC',
      logoIcon: 'building-2',
      style: 'clean-corporate',
    },
    terminology: {
      organization: 'Organisation',
      member: 'Mitglied',
      members: 'Mitglieder',
      department: 'Bereich',
      project: 'Projekt',
      task: 'Aufgabe',
      incident: 'Meldung',
      award: 'Auszeichnung',
      activityFeed: 'Aktivitäten',
    },
    suggestedTitles: [],
    suggestedDepartments: [],
    incidentCategories: [],
    awardTemplates: [],
    statusMessages: [],
    messages: {
      welcome: 'Willkommen',
      emptyProjects: 'Leer',
      emptyTasks: 'Leer',
      emptyIncidents: 'Leer',
      emptyActivityFeed: 'Leer',
    },
  },
}

describe('theme variables', () => {
  it('maps validated visuals to the supported CSS variables', () => {
    expect(getThemeCssVariables(themePack.configuration.visuals)).toMatchObject(
      {
        '--theme-primary': '#112233',
        '--theme-background': '#445566',
        '--theme-success': '#AABBCC',
      },
    )
  })

  it('applies and resets the selected theme centrally', () => {
    applyTheme(themePack)

    expect(
      document.documentElement.style.getPropertyValue('--theme-primary'),
    ).toBe('#112233')
    expect(document.documentElement.dataset.themeStyle).toBe('clean-corporate')

    resetTheme()

    expect(
      document.documentElement.style.getPropertyValue('--theme-primary'),
    ).toBe('#F59E0B')
    expect(document.documentElement.dataset.themeStyle).toBe(
      'industrial-corporate',
    )
  })
})
