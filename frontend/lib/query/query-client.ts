import { QueryClient } from "@tanstack/react-query"
import { ApiError } from "@/lib/api/api-error"

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        gcTime: 5 * 60_000,
        refetchOnWindowFocus: false,
        retry: (failureCount, error) => {
          // Do not retry on non-retryable API errors
          if (error instanceof ApiError) {
            const nonRetryableStatuses = [400, 401, 403, 404, 409, 422]
            if (nonRetryableStatuses.includes(error.status)) {
              return false
            }
          }
          return failureCount < 2
        },
      },
      mutations: {
        retry: false,
      },
    },
  })
}
