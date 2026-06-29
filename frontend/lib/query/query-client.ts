import { QueryClient } from "@tanstack/react-query"
import { AppError } from "@/lib/errors/app-error"

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        gcTime: 5 * 60_000,
        refetchOnWindowFocus: false,
        retry: (failureCount, error) => {
          // Do not retry on non-retryable API errors
          if (error instanceof AppError) {
            const nonRetryableKinds = ["auth", "forbidden", "not_found", "conflict", "validation"]
            if (nonRetryableKinds.includes(error.kind)) {
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
