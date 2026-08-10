import { AppError } from "@notrelix/kernel";

export function shouldRetryQuery(
  failureCount: number,
  error: unknown,
): boolean {
  if (failureCount >= 3) {
    return false;
  }

  if (error instanceof AppError) {
    switch (error.kind) {
      case "auth": // 401
      case "forbidden": // 403
      case "not_found": // 404
      case "conflict": // 409
      case "aborted":
        return false;
      case "rate_limited": // 429
      case "network":
      case "server":
        return failureCount < 2;
      default:
        return false;
    }
  }

  return false;
}
