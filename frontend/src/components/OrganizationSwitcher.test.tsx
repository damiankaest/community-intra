import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { OrganizationSwitcher } from './OrganizationSwitcher'

describe('OrganizationSwitcher', () => {
  it('selects another organization', () => {
    const onSelect = vi.fn()

    render(
      <OrganizationSwitcher
        selectedId="organization-a"
        onSelect={onSelect}
        organizations={[
          {
            id: 'organization-a',
            name: 'FICSIT Nord',
            slug: 'ficsit-nord',
            themePackKey: 'satisfactory-ficsit',
            themePackVersion: '1.0.0',
            language: 'de',
            permissionRole: 'Owner',
          },
          {
            id: 'organization-b',
            name: 'FICSIT Süd',
            slug: 'ficsit-sued',
            themePackKey: 'satisfactory-ficsit',
            themePackVersion: '1.0.0',
            language: 'de',
            permissionRole: 'Member',
          },
        ]}
      />,
    )

    fireEvent.change(screen.getByLabelText('Organisation auswählen'), {
      target: { value: 'organization-b' },
    })

    expect(onSelect).toHaveBeenCalledWith('organization-b')
  })
})
