'use client'

import { useActionState, useEffect } from 'react'
import { login, type LoginState } from '@/app/actions/auth'
import { Button } from '@/components/ui/button'
import { forgetPinnedStudents } from '@/stores/pinnedStudents'

// A Client Component only because useActionState needs one: the form posts
// straight to the login Server Action, and shows what it sends back.
export function LoginForm({ expired, next }: { expired: boolean; next: string | null }) {
  const [state, formAction, pending] = useActionState<LoginState, FormData>(login, null)

  // Day 4: whoever signs in next starts with no pins, even if the last
  // session ended without a sign-out (it expired, or was revoked).
  useEffect(forgetPinnedStudents, [])

  return (
    <form action={formAction} className="space-y-4">
      {/* Where proxy.ts says they were going. The action checks it. */}
      {next !== null && <input type="hidden" name="next" value={next} />}
      {expired && state === null && (
        <p role="status" className="text-sm text-muted-foreground">
          Your session has ended. Please sign in again.
        </p>
      )}
      <div className="space-y-2">
        <label htmlFor="email" className="text-sm font-medium">Email</label>
        <input id="email" name="email" type="email" autoComplete="username" required className="h-9 w-full rounded-lg border border-input bg-transparent px-2.5 text-sm" />
      </div>
      <div className="space-y-2">
        <label htmlFor="password" className="text-sm font-medium">Password</label>
        <input id="password" name="password" type="password" autoComplete="current-password" required className="h-9 w-full rounded-lg border border-input bg-transparent px-2.5 text-sm" />
      </div>
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
      <Button type="submit" className="w-full" disabled={pending}>
        {pending ? 'Signing in…' : 'Sign in'}
      </Button>
    </form>
  )
}
