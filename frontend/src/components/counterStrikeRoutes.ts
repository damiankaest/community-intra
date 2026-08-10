export function cs2Path(organizationId: string, route = '') {
  return route ? `/cs2/${organizationId}/${route}` : `/cs2/${organizationId}`
}
