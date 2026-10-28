import 'server-only'
import createClient from 'openapi-fetch'
import { notFound, redirect } from 'next/navigation'
import type { paths } from '@/api/schema'
import { ApiError } from '@/api/errors'
import { getSession, type Session } from '@/lib/session'

// Server-only: the browser never calls the API any more. No CORS, no token
// in the browser, and the API's address stays private.
const baseUrl = process.env.API_BASE_URL

// For the login itself, before anyone has a token.
export function publicApi() {
  return createClient<paths>({ baseUrl })
}

// For everything else: the signed-in user's client, with their token on
// every request. No session means no request: straight to the sign-in page.
export async function signedInApi(): Promise<{ api: ReturnType<typeof publicApi>; session: Session }> {
  const session = await getSession()
  if (session === null) redirect('/login')
  const api = createClient<paths>({ baseUrl, headers: { Authorization: `Bearer ${session.accessToken}` } })
  return { api, session }
}

type Result<T> = { data?: T; error?: unknown; response: Response }

// What a page does with an API answer:
// - data: return it;
// - 401: the access token has expired (5 minutes, no refresh until Day 3),
//   so sign in again;
// - 404: "doesn't exist" and "not yours" look the same, so not-found.tsx;
// - anything else: an ApiError the page can show, with its correlationId.
export function readOrFail<T>(result: Result<T>): T | ApiError {
  if (result.response.ok && result.data !== undefined) return result.data
  if (result.response.status === 401) redirect('/login?expired=1')
  if (result.response.status === 404) notFound()
  return ApiError.from(result.response, result.error)
}
