import type { UseFormSetError, FieldPath } from "react-hook-form"
import { AppError } from "./app-error"

// Helper to automatically apply server-side validation error dictionaries to react-hook-form fields.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function applyServerValidationErrors<T extends Record<string, any>>(
  form: { setError: UseFormSetError<T> },
  error: unknown
) {
  if (!(error instanceof AppError) || error.kind !== "validation") {
    return
  }

  const validationErrors = error.validationErrors
  if (!validationErrors || typeof validationErrors !== "object") {
    form.setError("root" as FieldPath<T>, {
      type: "server",
      message: error.message || "Validation failed.",
    })
    return
  }

  try {
    Object.entries(validationErrors).forEach(([field, messages]) => {
      const message = Array.isArray(messages) ? messages[0] : String(messages)
      form.setError(field as FieldPath<T>, {
        type: "server",
        message: message || "Invalid value",
      })
    })
  } catch {
    // Do not throw if server shape differs
    form.setError("root" as FieldPath<T>, {
      type: "server",
      message: error.message || "Validation failed.",
    })
  }
}
