import { clearSession, getStoredToken } from '../auth/session'
import { apiUrl } from '../config/env'

export type ApiFetchOptions = RequestInit & {
  /** When true, 401 does not clear session or trigger logout redirect (e.g. login form). */
  skipAuthRedirect?: boolean
}

let onUnauthorized: (() => void) | undefined

export function setOnUnauthorized(handler: (() => void) | undefined): void {
  onUnauthorized = handler
}

export async function apiFetch(path: string, init?: ApiFetchOptions): Promise<Response> {
  const { skipAuthRedirect, ...rest } = init ?? {}
  const hadToken = Boolean(getStoredToken())

  const headers = new Headers(rest.headers)
  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json')
  }

  const token = getStoredToken()
  if (token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const res = await fetch(apiUrl(path), { ...rest, headers })

  if (res.status === 401 && !skipAuthRedirect && hadToken) {
    clearSession()
    onUnauthorized?.()
  }

  return res
}
