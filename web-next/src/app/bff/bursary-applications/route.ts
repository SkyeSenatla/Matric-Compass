import type { NextRequest } from 'next/server'
import { signedInApi } from '@/lib/api'
import { loadBursaryFilters, PAGE_SIZE } from '@/lib/bursaryFilters'
import { getSession } from '@/lib/session'

// GET /bff/bursary-applications?status=…&sort=…&pageToken=…
//
// The backend for the frontend, as a URL the browser can fetch: same site,
// the session cookie goes along, no CORS. It's a public endpoint like any
// Server Action, so it checks the session and lets the API decide the rest.
export async function GET(request: NextRequest) {
  // A JSON 401, not a redirect to the sign-in page: the caller is fetch(),
  // which can't do anything with an HTML page.
  if ((await getSession()) === null) {
    return Response.json({ title: 'Your session has ended.' }, { status: 401 })
  }

  // The same parsers as the page: ?status=Banana becomes "every status".
  const { status, sort } = loadBursaryFilters(request)
  const pageToken = request.nextUrl.searchParams.get('pageToken') ?? ''

  const { api } = await signedInApi()
  const result = await api.GET('/api/v1/bursary-applications', {
    params: {
      query: { Status: status ?? undefined, OrderBy: sort, PageSize: PAGE_SIZE, PageToken: pageToken || undefined },
    },
  })

  // Pass the API's answer through: its data, or its problem and status.
  if (result.data === undefined) return Response.json(result.error ?? {}, { status: result.response.status })
  return Response.json({ items: result.data.items, nextPageToken: result.data.nextPageToken })
}
