export function cs2Path(organizationId: string, route = '') {
  return route ? `/cs2/${organizationId}/${route}` : `/cs2/${organizationId}`
}

export function cs2InvitationLink(
  origin: string,
  organizationId: string,
  token: string,
) {
  const returnTo = cs2Path(organizationId, 'squad')
  return `${origin}/invite?returnTo=${encodeURIComponent(returnTo)}#${token}`
}
