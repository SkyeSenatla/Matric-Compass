import { Suspense } from 'react'
import { ApiError } from '@/api/errors'
import { BursaryApplicationList } from '@/components/BursaryApplicationList'
import { CardSkeletons } from '@/components/CardSkeletons'
import { ErrorView } from '@/components/ErrorView'
import { readOrFail, signedInApi } from '@/lib/api'

// Not async: it returns at once, so a click on a link shows the skeletons
// straight away. The session read and the API calls happen inside Suspense.
export default function LearnerDashboardPage() {
  return (
    <Suspense fallback={<CardSkeletons label="Loading your dashboard" />}>
      <LearnerDashboard />
    </Suspense>
  )
}

// The learner's dashboard: one request, shaped by the API for this screen.
async function LearnerDashboard() {
  const { api } = await signedInApi()
  const dashboard = readOrFail(await api.GET('/api/v1/me/dashboard'))
  if (dashboard instanceof ApiError) return <ErrorView error={dashboard} />

  return (
    <div className="space-y-10">
      <h1 className="text-xl font-semibold">Hi, {dashboard.fullName}</h1>
      <section className="space-y-2">
        <h2 className="font-medium">Your subjects</h2>
        {dashboard.subjects.length === 0 ? (
          <p className="text-muted-foreground">No subjects captured yet.</p>
        ) : (
          <ul className="space-y-1 text-sm">
            {dashboard.subjects.map((subject) => (
              <li key={subject.code}>
                {subject.code}: {subject.currentMark}% (target {subject.targetMark}%){' '}
                {subject.onTrack ? '✓ on track' : '⚠ below target'}
              </li>
            ))}
          </ul>
        )}
      </section>
      <section className="space-y-4">
        <h2 className="font-medium">Your bursary applications</h2>
        {/* The Week 7 list, with its sort button: a Client Component. */}
        <BursaryApplicationList applications={dashboard.bursaryApplications} />
      </section>
    </div>
  )
}
