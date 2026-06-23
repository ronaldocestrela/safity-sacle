import { describe, expect, it } from 'vitest'
import { buildApiUrl, normalizeApiBase } from './env'

describe('normalizeApiBase', () => {
  it('returns empty for undefined or blank', () => {
    expect(normalizeApiBase(undefined)).toBe('')
    expect(normalizeApiBase('')).toBe('')
    expect(normalizeApiBase('  ')).toBe('')
  })

  it('trims and strips trailing slash', () => {
    expect(normalizeApiBase(' https://x.com/ ')).toBe('https://x.com')
  })
})

describe('buildApiUrl', () => {
  it('uses relative path when base empty', () => {
    expect(buildApiUrl('', 'api/health')).toBe('/api/health')
    expect(buildApiUrl('', '/api/health')).toBe('/api/health')
  })

  it('joins base and path', () => {
    expect(buildApiUrl('https://localhost:7104', '/api/health')).toBe(
      'https://localhost:7104/api/health',
    )
  })
})
