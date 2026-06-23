import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { loginRequest } from '../../features/auth/loginApi'
import { setOnUnauthorized } from '../api/http'
import type { AuthSession } from './types'
import { AuthContext } from './authContext'
import { clearSession, loadSession, saveSessionToken } from './session'

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate()
  const [session, setSession] = useState<AuthSession | null>(() => loadSession())

  const logout = useCallback(() => {
    clearSession()
    setSession(null)
    navigate('/login', { replace: true })
  }, [navigate])

  useEffect(() => {
    setOnUnauthorized(() => {
      clearSession()
      setSession(null)
      navigate('/login', { replace: true, state: { reason: 'session-expired' as const } })
    })
    return () => setOnUnauthorized(undefined)
  }, [navigate])

  const login = useCallback(async (email: string, password: string) => {
    const result = await loginRequest(email.trim(), password)
    if ('error' in result) {
      return { ok: false as const, reason: result.error }
    }
    const next = saveSessionToken(result.token)
    if (!next) {
      return { ok: false as const, reason: 'network' as const }
    }
    setSession(next)
    return { ok: true as const }
  }, [])

  const value = useMemo(
    () => ({
      session,
      login,
      logout,
    }),
    [session, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
