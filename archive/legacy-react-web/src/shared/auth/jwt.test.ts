import { describe, expect, it } from 'vitest'
import {
  collectRoleClaims,
  emailFromPayload,
  filterAppRoles,
  isJwtExpired,
  parseJwtPayload,
  tenantIdFromPayload,
} from './jwt'
import { expSoon, makeUnsignedJwt } from '../../test/jwtTestUtils'

describe('jwt helpers', () => {
  it('parses payload from token', () => {
    const token = makeUnsignedJwt({ sub: 'u1', email: 'x@y.com', exp: expSoon() })
    const payload = parseJwtPayload(token)
    expect(payload?.sub).toBe('u1')
    expect(emailFromPayload(payload!)).toBe('x@y.com')
  })

  it('collects roles from short and MS claim keys', () => {
    const p1 = {
      role: 'Admin',
    }
    expect(filterAppRoles(collectRoleClaims(p1))).toEqual(['Admin'])

    const p2 = {
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': ['Supervisor', 'Extra'],
    }
    const roles = filterAppRoles(collectRoleClaims(p2))
    expect(roles).toEqual(['Supervisor'])
  })

  it('detects expiration', () => {
    expect(isJwtExpired({ exp: Math.floor(Date.now() / 1000) - 10 })).toBe(true)
    expect(isJwtExpired({ exp: expSoon() })).toBe(false)
  })

  it('extracts tenant id claim', () => {
    const tid = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
    const token = makeUnsignedJwt({ tenant_id: tid, exp: expSoon() })
    const payload = parseJwtPayload(token)
    expect(tenantIdFromPayload(payload!)).toBe(tid)
  })
})
