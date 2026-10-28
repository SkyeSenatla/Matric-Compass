// Every error from our API is application/problem+json (RFC 9457) with two
// extensions our OpenAPI document doesn't describe: correlationId and, for
// a 400, an errors map. So we type those here, once.
type ProblemBody = {
  title?: string | null
  detail?: string | null
  status?: number | null
  correlationId?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly detail: string | null
  readonly correlationId: string | null
  readonly fieldErrors: Record<string, string[]>
  readonly retryAfterSeconds: number | null

  constructor(status: number, body: ProblemBody, retryAfterSeconds: number | null) {
    super(body.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.detail = body.detail ?? null
    this.correlationId = body.correlationId ?? null
    this.fieldErrors = body.errors ?? {}
    this.retryAfterSeconds = retryAfterSeconds
  }

  // `body` is what openapi-fetch already parsed; it may be missing (a
  // network-level error page, an empty 401) or not an object at all.
  static from(response: Response, body: unknown): ApiError {
    const problem = typeof body === 'object' && body !== null ? (body as ProblemBody) : {}
    return new ApiError(
      response.status,
      problem,
      parseRetryAfter(response.headers.get('Retry-After'), Date.now()),
    )
  }
}

// Retry-After is either a number of seconds ("30") or an HTTP date
// ("Wed, 21 Oct 2026 07:28:00 GMT"). Returns whole seconds to wait, or null.
export function parseRetryAfter(value: string | null, nowMs: number): number | null {
  if (value === null || value.trim() === '') return null
  if (/^\d+$/.test(value.trim())) return Number(value.trim())
  const dateMs = Date.parse(value)
  if (Number.isNaN(dateMs)) return null
  return Math.max(0, Math.ceil((dateMs - nowMs) / 1000))
}
