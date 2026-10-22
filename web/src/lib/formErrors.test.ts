import { describe, expect, it } from 'vitest'
import { ApiError } from '@/api/errors'
import { placeServerErrors } from './formErrors'

const fieldMap = { funder: 'funder', amountInCents: 'amount' } as const

describe('placeServerErrors', () => {
  it('puts each message on its field, renaming the API key where the form differs', () => {
    const error = new ApiError(400, {
      errors: {
        funder: ['Funder names are 100 characters or fewer.'],
        amountInCents: ['Enter an amount greater than R0.'],
      },
    }, null)

    expect(placeServerErrors(error, fieldMap)).toEqual([
      { field: 'funder', message: 'Funder names are 100 characters or fewer.' },
      { field: 'amount', message: 'Enter an amount greater than R0.' },
    ])
  })

  it('never drops a message: an unknown key goes to the whole form', () => {
    const error = new ApiError(400, { errors: { requiredDocuments: ["'Required Documents' must not be empty."] } }, null)

    expect(placeServerErrors(error, fieldMap)).toEqual([
      { field: null, message: "'Required Documents' must not be empty." },
    ])
  })

  it('does not treat an inherited property name as a field', () => {
    const error = new ApiError(400, { errors: { toString: ['Odd, but possible.'] } }, null)

    expect(placeServerErrors(error, fieldMap)[0].field).toBeNull()
  })

  it('joins several messages for one field into one line', () => {
    const error = new ApiError(400, { errors: { funder: ['First.', 'Second.'] } }, null)

    expect(placeServerErrors(error, fieldMap)[0].message).toBe('First. Second.')
  })
})
