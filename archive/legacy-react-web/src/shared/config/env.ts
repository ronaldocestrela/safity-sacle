/** Exported for tests — keeps URL rules pure. */
export function normalizeApiBase(raw: string | undefined): string {
  const t = raw?.trim()
  if (!t) return ''
  return t.replace(/\/$/, '')
}

/** Exported for tests — builds absolute or same-origin path. */
export function buildApiUrl(base: string, path: string): string {
  const p = path.startsWith('/') ? path : `/${path}`
  return base ? `${base}${p}` : p
}

export function getApiBaseUrl(): string {
  return normalizeApiBase(import.meta.env.VITE_API_BASE_URL)
}

export function apiUrl(path: string): string {
  return buildApiUrl(getApiBaseUrl(), path)
}
