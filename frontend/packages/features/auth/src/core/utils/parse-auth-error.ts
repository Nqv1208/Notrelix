export interface ParsedAuthError {
  messageKey: string | null;
  rawMessage: string;
  fieldErrors: Record<string, string>;
  kind: 'validation' | 'server' | 'network' | 'unknown';
}

export function parseAuthError(error: unknown): ParsedAuthError {
  const result: ParsedAuthError = {
    messageKey: null,
    rawMessage: 'An unexpected error occurred',
    fieldErrors: {},
    kind: 'unknown',
  };

  if (!error || typeof error !== 'object') return result;

  const err = error as Record<string, unknown>;

  // Handle AppError from @notrelix/kernel
  if (typeof err.message === 'string') {
    result.rawMessage = err.message;
  }

  if (typeof err.kind === 'string') {
    if (err.kind === 'validation') {
      result.kind = 'validation';
    } else if (err.kind === 'network') {
      result.kind = 'network';
    } else {
      result.kind = 'server';
    }
  }

  // Handle validation errors from API
  if (err.fieldErrors && typeof err.fieldErrors === 'object') {
    result.fieldErrors = err.fieldErrors as Record<string, string>;
    result.kind = 'validation';
  }

  // Map known error messages to i18n keys
  if (typeof err.message === 'string') {
    if (err.message.includes('Invalid credentials')) {
      result.messageKey = 'auth.errors.invalid_credentials';
    } else if (err.message.includes('already exists')) {
      result.messageKey = 'auth.errors.email_already_exists';
    } else if (err.message.includes('Too many')) {
      result.messageKey = 'auth.errors.rate_limited';
    }
  }

  return result;
}
