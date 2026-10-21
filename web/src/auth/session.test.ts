import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

// A fake API. The access token "good" is accepted; anything else gets a 401.
// /auth/refresh hands out "good" after a short delay, so parallel callers
// really do overlap, as they would in a browser.
function fakeApi({ refreshSucceeds }: { refreshSucceeds: boolean }) {
  const calls = { refresh: 0, protected: 0 }
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const request = input instanceof Request ? input : new Request(input, init)
    if (request.url.endsWith('/api/v1/auth/refresh')) {
      calls.refresh++
      await new Promise((resolve) => setTimeout(resolve, 20))
      return refreshSucceeds
        ? Response.json({ accessToken: 'good', expiresAt: '2026-10-21T10:05:00Z' })
        : new Response(null, { status: 401 })
    }
    calls.protected++
    const ok = request.headers.get('Authorization') === 'Bearer good'
    return ok ? Response.json({ ok: true }) : new Response(null, { status: 401 })
  })
  return { calls, fetchMock }
}

const url = 'http://localhost:5146/api/v1/me'

describe('authFetch', () => {
  beforeEach(() => {
    vi.resetModules() // a fresh session module (no token) for every test
    vi.stubEnv('VITE_API_BASE_URL', 'http://localhost:5146')
  })
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.unstubAllEnvs()
  })

  it('sends exactly ONE refresh when five requests get a 401 at once', async () => {
    const { calls, fetchMock } = fakeApi({ refreshSucceeds: true })
    vi.stubGlobal('fetch', fetchMock)
    const session = await import('./session')
    session.expireAccessTokenForDemo()

    const responses = await Promise.all(
      Array.from({ length: 5 }, () => session.authFetch(new Request(url))),
    )

    expect(calls.refresh).toBe(1)
    expect(responses.map((r) => r.status)).toEqual([200, 200, 200, 200, 200])
  })

  it('after a reload (no token in memory), refreshes once and sends no failed requests', async () => {
    const { calls, fetchMock } = fakeApi({ refreshSucceeds: true })
    vi.stubGlobal('fetch', fetchMock)
    const session = await import('./session')

    const responses = await Promise.all(
      Array.from({ length: 3 }, () => session.authFetch(new Request(url))),
    )

    expect(calls.refresh).toBe(1)
    expect(calls.protected).toBe(3) // no 401 round trips
    expect(responses.every((r) => r.status === 200)).toBe(true)
  })

  it('when the refresh fails, returns the 401 once and announces the sign-out', async () => {
    const { calls, fetchMock } = fakeApi({ refreshSucceeds: false })
    vi.stubGlobal('fetch', fetchMock)
    const session = await import('./session')
    const signedOut = vi.fn()
    session.onSignedOut(signedOut)
    session.expireAccessTokenForDemo()

    const response = await session.authFetch(new Request(url))

    expect(response.status).toBe(401)
    expect(calls.refresh).toBe(1)
    expect(signedOut).toHaveBeenCalled()
  })

  it('with no session at all, does not loop: one refresh attempt, then the 401', async () => {
    const { calls, fetchMock } = fakeApi({ refreshSucceeds: false })
    vi.stubGlobal('fetch', fetchMock)
    const session = await import('./session')
    const signedOut = vi.fn()
    session.onSignedOut(signedOut)

    const response = await session.authFetch(new Request(url))

    expect(response.status).toBe(401)
    expect(calls.refresh).toBe(1)
    expect(signedOut).not.toHaveBeenCalled() // nobody was signed in
  })

  it('retries a POST with its body intact', async () => {
    const { fetchMock } = fakeApi({ refreshSucceeds: true })
    const bodies: string[] = []
    vi.stubGlobal('fetch', async (input: RequestInfo | URL, init?: RequestInit) => {
      const request = input instanceof Request ? input : new Request(input, init)
      if (!request.url.endsWith('/refresh')) bodies.push(await request.clone().text())
      return fetchMock(request)
    })
    const session = await import('./session')
    session.expireAccessTokenForDemo()

    await session.authFetch(new Request(url, { method: 'POST', body: '{"funder":"NSFAS"}' }))

    expect(bodies).toEqual(['{"funder":"NSFAS"}', '{"funder":"NSFAS"}'])
  })
})
