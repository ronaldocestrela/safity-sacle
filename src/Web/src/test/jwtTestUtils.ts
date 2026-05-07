/** UTF-8 JSON → base64url (works in browser + Vitest). */
function toBase64Url(json: string): string {
  const bytes = new TextEncoder().encode(json)
  let binary = ''
  for (const b of bytes) binary += String.fromCharCode(b)
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

/** Minimal unsigned JWT for tests (payload only verified client-side). */
export function makeUnsignedJwt(payload: Record<string, unknown>): string {
  const header = toBase64Url(JSON.stringify({ alg: 'none', typ: 'JWT' }))
  const body = toBase64Url(JSON.stringify(payload))
  return `${header}.${body}.sig`
}

export function expSoon(secondsFromNow = 3600): number {
  return Math.floor(Date.now() / 1000) + secondsFromNow
}
