/** UTF-8 JSON → base64url (works in browser + Vitest). */
function toBase64Url(json: string): string {
  const bytes = new TextEncoder().encode(json)
  let binary = ''
  for (const b of bytes) binary += String.fromCharCode(b)
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

/** Matches backend migration default tenant for local/dev JWT stubs. */
const DEFAULT_TEST_TENANT_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'

/** Minimal unsigned JWT for tests (payload only verified client-side). */
export function makeUnsignedJwt(payload: Record<string, unknown>): string {
  const merged = { tenant_id: DEFAULT_TEST_TENANT_ID, ...payload }
  const header = toBase64Url(JSON.stringify({ alg: 'none', typ: 'JWT' }))
  const body = toBase64Url(JSON.stringify(merged))
  return `${header}.${body}.sig`
}

export function expSoon(secondsFromNow = 3600): number {
  return Math.floor(Date.now() / 1000) + secondsFromNow
}
