'use client'

import { useInfiniteQuery } from '@tanstack/react-query'
import { useRouter } from 'next/navigation'
import { useEffect, useRef, useState } from 'react'
import type { BursaryApplicationResponse } from '@/api/types'
import { BursaryApplicationCard } from '@/components/BursaryApplicationCard'
import { Button } from '@/components/ui/button'

type Page = { items: BursaryApplicationResponse[]; nextPageToken: string }

class SessionEndedError extends Error {}

// The browser can't call the API (Day 1), so it asks our own Route Handler,
// which reads the session and calls the API with the token.
async function fetchPage(status: string | null, sort: string, pageToken: string): Promise<Page> {
  const query = new URLSearchParams({ sort, pageToken })
  if (status !== null) query.set('status', status)
  const response = await fetch(`/bff/bursary-applications?${query}`)
  if (response.status === 401) throw new SessionEndedError('Your session has ended.')
  if (!response.ok) throw new Error(`Loading more failed (${response.status}).`)
  return response.json() as Promise<Page>
}

// The server rendered page one, and passes it in as children. This component
// adds the rest after it, in the same grid, starting from the token page one
// ended with. The filters and that token are in the query
// key: a new filter is a new list, starting again from its own page one.
export function BursaryApplicationFeed({
  status,
  sort,
  after,
  shownByServer,
  children,
}: {
  status: string | null
  sort: string
  after: string
  shownByServer: number
  children: React.ReactNode
}) {
  // Nothing loads until the person scrolls near the end or presses the button.
  const [wanted, setWanted] = useState(false)
  const more = useInfiniteQuery({
    queryKey: ['bursary-applications', { status, sort, after }],
    queryFn: ({ pageParam }) => fetchPage(status, sort, pageParam),
    initialPageParam: after,
    // An empty nextPageToken means "no more": undefined tells TanStack so.
    getNextPageParam: (last) => last.nextPageToken || undefined,
    enabled: wanted,
    // A signed-out answer won't change if we ask again; anything else gets one retry.
    retry: (failures, error) => !(error instanceof SessionEndedError) && failures < 1,
  })

  // A 401 from our Route Handler: the session is over, so go and sign in.
  const router = useRouter()
  const sessionEnded = more.error instanceof SessionEndedError
  useEffect(() => {
    if (sessionEnded) router.replace('/login?expired=1')
  }, [sessionEnded, router])

  const items = more.data?.pages.flatMap((page) => page.items) ?? []
  // More to come: page one ended with a token, and either we haven't asked
  // yet, the first extra page is still on its way, or the last one had a token.
  const canLoadMore = after !== '' && (!wanted || more.isPending || more.hasNextPage)
  const busy = more.isFetching

  function loadMore() {
    if (busy) return
    if (!wanted) setWanted(true)
    else if (more.hasNextPage) void more.fetchNextPage()
  }

  // Infinite scroll: a sentinel at the end of the list. When it comes into
  // view, load the next page. The button below does the same for keyboards.
  const sentinel = useRef<HTMLDivElement>(null)
  const loadMoreRef = useRef(loadMore)
  useEffect(() => {
    loadMoreRef.current = loadMore
  })
  // Observed again after each page: if the end is still on screen, the
  // observer reports it straight away and the next page loads.
  useEffect(() => {
    const element = sentinel.current
    if (element === null || !canLoadMore) return
    const observer = new IntersectionObserver((entries) => {
      if (entries.some((entry) => entry.isIntersecting)) loadMoreRef.current()
    })
    observer.observe(element)
    return () => observer.disconnect()
  }, [canLoadMore, items.length])

  return (
    <>
      {/* data-filters: which list this is, for tests and DevTools. */}
      <div data-filters={`${status ?? 'all'}|${sort}`} className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {children}
        {items.map((application) => (
          <BursaryApplicationCard key={application.id} application={application} />
        ))}
      </div>
      <div ref={sentinel} className="space-y-2">
        {/* Announced to screen readers as it changes. */}
        <p role="status" className="text-sm text-muted-foreground">
          {canLoadMore
            ? `Showing ${shownByServer + items.length} applications.`
            : `Showing all ${shownByServer + items.length} applications.`}
        </p>
        {more.isError && (
          <p role="alert" className="text-sm text-red-700 dark:text-red-300">
            {more.error.message}
          </p>
        )}
        {canLoadMore && (
          <Button type="button" variant="outline" size="sm" disabled={busy} onClick={loadMore}>
            {busy ? 'Loading…' : more.isError ? 'Try again' : 'Load more'}
          </Button>
        )}
      </div>
    </>
  )
}
