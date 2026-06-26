import type { UseFormSetError, FieldPath } from "react-hook-form"

// Helper to automatically apply server-side validation error dictionaries to react-hook-form fields.
export function applyServerValidationErrors<T extends Record<string, unknown>>(
  errors: Record<string, string[]>,
  setError: UseFormSetError<T>
) {
  Object.entries(errors).forEach(([field, messages]) => {
    setError(field as FieldPath<T>, {
      type: "server",
      message: messages[0] || "Invalid value",
    })
  })
}
