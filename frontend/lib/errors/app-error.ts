// Unified custom application error for Notrelix.

export class AppError extends Error {
  constructor(
    public override message: string,
    public code?: string,
    public status?: number,
    public errors?: Record<string, string[]>
  ) {
    super(message)
    this.name = "AppError"
    Object.setPrototypeOf(this, AppError.prototype)
  }
}
