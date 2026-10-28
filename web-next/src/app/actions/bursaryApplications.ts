'use server'

import { refresh } from 'next/cache'
import { redirect } from 'next/navigation'
import { z } from 'zod'
import { ApiError } from '@/api/errors'
import { apiFieldToFormField, bursaryApplicationSchema, toCreateRequest } from '@/forms/bursaryApplication'
import { signedInApi } from '@/lib/api'
import { describeError } from '@/lib/errorMessages'
import { placeServerErrors } from '@/lib/formErrors'
import { formatCents } from '@/lib/money'

type Field = 'studentId' | 'funder' | 'amount' | 'deadline'

// What the action sends back to the form. Plain data only: it travels to
// the browser, so no ApiError instances and nothing the form won't show.
export type BursaryFormState = {
  // What the person typed, so a rejected form can show it again.
  values: Record<Field, string>
  fieldErrors: Partial<Record<Field, string>>
  message: string | null
  correlationId: string | null
  saved: string | null
}

const blank: Record<Field, string> = { studentId: '', funder: '', amount: '', deadline: '' }

// Week 7's onValid + showServerError, moved to the server. The schema, the
// field-name map and placeServerErrors are the Week 7 files, unchanged.
export async function createBursaryApplication(
  _previous: BursaryFormState,
  formData: FormData,
): Promise<BursaryFormState> {
  // 1. A Server Action is a public endpoint: no session, no request.
  const { api } = await signedInApi()

  // 2. FormData is untrusted. Same schema as Week 7, now run on the server.
  const values = {
    studentId: String(formData.get('studentId') ?? ''),
    funder: String(formData.get('funder') ?? ''),
    amount: String(formData.get('amount') ?? ''),
    deadline: String(formData.get('deadline') ?? ''),
  }
  const parsed = bursaryApplicationSchema.safeParse(values)
  if (!parsed.success) {
    const { fieldErrors } = z.flattenError(parsed.error)
    return {
      values,
      fieldErrors: {
        studentId: fieldErrors.studentId?.[0],
        funder: fieldErrors.funder?.[0],
        amount: fieldErrors.amount?.[0],
        deadline: fieldErrors.deadline?.[0],
      },
      message: null,
      correlationId: null,
      saved: null,
    }
  }

  // 3. The API checks everything again, and decides who may do this.
  const result = await api.POST('/api/v1/bursary-applications', { body: toCreateRequest(parsed.data) })
  if (result.response.status === 401) redirect('/login?expired=1')
  if (result.data === undefined) {
    const error = ApiError.from(result.response, result.error)
    return { values, ...placeError(error), correlationId: error.correlationId, saved: null }
  }

  // 4. Re-render this page in the same response, so the new card is there.
  // refresh(), not updateTag(): nothing here is cached, so there's no tag.
  refresh()
  return {
    values: blank,
    fieldErrors: {},
    message: null,
    correlationId: null,
    saved: `${result.data.funder} application for ${formatCents(result.data.amountInCents)} saved.`,
  }
}

// Week 7's error table: a 400's errors map goes next to each field, the
// 422 (deadline in the past) goes on the deadline, the rest on the form.
function placeError(error: ApiError): Pick<BursaryFormState, 'fieldErrors' | 'message'> {
  if (error.status === 400 && Object.keys(error.fieldErrors).length > 0) {
    const fieldErrors: Partial<Record<Field, string>> = {}
    let message: string | null = null
    for (const { field, message: text } of placeServerErrors(error, apiFieldToFormField)) {
      if (field === null) message = text
      else fieldErrors[field] = text
    }
    return { fieldErrors, message }
  }
  if (error.status === 422) return { fieldErrors: { deadline: describeError(error) }, message: null }
  return { fieldErrors: {}, message: describeError(error) }
}
