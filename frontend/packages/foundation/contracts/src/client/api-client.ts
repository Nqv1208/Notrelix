import { endpoints } from "../endpoints/endpoints"
import { AppError } from "@notrelix/kernel"
import { mapStatusToKind } from "@notrelix/kernel"
import { getCsrfToken } from "./csrf"
import { generateCorrelationId } from "@notrelix/kernel"

export type ApiRequestOptions = {
  signal?: AbortSignal
  headers?: Record<string, string>
  skipAuthRefresh?: boolean
  skipGlobalErrorToast?: boolean
  correlationId?: string
}

let activeBaseUrl = "/api/v1";

export function configureApi(baseUrl: string) {
  activeBaseUrl = baseUrl;
}

export async function apiFetch<TResponse>(
  baseUrl: string,
  url: string,
  options: RequestInit & ApiRequestOptions = {},
  retry = true
): Promise<TResponse> {
  const correlationId = options.correlationId || generateCorrelationId()

  const headers: HeadersInit = {
    "Content-Type": "application/json",
    "X-Correlation-ID": correlationId,
    ...(options.headers as Record<string, string>),
  }

  // Attach CSRF token if available for unsafe methods
  const csrfToken = getCsrfToken()
  if (csrfToken && options.method && ["POST", "PUT", "PATCH", "DELETE"].includes(options.method.toUpperCase())) {
    headers["X-XSRF-TOKEN"] = csrfToken
  }

  let response: Response
  try {
    response = await fetch(`${baseUrl}${url}`, {
      credentials: "include",
      ...options,
      headers,
    })
  } catch (error) {
    const isAbort = (error as { name?: string }).name === "AbortError"
    throw new AppError({
      kind: isAbort ? "aborted" : "network",
      message: isAbort ? "Request was aborted." : "Network error. Please check your internet connection.",
      correlationId,
      cause: error,
    })
  }

  // Handle 401 Unauthorized (Auth Refresh)
  if (
    response.status === 401 &&
    retry &&
    !options.skipAuthRefresh &&
    url !== endpoints.auth.refresh &&
    url !== endpoints.auth.login &&
    url !== endpoints.auth.register
  ) {
    try {
      await refreshOnce(baseUrl)
      // Retry original request with same correlationId and skipAuthRefresh = true
      return await apiFetch<TResponse>(
        baseUrl,
        url,
        { ...options, skipAuthRefresh: true, correlationId },
        false
      )
    } catch (refreshError) {
      if (refreshError instanceof AppError) {
        throw refreshError
      }
      throw new AppError({
        kind: "auth",
        message: "Session expired. Please sign in again.",
        status: 401,
        correlationId,
        cause: refreshError,
      })
    }
  }

  // Handle 204 No Content
  if (response.status === 204) {
    return null as unknown as TResponse
  }

  // Parse JSON safely
  const text = await response.text()
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let data: any = null
  if (text) {
    try {
      data = JSON.parse(text)
    } catch (parseError) {
      if (!response.ok) {
        throw new AppError({
          kind: mapStatusToKind(response.status),
          status: response.status,
          message: text || "Request failed",
          correlationId,
        })
      }
      throw new AppError({
        kind: "server",
        status: response.status,
        code: "parse_error",
        message: "Failed to parse server response.",
        correlationId,
        cause: parseError,
      })
    }
  }

  // Handle non-2xx responses
  if (!response.ok) {
    const message = extractErrorMessage(data)
    const kind = mapStatusToKind(response.status)
    const validationErrors = kind === "validation" ? (data?.errors || data?.validationErrors) : undefined

    throw new AppError({
      kind,
      status: response.status,
      message,
      details: data,
      validationErrors,
      correlationId,
    })
  }

  return (data ?? null) as TResponse
}

let refreshPromise: Promise<void> | null = null

async function refreshOnce(baseUrl: string): Promise<void> {
  if (!refreshPromise) {
    refreshPromise = (async () => {
      const refreshResponse = await fetch(`${baseUrl}${endpoints.auth.refresh}`, {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
      })

      if (!refreshResponse.ok) {
        if (typeof window !== "undefined") {
          window.dispatchEvent(new CustomEvent("auth:failure"))
        }
        throw new AppError({
          kind: "auth",
          status: refreshResponse.status,
          message: "Session expired. Please sign in again.",
        })
      }
    })().finally(() => {
      refreshPromise = null
    })
  }
  return refreshPromise;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function extractErrorMessage(data: any): string {
  if (!data || typeof data !== "object") return "Request failed"
  if (data.message) return data.message
  if (data.detail) return data.detail
  if (Array.isArray(data.errors) && data.errors.length > 0) {
    return String(data.errors[0])
  }
  return "Request failed"
}

export const api = {
  get<TResponse>(url: string, options?: ApiRequestOptions): Promise<TResponse> {
    return apiFetch<TResponse>(activeBaseUrl, url, { method: "GET", ...options })
  },

  post<TResponse, TBody = unknown>(url: string, body?: TBody, options?: ApiRequestOptions): Promise<TResponse> {
    const init: RequestInit & ApiRequestOptions = {
      method: "POST",
      ...options,
    }
    if (body !== undefined) {
      init.body = JSON.stringify(body)
    }
    return apiFetch<TResponse>(activeBaseUrl, url, init)
  },

  put<TResponse, TBody = unknown>(url: string, body?: TBody, options?: ApiRequestOptions): Promise<TResponse> {
    const init: RequestInit & ApiRequestOptions = {
      method: "PUT",
      ...options,
    }
    if (body !== undefined) {
      init.body = JSON.stringify(body)
    }
    return apiFetch<TResponse>(activeBaseUrl, url, init)
  },

  patch<TResponse, TBody = unknown>(url: string, body?: TBody, options?: ApiRequestOptions): Promise<TResponse> {
    const init: RequestInit & ApiRequestOptions = {
      method: "PATCH",
      ...options,
    }
    if (body !== undefined) {
      init.body = JSON.stringify(body)
    }
    return apiFetch<TResponse>(activeBaseUrl, url, init)
  },

  delete<TResponse>(url: string, options?: ApiRequestOptions): Promise<TResponse> {
    return apiFetch<TResponse>(activeBaseUrl, url, { method: "DELETE", ...options })
  },
}

export function createNotrelixClient(config: { baseUrl: string }) {
  const customBaseUrl = config.baseUrl;
  return {
    api: {
      get<TResponse>(url: string, options?: ApiRequestOptions): Promise<TResponse> {
        return apiFetch<TResponse>(customBaseUrl, url, { method: "GET", ...options })
      },
      post<TResponse, TBody = unknown>(url: string, body?: TBody, options?: ApiRequestOptions): Promise<TResponse> {
        const init: RequestInit & ApiRequestOptions = { method: "POST", ...options }
        if (body !== undefined) init.body = JSON.stringify(body)
        return apiFetch<TResponse>(customBaseUrl, url, init)
      },
      put<TResponse, TBody = unknown>(url: string, body?: TBody, options?: ApiRequestOptions): Promise<TResponse> {
        const init: RequestInit & ApiRequestOptions = { method: "PUT", ...options }
        if (body !== undefined) init.body = JSON.stringify(body)
        return apiFetch<TResponse>(customBaseUrl, url, init)
      },
      patch<TResponse, TBody = unknown>(url: string, body?: TBody, options?: ApiRequestOptions): Promise<TResponse> {
        const init: RequestInit & ApiRequestOptions = { method: "PATCH", ...options }
        if (body !== undefined) init.body = JSON.stringify(body)
        return apiFetch<TResponse>(customBaseUrl, url, init)
      },
      delete<TResponse>(url: string, options?: ApiRequestOptions): Promise<TResponse> {
        return apiFetch<TResponse>(customBaseUrl, url, { method: "DELETE", ...options })
      },
    },
    endpoints,
  }
}

export type NotrelixClient = ReturnType<typeof createNotrelixClient>;
