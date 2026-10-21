import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api, unwrap } from './client'

// Every query key in one place. A key is the data's IDENTITY: it must
// contain everything that changes the answer. Two different requests that
// share a key would show each other's data.
export const queryKeys = {
  me: ['me'] as const,
  dashboard: ['me', 'dashboard'] as const,
  students: ['students'] as const,
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
