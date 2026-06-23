import { apiFetch } from '../../shared/api/http'
import { readApiErrorMessage } from '../../shared/api/readApiError'
import type { AddUnavailableDayPayload, UnavailableDayDto } from './types'

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
    msg = msg ?? 'This date is already marked unavailable for this personnel.'
  }

  throw new ApiError(response.status, msg ?? fallback)
}

export async function listUnavailableDays(securityGuardId: string): Promise<UnavailableDayDto[]> {
  const res = await apiFetch(`/api/security-guards/${encodeURIComponent(securityGuardId)}/unavailable-days`)
  await ensureOk(res, 'Could not load restrictions.')
  return (await res.json()) as UnavailableDayDto[]
}

export async function addUnavailableDay(
  securityGuardId: string,
  payload: AddUnavailableDayPayload,
): Promise<{ id: string }> {
  const res = await apiFetch(`/api/security-guards/${encodeURIComponent(securityGuardId)}/unavailable-days`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      date: payload.date,
      reason: payload.reason?.trim() ? payload.reason.trim() : null,
    }),
  })
  await ensureOk(res, 'Could not save restriction.')
  const data = (await res.json()) as { id: string }
  return data
}

export async function removeUnavailableDay(id: string): Promise<void> {
  const res = await apiFetch(`/api/unavailable-days/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  })
  await ensureOk(res, 'Could not remove restriction.')
}
