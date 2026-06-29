// Unified custom application error for Notrelix.

export type AppErrorKind =
  | "network"
  | "auth"
  | "forbidden"
  | "not_found"
  | "conflict"
  | "validation"
  | "rate_limited"
  | "server"
  | "aborted"
  | "unknown"

export class AppError extends Error {
  public kind: AppErrorKind
  public status?: number
  public code?: string
  public details?: unknown
  public validationErrors?: Record<string, string[]>
  public correlationId?: string
  public cause?: unknown

  constructor(params: {
    kind: AppErrorKind
    message: string
    status?: number
    code?: string
    details?: unknown
    validationErrors?: Record<string, string[]>
    correlationId?: string
    cause?: unknown
  }) {
    super(params.message)
    this.name = "AppError"
    this.kind = params.kind
    this.status = params.status
    this.code = params.code
    this.details = params.details
    this.validationErrors = params.validationErrors
    this.correlationId = params.correlationId
    this.cause = params.cause
    Object.setPrototypeOf(this, AppError.prototype)
  }
}
