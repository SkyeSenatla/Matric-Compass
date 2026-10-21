// The ONLY module that touches tokens.
//
// - The access token lives in this module's memory. Never localStorage:
//   anything stored there can be read by any script that gets onto the page.
// - The refresh token is an HttpOnly cookie. This code can't read it; the
//   browser sends it to /api/v1/auth/* when we ask with credentials: 'include'.
// - Refresh is SINGLE-FLIGHT: however many requests discover an expired
//   token at once, only one /auth/refresh is sent, and they all share it.
//   Our API rotates refresh tokens and treats a reused one as theft, so
//   two refreshes at once would sign the user out everywhere.

import { ApiError } from '@/api/errors'

const API = import.meta.env.VITE_API_BASE_URL

let accessToken: string | null = null
let refreshing: Promise<string | null> | null = null
const signedOutListeners = new Set<() => void>()

// For the demo panel and the tests: how many refreshes actually went out.
export const authStats = { refreshCalls: 0 }

export function getAccessToken(): string | null {
  return accessToken
}

export async function login(email: string, password: string): Promise<void> {
  const response = await fetch(`${API}/api/v1/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
    // The response sets the refresh cookie. Cross-origin, the browser only
    // stores it when we send credentials.
    credentials: 'include',
  })
  const body: unknown = await response.json().catch(() => null)
  if (!response.ok) throw ApiError.from(response, body)
  accessToken = (body as { accessToken: string }).accessToken
}

export async function logout(): Promise<void> {
  accessToken = null
  await fetch(`${API}/api/v1/auth/logout`, { method: 'POST', credentials: 'include' }).catch(() => {})
  notifySignedOut()
}

// Single-flight: the first caller starts the refresh; everyone who asks
// while it's running gets the SAME promise. `finally` clears it whether it
// succeeded or failed, so the next expiry starts a fresh one.
export function refreshAccessToken(): Promise<string | null> {
  refreshing ??= doRefresh().finally(() => {
    refreshing = null
  })
  return refreshing
}

async function doRefresh(): Promise<string | null> {
  authStats.refreshCalls++
  const response = await fetch(`${API}/api/v1/auth/refresh`, {
    method: 'POST',
    credentials: 'include',
  }).catch(() => null)

  if (!response?.ok) {
    accessToken = null
    return null
  }
  accessToken = ((await response.json()) as { accessToken: string }).accessToken
  return accessToken
}

// Every API call goes through here (see src/api/client.ts).
export async function authFetch(request: Request): Promise<Response> {
  // A request body can be read only once, so keep a copy for the retry.
  const retry = request.clone()

  // After a page reload memory is empty: refresh first, using the cookie.
  // On a fresh visit with no cookie this returns null and we send no token.
  const token = accessToken ?? (await refreshAccessToken())

  const response = await fetch(withToken(request, token))

  // Not a 401, or we had no session at all (nothing to refresh): done.
  if (response.status !== 401 || token === null) return response

  // 401: our token has expired. If another request has ALREADY refreshed
  // while we were waiting, use its token instead of refreshing again.
  const fresh = accessToken !== token ? accessToken : await refreshAccessToken()
  if (fresh === null) {
    notifySignedOut()
    return response
  }

  // Retry once. A second 401 is returned as it is: no loops.
  return fetch(withToken(retry, fresh))
}

function withToken(request: Request, token: string | null): Request {
  if (token === null) return request
  const headers = new Headers(request.headers)
  headers.set('Authorization', `Bearer ${token}`)
  return new Request(request, { headers })
}

export function onSignedOut(listener: () => void): () => void {
  signedOutListeners.add(listener)
  return () => signedOutListeners.delete(listener)
}

function notifySignedOut() {
  for (const listener of signedOutListeners) listener()
}

// Demo only: replace the access token with one the API will reject, as if
// it had expired. The next requests all get a 401 at the same moment.
export function expireAccessTokenForDemo(): void {
  accessToken = 'expired-for-the-demo'
}
