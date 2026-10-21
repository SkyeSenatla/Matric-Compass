import { describe, expect, it } from 'vitest'
import { ApiError } from '@/api/errors'
import { describeError, describeLoginError, isStaff } from './errorMessages'

const apiError = (status: number, body: object = {}, retryAfter: number | null = null) =>
  new ApiError(status, body, retryAfter)

describe('describeError', () => {
  it('maps each status in our error table to words a person can act on', () => {
    expect(describeError(apiError(403))).toBe("You don't have access to this.")
    expect(describeError(apiError(404))).toBe("We couldn't find that.")
    expect(describeError(apiError(409, { detail: 'An active NSFAS application already exists.' }))).toBe(
      'An active NSFAS application already exists.',
    )
    expect(describeError(apiError(500))).toMatch(/quote the reference/)
  })

  it('says how long to wait on a 429', () => {
    expect(describeError(apiError(429, {}, 42))).toBe('Too many attempts. Try again in 42 seconds.')
  })

  it('treats anything that is not an ApiError as a network failure', () => {
    expect(describeError(new TypeError('Failed to fetch'))).toMatch(/couldn't reach the server/)
  })
})

describe('describeLoginError', () => {
  it('never says whether the email or the password was wrong', () => {
    expect(describeLoginError(apiError(401))).toBe("That email and password don't match.")
  })
})

describe('isStaff', () => {
  it('is true for counsellors and admins only', () => {
    expect(isStaff('Counsellor')).toBe(true)
    expect(isStaff('Admin')).toBe(true)
    expect(isStaff('Learner')).toBe(false)
    expect(isStaff('counsellor')).toBe(false) // the API sends exact names
  })
})
