/**
 * API error response types
 */
export type ApiError = {
  code: string
  message: string
  details?: Record<string, unknown>
}

export type ValidationError = {
  field: string
  message: string
}
