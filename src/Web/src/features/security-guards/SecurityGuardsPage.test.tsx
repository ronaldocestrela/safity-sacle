import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../../shared/auth/AuthProvider'
import { AUTH_SESSION_STORAGE_KEY } from '../../shared/auth/session'
import { clearSession } from '../../shared/auth/session'
import * as sectorsApi from '../sectors/sectorsApi'
import { ApiError } from './securityGuardsApi'
import * as securityGuardsApi from './securityGuardsApi'
import { SecurityGuardsPage } from './SecurityGuardsPage'
import { expSoon, makeUnsignedJwt } from '../../test/jwtTestUtils'
import type { SectorNestedDto, SecurityGuardDto } from './types'

const mockSectorPick = [
  {
    id: 's1',
    name: 'Sector A',
    description: null,
    requiredGuardsPerDay: 1,
    isActive: true,
    createdAt: '2026-03-02T14:30:00.000Z',
  },
]

function seedSession(role: 'Admin' | 'Supervisor'): void {
  const token = makeUnsignedJwt({
    exp: expSoon(),
    role,
    email: role === 'Admin' ? 'admin@test.com' : 'supervisor@test.com',
  })
  sessionStorage.setItem(AUTH_SESSION_STORAGE_KEY, JSON.stringify({ token }))
}

function renderPage(role: 'Admin' | 'Supervisor'): ReturnType<typeof render> {
  seedSession(role)
  return render(
    <MemoryRouter initialEntries={['/app/security-guards']}>
      <AuthProvider>
        <Routes>
          <Route path="/app/security-guards" element={<SecurityGuardsPage />} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('SecurityGuardsPage', () => {
  beforeEach(() => {
    vi.spyOn(sectorsApi, 'listSectors').mockResolvedValue(mockSectorPick)
    vi.spyOn(securityGuardsApi, 'listSecurityGuards').mockResolvedValue([
      {
        id: 'a1',
        name: 'Ana Costa',
        isActive: true,
        createdAt: '2026-03-02T14:30:00.000Z',
        sectors: [],
      },
    ])
    vi.spyOn(securityGuardsApi, 'createSecurityGuard').mockResolvedValue({ id: 'new-id' })
    vi.spyOn(securityGuardsApi, 'setGuardSectors').mockResolvedValue(undefined)
    vi.spyOn(securityGuardsApi, 'updateSecurityGuard').mockResolvedValue(undefined)
    vi.spyOn(securityGuardsApi, 'inactivateSecurityGuard').mockResolvedValue(undefined)
  })

  afterEach(() => {
    vi.restoreAllMocks()
    clearSession()
    sessionStorage.clear()
  })

  it('shows list for Supervisor without admin controls', async () => {
    renderPage('Supervisor')
    await waitFor(() => {
      expect(securityGuardsApi.listSecurityGuards).toHaveBeenCalledWith(undefined)
    })
    expect(await screen.findByText('Ana Costa')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /add personnel/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Ana Costa' })).not.toBeInTheDocument()
  })

  it('shows admin actions for Admin user', async () => {
    renderPage('Admin')
    expect(await screen.findByRole('button', { name: /add personnel/i })).toBeInTheDocument()
    expect(await screen.findByRole('button', { name: 'Ana Costa' })).toBeInTheDocument()
  })

  it('shows empty message when nothing returned', async () => {
    vi.mocked(securityGuardsApi.listSecurityGuards).mockResolvedValueOnce([])
    renderPage('Supervisor')
    expect(await screen.findByText(/no personnel found/i)).toBeInTheDocument()
  })

  it('shows load error message from ApiError when fetch fails', async () => {
    vi.mocked(securityGuardsApi.listSecurityGuards).mockRejectedValueOnce(new ApiError(403, 'Sem permissão na API.'))
    renderPage('Admin')
    expect(await screen.findByRole('alert')).toHaveTextContent(/sem permissão na api/i)
  })

  it('validates mandatory name before create submit', async () => {
    const user = userEvent.setup()
    renderPage('Admin')
    await screen.findByText('Ana Costa')

    await user.click(screen.getByRole('button', { name: /add personnel/i }))
    await user.click(within(screen.getByRole('form')).getByRole('button', { name: /^create$/i }))

    expect(await screen.findByText(/enter a name/i)).toBeInTheDocument()
    expect(securityGuardsApi.createSecurityGuard).not.toHaveBeenCalled()
  })

  it('submits create, assigns sectors, and refreshes list', async () => {
    const user = userEvent.setup()
    vi.mocked(securityGuardsApi.listSecurityGuards).mockReset()
    const ana: SecurityGuardDto = {
      id: 'a1',
      name: 'Ana Costa',
      isActive: true,
      createdAt: '2026-03-02T14:30:00.000Z',
      sectors: [],
    }
    const nested: SectorNestedDto = {
      id: 's1',
      name: 'Sector A',
      description: null,
      requiredGuardsPerDay: 1,
      isActive: true,
      createdAt: '2026-03-02T14:30:00.000Z',
    }
    const maria: SecurityGuardDto = {
      id: 'new-id',
      name: 'Maria Souza',
      isActive: true,
      createdAt: '2026-03-02T14:30:00.000Z',
      sectors: [nested],
    }
    vi.mocked(securityGuardsApi.listSecurityGuards)
      .mockResolvedValueOnce([ana])
      .mockResolvedValueOnce([maria, ana])

    renderPage('Admin')
    expect(await screen.findByText('Ana Costa')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /add personnel/i }))
    await screen.findByLabelText(/sector a/i)
    await user.click(screen.getByLabelText(/sector a/i))
    await user.type(screen.getByLabelText(/^name$/i), 'Maria Souza')

    await user.click(within(screen.getByRole('form')).getByRole('button', { name: /^create$/i }))

    await waitFor(() => {
      expect(securityGuardsApi.createSecurityGuard).toHaveBeenCalledWith('Maria Souza')
    })
    await waitFor(() => {
      expect(securityGuardsApi.setGuardSectors).toHaveBeenCalledWith('new-id', ['s1'])
    })

    await waitFor(() => {
      expect(screen.getByText('Maria Souza')).toBeInTheDocument()
    })
  })
})
