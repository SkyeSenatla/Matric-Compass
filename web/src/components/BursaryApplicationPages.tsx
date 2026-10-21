import { useState } from 'react'
import { useBursaryApplications } from '@/api/queries'
import { BursaryApplicationCard } from '@/components/BursaryApplicationCard'
import { CardSkeletons } from '@/components/CardSkeletons'
import { ErrorView } from '@/components/ErrorView'
import { Button } from '@/components/ui/button'

// The counsellor's list: every learner's applications, ten at a time,
// in the order the API sorts them (soonest deadline first).
export function BursaryApplicationPages() {
  // The tokens of the pages we've visited. "" is the first page. Going back
  // pops a token; the opaque tokens themselves are never read or built.
  const [pageTokens, setPageTokens] = useState<string[]>([''])
  const currentToken = pageTokens[pageTokens.length - 1]
  const page = useBursaryApplications(currentToken)

  if (page.isPending) return <CardSkeletons label="Loading bursary applications" />
  if (page.isError) return <ErrorView error={page.error} onRetry={() => page.refetch()} />

  const { items, nextPageToken } = page.data
  // An EMPTY string, not null, is our API's "there is no next page".
  const hasNextPage = nextPageToken !== ''

  if (items.length === 0 && pageTokens.length === 1) {
    return <p className="text-muted-foreground">No bursary applications yet.</p>
  }

  return (
    <div className="space-y-4">
      {/* No client-side sort here: sorting one page of ten would be wrong.
          Ordering is the server's job, through its orderBy parameter. */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {items.map((application) => (
          <BursaryApplicationCard key={application.id} application={application} />
        ))}
      </div>

      <nav aria-label="Pages" className="flex items-center gap-3">
        <Button
          variant="outline"
          size="sm"
          disabled={pageTokens.length === 1 || page.isPlaceholderData}
          onClick={() => setPageTokens(pageTokens.slice(0, -1))}
        >
          Previous
        </Button>
        <span className="text-sm text-muted-foreground">Page {pageTokens.length}</span>
        <Button
          variant="outline"
          size="sm"
          // While the next page loads, the current one stays on screen
          // (keepPreviousData) and the buttons wait.
          disabled={!hasNextPage || page.isPlaceholderData}
          onClick={() => setPageTokens([...pageTokens, nextPageToken])}
        >
          Next
        </Button>
        {page.isFetching && <span className="text-sm text-muted-foreground">Updating…</span>}
      </nav>
    </div>
  )
}
