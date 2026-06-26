// Standard error messages mapping.

export const errorMap: Record<string, string> = {
  "ERR_NETWORK": "Network error. Please check your internet connection.",
  "ERR_UNAUTHORIZED": "Unauthorized. Please sign in again.",
  "ERR_FORBIDDEN": "You do not have permission to perform this action.",
  "ERR_NOT_FOUND": "The requested resource was not found.",
  "ERR_VALIDATION": "Validation failed. Please check your inputs.",
  "ERR_SERVER": "Internal server error. Please try again later.",
}

export function getErrorMessage(code: string, fallback = "An unexpected error occurred."): string {
  return errorMap[code] || fallback
}
