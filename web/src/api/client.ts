import createClient from 'openapi-fetch'
import type { paths } from './schema'
import { ApiError } from './errors'
import { authFetch } from '@/auth/session'

// The typed client. `paths` comes from schema.d.ts, so every path, parameter
// and response below is checked by TypeScript against the real API.
export const api = createClient<paths>({
  baseUrl: import.meta.env.VITE_API_BASE_URL,
  // Every request goes through authFetch: it adds the token and handles a 401.
  fetch: authFetch,
})

// openapi-fetch never throws: it returns { data } or { error }. TanStack
// Query needs a thrown error to know a query failed, so this turns the
// error half into an ApiError carrying the status and correlationId.
export function unwrap<T>(result: { data?: T; error?: unknown; response: Response }): T {
  if (result.response.ok && result.data !== undefined) return result.data
  throw ApiError.from(result.response, result.error)
}
