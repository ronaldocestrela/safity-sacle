import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../../shared/auth/AuthProvider'
import { AUTH_SESSION_STORAGE_KEY } from '../../shared/auth/session'
import { clearSession } from '../../shared/auth/session'
import { expSoon, makeUnsignedJwt } from '../../test/jwtTestUtils'
import { ApiError as GuardsApiError } from '../security-guards/securityGuardsApi'
import * as securityGuardsApi from '../security-guards/securityGuardsApi'
import { ApiError as SchedulesApiError } from '../schedules/schedulesApi'
import * as schedulesApi from '../schedules/schedulesApi'
import { WelcomePage } from './WelcomePage'

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
    <MemoryRouter initialEntries={['/app']}>
      <AuthProvider>
        <Routes>
          <Route path="/app" element={<WelcomePage />} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('WelcomePage (dashboard)', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    vi.setSystemTime(new Date('2026-05-07T15:00:00.000Z'))

    vi.spyOn(securityGuardsApi, 'listSecurityGuards').mockResolvedValue([
      {
        id: 'g-active',
        name: 'Ana Costa',
        isActive: true,
        createdAt: '2026-03-02T14:30:00.000Z',
      },
      {
        id: 'g-inactive',
        name: 'Bruno Silva',
        isActive: false,
        createdAt: '2026-03-03T14:30:00.000Z',
      },
    ])
    vi.spyOn(schedulesApi, 'getScheduleByMonthYear').mockResolvedValue({
      id: 'sched-1',
      month: 5,
      year: 2026,
      generatedAt: '2026-05-01T10:00:00.000Z',
      items: [
        {
          id: 'i-past',
          securityGuardId: 'g-active',
          securityGuardName: 'Ana Costa',
          securityGuardIsActive: true,
          date: '2026-05-05',
          isWeekend: false,
        },
        {
          id: 'i-future',
          securityGuardId: 'g-active',
          securityGuardName: 'Ana Costa',
          securityGuardIsActive: true,
          date: '2026-05-15',
          isWeekend: false,
        },
        {
          id: 'i-weekend',
          securityGuardId: 'g-inactive',
          securityGuardName: 'Bruno Silva',
          securityGuardIsActive: false,
          date: '2026-05-17',
          isWeekend: true,
        },
      ],
    })
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
    clearSession()
    sessionStorage.clear()
  })

  it('shows loading skeleton then KPIs from API data', async () => {
    renderPage('Admin')
    expect(screen.getByLabelText(/loading dashboard/i)).toBeInTheDocument()

    await waitFor(() => {
      expect(screen.queryByLabelText(/loading dashboard/i)).not.toBeInTheDocument()
    })

    expect(screen.getByTestId('kpi-active')).toHaveTextContent('1')
    expect(screen.getByTestId('kpi-inactive')).toHaveTextContent('1')
    expect(screen.getByTestId('kpi-assignments')).toHaveTextContent('3')
    expect(screen.getByTestId('kpi-weekend')).toHaveTextContent('1')

    await waitFor(() => {
      expect(screen.getByText(/no assignments on this day/i)).toBeInTheDocument()
    })

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime })
    await user.click(screen.getByRole('button', { name: /May 15, 2026, 1 assignment/i }))
    expect(screen.getByRole('heading', { level: 3, name: /May 15, 2026/ })).toBeInTheDocument()
    expect(screen.getAllByText('Ana Costa').length).toBeGreaterThan(0)

    await user.click(screen.getByRole('button', { name: /May 17, 2026, 1 assignment/i }))
    expect(screen.getAllByText('Bruno Silva').length).toBeGreaterThan(0)
    expect(screen.getByTestId('weekend-day-hint')).toHaveTextContent('Weekend shift')
  })

  it('shows empty schedule state with Admin CTA', async () => {
    vi.mocked(schedulesApi.getScheduleByMonthYear).mockResolvedValueOnce(null)
    renderPage('Admin')

    await waitFor(() => {
      expect(screen.queryByLabelText(/loading dashboard/i)).not.toBeInTheDocument()
    })

    expect(screen.getByText(/no schedule for may 2026/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /open schedules to generate/i })).toHaveAttribute(
      'href',
      '/app/schedules',
    )
  })

  it('shows empty schedule state with Supervisor CTA', async () => {
    vi.mocked(schedulesApi.getScheduleByMonthYear).mockResolvedValueOnce(null)
    renderPage('Supervisor')

    await waitFor(() => {
      expect(screen.queryByLabelText(/loading dashboard/i)).not.toBeInTheDocument()
    })

    expect(screen.getByRole('link', { name: /view schedules/i })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /open schedules to generate/i })).not.toBeInTheDocument()
  })

  it('shows guards error banner and retry refetches', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime })
    vi.mocked(securityGuardsApi.listSecurityGuards).mockRejectedValueOnce(
      new GuardsApiError(403, 'Sem permissão.'),
    )
    vi.mocked(securityGuardsApi.listSecurityGuards).mockResolvedValueOnce([
      {
        id: 'g1',
        name: 'Only One',
        isActive: true,
        createdAt: '2026-03-02T14:30:00.000Z',
      },
    ])

    renderPage('Admin')

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent(/sem permissão/i)

    const retryButtons = within(document.body).getAllByRole('button', { name: /retry/i })
    await user.click(retryButtons[0])

    await waitFor(() => {
      expect(securityGuardsApi.listSecurityGuards).toHaveBeenCalledTimes(2)
    })
    await waitFor(() => {
      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })
  })

  it('shows schedule error banner when roster fetch fails', async () => {
    vi.mocked(schedulesApi.getScheduleByMonthYear).mockRejectedValueOnce(
      new SchedulesApiError(500, 'Server error'),
    )
    renderPage('Supervisor')

    expect(await screen.findByRole('alert')).toHaveTextContent(/server error/i)
    expect(screen.getByTestId('kpi-active')).toHaveTextContent('1')
  })
})
