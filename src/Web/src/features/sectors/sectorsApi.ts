import { apiFetch } from '../../shared/api/http'
import { readApiErrorMessage } from '../../shared/api/readApiError'
import type { SectorDto } from './types'

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
export async function listSectors(isActive?: boolean): Promise<SectorDto[]> {
  const qs = isActive === undefined ? '' : `?isActive=${isActive}`
  const res = await apiFetch(`/api/sectors${qs}`)
  await ensureOk(res, 'Não foi possível carregar setores.')
  return (await res.json()) as SectorDto[]
}

export async function createSector(name: string, description: string | null): Promise<{ id: string }> {
  const res = await apiFetch('/api/sectors', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, description: description?.trim() ? description.trim() : null }),
  })
  await ensureOk(res, 'Não foi possível criar setor.')
  const data = (await res.json()) as { id: string }
  return data
}

export async function updateSector(id: string, name: string, description: string | null): Promise<void> {
  const res = await apiFetch(`/api/sectors/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, description: description?.trim() ? description.trim() : null }),
  })
  await ensureOk(res, 'Não foi possível salvar alterações.')
}

export async function inactivateSector(id: string): Promise<void> {
  const res = await apiFetch(`/api/sectors/${encodeURIComponent(id)}/inactive`, {
    method: 'PATCH',
  })
  await ensureOk(res, 'Não foi possível inativar.')
}

export async function activateSector(id: string): Promise<void> {
  const res = await apiFetch(`/api/sectors/${encodeURIComponent(id)}/active`, {
    method: 'PATCH',
  })
  await ensureOk(res, 'Não foi possível reativar.')
}
