'use server'

import { redirect } from 'next/navigation'
import { ApiError } from '@/api/errors'
import { signedInApi } from '@/lib/api'
import { describeError } from '@/lib/errorMessages'

export type DeleteStudentState = { message: string; correlationId: string | null } | null

// Only an Admin sees the button. That's convenience: this action is a public
// POST that anyone signed in can send. The API's [Authorize(Roles = Admin)]
// is the rule, so the action lets the API answer, then shows the answer.
export async function deleteStudent(_previous: DeleteStudentState, formData: FormData): Promise<DeleteStudentState> {
  const { api } = await signedInApi()
  const id = String(formData.get('studentId') ?? '')

  const result = await api.DELETE('/api/v1/Students/{id}', { params: { path: { id } } })
  if (result.response.status === 401) redirect('/login?expired=1')
  if (!result.response.ok) {
    const error = ApiError.from(result.response, result.error)
    // 403 is "I know who you are, and no": say so, and stay signed in.
    const message = error.status === 403 ? 'Only an administrator can delete students.' : describeError(error)
    return { message, correlationId: error.correlationId }
  }

  redirect('/students')
}
