import { describe, expect, it } from 'vitest'
import { ApiError, parseRetryAfter } from './errors'

describe('parseRetryAfter', () => {
  const now = Date.parse('2026-10-21T10:00:00Z')

  it('reads a number of seconds', () => {
    expect(parseRetryAfter('42', now)).toBe(42)
  })

  it('reads an HTTP date as the seconds until then', () => {
    expect(parseRetryAfter('Wed, 21 Oct 2026 10:00:30 GMT', now)).toBe(30)
  })

  it('never returns a negative wait for a date in the past', () => {
    expect(parseRetryAfter('Wed, 21 Oct 2026 09:59:00 GMT', now)).toBe(0)
  })

  it('returns null for a missing or unreadable header', () => {
    expect(parseRetryAfter(null, now)).toBeNull()
    expect(parseRetryAfter('soon', now)).toBeNull()
  })
})

describe('ApiError.from', () => {
  it('keeps the status, detail, correlationId and Retry-After', () => {
    const response = new Response(null, { status: 429, headers: { 'Retry-After': '17' } })
    const error = ApiError.from(response, {
      title: 'Too many requests',
      detail: 'Try again later.',
      correlationId: '4bf92f3577b34da6a3ce929d0e0e4736',
    })

    expect(error.status).toBe(429)
    expect(error.detail).toBe('Try again later.')
    expect(error.correlationId).toBe('4bf92f3577b34da6a3ce929d0e0e4736')
    expect(error.retryAfterSeconds).toBe(17)
  })

  it('copes with a response that has no body', () => {
    const error = ApiError.from(new Response(null, { status: 500 }), undefined)

    expect(error.status).toBe(500)
    expect(error.correlationId).toBeNull()
  })
})
