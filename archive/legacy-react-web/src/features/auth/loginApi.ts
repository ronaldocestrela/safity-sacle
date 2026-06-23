import { apiFetch } from '../../shared/api/http'

export type LoginResult =
  | { token: string }
  | { error: 'invalid' | 'network' }

export async function loginRequest(email: string, password: string): Promise<LoginResult> {
  try {
    const res = await apiFetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
      skipAuthRedirect: true,
    })
    if (res.status === 401) {
      return { error: 'invalid' }
    }
    if (!res.ok) {
      return { error: 'network' }
    }
    const data = (await res.json()) as { token?: string }
    if (!data.token) {
      return { error: 'network' }
    }
    return { token: data.token }
  } catch {
    return { error: 'network' }
  }
}
