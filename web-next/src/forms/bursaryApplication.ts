import { z } from 'zod'
import type { BursaryApplicationCreateRequest } from '@/api/types'
import { parseRandsToCents } from '@/lib/money'

// What a PERSON may type into the bursary form. This is not the API's
// request: people type rands as text, the API wants integer cents. The
// schema checks the text and, on the way out, turns it into cents.
//
// These rules MIRROR the API's simple ones so feedback is instant. The API
// still checks everything again: anyone can skip this code.
export const bursaryApplicationSchema = z.object({
  studentId: z.string().min(1, 'Choose a student.'),
  funder: z.string().trim().min(1, "Enter the funder's name."),
  amount: z.string().transform((value, context) => {
    const cents = parseRandsToCents(value)
    if (cents === null) {
      context.addIssue({ code: 'custom', message: 'Enter an amount in rands, like 1500 or 1 500,50.' })
      return z.NEVER
    }
    if (cents === 0) {
      context.addIssue({ code: 'custom', message: 'Enter an amount greater than R0.' })
      return z.NEVER
    }
    return cents
  }),
  // <input type="date"> gives "2026-11-30": a date, no time, no time zone.
  // We send it exactly as picked. Whether it is "in the past" is the API's
  // call (a 422), so the browser's clock and time zone never decide it.
  deadline: z.iso.date('Choose a deadline.'),
})

// Two types from one schema: what the inputs hold (all strings) and what
// comes out after validation (amount is now a whole number of cents).
export type BursaryApplicationFormInput = z.input<typeof bursaryApplicationSchema>
export type BursaryApplicationFormValues = z.output<typeof bursaryApplicationSchema>

// The form's values are not the request body. This is the one place they
// are turned into it, and the generated type checks the result: if the API
// renames a field, this stops compiling.
export function toCreateRequest(values: BursaryApplicationFormValues): BursaryApplicationCreateRequest {
  return {
    studentId: values.studentId,
    funder: values.funder,
    amountInCents: values.amount,
    deadline: values.deadline,
    requiredDocuments: [],
  }
}

// The API's errors map uses the REQUEST's field names (camelCase). Where a
// form field has a different name, say so here, or the message is lost.
export const apiFieldToFormField = {
  studentId: 'studentId',
  funder: 'funder',
  amountInCents: 'amount',
  deadline: 'deadline',
} as const satisfies Record<string, keyof BursaryApplicationFormInput>
