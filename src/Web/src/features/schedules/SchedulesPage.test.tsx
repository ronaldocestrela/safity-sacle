import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../../shared/auth/AuthProvider'
import { AUTH_SESSION_STORAGE_KEY } from '../../shared/auth/session'
import { clearSession } from '../../shared/auth/session'
import { ApiError } from './schedulesApi'
import * as schedulesApi from './schedulesApi'
import { SchedulesPage } from './SchedulesPage'
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
    <MemoryRouter initialEntries={['/app/schedules']}>
      <AuthProvider>
        <Routes>
          <Route path="/app/schedules" element={<SchedulesPage />} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

const sampleSchedule = {
  id: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
  month: 5,
  year: 2026,
  generatedAt: '2026-05-01T10:00:00.000Z',
  items: [
    {
      id: 'iiiiiiii-iiii-iiii-iiii-iiiiiiiiiiii',
      securityGuardId: 'gggggggg-gggg-gggg-gggg-gggggggggggg',
      securityGuardName: 'Pat Smith',
      securityGuardIsActive: true,
      sectorId: 'ssssssss-ssss-ssss-ssss-ssssssssssss',
      sectorName: 'Primary',
      date: '2026-05-07',
      isWeekend: false,
    },
    {
      id: 'jjjjjjjj-jjjj-jjjj-jjjj-jjjjjjjjjjjj',
      securityGuardId: 'hhhhhhhh-hhhh-hhhh-hhhh-hhhhhhhhhhhh',
      securityGuardName: 'Alex Inactive',
      securityGuardIsActive: false,
      sectorId: 'ssssssss-ssss-ssss-ssss-ssssssssssss',
      sectorName: 'Primary',
      date: '2026-05-10',
      isWeekend: true,
    },
  ],
}

describe('SchedulesPage', () => {
  beforeEach(() => {
    vi.spyOn(schedulesApi, 'getScheduleByMonthYear').mockResolvedValue(null)
    vi.spyOn(schedulesApi, 'generateSchedule').mockResolvedValue({ id: sampleSchedule.id })
  })

  afterEach(() => {
    vi.restoreAllMocks()
    clearSession()
    sessionStorage.clear()
  })

  it('loads roster on mount', async () => {
    renderPage('Supervisor')
    await waitFor(() => {
      expect(schedulesApi.getScheduleByMonthYear).toHaveBeenCalled()
    })
    const calls = vi.mocked(schedulesApi.getScheduleByMonthYear).mock.calls
    const [m, y] = calls[0]!
    expect(typeof m).toBe('number')
    expect(typeof y).toBe('number')
  })

  it('shows Generate schedule for Admin only', async () => {
    renderPage('Admin')
    expect(await screen.findByRole('button', { name: /generate schedule/i })).toBeInTheDocument()
  })

  it('hides Generate schedule for Supervisor', async () => {
    renderPage('Supervisor')
    await waitFor(() => expect(schedulesApi.getScheduleByMonthYear).toHaveBeenCalled())
    expect(screen.queryByRole('button', { name: /generate schedule/i })).not.toBeInTheDocument()
  })

  it('shows assignments when API returns data', async () => {
    vi.mocked(schedulesApi.getScheduleByMonthYear).mockResolvedValue(sampleSchedule)
    renderPage('Supervisor')
    expect(await screen.findByText('Pat Smith')).toBeInTheDocument()
    expect(screen.getByText('Alex Inactive')).toBeInTheDocument()
    expect(screen.getAllByText('Primary').length).toBe(2)
    expect(screen.getByText('Weekend')).toBeInTheDocument()
    expect(screen.getByText('Inactive')).toBeInTheDocument()
  })

  it('shows not-found banner when schedule is missing', async () => {
    vi.mocked(schedulesApi.getScheduleByMonthYear).mockResolvedValue(null)
    renderPage('Supervisor')
    expect(await screen.findByRole('alert')).toHaveTextContent(/no schedule found/i)
  })

  it('Admin generate triggers POST and reload', async () => {
    const user = userEvent.setup()
    vi.mocked(schedulesApi.getScheduleByMonthYear).mockResolvedValueOnce(null).mockResolvedValueOnce(sampleSchedule)

    renderPage('Admin')
    await waitFor(() => expect(schedulesApi.getScheduleByMonthYear).toHaveBeenCalled())

    await user.click(screen.getByRole('button', { name: /generate schedule/i }))

    await waitFor(() => {
      expect(schedulesApi.generateSchedule).toHaveBeenCalledWith(expect.any(Number), expect.any(Number))
    })
    await waitFor(() => {
      expect(schedulesApi.getScheduleByMonthYear).toHaveBeenCalledTimes(2)
    })
  })

  it('shows API coverage error message when generate fails with 400', async () => {
    const user = userEvent.setup()
    vi.mocked(schedulesApi.generateSchedule).mockRejectedValueOnce(
      new ApiError(
        400,
        'Não foi possível gerar a escala para 02/05/2026 porque não há seguranças elegíveis suficientes para cobrir todas as vagas do dia.',
      ),
    )

    renderPage('Admin')
    await waitFor(() => expect(schedulesApi.getScheduleByMonthYear).toHaveBeenCalled())

    await user.click(screen.getByRole('button', { name: /generate schedule/i }))

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent(/não foi possível gerar a escala/i)
    expect(alert).toHaveTextContent(/02\/05\/2026/)
    expect(alert).toHaveTextContent(/seguranças elegíveis/i)
  })
})
