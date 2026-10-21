import { ApiError } from '@/api/errors'
import { useMe } from '@/api/queries'
import { AppHeader } from '@/components/AppHeader'
import { CardSkeletons } from '@/components/CardSkeletons'
import { DevAuthPanel } from '@/components/DevAuthPanel'
import { ErrorView } from '@/components/ErrorView'
import { LearnerView } from '@/components/LearnerView'
import { LoginForm } from '@/components/LoginForm'
import { StaffView } from '@/components/StaffView'
import { isStaff } from '@/lib/errorMessages'

// "Who am I?" decides everything: not signed in, a learner, or staff.
export default function App() {
  const me = useMe()

  return (
    <div className="min-h-svh bg-background text-foreground">
      {me.isPending ? (
        // On a reload this is the silent refresh restoring the session.
        <main className="mx-auto max-w-6xl px-4 py-6">
          <CardSkeletons label="Signing you in" />
        </main>
      ) : me.isError ? (
        me.error instanceof ApiError && me.error.status === 401 ? (
          <main className="px-4">
            <LoginForm />
          </main>
        ) : (
          <main className="mx-auto max-w-6xl px-4 py-6">
            <ErrorView error={me.error} onRetry={() => me.refetch()} />
          </main>
        )
      ) : (
        <>
          <AppHeader me={me.data} />
          <main className="mx-auto max-w-6xl space-y-10 px-4 py-6">
            {/* Choosing the screen by role is convenience, not security:
                the API refuses a learner who calls a staff endpoint. */}
            {isStaff(me.data.role) ? <StaffView /> : <LearnerView />}
            {import.meta.env.DEV && <DevAuthPanel />}
          </main>
        </>
      )}
    </div>
  )
}
