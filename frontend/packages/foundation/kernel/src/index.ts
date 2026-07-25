/**
 * @notrelix/kernel — Core utilities and types
 * 
 * Foundation layer providing essential utilities used across all packages.
 * Zero dependencies on other Notrelix packages.
 */

// Result types and error handling
export { AppError, type AppErrorKind } from './result/app-error'
export { errorMap, getErrorMessage, mapStatusToKind } from './result/error-map'
export { applyServerValidationErrors } from './result/apply-server-validation-errors'
export { getFormErrorMessage } from './result/get-form-error-message'

// Environment configuration
export { env, parseEnv, envSchema, type Env, type ResolvedEnv } from './env/env-schema'

// ID generation
export { generateCorrelationId } from './ids/correlation-id'

// Assertions
export { invariant, assertNonNull } from './assertions/invariant'
