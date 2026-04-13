export interface ApiResponse<T> {
  data: T
  message?: string
  success: boolean
}

export interface ApiError {
  message: string
  statusCode: number
}

export interface Pagination {
  page: number
  limit: number
  total: number
}

export interface ApiListResponse<T> {
  data: T[]
  pagination: Pagination
}