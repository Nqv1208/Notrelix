// Standard error messages mapping.
import type { AppErrorKind } from "./app-error";

export const errorMap: Record<string, string> = {
  ERR_NETWORK: "Network error. Please check your internet connection.",
  ERR_UNAUTHORIZED: "Unauthorized. Please sign in again.",
  ERR_FORBIDDEN: "You do not have permission to perform this action.",
  ERR_NOT_FOUND: "The requested resource was not found.",
  ERR_VALIDATION: "Validation failed. Please check your inputs.",
  ERR_SERVER: "Internal server error. Please try again later.",
};

export function getErrorMessage(
  code: string,
  fallback = "An unexpected error occurred.",
): string {
  return errorMap[code] || fallback;
}

export function mapStatusToKind(status: number): AppErrorKind {
  if (status === 400) return "validation";
  if (status === 401) return "auth";
  if (status === 403) return "forbidden";
  if (status === 404) return "not_found";
  if (status === 409) return "conflict";
  if (status === 422) return "validation";
  if (status === 429) return "rate_limited";
  if (status >= 500) return "server";
  return "unknown";
}
