import { apiFetch } from '../../shared/api/http'
import { readApiErrorMessage } from '../../shared/api/readApiError'
import type { MonthlyScheduleDto } from './types'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function ensureOk(response: Response, fallback: string): Promise<void> {
  if (response.ok) {
    return
  }

  let msg = await readApiErrorMessage(response)

  if (response.status === 403) {
    msg = msg ?? 'Você não tem permissão para esta ação.'
  } else if (response.status === 404) {
    msg = msg ?? 'Registro não encontrado.'
  } else if (response.status === 400 || response.status === 422) {
    msg = msg ?? 'Dados inválidos. Confira os campos.'
  } else if (response.status === 409) {
    msg = msg ?? 'Escala já gerada para este mês e ano.'
  }

  throw new ApiError(response.status, msg ?? fallback)
}

function monthYearMessage(month: number, year: number): string {
  return `${String(month).padStart(2, '0')}/${year}`
}

export async function getScheduleByMonthYear(month: number, year: number): Promise<MonthlyScheduleDto | null> {
  const res = await apiFetch(`/api/schedules/month/${month}/year/${year}`)
  if (res.status === 404) {
    return null
  }
  await ensureOk(res, `Could not load schedule for ${monthYearMessage(month, year)}.`)
  return (await res.json()) as MonthlyScheduleDto
}

export async function getScheduleById(id: string): Promise<MonthlyScheduleDto | null> {
  const res = await apiFetch(`/api/schedules/${encodeURIComponent(id)}`)
  if (res.status === 404) {
    return null
  }
  await ensureOk(res, 'Could not load schedule.')
  return (await res.json()) as MonthlyScheduleDto
}

export async function generateSchedule(month: number, year: number): Promise<{ id: string }> {
  const res = await apiFetch('/api/schedules/generate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ month, year }),
  })

  if (res.status === 201) {
    return (await res.json()) as { id: string }
  }

  await ensureOk(res, `Não foi possível gerar a escala para ${monthYearMessage(month, year)}.`)
  throw new ApiError(res.status, 'Resposta inesperada ao gerar a escala.')
}
