import { AppError } from '../result/app-error';

export interface NormalizedAppError {
  readonly code: string;
  readonly category:
    | 'validation'
    | 'authentication'
    | 'authorization'
    | 'not-found'
    | 'conflict'
    | 'rate-limit'
    | 'network'
    | 'contract'
    | 'unknown';

  readonly correlationId?: string;
  readonly retryable: boolean;
  readonly validationErrors?: ReadonlyArray<{
    readonly field: string;
    readonly code: string;
  }>;
}

export function isAppError(error: unknown): error is AppError {
  return error instanceof AppError;
}

export function getUserFacingErrorMessage(error: unknown): string {
  if (error instanceof AppError) {
    switch (error.kind) {
      case 'auth':
        return 'Session expired or invalid credentials. Please sign in again.';
      case 'forbidden':
        return 'You do not have permission to perform this action.';
      case 'not_found':
        return 'The requested resource was not found.';
      case 'conflict':
        return 'A conflict occurred with existing data. Please refresh and try again.';
      case 'rate_limited':
        return 'Too many requests. Please wait a moment and try again.';
      case 'network':
        return 'Network error encountered. Please check your internet connection.';
      case 'validation':
        return 'Please review input errors and try again.';
      default:
        return error.message || 'An unexpected error occurred. Please try again.';
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'An unexpected error occurred. Please try again.';
}
