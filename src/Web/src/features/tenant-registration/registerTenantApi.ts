import { apiFetch } from '../../shared/api/http'

export type RegisterTenantPayload = {
  tenantName: string
  adminName: string
  adminEmail: string
  adminPassword: string
  confirmPassword: string
}

export type RegisterTenantOutcome =
  | { ok: true; tenantId: string; tenantSlug: string; adminUserId: string }
  | {
      ok: false
      reason: 'tenant-exists' | 'email-exists' | 'invalid-password' | 'validation' | 'network'
      messages?: string[]
    }

type RegisterTenantResponseBody = {
  tenantId?: string
  tenantSlug?: string
  adminUserId?: string
}

type ProblemBody = {
  errors?: unknown
  message?: string
}

function parseErrors(data: ProblemBody): string[] {
  const errs = data.errors
  if (Array.isArray(errs)) {
    return errs.filter((x): x is string => typeof x === 'string')
  }
  if (typeof errs === 'object' && errs !== null) {
    return Object.entries(errs as Record<string, unknown>).flatMap(([key, values]) =>
      typeof values === 'string'
        ? [`${key}: ${values}`]
        : Array.isArray(values)
          ? values.filter((v): v is string => typeof v === 'string').map((v) => `${key}: ${v}`)
          : [],
    )
  }

  return []
}

export async function registerTenantRequest(
  payload: RegisterTenantPayload,
): Promise<RegisterTenantOutcome> {
  try {
    const res = await apiFetch('/api/tenants/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        tenantName: payload.tenantName,
        adminName: payload.adminName,
        adminEmail: payload.adminEmail,
        adminPassword: payload.adminPassword,
        confirmPassword: payload.confirmPassword,
      }),
      skipAuthRedirect: true,
    })

    if (res.status === 201) {
      const data = (await res.json()) as RegisterTenantResponseBody
      if (
        typeof data.tenantId !== 'string' ||
        typeof data.tenantSlug !== 'string' ||
        typeof data.adminUserId !== 'string'
      ) {
        return { ok: false, reason: 'network' }
      }
      return {
        ok: true,
        tenantId: data.tenantId,
        tenantSlug: data.tenantSlug,
        adminUserId: data.adminUserId,
      }
    }

    if (res.status === 409) {
      const data = (await res.json()) as ProblemBody
      const msg = typeof data.message === 'string' ? data.message : ''
      if (msg.includes('identificador')) {
        return { ok: false, reason: 'tenant-exists' }
      }
      return { ok: false, reason: 'email-exists', messages: [msg].filter(Boolean) }
    }

    if (res.status === 400) {
      const data = (await res.json()) as ProblemBody
      const errs = parseErrors(data)
      if (errs.some((e) => e.toLowerCase().includes('senha') || e.toLowerCase().includes('password'))) {
        return { ok: false, reason: 'invalid-password', messages: errs.length ? errs : undefined }
      }
      return { ok: false, reason: 'validation', messages: errs.length ? errs : undefined }
    }

    return { ok: false, reason: 'network' }
  } catch {
    return { ok: false, reason: 'network' }
  }
}
