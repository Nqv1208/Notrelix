/**
 * @notrelix/contracts — API client and contracts
 * 
 * Framework-neutral HTTP client and API contracts.
 * No React, no DOM dependencies.
 */

export { api, apiFetch, type ApiRequestOptions } from './client'
export { getCsrfToken } from './client'
export { endpoints } from './endpoints'
export type { ApiError, ValidationError, PaginationParams, PaginatedResponse } from './types'
