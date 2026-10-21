import type { FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { ApiError } from '@/api/errors'
import { queryKeys } from '@/api/queries'
import { login } from '@/auth/session'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { describeLoginError } from '@/lib/errorMessages'

export function LoginForm() {
  const queryClient = useQueryClient()

  const signIn = useMutation({
    mutationFn: ({ email, password }: { email: string; password: string }) => login(email, password),
    // Signed in: ask "who am I?" again. App switches to the right screen.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.me }),
  })

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    signIn.mutate({ email: String(form.get('email')), password: String(form.get('password')) })
  }

  const correlationId = signIn.error instanceof ApiError ? signIn.error.correlationId : null

  return (
    <Card className="mx-auto mt-16 w-full max-w-sm">
      <CardHeader>
        <CardTitle>
          <h1 className="text-lg">Sign in to Matric Compass</h1>
        </CardTitle>
        <CardDescription>Use your school email address.</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            {/* autoComplete lets a password manager fill both fields. */}
            <Input id="email" name="email" type="email" autoComplete="username" required />
          </div>
          <div className="space-y-2">
            <Label htmlFor="password">Password</Label>
            <Input id="password" name="password" type="password" autoComplete="current-password" required />
          </div>

          {signIn.isError && (
            <div role="alert" className="space-y-1 text-sm text-destructive">
              <p>{describeLoginError(signIn.error)}</p>
              {correlationId !== null && (
                <p className="text-muted-foreground">
                  Reference: <code className="select-all font-mono text-xs">{correlationId}</code>
                </p>
              )}
            </div>
          )}

          {/* Disabled while the request is in flight: one click, one sign-in. */}
          <Button type="submit" className="w-full" disabled={signIn.isPending}>
            {signIn.isPending ? 'Signing in…' : 'Sign in'}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
