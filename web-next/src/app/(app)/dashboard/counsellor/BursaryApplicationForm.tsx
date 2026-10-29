'use client'

import { useQueryClient } from '@tanstack/react-query'
import { useActionState, useEffect } from 'react'
import { createBursaryApplication, type BursaryFormState } from '@/app/actions/bursaryApplications'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { NativeSelect, NativeSelectOption } from '@/components/ui/native-select'

const initialState: BursaryFormState = {
  values: { studentId: '', funder: '', amount: '', deadline: '' },
  fieldErrors: {},
  message: null,
  correlationId: null,
  saved: null,
}

type StudentOption = { id: string; label: string }

// Week 7's form, minus React Hook Form and TanStack Query: the Server
// Action validates, calls the API and returns what this form should show.
export function BursaryApplicationForm({ students }: { students: StudentOption[] }) {
  const [state, formAction, pending] = useActionState(createBursaryApplication, initialState)
  const { values, fieldErrors } = state

  // Day 4: refresh() rebuilt page one on the server, but pages two onwards
  // were loaded in the browser. After a save, mark them stale so TanStack
  // loads them again: Week 7's invalidateQueries, for the pages it owns.
  const queryClient = useQueryClient()
  useEffect(() => {
    if (state.saved !== null) void queryClient.invalidateQueries({ queryKey: ['bursary-applications'] })
  }, [state, queryClient])

  return (
    <Card>
      <CardHeader>
        <CardTitle>
          <h2>New bursary application</h2>
        </CardTitle>
        <CardDescription>Record an application a learner has made to a funder.</CardDescription>
      </CardHeader>
      <CardContent>
        {/* React resets an action form after it runs. The key makes React
            rebuild the inputs from the values the action sent back, so a
            rejected form keeps what the person typed. */}
        <form key={JSON.stringify(values)} action={formAction} noValidate className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="studentId">Student</Label>
            <NativeSelect
              id="studentId"
              name="studentId"
              className="w-full"
              defaultValue={values.studentId}
              aria-invalid={fieldErrors.studentId ? true : undefined}
              aria-describedby={fieldErrors.studentId ? 'studentId-error' : undefined}
            >
              <NativeSelectOption value="">Choose a student</NativeSelectOption>
              {students.map((student) => (
                <NativeSelectOption key={student.id} value={student.id}>
                  {student.label}
                </NativeSelectOption>
              ))}
            </NativeSelect>
            <FieldError id="studentId-error" message={fieldErrors.studentId} />
          </div>

          <div className="space-y-2">
            <Label htmlFor="funder">Funder</Label>
            <Input
              id="funder"
              name="funder"
              autoComplete="off"
              defaultValue={values.funder}
              aria-invalid={fieldErrors.funder ? true : undefined}
              aria-describedby={fieldErrors.funder ? 'funder-error' : undefined}
            />
            <FieldError id="funder-error" message={fieldErrors.funder} />
          </div>

          <div className="space-y-2">
            <Label htmlFor="amount">Amount (rands)</Label>
            <Input
              id="amount"
              name="amount"
              inputMode="decimal"
              autoComplete="off"
              placeholder="e.g. 1 500,50"
              defaultValue={values.amount}
              aria-invalid={fieldErrors.amount ? true : undefined}
              aria-describedby={fieldErrors.amount ? 'amount-error' : undefined}
            />
            <FieldError id="amount-error" message={fieldErrors.amount} />
          </div>

          <div className="space-y-2">
            <Label htmlFor="deadline">Deadline</Label>
            <Input
              id="deadline"
              name="deadline"
              type="date"
              defaultValue={values.deadline}
              aria-invalid={fieldErrors.deadline ? true : undefined}
              aria-describedby={fieldErrors.deadline ? 'deadline-error' : undefined}
            />
            <FieldError id="deadline-error" message={fieldErrors.deadline} />
          </div>

          <div className="space-y-3 sm:col-span-2">
            {state.message !== null && (
              <div role="alert" className="space-y-1 text-sm">
                <p className="font-medium text-red-700 dark:text-red-300">{state.message}</p>
                {state.correlationId !== null && (
                  <p className="text-muted-foreground">
                    Reference: <code className="select-all font-mono text-xs">{state.correlationId}</code>
                  </p>
                )}
              </div>
            )}
            {state.saved !== null && (
              <p role="status" className="text-sm font-medium">
                {state.saved}
              </p>
            )}
            {/* pending is Week 7's isSubmitting: one click, one request. */}
            <Button type="submit" disabled={pending}>
              {pending ? 'Saving…' : 'Save application'}
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
