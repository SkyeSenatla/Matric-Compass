import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ApiError } from '@/api/errors'
import { useCreateBursaryApplication, useStudents } from '@/api/queries'
import {
  apiFieldToFormField,
  bursaryApplicationSchema,
  toCreateRequest,
  type BursaryApplicationFormInput,
  type BursaryApplicationFormValues,
} from '@/forms/bursaryApplication'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { NativeSelect, NativeSelectOption } from '@/components/ui/native-select'
import { describeError } from '@/lib/errorMessages'
import { placeServerErrors } from '@/lib/formErrors'
import { formatCents } from '@/lib/money'

const empty: BursaryApplicationFormInput = { studentId: '', funder: '', amount: '', deadline: '' }

// A counsellor records a bursary application for a learner.
export function BursaryApplicationForm() {
  const students = useStudents()
  const create = useCreateBursaryApplication()
  const [saved, setSaved] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    reset,
    setFocus,
    formState: { errors, isSubmitting },
    // Input type: what the inputs hold (strings). Output type: what the
    // schema hands to onValid (amount already in cents).
  } = useForm<BursaryApplicationFormInput, unknown, BursaryApplicationFormValues>({
    resolver: zodResolver(bursaryApplicationSchema),
    defaultValues: empty,
  })

  async function onValid(values: BursaryApplicationFormValues) {
    setSaved(null)
    try {
      // mutateAsync, awaited: isSubmitting stays true until the API has
      // answered AND the list has refetched, so the button stays disabled
      // for the whole round trip.
      const created = await create.mutateAsync(toCreateRequest(values))
      // keepFieldsRef: by default reset() forgets every registered input and
      // waits for React to call each input's ref again. The React Compiler
      // memoizes {...register()}, so React never does: the inputs would keep
      // their text and ignore anything typed next. This writes the empty
      // values through the refs React Hook Form already has.
      reset(empty, { keepFieldsRef: true })
      setSaved(`${created.funder} application for ${formatCents(created.amountInCents)} saved.`)
      setFocus('studentId')
    } catch (error) {
      showServerError(error)
    }
  }

  // Every API error lands somewhere a person will see it.
  function showServerError(error: unknown) {
    if (error instanceof ApiError && error.status === 400 && Object.keys(error.fieldErrors).length > 0) {
      // A 400 with an errors map: each message next to its own field.
      for (const { field, message } of placeServerErrors(error, apiFieldToFormField)) {
        setError(field ?? 'root.server', { message }, { shouldFocus: true })
      }
    } else if (error instanceof ApiError && error.status === 422) {
      // This endpoint's only 422 is "the deadline is in the past". It has no
      // errors map, so placing it on the deadline field is OUR decision.
      setError('deadline', { message: describeError(error) }, { shouldFocus: true })
    } else {
      // 409 (its detail says why), 5xx, or the network: the form as a whole.
      setError('root.server', { message: describeError(error) })
    }
  }

  const correlationId = create.error instanceof ApiError ? create.error.correlationId : null

  return (
    <Card>
      <CardHeader>
        <CardTitle>
          <h3>New bursary application</h3>
        </CardTitle>
        <CardDescription>Record an application a learner has made to a funder.</CardDescription>
      </CardHeader>
      <CardContent>
        {/* noValidate: our own messages instead of the browser's bubbles,
            so every error looks and reads the same. */}
        <form onSubmit={handleSubmit(onValid)} noValidate className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="studentId">Student</Label>
            <NativeSelect
              id="studentId"
              className="w-full"
              disabled={!students.isSuccess}
              aria-invalid={errors.studentId ? true : undefined}
              aria-describedby={errors.studentId ? 'studentId-error' : undefined}
              {...register('studentId')}
            >
              <NativeSelectOption value="">{students.isPending ? 'Loading students…' : 'Choose a student'}</NativeSelectOption>
              {students.data?.map((student) => (
                <NativeSelectOption key={student.id} value={student.id}>
                  {student.fullName} ({student.learnerReferenceNumber})
                </NativeSelectOption>
              ))}
            </NativeSelect>
            <FieldError id="studentId-error" message={errors.studentId?.message} />
          </div>

          <div className="space-y-2">
            <Label htmlFor="funder">Funder</Label>
            <Input
              id="funder"
              autoComplete="off"
              aria-invalid={errors.funder ? true : undefined}
              aria-describedby={errors.funder ? 'funder-error' : undefined}
              {...register('funder')}
            />
            <FieldError id="funder-error" message={errors.funder?.message} />
          </div>

          <div className="space-y-2">
            <Label htmlFor="amount">Amount (rands)</Label>
            {/* Text, not type="number": a number input rejects "1 500,50",
                and its value is a float. inputMode still brings up the
                number keyboard on a phone. */}
            <Input
              id="amount"
              inputMode="decimal"
              autoComplete="off"
              placeholder="e.g. 1 500,50"
              aria-invalid={errors.amount ? true : undefined}
              aria-describedby={errors.amount ? 'amount-error' : undefined}
              {...register('amount')}
            />
            <FieldError id="amount-error" message={errors.amount?.message} />
          </div>

          <div className="space-y-2">
            <Label htmlFor="deadline">Deadline</Label>
            <Input
              id="deadline"
              type="date"
              aria-invalid={errors.deadline ? true : undefined}
              aria-describedby={errors.deadline ? 'deadline-error' : undefined}
              {...register('deadline')}
            />
            <FieldError id="deadline-error" message={errors.deadline?.message} />
          </div>

          <div className="space-y-3 sm:col-span-2">
            {errors.root?.server && (
              <div role="alert" className="space-y-1 text-sm">
                <p className="font-medium text-red-700 dark:text-red-300">{errors.root.server.message}</p>
                {correlationId !== null && (
                  <p className="text-muted-foreground">
                    Reference: <code className="select-all font-mono text-xs">{correlationId}</code>
                  </p>
                )}
              </div>
            )}
            {saved !== null && (
              <p role="status" className="text-sm font-medium">
                {saved}
              </p>
            )}
            {/* Disabled from the click until the list has refetched: one
                click, one application. */}
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Saving…' : 'Save application'}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}

function FieldError({ id, message }: { id: string; message: string | undefined }) {
  if (message === undefined) return null
  // Text, not just a red border: colour alone isn't enough (WCAG 1.4.1).
  return (
    <p id={id} className="text-sm text-red-700 dark:text-red-300">
      {message}
    </p>
  )
}
