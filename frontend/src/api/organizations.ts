import { apiRequest } from './client'

export type PermissionRole =
  'Owner' | 'Administrator' | 'Moderator' | 'Member' | 'Guest'

export interface OrganizationSummary {
  id: string
  name: string
  slug: string
  description?: string
  language: string
  permissionRole: PermissionRole
  visibleTitle?: string
}

export interface Organization extends OrganizationSummary {
  themePackId?: string
  timeZone: string
  ownerUserId: string
  createdAt: string
  updatedAt: string
  isArchived: boolean
}

export interface CreateOrganizationInput {
  name: string
  description?: string
  language: string
  timeZone: string
  visibleTitle?: string
}

export function listOrganizations() {
  return apiRequest<OrganizationSummary[]>('/api/organizations')
}

export function getOrganization(organizationId: string) {
  return apiRequest<Organization>(`/api/organizations/${organizationId}`)
}

export function createOrganization(input: CreateOrganizationInput) {
  return apiRequest<Organization>('/api/organizations', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}
