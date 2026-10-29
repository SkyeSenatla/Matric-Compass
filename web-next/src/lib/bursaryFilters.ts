import { createLoader, parseAsStringLiteral } from 'nuqs/server'

// The bursary list's filters live in the URL: ?status=Submitted&sort=Amount.
// One definition, used by the page (server), the Route Handler (server) and
// the filter controls (browser), so all three agree on what's valid.
//
// Allow-lists, with the same words the API accepts. Anything else in the URL
// (?status=Banana) parses to null, which means "the default", not an error.
export const statuses = ['Draft', 'Submitted', 'UnderReview', 'Approved', 'Rejected'] as const
export const sortOrders = ['Deadline', 'Amount'] as const

export const bursaryFilters = {
  status: parseAsStringLiteral(statuses), // null: every status
  sort: parseAsStringLiteral(sortOrders).withDefault('Deadline'),
}

// On the server: read the same typed values from a page's searchParams, or
// from a Route Handler's request.
export const loadBursaryFilters = createLoader(bursaryFilters)

// Small on purpose, so a few clicks show several pages.
export const PAGE_SIZE = 4
