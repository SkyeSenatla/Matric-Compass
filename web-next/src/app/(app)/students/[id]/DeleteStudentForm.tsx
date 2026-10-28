'use client'

import { useActionState } from 'react'
import { deleteStudent, type DeleteStudentState } from '@/app/actions/students'
import { Button } from '@/components/ui/button'

export function DeleteStudentForm({ studentId, name }: { studentId: string; name: string }) {
  const [state, formAction, pending] = useActionState<DeleteStudentState, FormData>(deleteStudent, null)

  return (
    <form action={formAction} className="space-y-2">
      <input type="hidden" name="studentId" value={studentId} />
      <Button type="submit" variant="destructive" size="sm" disabled={pending}>
        {pending ? 'Deleting…' : `Delete ${name}`}
      </Button>
      {state !== null && (
        <div role="alert" className="space-y-1 text-sm">
          <p className="text-red-700 dark:text-red-300">{state.message}</p>
          {state.correlationId !== null && (
            <p className="text-muted-foreground">
              Reference: <code className="select-all font-mono text-xs">{state.correlationId}</code>
            </p>
          )}
        </div>
      )}
    </form>
  )
}
