import 'server-only'
import { decodeJwt } from 'jose'
import type { Session } from '@/lib/session'

// Refresh when less than this is left on the 5-minute access token. The
// race demo raises it (REFRESH_MARGIN_SECONDS=295) so that every request
// refreshes; leave it unset otherwise.
const MARGIN_SECONDS = Number(process.env.REFRESH_MARGIN_SECONDS ?? 60)

export function needsRefresh(session: Session): boolean {
  // Decoded, not verified: exp only decides WHEN we refresh. Nothing is
  // allowed or refused because of it. The API still verifies every token.
  const { exp } = decodeJwt(session.accessToken)
  return exp === undefined || exp - Date.now() / 1000 < MARGIN_SECONDS
}

// One call to the API's refresh endpoint, server to server. Returns the new
// session, or null when the API says no: the refresh token has expired, was
// revoked, or was already used.
export async function refreshSession(session: Session): Promise<Session | null> {
  if (session.refreshToken === null) return null
  console.log('Refreshing the access token')

  const response = await fetch(`${process.env.API_BASE_URL}/api/v1/auth/refresh`, {
    method: 'POST',
    headers: { Cookie: `refresh_token=${session.refreshToken}` },
  })
  if (!response.ok) return null

  const body = (await response.json()) as { accessToken: string }
  // Rotation: the API spent the old refresh token and sent its successor
  // in a Set-Cookie, on its response to us. Same as the login on Day 1.
  const refreshToken =
    response.headers
      .getSetCookie()
      .find((cookie) => cookie.startsWith('refresh_token='))
      ?.split(';')[0]
      .slice('refresh_token='.length) ?? null
  return { ...session, accessToken: body.accessToken, refreshToken }
}

// The fix for the rotation race. Requests that carry the SAME refresh token
// (three tabs, a page full of prefetching links) share ONE call to the API
// and all get the same new session. A request that arrives a few seconds
// late, still carrying the old cookie, gets that result too, instead of
// spending a token that's already been spent.
//
// It lives in this server's memory: right for one Next.js server, like
// ours. With several servers behind a load balancer they can't see each
// other's memory, and the API's grace window (or a shared store) takes over.
const SHARE_FOR_MS = 10_000
const recent = new Map<string, { result: Promise<Session | null>; startedAt: number }>()

export function refreshOnce(session: Session): Promise<Session | null> {
  const now = Date.now()
  for (const [token, entry] of recent) {
    if (now - entry.startedAt > SHARE_FOR_MS) recent.delete(token)
  }

  const token = session.refreshToken ?? ''
  const shared = recent.get(token)
  if (shared !== undefined) return shared.result

  const result = refreshSession(session)
  recent.set(token, { result, startedAt: now })
  return result
}
