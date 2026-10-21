import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import './index.css'
import App from './App.tsx'
import { ApiError } from '@/api/errors'
import { onSignedOut } from '@/auth/session'
import { ThemeProvider } from '@/components/ThemeProvider'

// One cache for the whole app.
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Data is "fresh" for 30 seconds: no refetch on every focus or mount.
      staleTime: 30_000,
      // TanStack retries every failure 3 times by default. A 4xx won't fix
      // itself (and a 401 is already handled by authFetch), so only retry
      // network failures and 5xx, and only twice.
      retry: (failureCount, error) =>
        error instanceof ApiError && error.status < 500 ? false : failureCount < 2,
    },
  },
})

// Signed out (by the user, or because the session expired): forget every
// cached response, so the next person on this device sees none of it.
// Resetting also refetches "who am I?", which now fails, so App shows the
// sign-in form.
onSignedOut(() => {
  void queryClient.resetQueries()
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      {/* Outermost, so every component below can call useTheme(). */}
      <ThemeProvider>
        <App />
      </ThemeProvider>
    </QueryClientProvider>
  </StrictMode>,
)
