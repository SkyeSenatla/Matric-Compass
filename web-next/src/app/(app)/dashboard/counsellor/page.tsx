import { Suspense } from 'react'
import { ApiError } from '@/api/errors'
import { BursaryApplicationCard } from '@/components/BursaryApplicationCard'
import { CardSkeletons } from '@/components/CardSkeletons'
import { ErrorView } from '@/components/ErrorView'
import { readOrFail, signedInApi } from '@/lib/api'
import { BursaryApplicationForm } from './BursaryApplicationForm'

// Not async: it returns at once, so a click on a link shows the skeletons
// straight away. The session read and the API calls happen inside Suspense.
export default function CounsellorDashboardPage() {
  return (
    <Suspense fallback={<CardSkeletons label="Loading bursary applications" />}>
      <CounsellorDashboard />
    </Suspense>
  )
}

// A Server Component: async, awaits its data, and sends finished HTML.
// No useQuery, no isPending: the Suspense above covers the wait.
async function CounsellorDashboard() {
  const { api } = await signedInApi()

  // Two requests that don't need each other's answers: start both, then
  // wait for the slower one. One await per line would wait for each in turn.
  const [applicationsResult, studentsResult] = await Promise.all([
    api.GET('/api/v1/bursary-applications', { params: { query: { PageSize: 10 } } }),
    api.GET('/api/v1/Students'),
  ])
  const page = readOrFail(applicationsResult)
  const students = readOrFail(studentsResult)

  return (
    <div className="space-y-8">
      {/* Only what the form shows crosses to the browser: id and a label. */}
      {!(students instanceof ApiError) && (
        <BursaryApplicationForm
          students={students.map((s) => ({ id: s.id, label: `${s.fullName} (${s.learnerReferenceNumber})` }))}
        />
      )}

      <section className="space-y-4">
        <h1 className="text-xl font-semibold">Bursary applications</h1>
        {page instanceof ApiError ? (
          <ErrorView error={page} />
        ) : page.items.length === 0 ? (
          <p className="text-muted-foreground">No bursary applications yet.</p>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {page.items.map((application) => (
              // BursaryApplicationCard is a Client Component (its "Show
              // details" toggle): its props travel to the browser as data.
              <BursaryApplicationCard key={application.id} application={application} />
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
