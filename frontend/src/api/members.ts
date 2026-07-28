import { apiRequest } from './client'
import type { PermissionRole } from './organizations'

export interface Member {
  id: string
  userId: string
  displayName: string
  email: string
  avatarUrl?: string
  permissionRole: PermissionRole
  visibleTitle?: string
  departmentId?: string
  departmentName?: string
  statusMessage?: string
  joinedAt: string
  isActive: boolean
}

export interface UpdateMemberInput {
  permissionRole: PermissionRole
  visibleTitle?: string
  departmentId?: string
  statusMessage?: string
  isActive: boolean
}

export interface Department {
  id: string
  name: string
  description?: string
  sortOrder: number
  icon: string
  isArchived: boolean
}

export interface CreateDepartmentInput {
  name: string
  description?: string
  icon: string
}

export interface Invitation {
  id: string
  createdByDisplayName: string
  defaultPermissionRole: PermissionRole
  createdAt: string
  expiresAt: string
  maximumUses: number
  currentUses: number
  isRevoked: boolean
  isUsable: boolean
}

export interface CreateInvitationInput {
  defaultPermissionRole: PermissionRole
  expiresInDays: number
  maximumUses: number
}

export interface CreatedInvitation {
  id: string
  token: string
  defaultPermissionRole: PermissionRole
  expiresAt: string
  maximumUses: number
}

export interface InvitationPreview {
  invitationId: string
  organizationId: string
  organizationName: string
  themePackKey: string
  defaultPermissionRole: PermissionRole
  expiresAt: string
  remainingUses: number
}

export interface AcceptedInvitation {
  organizationId: string
  organizationName: string
  membershipId: string
  permissionRole: PermissionRole
}

export function listMembers(organizationId: string) {
  return apiRequest<Member[]>(`/api/organizations/${organizationId}/members`)
}

export function getMember(organizationId: string, memberId: string) {
  return apiRequest<Member>(
    `/api/organizations/${organizationId}/members/${memberId}`,
  )
}

export function updateMember(
  organizationId: string,
  memberId: string,
  input: UpdateMemberInput,
) {
  return apiRequest<void>(
    `/api/organizations/${organizationId}/members/${memberId}`,
    {
      method: 'PATCH',
      body: JSON.stringify(input),
    },
  )
}

export function listDepartments(organizationId: string) {
  return apiRequest<Department[]>(
    `/api/organizations/${organizationId}/departments`,
  )
}

export function createDepartment(
  organizationId: string,
  input: CreateDepartmentInput,
) {
  return apiRequest<Department>(
    `/api/organizations/${organizationId}/departments`,
    {
      method: 'POST',
      body: JSON.stringify(input),
    },
  )
}

export function archiveDepartment(
  organizationId: string,
  departmentId: string,
) {
  return apiRequest<void>(
    `/api/organizations/${organizationId}/departments/${departmentId}`,
    { method: 'DELETE' },
  )
}

export function listInvitations(organizationId: string) {
  return apiRequest<Invitation[]>(
    `/api/organizations/${organizationId}/invitations`,
  )
}

export function createInvitation(
  organizationId: string,
  input: CreateInvitationInput,
) {
  return apiRequest<CreatedInvitation>(
    `/api/organizations/${organizationId}/invitations`,
    {
      method: 'POST',
      body: JSON.stringify(input),
    },
  )
}

export function revokeInvitation(organizationId: string, invitationId: string) {
  return apiRequest<void>(
    `/api/organizations/${organizationId}/invitations/${invitationId}`,
    { method: 'DELETE' },
  )
}

export function resolveInvitation(token: string) {
  return apiRequest<InvitationPreview>(
    '/api/invitations/resolve',
    {
      method: 'POST',
      body: JSON.stringify({ token }),
    },
    false,
  )
}

export function acceptInvitation(token: string) {
  return apiRequest<AcceptedInvitation>('/api/invitations/accept', {
    method: 'POST',
    body: JSON.stringify({ token }),
  })
}
