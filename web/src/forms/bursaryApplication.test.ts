import { describe, expect, it } from 'vitest'
import { bursaryApplicationSchema, toCreateRequest } from './bursaryApplication'

const valid = {
  studentId: '0b9c6c2e-9a0e-4a43-8f43-3f0f1b0e2c11',
  funder: '  Sasol  ',
  amount: '1 500,50',
  deadline: '2026-11-30',
}

describe('bursaryApplicationSchema', () => {
  it('turns what a person typed into a request in cents', () => {
    const values = bursaryApplicationSchema.parse(valid)

    expect(toCreateRequest(values)).toEqual({
      studentId: valid.studentId,
      funder: 'Sasol', // trimmed
      amountInCents: 150_050,
      deadline: '2026-11-30', // sent exactly as picked
      requiredDocuments: [],
    })
  })

  it('reports every empty field with a message a person can act on', () => {
    const result = bursaryApplicationSchema.safeParse({ studentId: '', funder: '   ', amount: '', deadline: '' })

    expect(result.success).toBe(false)
    const messages = Object.fromEntries(result.error!.issues.map((issue) => [issue.path[0], issue.message]))
    expect(messages).toEqual({
      studentId: 'Choose a student.',
      funder: "Enter the funder's name.",
      amount: 'Enter an amount in rands, like 1500 or 1 500,50.',
      deadline: 'Choose a deadline.',
    })
  })

  it('refuses R0, with its own message', () => {
    const result = bursaryApplicationSchema.safeParse({ ...valid, amount: '0,00' })

    expect(result.error?.issues[0].message).toBe('Enter an amount greater than R0.')
  })

  it('refuses an amount it would have to round', () => {
    expect(bursaryApplicationSchema.safeParse({ ...valid, amount: '19.999' }).success).toBe(false)
  })
})
