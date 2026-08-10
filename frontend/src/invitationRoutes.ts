export function invitationReturnPath(
  requestedPath: string,
  organizationId: string,
) {
  const cs2Root = `/cs2/${organizationId}`
  return requestedPath === cs2Root || requestedPath.startsWith(`${cs2Root}/`)
    ? requestedPath
    : `/organizations/${organizationId}`
}
