import { ApiError } from "@/lib/api/api-error";
import { AUTH_ERROR_KEYS, AuthErrorKey, isAuthErrorKey } from "@/features/auth/i18n/auth-error-keys";

type ParsedAuthError = {
  messageKey: AuthErrorKey;
  fieldErrors: Record<string, AuthErrorKey>;
};

type ApiErrorPayload = {
  message?: string;
  detail?: string;
  type?: string;
  errors?: unknown;
};

export function parseAuthError(error: unknown): ParsedAuthError {
  const fallback: ParsedAuthError = {
    messageKey: AUTH_ERROR_KEYS.SERVER_GENERIC,
    fieldErrors: {},
  };

  if (!(error instanceof ApiError)) {
    return fallback;
  }

  const payload = (error.data ?? {}) as ApiErrorPayload;
  const messageKey =
    mapErrorMessageToKey(payload.message ?? payload.detail, payload.type) ??
    mapErrorMessageToKey(error.message, payload.type) ??
    fallback.messageKey;
  const result: ParsedAuthError = {
    messageKey,
    fieldErrors: {},
  };

  if (Array.isArray(payload.errors)) {
    if (payload.errors.length > 0) {
      result.messageKey = mapErrorMessageToKey(String(payload.errors[0]), payload.type) ?? result.messageKey;
    }
    return result;
  }

  if (payload.errors && typeof payload.errors === "object") {
    for (const [key, value] of Object.entries(payload.errors)) {
      if (!Array.isArray(value) || value.length === 0) continue;
      result.fieldErrors[key] = mapErrorMessageToKey(String(value[0]), payload.type) ?? AUTH_ERROR_KEYS.SERVER_GENERIC;
    }
  }

  return result;
}

function mapErrorMessageToKey(message?: string, type?: string): AuthErrorKey | undefined {
  if (type === "Unauthorized") {
    return AUTH_ERROR_KEYS.UNAUTHORIZED;
  }

  if (!message) return undefined;
  if (isAuthErrorKey(message)) return message;

  const normalized = message.trim().toLowerCase();
  if (normalized.includes("email hoặc mật khẩu không đúng")) {
    return AUTH_ERROR_KEYS.LOGIN_INVALID_CREDENTIALS;
  }
  if (normalized.includes("email đã được sử dụng")) {
    return AUTH_ERROR_KEYS.REGISTER_EMAIL_TAKEN;
  }
  if (normalized.includes("tài khoản đã bị vô hiệu hóa")) {
    return AUTH_ERROR_KEYS.ACCOUNT_INACTIVE;
  }
  if (normalized.includes("tài khoản đã bị tạm khóa")) {
    return AUTH_ERROR_KEYS.ACCOUNT_SUSPENDED;
  }
  if (normalized.includes("refresh token không hợp lệ") || normalized.includes("hết hạn")) {
    return AUTH_ERROR_KEYS.REFRESH_INVALID;
  }

  return undefined;
}
