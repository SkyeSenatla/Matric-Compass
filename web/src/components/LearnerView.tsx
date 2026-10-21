import { useDashboard } from '@/api/queries'
import { BursaryApplicationList } from '@/components/BursaryApplicationList'
import { CardSkeletons } from '@/components/CardSkeletons'
import { ErrorView } from '@/components/ErrorView'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

// The learner's screen: one request (/me/dashboard), shaped by the API for
// exactly this screen in Week 6.
export function LearnerView() {
  const dashboard = useDashboard()

  // State 1 of 4: nothing yet.
  if (dashboard.isPending) return <CardSkeletons label="Loading your dashboard" />

  // State 2 of 4: it failed.
  if (dashboard.isError) return <ErrorView error={dashboard.error} onRetry={() => dashboard.refetch()} />

  // States 3 and 4: data, which may be empty (BursaryApplicationList shows
  // its own "No bursary applications yet").
  const { fullName, subjects, bursaryApplications } = dashboard.data

  return (
    <div className="space-y-10">
      <h2 className="text-xl font-semibold">Hi, {fullName}</h2>

      <section className="space-y-4">
        <h2 className="text-xl font-semibold">Your subjects</h2>
        <Card>
          <CardHeader>
            <CardTitle>
              <h3>Marks against your targets</h3>
            </CardTitle>
          </CardHeader>
          <CardContent>
            {subjects.length === 0 ? (
              <p className="text-muted-foreground">No subjects captured yet.</p>
            ) : (
              <ul className="space-y-1 text-sm">
                {subjects.map((subject) => (
                  <li key={subject.code} className="flex justify-between gap-2">
                    <span>
                      {subject.code}: {subject.currentMark}% (target {subject.targetMark}%)
                    </span>
                    {/* onTrack is decided by the API, once, not re-worked here. */}
                    <span className="text-muted-foreground">
                      {subject.onTrack ? '✓ on track' : '⚠ below target'}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>
      </section>

      <section className="space-y-4">
        <h2 className="text-xl font-semibold">Your bursary applications</h2>
        <BursaryApplicationList applications={bursaryApplications} />
      </section>
    </div>
  )
}
