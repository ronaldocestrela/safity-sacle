import { createContext } from 'react'
import type { AuthSession } from './types'

export type AuthContextValue = {
  session: AuthSession | null
  login: (
    email: string,
    password: string,
  ) => Promise<{ ok: true } | { ok: false; reason: 'invalid' | 'network' }>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
