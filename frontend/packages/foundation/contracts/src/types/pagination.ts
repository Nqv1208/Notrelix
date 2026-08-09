/**
 * Pagination types for API responses
 */
export type PaginationParams = {
  page?: number;
  pageSize?: number;
};

export type PaginatedResponse<T> = {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
};
