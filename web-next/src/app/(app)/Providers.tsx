'use client'

import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useState } from 'react'
import { PinnedStudentsProvider } from '@/stores/PinnedStudentsProvider'

// The browser-side state for signed-in pages: TanStack Query, back for one
// job (loading more pages in the browser), and the pinned-students store.
// useState, not a module variable: one client per tab, never shared.
export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => new QueryClient())
  return (
    <QueryClientProvider client={queryClient}>
      <PinnedStudentsProvider>{children}</PinnedStudentsProvider>
    </QueryClientProvider>
  )
}
