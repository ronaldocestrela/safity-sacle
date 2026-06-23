import { afterEach, describe, expect, it } from 'vitest'
import { AUTH_SESSION_STORAGE_KEY, clearSession, loadSession, saveSessionToken } from './session'
import { expSoon, makeUnsignedJwt } from '../../test/jwtTestUtils'

describe('session storage', () => {
  afterEach(() => {
    sessionStorage.clear()
  })

  it('returns null when empty', () => {
    expect(loadSession()).toBeNull()
  })

  it('round-trips valid token', () => {
    const token = makeUnsignedJwt({
      exp: expSoon(),
      email: 'a@b.com',
      role: 'Admin',
    })
    const session = saveSessionToken(token)
    expect(session?.email).toBe('a@b.com')
    expect(session?.roles).toEqual(['Admin'])
    expect(session?.tenantId).toBe('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa')
    expect(loadSession()?.token).toBe(token)
  })

  it('clears expired token on load', () => {
    const token = makeUnsignedJwt({
      exp: Math.floor(Date.now() / 1000) - 60,
      role: 'Admin',
    })
    sessionStorage.setItem(AUTH_SESSION_STORAGE_KEY, JSON.stringify({ token }))
    expect(loadSession()).toBeNull()
    expect(sessionStorage.getItem(AUTH_SESSION_STORAGE_KEY)).toBeNull()
  })

  it('clearSession removes key', () => {
    sessionStorage.setItem(AUTH_SESSION_STORAGE_KEY, JSON.stringify({ token: 'x.y.z' }))
    clearSession()
    expect(sessionStorage.getItem(AUTH_SESSION_STORAGE_KEY)).toBeNull()
  })
})
