import { ApiError } from "@/lib/api/api-error"
import { AUTH_ERROR_KEYS, type AuthErrorKey, isAuthErrorKey } from "@/features/auth/i18n/auth-error-keys"
import { matchServerError } from "@/features/auth/i18n/server-error-map"

export type ParsedAuthError = {
  messageKey: AuthErrorKey | null
  rawMessage: string | null
  fieldErrors: Record<string, AuthErrorKey | string>
}

type MiddlewareErrorBody = {
  type?: string
  message?: string
  detail?: string
  errors?: Record<string, string[]> | string[]
}

export function parseAuthError(error: unknown): ParsedAuthError {
  const fallback: ParsedAuthError = {
    messageKey: AUTH_ERROR_KEYS.SERVER_GENERIC,
    rawMessage: null,
    fieldErrors: {},
  }

  if (!(error instanceof ApiError)) return fallback

  const body = (error.data ?? {}) as MiddlewareErrorBody

  if (body.type === "Unauthorized") {
    return { messageKey: AUTH_ERROR_KEYS.UNAUTHORIZED, rawMessage: null, fieldErrors: {} }
  }

  if (Array.isArray(body.errors)) {
    return parseResultErrors(body.errors)
  }

  if (body.errors && typeof body.errors === "object") {
    return parseValidationErrors(body.errors as Record<string, string[]>, body.message)
  }

  const msg = body.message ?? body.detail ?? error.message
  return resolveMessage(msg)
}

function parseResultErrors(errors: string[]): ParsedAuthError {
  if (errors.length === 0) {
    return { messageKey: AUTH_ERROR_KEYS.SERVER_GENERIC, rawMessage: null, fieldErrors: {} }
  }

  const firstError = errors[0]
  return resolveMessage(firstError)
}

function parseValidationErrors(
  errors: Record<string, string[]>,
  topMessage?: string
): ParsedAuthError {
  const fieldErrors: Record<string, AuthErrorKey | string> = {}

  for (const [field, messages] of Object.entries(errors)) {
    if (!Array.isArray(messages) || messages.length === 0) continue
    const msg = messages[0]
    fieldErrors[field.toLowerCase()] = resolveFieldMessage(msg)
  }

  const resolved = topMessage ? resolveMessage(topMessage) : null

  return {
    messageKey: resolved?.messageKey ?? null,
    rawMessage: resolved?.rawMessage ?? null,
    fieldErrors,
  }
}

function resolveMessage(message: string): ParsedAuthError {
  if (isAuthErrorKey(message)) {
    return { messageKey: message, rawMessage: null, fieldErrors: {} }
  }

  const mapped = matchServerError(message)
  if (mapped) {
    return { messageKey: mapped, rawMessage: null, fieldErrors: {} }
  }

  return { messageKey: null, rawMessage: message, fieldErrors: {} }
}

function resolveFieldMessage(message: string): AuthErrorKey | string {
  if (isAuthErrorKey(message)) return message
  return matchServerError(message) ?? message
}
