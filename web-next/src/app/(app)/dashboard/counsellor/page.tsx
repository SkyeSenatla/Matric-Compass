import { Suspense } from 'react'
import { ApiError } from '@/api/errors'
import { BursaryApplicationCard } from '@/components/BursaryApplicationCard'
import { CardSkeletons } from '@/components/CardSkeletons'
import { ErrorView } from '@/components/ErrorView'
import { readOrFail, signedInApi } from '@/lib/api'
import { loadBursaryFilters, PAGE_SIZE } from '@/lib/bursaryFilters'
import { BursaryApplicationForm } from './BursaryApplicationForm'
import { BursaryFilters } from './BursaryFilters'
import { BursaryApplicationFeed } from './BursaryApplicationFeed'

// Not async: it returns at once, so a click on a link shows the skeletons
// straight away. The URL, the session and the API calls wait inside Suspense.
export default function CounsellorDashboardPage({ searchParams }: PageProps<'/dashboard/counsellor'>) {
  return (
    <Suspense fallback={<CardSkeletons label="Loading bursary applications" />}>
      <CounsellorDashboard searchParams={searchParams} />
    </Suspense>
  )
}

// A Server Component: async, awaits its data, and sends finished HTML.
async function CounsellorDashboard({ searchParams }: Pick<PageProps<'/dashboard/counsellor'>, 'searchParams'>) {
  // The filters come from the URL, through the same parsers the browser uses.
  const { status, sort } = await loadBursaryFilters(searchParams)
  const { api } = await signedInApi()

  // Two requests that don't need each other's answers: start both, then
  // wait for the slower one. One await per line would wait for each in turn.
  const [applicationsResult, studentsResult] = await Promise.all([
    api.GET('/api/v1/bursary-applications', {
      params: { query: { Status: status ?? undefined, OrderBy: sort, PageSize: PAGE_SIZE } },
    }),
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
        <BursaryFilters />
        {page instanceof ApiError ? (
          <ErrorView error={page} />
        ) : page.items.length === 0 ? (
          <p className="text-muted-foreground">No bursary applications match these filters.</p>
        ) : (
          // The rest is loaded in the browser. The key starts a fresh one
          // (and a fresh "load more" state) whenever page one changes.
          <BursaryApplicationFeed
            key={`${status}|${sort}|${page.nextPageToken}`}
            status={status}
            sort={sort}
            after={page.nextPageToken}
            shownByServer={page.items.length}
          >
            {/* Page one, rendered here on the server, from the URL. */}
            {page.items.map((application) => (
              <BursaryApplicationCard key={application.id} application={application} />
            ))}
          </BursaryApplicationFeed>
        )}
      </section>
    </div>
  )
}
