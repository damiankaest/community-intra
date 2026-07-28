import { apiRequest } from './client'

export interface ThemeVisuals {
  primaryColor: string
  secondaryColor: string
  accentColor: string
  backgroundColor: string
  surfaceColor: string
  textColor: string
  mutedColor: string
  dangerColor: string
  warningColor: string
  successColor: string
  logoIcon: string
  style: 'clean-corporate' | 'industrial-corporate'
}

export interface ThemeTerminology {
  organization: string
  member: string
  members: string
  department: string
  project: string
  task: string
  incident: string
  award: string
  activityFeed: string
}

export interface SuggestedDepartment {
  name: string
  icon: string
}

export interface AwardTemplate {
  name: string
  descriptionTemplate: string
}

export interface ThemeMessages {
  welcome: string
  emptyProjects: string
  emptyTasks: string
  emptyIncidents: string
  emptyActivityFeed: string
}

export interface ThemePackConfiguration {
  key: string
  name: string
  description: string
  version: string
  author: string
  visuals: ThemeVisuals
  terminology: ThemeTerminology
  suggestedTitles: string[]
  suggestedDepartments: SuggestedDepartment[]
  incidentCategories: string[]
  awardTemplates: AwardTemplate[]
  statusMessages: string[]
  messages: ThemeMessages
}

export interface ThemePack {
  id: string
  key: string
  name: string
  description: string
  version: string
  author: string
  isSystemTheme: boolean
  configuration: ThemePackConfiguration
}

export function listThemePacks() {
  return apiRequest<ThemePack[]>('/api/theme-packs')
}

export function getThemePack(key: string) {
  return apiRequest<ThemePack>(`/api/theme-packs/${encodeURIComponent(key)}`)
}
