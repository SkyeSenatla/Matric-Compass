'use client' // error boundaries are Client Components: they need retry()

import { Button } from '@/components/ui/button'

// For the unexpected: anything a page throws. Expected errors (403, 429,
// 500 from the API) are shown by the page itself, with their reference.
export default function SignedInError({ retry }: { error: unknown; retry: () => void }) {
  return (
    <div role="alert" className="space-y-3">
      <p className="font-medium">Something went wrong on this page.</p>
      <Button variant="outline" size="sm" onClick={() => retry()}>
        Try again
      </Button>
    </div>
  )
}
