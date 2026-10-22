import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, unwrap } from './client'
import type { BursaryApplicationCreateRequest } from './types'

// Every query key in one place. A key is the data's IDENTITY: it must
// contain everything that changes the answer. Two different requests that
// share a key would show each other's data.
export const queryKeys = {
  me: ['me'] as const,
  dashboard: ['me', 'dashboard'] as const,
  students: ['students'] as const,
  // Day 4: the prefix every page of the list shares, so one invalidation
  // reaches page 1, page 2 and any filter at once.
  bursaryApplicationsAll: ['bursary-applications'] as const,
  bursaryApplications: (pageToken: string) => ['bursary-applications', { pageToken }] as const,
}

export function useMe() {
  return useQuery({
    queryKey: queryKeys.me,
    queryFn: async () => unwrap(await api.GET('/api/v1/me')),
  })
}

export function useDashboard() {
  return useQuery({
    queryKey: queryKeys.dashboard,
    queryFn: async () => unwrap(await api.GET('/api/v1/me/dashboard')),
  })
}

export function useStudents() {
  return useQuery({
    queryKey: queryKeys.students,
    queryFn: async () => unwrap(await api.GET('/api/v1/Students')),
  })
}

export function useBursaryApplications(pageToken: string) {
  return useQuery({
    queryKey: queryKeys.bursaryApplications(pageToken),
    queryFn: async () =>
      unwrap(
        await api.GET('/api/v1/bursary-applications', {
          // Typed: a misspelled parameter name fails to compile. (The API's
          // query parameters are PascalCase because the C# class's are.)
          params: { query: { PageSize: 10, PageToken: pageToken || undefined } },
        }),
      ),
    // Paging through ONE list: keep showing the current page while the next
    // loads, instead of flashing a skeleton. Never do this when switching
    // between different things (another learner, another stokvel): the old
    // one's data would show under the new one's name.
    placeholderData: keepPreviousData,
  })
}

// Day 4: the first write. A mutation runs only when someone acts, and never
// retries by default: repeating a POST could create a second application.
export function useCreateBursaryApplication() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (body: BursaryApplicationCreateRequest) =>
      unwrap(await api.POST('/api/v1/bursary-applications', { body })),
    // The new application sorts by deadline, so it could belong on ANY page.
    // Mark every page stale; whatever is on screen refetches. Returning the
    // promise keeps the mutation pending until the list has caught up.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.bursaryApplicationsAll }),
  })
}
