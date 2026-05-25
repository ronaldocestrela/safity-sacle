import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../../shared/auth/AuthProvider'
import { AUTH_SESSION_STORAGE_KEY } from '../../shared/auth/session'
import { clearSession } from '../../shared/auth/session'
import { ApiError } from './sectorsApi'
import * as sectorsApi from './sectorsApi'
import { SectorsPage } from './SectorsPage'
import { expSoon, makeUnsignedJwt } from '../../test/jwtTestUtils'

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
    <MemoryRouter initialEntries={['/app/sectors']}>
      <AuthProvider>
        <Routes>
          <Route path="/app/sectors" element={<SectorsPage />} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('SectorsPage', () => {
  beforeEach(() => {
    vi.spyOn(sectorsApi, 'listSectors').mockResolvedValue([
      {
        id: 's1',
        name: 'Perimeter',
        description: 'Outer ring',
        requiredGuardsPerDay: 2,
        isActive: true,
        createdAt: '2026-03-02T14:30:00.000Z',
      },
    ])
    vi.spyOn(sectorsApi, 'createSector').mockResolvedValue({ id: 'new-id' })
    vi.spyOn(sectorsApi, 'updateSector').mockResolvedValue(undefined)
    vi.spyOn(sectorsApi, 'inactivateSector').mockResolvedValue(undefined)
  })

  afterEach(() => {
    vi.restoreAllMocks()
    clearSession()
    sessionStorage.clear()
  })

  it('shows list for Supervisor without admin controls', async () => {
    renderPage('Supervisor')
    await waitFor(() => {
      expect(sectorsApi.listSectors).toHaveBeenCalledWith(undefined)
    })
    expect(await screen.findByText('Perimeter')).toBeInTheDocument()
    expect(screen.getByText(/2 positions\/day/i)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /adicionar setor/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Perimeter' })).not.toBeInTheDocument()
  })

  it('shows admin actions for Admin user', async () => {
    renderPage('Admin')
    expect(await screen.findByRole('button', { name: /adicionar setor/i })).toBeInTheDocument()
    expect(await screen.findByRole('button', { name: 'Perimeter' })).toBeInTheDocument()
  })

  it('shows empty message when nothing returned', async () => {
    vi.mocked(sectorsApi.listSectors).mockResolvedValueOnce([])
    renderPage('Supervisor')
    expect(await screen.findByText(/não há setores encontrados para este filtro/i)).toBeInTheDocument()
  })

  it('shows load error message from ApiError when fetch fails', async () => {
    vi.mocked(sectorsApi.listSectors).mockRejectedValueOnce(new ApiError(403, 'Sem permissão na API.'))
    renderPage('Admin')
    expect(await screen.findByRole('alert')).toHaveTextContent(/sem permissão na api/i)
  })

  it('validates mandatory name before create submit', async () => {
    const user = userEvent.setup()
    renderPage('Admin')
    await screen.findByText('Perimeter')

    await user.click(screen.getByRole('button', { name: /adicionar setor/i }))
    await user.click(within(screen.getByRole('form')).getByRole('button', { name: /^criar$/i }))

    expect(await screen.findByText(/informe um nome/i)).toBeInTheDocument()
    expect(sectorsApi.createSector).not.toHaveBeenCalled()
  })

  it('submits create and refreshes list', async () => {
    const user = userEvent.setup()
    vi.mocked(sectorsApi.listSectors).mockReset()
    const perimeter = {
      id: 's1',
      name: 'Perimeter',
      description: 'Outer ring',
      requiredGuardsPerDay: 2,
      isActive: true,
      createdAt: '2026-03-02T14:30:00.000Z',
    }
    const lobby = {
      id: 'new-id',
      name: 'Lobby',
      description: null,
      requiredGuardsPerDay: 3,
      isActive: true,
      createdAt: '2026-03-02T14:30:00.000Z',
    }
    vi.mocked(sectorsApi.listSectors).mockResolvedValueOnce([perimeter]).mockResolvedValueOnce([lobby, perimeter])

    renderPage('Admin')
    expect(await screen.findByText('Perimeter')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /adicionar setor/i }))
    await user.type(screen.getByLabelText(/^nome$/i), 'Lobby')
    await user.type(screen.getByLabelText(/^descrição$/i), 'Main entrance')
    const positions = screen.getByLabelText(/^posições por dia$/i)
    await user.clear(positions)
    await user.type(positions, '3')

    await user.click(within(screen.getByRole('form')).getByRole('button', { name: /^criar$/i }))

    await waitFor(() => {
      expect(sectorsApi.createSector).toHaveBeenCalledWith('Lobby', 'Main entrance', 3)
    })

    await waitFor(() => {
      expect(screen.getByText('Lobby')).toBeInTheDocument()
    })
  })
})
