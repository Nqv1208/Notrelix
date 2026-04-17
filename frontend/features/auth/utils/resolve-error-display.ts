import { isAuthErrorKey } from "@/features/auth/i18n/auth-error-keys"

type TranslateFn = (key: string) => string

export function resolveErrorDisplay(
  message: string | undefined,
  t: TranslateFn
): string {
  if (!message) return ""
  if (isAuthErrorKey(message)) return t(message)
  return message
}
