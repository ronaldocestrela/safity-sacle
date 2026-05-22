import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../../shared/auth/AuthProvider'
import { AUTH_SESSION_STORAGE_KEY } from '../../shared/auth/session'
import { clearSession } from '../../shared/auth/session'
import * as securityGuardsApi from '../security-guards/securityGuardsApi'
import { ApiError as GuardsApiError } from '../security-guards/securityGuardsApi'
import * as unavailableDaysApi from './unavailableDaysApi'
import { UnavailableDaysPage } from './UnavailableDaysPage'
import { expSoon, makeUnsignedJwt } from '../../test/jwtTestUtils'

function pad2(n: number): string {
  return String(n).padStart(2, '0')
}

function keyDay1(): string {
  const d = new Date()
  return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-01`
}

function keyDay5(): string {
  const d = new Date()
  return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-05`
}

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
    <MemoryRouter initialEntries={['/app/unavailable-days']}>
      <AuthProvider>
        <Routes>
          <Route path="/app/unavailable-days" element={<UnavailableDaysPage />} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('UnavailableDaysPage', () => {
  const guardId = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'

  beforeEach(() => {
    vi.spyOn(securityGuardsApi, 'listSecurityGuards').mockResolvedValue([
      {
        id: guardId,
        name: 'Ana Costa',
        isActive: true,
        createdAt: '2026-03-02T14:30:00.000Z',
        sectors: [],
      },
    ])
    vi.spyOn(unavailableDaysApi, 'listUnavailableDays').mockResolvedValue([])
    vi.spyOn(unavailableDaysApi, 'addUnavailableDay').mockResolvedValue({ id: 'new-u' })
    vi.spyOn(unavailableDaysApi, 'removeUnavailableDay').mockResolvedValue(undefined)
  })

  afterEach(() => {
    vi.restoreAllMocks()
    clearSession()
    sessionStorage.clear()
  })

  it('shows restrictions for Supervisor without admin controls', async () => {
    renderPage('Supervisor')
    await waitFor(() => expect(unavailableDaysApi.listUnavailableDays).toHaveBeenCalledWith(guardId))
    expect(screen.queryByRole('button', { name: /save restrictions/i })).not.toBeInTheDocument()
    expect(screen.queryByLabelText(/reason \(optional\)/i)).not.toBeInTheDocument()
    const d1 = screen.getByRole('button', { name: keyDay1() })
    expect(d1).toBeDisabled()
  })

  it('shows load error when personnel list fails', async () => {
    vi.mocked(securityGuardsApi.listSecurityGuards).mockRejectedValueOnce(new GuardsApiError(403, 'Sem permissão na API.'))
    renderPage('Admin')
    expect(await screen.findByRole('alert')).toHaveTextContent(/sem permissão na api/i)
  })

  it('shows unavailable tags from API', async () => {
    const k5 = keyDay5()
    vi.mocked(unavailableDaysApi.listUnavailableDays).mockResolvedValueOnce([
      {
        id: 'u-existing',
        securityGuardId: guardId,
        date: k5,
        reason: null,
      },
    ])
    renderPage('Admin')
    expect(await screen.findByText('UNAVAIL')).toBeInTheDocument()
  })

  it('submits pending adds on save and refreshes list', async () => {
    const user = userEvent.setup()
    const k1 = keyDay1()
    renderPage('Admin')
    await waitFor(() => expect(screen.getByRole('button', { name: k1 })).toBeEnabled())

    await user.click(screen.getByRole('button', { name: k1 }))
    const saveBtn = screen.getByRole('button', { name: /save restrictions/i })
    expect(saveBtn).toBeEnabled()
    await user.type(screen.getByLabelText(/reason \(optional\)/i), 'Conference')
    await user.click(saveBtn)

    await waitFor(() => {
      expect(unavailableDaysApi.addUnavailableDay).toHaveBeenCalledWith(guardId, {
        date: k1,
        reason: 'Conference',
      })
    })
    await waitFor(() => expect(unavailableDaysApi.listUnavailableDays).toHaveBeenCalled())
  })

  it('submits removals on save', async () => {
    const user = userEvent.setup()
    const k5 = keyDay5()
    vi.mocked(unavailableDaysApi.listUnavailableDays).mockResolvedValue([
      {
        id: 'u-remove',
        securityGuardId: guardId,
        date: k5,
        reason: null,
      },
    ])
    vi.mocked(unavailableDaysApi.addUnavailableDay).mockClear()

    renderPage('Admin')

    await waitFor(() => expect(screen.getByRole('button', { name: k5 })).toBeEnabled())
    await user.click(screen.getByRole('button', { name: k5 }))

    await user.click(screen.getByRole('button', { name: /save restrictions/i }))

    await waitFor(() => {
      expect(unavailableDaysApi.removeUnavailableDay).toHaveBeenCalledWith('u-remove')
    })
    expect(unavailableDaysApi.addUnavailableDay).not.toHaveBeenCalled()
  })

  it('surfaces duplicate date error from API', async () => {
    const user = userEvent.setup()
    vi.mocked(unavailableDaysApi.addUnavailableDay).mockRejectedValueOnce(
      new unavailableDaysApi.ApiError(409, 'Duplicate date'),
    )
    const k1 = keyDay1()
    renderPage('Admin')
    await waitFor(() => expect(screen.getByRole('button', { name: k1 })).toBeEnabled())
    await user.click(screen.getByRole('button', { name: k1 }))
    await user.click(screen.getByRole('button', { name: /save restrictions/i }))
    expect(await screen.findByRole('alert')).toHaveTextContent(/duplicate date/i)
  })

  it('shows days load error', async () => {
    vi.mocked(unavailableDaysApi.listUnavailableDays).mockRejectedValueOnce(
      new unavailableDaysApi.ApiError(403, 'No access'),
    )
    renderPage('Admin')
    expect(await screen.findByRole('alert')).toHaveTextContent(/no access/i)
  })
})
