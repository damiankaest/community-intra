import { Building2, ChevronDown } from 'lucide-react'
import type { OrganizationSummary } from '../api/organizations'

interface OrganizationSwitcherProps {
  organizations: OrganizationSummary[]
  selectedId?: string
  onSelect: (organizationId: string) => void
}

export function OrganizationSwitcher({
  organizations,
  selectedId,
  onSelect,
}: OrganizationSwitcherProps) {
  return (
    <label className="relative block">
      <span className="sr-only">Organisation auswählen</span>
      <Building2
        aria-hidden="true"
        className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-[var(--theme-primary)]"
        size={17}
      />
      <select
        aria-label="Organisation auswählen"
        value={selectedId ?? ''}
        onChange={(event) => onSelect(event.target.value)}
        className="h-11 min-w-56 appearance-none rounded-xl border border-white/10 bg-[#15181e] pr-10 pl-10 text-sm font-medium text-white outline-none focus:border-[var(--theme-primary)]"
      >
        <option value="" disabled>
          Organisation auswählen
        </option>
        {organizations.map((organization) => (
          <option key={organization.id} value={organization.id}>
            {organization.name}
          </option>
        ))}
      </select>
      <ChevronDown
        aria-hidden="true"
        className="pointer-events-none absolute top-1/2 right-3 -translate-y-1/2 text-[var(--theme-muted)]"
        size={16}
      />
    </label>
  )
}
