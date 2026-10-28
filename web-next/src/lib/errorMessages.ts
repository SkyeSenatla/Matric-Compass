import { ApiError } from '@/api/errors'

// Our README's error table, as words a person can act on. A pure function,
// so every status is tested without rendering anything.
export function describeError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    // fetch itself failed: the API is down, or the network is.
    return "We couldn't reach the server. Check your connection and try again."
  }
  switch (error.status) {
    case 401:
      return 'Your session has ended. Please sign in again.'
    case 403:
      return "You don't have access to this."
    case 404:
      return "We couldn't find that."
    case 409:
    case 422:
      return error.detail ?? 'That conflicts with something that already exists.'
    case 429:
      return error.retryAfterSeconds !== null
        ? `Too many attempts. Try again in ${error.retryAfterSeconds} seconds.`
        : 'Too many attempts. Please wait a moment and try again.'
    default:
      return error.status >= 500
        ? 'Something went wrong on our side. If it keeps happening, quote the reference below.'
        : (error.detail ?? 'Something went wrong. Please try again.')
  }
}

// Sign-in has its own wording for a 401: the same message whether the email
// or the password was wrong, so nobody can use the form to find accounts.
export function describeLoginError(error: unknown): string {
  if (error instanceof ApiError && error.status === 401) {
    return "That email and password don't match."
  }
  return describeError(error)
}

export function isStaff(role: string): boolean {
  return role === 'Counsellor' || role === 'Admin'
}
