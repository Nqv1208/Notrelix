import { describe, it, expect } from 'vitest';
import { AppError } from '../result/app-error';

describe('AppError', () => {
  it('creates an error with required fields', () => {
    const error = new AppError({
      kind: 'network',
      message: 'Connection failed',
    });

    expect(error).toBeInstanceOf(Error);
    expect(error).toBeInstanceOf(AppError);
    expect(error.name).toBe('AppError');
    expect(error.kind).toBe('network');
    expect(error.message).toBe('Connection failed');
  });

  it('creates an error with optional fields', () => {
    const error = new AppError({
      kind: 'validation',
      message: 'Invalid input',
      status: 422,
      code: 'VALIDATION_ERROR',
      details: { field: 'email' },
      validationErrors: { email: ['Invalid email'] },
      correlationId: 'abc-123',
    });

    expect(error.status).toBe(422);
    expect(error.code).toBe('VALIDATION_ERROR');
    expect(error.details).toEqual({ field: 'email' });
    expect(error.validationErrors).toEqual({ email: ['Invalid email'] });
    expect(error.correlationId).toBe('abc-123');
  });

  it('preserves prototype chain', () => {
    const error = new AppError({
      kind: 'server',
      message: 'Internal error',
    });

    expect(error instanceof AppError).toBe(true);
    expect(error instanceof Error).toBe(true);
  });

  it('supports all error kinds', () => {
    const kinds = [
      'network', 'auth', 'forbidden', 'not_found', 'conflict',
      'validation', 'rate_limited', 'server', 'aborted', 'unknown',
    ] as const;

    for (const kind of kinds) {
      const error = new AppError({ kind, message: `Test ${kind}` });
      expect(error.kind).toBe(kind);
    }
  });
});
