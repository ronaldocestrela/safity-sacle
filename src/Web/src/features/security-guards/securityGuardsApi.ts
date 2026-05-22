import { apiFetch } from '../../shared/api/http'
import { readApiErrorMessage } from '../../shared/api/readApiError'
import type { SecurityGuardDto } from './types'

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
  }

  throw new ApiError(response.status, msg ?? fallback)
}

/** `isActive`: `undefined` = all; otherwise filter active/inactive. */
export async function listSecurityGuards(isActive?: boolean): Promise<SecurityGuardDto[]> {
  const qs = isActive === undefined ? '' : `?isActive=${isActive}`
  const res = await apiFetch(`/api/security-guards${qs}`)
  await ensureOk(res, 'Não foi possível carregar seguranças.')
  return (await res.json()) as SecurityGuardDto[]
}

export async function createSecurityGuard(name: string): Promise<{ id: string }> {
  const res = await apiFetch('/api/security-guards', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name }),
  })
  await ensureOk(res, 'Não foi possível criar segurança.')
  const data = (await res.json()) as { id: string }
  return data
}

export async function updateSecurityGuard(id: string, name: string): Promise<void> {
  const res = await apiFetch(`/api/security-guards/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name }),
  })
  await ensureOk(res, 'Não foi possível salvar alterações.')
}

export async function inactivateSecurityGuard(id: string): Promise<void> {
  const res = await apiFetch(`/api/security-guards/${encodeURIComponent(id)}/inactive`, {
    method: 'PATCH',
  })
  await ensureOk(res, 'Não foi possível inativar.')
}

export async function activateSecurityGuard(id: string): Promise<void> {
  const res = await apiFetch(`/api/security-guards/${encodeURIComponent(id)}/active`, {
    method: 'PATCH',
  })
  await ensureOk(res, 'Não foi possível reativar.')
}

export async function setGuardSectors(guardId: string, sectorIds: string[]): Promise<void> {
  const res = await apiFetch(`/api/security-guards/${encodeURIComponent(guardId)}/sectors`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ sectorIds }),
  })
  await ensureOk(res, 'Não foi possível salvar setores do segurança.')
}
