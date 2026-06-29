import { AppError } from "@/lib/errors/app-error"

/**
 * Extracts a user-friendly error message from any error object,
 * specifically prioritizing AppError messages for forms.
 */
export function getFormErrorMessage(error: unknown, defaultMessage = "Đã xảy ra lỗi. Vui lòng thử lại."): string {
  if (!error) return ""

  if (error instanceof AppError) {
    if (error.kind === "validation") {
      return error.message || "Vui lòng kiểm tra lại thông tin nhập vào."
    }
    return error.message
  }

  if (error instanceof Error) {
    return error.message
  }

  if (typeof error === "string") {
    return error
  }

  return defaultMessage
}
