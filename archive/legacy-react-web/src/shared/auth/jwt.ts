import type { UserRole } from './types'

const ROLE_CLAIM_KEYS = [
  'role',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
] as const

const TENANT_CLAIM_KEYS = ['tenant_id'] as const

function base64UrlToJson(segment: string): Record<string, unknown> | null {
  try {
    const padded = segment.replace(/-/g, '+').replace(/_/g, '/')
    const pad = padded.length % 4
    const base64 = pad ? padded + '='.repeat(4 - pad) : padded
    const json = atob(base64)
    return JSON.parse(json) as Record<string, unknown>
  } catch {
    return null
  }
}

/** Extracts role strings from a decoded JWT payload (ASP.NET Identity / ClaimTypes.Role). */
export function collectRoleClaims(payload: Record<string, unknown>): string[] {
  const roles: string[] = []
  for (const key of ROLE_CLAIM_KEYS) {
    const v = payload[key]
    if (typeof v === 'string') roles.push(v)
    else if (Array.isArray(v)) {
      for (const item of v) {
        if (typeof item === 'string') roles.push(item)
      }
    }
  }
  return [...new Set(roles)]
}

export function filterAppRoles(roles: string[]): UserRole[] {
  const allowed = new Set<UserRole>(['Admin', 'Supervisor'])
  return roles.filter((r): r is UserRole => allowed.has(r as UserRole))
}

export function parseJwtPayload(token: string): Record<string, unknown> | null {
  const parts = token.split('.')
  if (parts.length < 2) return null
  return base64UrlToJson(parts[1])
}

export function isJwtExpired(payload: Record<string, unknown>, nowMs = Date.now()): boolean {
  const exp = payload.exp
  if (typeof exp !== 'number') return false
  return nowMs >= exp * 1000
}

export function emailFromPayload(payload: Record<string, unknown>): string | null {
  const email = payload.email
  if (typeof email === 'string' && email.length > 0) return email
  const uniqueName = payload.unique_name
  if (typeof uniqueName === 'string' && uniqueName.includes('@')) return uniqueName
  return null
}

/** Reads the logical tenant id claim from the API-issued JWT. */
export function tenantIdFromPayload(payload: Record<string, unknown>): string | null {
  for (const key of TENANT_CLAIM_KEYS) {
    const v = payload[key]
    if (typeof v === 'string' && v.length > 0) {
      return v
    }
  }

  return null
}
