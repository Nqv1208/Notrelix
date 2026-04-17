import { AUTH_ERROR_KEYS, type AuthErrorKey } from "./auth-error-keys"

type ServerErrorPattern = {
  pattern: RegExp
  key: AuthErrorKey
}

const SERVER_ERROR_PATTERNS: ServerErrorPattern[] = [
  { pattern: /invalid email or password/i, key: AUTH_ERROR_KEYS.LOGIN_INVALID_CREDENTIALS },
  { pattern: /email is already in use/i, key: AUTH_ERROR_KEYS.REGISTER_EMAIL_TAKEN },
  { pattern: /account has been deactivated/i, key: AUTH_ERROR_KEYS.ACCOUNT_INACTIVE },
  { pattern: /account has been suspended/i, key: AUTH_ERROR_KEYS.ACCOUNT_SUSPENDED },
  { pattern: /refresh token is invalid or expired/i, key: AUTH_ERROR_KEYS.REFRESH_INVALID },
  { pattern: /user not found/i, key: AUTH_ERROR_KEYS.UNAUTHORIZED },

  { pattern: /email hoặc mật khẩu không đúng/i, key: AUTH_ERROR_KEYS.LOGIN_INVALID_CREDENTIALS },
  { pattern: /email đã được sử dụng/i, key: AUTH_ERROR_KEYS.REGISTER_EMAIL_TAKEN },
  { pattern: /tài khoản đã bị vô hiệu hóa/i, key: AUTH_ERROR_KEYS.ACCOUNT_INACTIVE },
  { pattern: /tài khoản đã bị tạm khóa/i, key: AUTH_ERROR_KEYS.ACCOUNT_SUSPENDED },
  { pattern: /refresh token không hợp lệ/i, key: AUTH_ERROR_KEYS.REFRESH_INVALID },
]

export function matchServerError(message: string): AuthErrorKey | undefined {
  for (const { pattern, key } of SERVER_ERROR_PATTERNS) {
    if (pattern.test(message)) return key
  }
  return undefined
}
