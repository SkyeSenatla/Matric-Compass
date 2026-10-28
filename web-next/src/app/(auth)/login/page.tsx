import { Suspense } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { LoginForm } from './LoginForm'

// (auth) is a route group: it keeps the login page out of the signed-in
// layout (no sidebar) without adding "/auth" to the URL. This is /login.
//
// Not async: the card is the same for everyone, so it goes in the static
// shell. Only the part that reads the URL waits, inside Suspense.
export default function LoginPage({ searchParams }: PageProps<'/login'>) {
  return (
    <main className="px-4">
      <Card className="mx-auto mt-16 w-full max-w-sm">
        <CardHeader>
          <CardTitle>
            <h1 className="text-lg">Sign in to Matric Compass</h1>
          </CardTitle>
          <CardDescription>Use your school email address.</CardDescription>
        </CardHeader>
        <CardContent>
          <Suspense fallback={<p className="text-sm text-muted-foreground">Loading…</p>}>
            <LoginFormFromUrl searchParams={searchParams} />
          </Suspense>
        </CardContent>
      </Card>
    </main>
  )
}

// searchParams is request data: awaited here, inside the boundary.
async function LoginFormFromUrl({ searchParams }: Pick<PageProps<'/login'>, 'searchParams'>) {
  const { expired, next } = await searchParams
  return <LoginForm expired={expired === '1'} next={typeof next === 'string' ? next : null} />
}
