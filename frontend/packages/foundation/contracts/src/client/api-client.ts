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

export interface NotrelixClientConfig {
  baseUrl: string
  onSessionExpired?: (error: AppError) => void
}

export async function apiFetch<TResponse>(
  baseUrl: string,
  url: string,
  options: RequestInit & ApiRequestOptions = {},
  onSessionExpired?: (error: AppError) => void,
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
      await refreshOnce(baseUrl, onSessionExpired)
      return await apiFetch<TResponse>(
        baseUrl,
        url,
        { ...options, skipAuthRefresh: true, correlationId },
        onSessionExpired,
        false
      )
    } catch (refreshError) {
      const authErr = refreshError instanceof AppError
        ? refreshError
        : new AppError({
            kind: "auth",
            message: "Session expired. Please sign in again.",
            status: 401,
            correlationId,
            cause: refreshError,
          })
      if (onSessionExpired) {
        onSessionExpired(authErr)
      }
      throw authErr
    }
  }

  // Handle 204 No Content
  if (response.status === 204) {
    return null as unknown as TResponse
  }

  // Parse JSON safely
  const text = await response.text()
  let data: unknown = null
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
    const validationErrors = kind === "validation"
      ? (((data as Record<string, unknown>)?.errors || (data as Record<string, unknown>)?.validationErrors) as Record<string, string[]> | undefined)
      : undefined

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

async function refreshOnce(baseUrl: string, onSessionExpired?: (error: AppError) => void): Promise<void> {
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
        const err = new AppError({
          kind: "auth",
          status: refreshResponse.status,
          message: "Session expired. Please sign in again.",
        })
        if (typeof window !== "undefined") {
          window.dispatchEvent(new CustomEvent("auth:failure"))
        }
        if (onSessionExpired) {
          onSessionExpired(err)
        }
        throw err
      }
    })().finally(() => {
      refreshPromise = null
    })
  }
  return refreshPromise
}

function extractErrorMessage(data: unknown): string {
  if (!data || typeof data !== "object") return "Request failed"
  const d = data as Record<string, unknown>
  if (typeof d.message === 'string') return d.message
  if (typeof d.detail === 'string') return d.detail
  if (Array.isArray(d.errors) && d.errors.length > 0) {
    return String(d.errors[0])
  }
  return "Request failed"
}

export function createNotrelixClient(config: NotrelixClientConfig) {
  const customBaseUrl = config.baseUrl
  const onSessionExpired = config.onSessionExpired

  return {
    api: {
      get<TResponse>(url: string, options?: ApiRequestOptions): Promise<TResponse> {
        return apiFetch<TResponse>(customBaseUrl, url, { method: "GET", ...options }, onSessionExpired)
      },
      post<TResponse, TBody = unknown>(url: string, body?: TBody, options?: ApiRequestOptions): Promise<TResponse> {
        const init: RequestInit & ApiRequestOptions = { method: "POST", ...options }
        if (body !== undefined) init.body = JSON.stringify(body)
        return apiFetch<TResponse>(customBaseUrl, url, init, onSessionExpired)
      },
      put<TResponse, TBody = unknown>(url: string, body?: TBody, options?: ApiRequestOptions): Promise<TResponse> {
        const init: RequestInit & ApiRequestOptions = { method: "PUT", ...options }
        if (body !== undefined) init.body = JSON.stringify(body)
        return apiFetch<TResponse>(customBaseUrl, url, init, onSessionExpired)
      },
      patch<TResponse, TBody = unknown>(url: string, body?: TBody, options?: ApiRequestOptions): Promise<TResponse> {
        const init: RequestInit & ApiRequestOptions = { method: "PATCH", ...options }
        if (body !== undefined) init.body = JSON.stringify(body)
        return apiFetch<TResponse>(customBaseUrl, url, init, onSessionExpired)
      },
      delete<TResponse>(url: string, options?: ApiRequestOptions): Promise<TResponse> {
        return apiFetch<TResponse>(customBaseUrl, url, { method: "DELETE", ...options }, onSessionExpired)
      },
    },
    endpoints,
  }
}

export type NotrelixClient = ReturnType<typeof createNotrelixClient>;
