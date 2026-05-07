import {
  collectRoleClaims,
  emailFromPayload,
  filterAppRoles,
  isJwtExpired,
  parseJwtPayload,
} from './jwt'
import type { AuthSession } from './types'

export const AUTH_SESSION_STORAGE_KEY = 'safetyscale.auth.session'

type StoredShape = {
  token: string
}

export function buildSessionFromToken(token: string): AuthSession | null {
  const payload = parseJwtPayload(token)
  if (!payload) return null
  if (isJwtExpired(payload)) return null
  const roles = filterAppRoles(collectRoleClaims(payload))
  return {
    token,
    email: emailFromPayload(payload),
    roles,
  }
}

export function getStoredToken(): string | null {
  const raw = sessionStorage.getItem(AUTH_SESSION_STORAGE_KEY)
  if (!raw) return null
  try {
    const parsed = JSON.parse(raw) as StoredShape
    return typeof parsed.token === 'string' ? parsed.token : null
  } catch {
    return null
  }
}

export function loadSession(): AuthSession | null {
  const token = getStoredToken()
  if (!token) return null
  const session = buildSessionFromToken(token)
  if (!session) {
    clearSession()
    return null
  }
  return session
}

export function saveSessionToken(token: string): AuthSession | null {
  const session = buildSessionFromToken(token)
  if (!session) return null
  const toStore: StoredShape = { token }
  sessionStorage.setItem(AUTH_SESSION_STORAGE_KEY, JSON.stringify(toStore))
  return session
}

export function clearSession(): void {
  sessionStorage.removeItem(AUTH_SESSION_STORAGE_KEY)
}
