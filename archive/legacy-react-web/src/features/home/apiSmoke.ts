import { apiFetch } from '../../shared/api/http'

export type SmokeResult =
  | { state: 'loading' }
  | { state: 'ok'; message: string }
  | { state: 'error'; message: string }

type LoginResponse = {
  token?: string
}

/**
 * Smoke integration: tries GET /api/health; if 401, optionally POST /api/auth/login
 * using VITE_SMOKE_LOGIN_* env vars, then retries health with Bearer token.
 */
export async function runApiSmoke(): Promise<Exclude<SmokeResult, { state: 'loading' }>> {
  try {
    const healthRes = await apiFetch('/api/health', {
      headers: { Accept: 'application/json' },
    })

    if (healthRes.ok) {
      const body: unknown = await healthRes.json().catch(() => null)
      return {
        state: 'ok',
        message: `GET /api/health OK: ${JSON.stringify(body)}`,
      }
    }

    if (healthRes.status === 401 || healthRes.status === 403) {
      const email = import.meta.env.VITE_SMOKE_LOGIN_EMAIL
      const password = import.meta.env.VITE_SMOKE_LOGIN_PASSWORD
      if (!email || !password) {
        return {
          state: 'ok',
          message:
            'API respondeu (sem token em /api/health). Para validar login + health, defina VITE_SMOKE_LOGIN_EMAIL e VITE_SMOKE_LOGIN_PASSWORD (veja src/Web/.env.example).',
        }
      }

      const loginRes = await apiFetch('/api/auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
        },
        body: JSON.stringify({ email, password }),
      })

      if (!loginRes.ok) {
        return {
          state: 'error',
          message: `POST /api/auth/login falhou: HTTP ${loginRes.status}`,
        }
      }

      const data = (await loginRes.json()) as LoginResponse
      if (!data.token) {
        return {
          state: 'error',
          message: 'Login retornou 200 mas sem campo token.',
        }
      }

      const authed = await apiFetch('/api/health', {
        headers: {
          Accept: 'application/json',
          Authorization: `Bearer ${data.token}`,
        },
      })

      if (!authed.ok) {
        return {
          state: 'error',
          message: `GET /api/health com Bearer falhou: HTTP ${authed.status}`,
        }
      }

      const body: unknown = await authed.json().catch(() => null)
      return {
        state: 'ok',
        message: `API OK (login + health): ${JSON.stringify(body)}`,
      }
    }

    return {
      state: 'error',
      message: `GET /api/health inesperado: HTTP ${healthRes.status}`,
    }
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e)
    return {
      state: 'error',
      message: `Falha de rede ou proxy: ${msg}. Suba a API (dotnet run) e use proxy em dev ou configure VITE_API_BASE_URL + CORS.`,
    }
  }
}
