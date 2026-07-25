export { apiFetch, createNotrelixClient, type ApiRequestOptions, type NotrelixClient } from './client'
/** @deprecated Use createNotrelixClient via AppRuntime instead */
export { api, configureApi } from './client'
export { getCsrfToken } from './client'
export { endpoints } from './endpoints'
export type { ApiError, ValidationError, PaginationParams, PaginatedResponse } from './types'
