/** Best-effort message from ASP.NET ProblemDetails-ish or FluentValidation payloads. */
export async function readApiErrorMessage(response: Response): Promise<string | null> {
  const ct = response.headers.get('Content-Type') ?? ''

  if (ct.includes('application/json')) {
    const body = await response.json().catch(() => null)
    if (body == null) {
      return null
    }

    const parsed =
      typeof body === 'object' &&
      body !== null &&
      'errors' in body &&
      Array.isArray((body as { errors?: unknown }).errors)
        ? (body as { errors?: unknown })
        : null

    if (parsed?.errors && Array.isArray(parsed.errors) && parsed.errors.length > 0) {
      return parsed.errors.map(String).join(' ')
    }

    const detail = (body as { detail?: unknown }).detail
    if (typeof detail === 'string' && detail.trim()) {
      return detail
    }

    const title = (body as { title?: unknown }).title
    if (typeof title === 'string' && title.trim()) {
      return title
    }

    const message = (body as { message?: unknown }).message
    if (typeof message === 'string' && message.trim()) {
      return message
    }

    return null
  }

  const text = await response.text()
  const trimmed = text.trim()
  if (!trimmed) {
    return null
  }

  try {
    const body = JSON.parse(trimmed) as {
      detail?: unknown
      title?: unknown
      errors?: unknown
      message?: unknown
    }
    if (Array.isArray(body.errors) && body.errors.length > 0) {
      return body.errors.map(String).join(' ')
    }
    if (typeof body.detail === 'string' && body.detail.trim()) {
      return body.detail
    }
    if (typeof body.title === 'string' && body.title.trim()) {
      return body.title
    }
    if (typeof body.message === 'string' && body.message.trim()) {
      return body.message
    }
  } catch {
    /* plain text body */
  }
  return trimmed
}
