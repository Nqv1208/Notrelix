import { endpoints } from "../endpoints/endpoints";
import { AppError } from "@notrelix/kernel";
import { mapStatusToKind } from "@notrelix/kernel";
import { getCsrfToken } from "./csrf";
import { generateCorrelationId } from "@notrelix/kernel";

export type ApiRequestOptions = {
  signal?: AbortSignal;
  headers?: Record<string, string>;
  skipAuthRefresh?: boolean;
  skipGlobalErrorToast?: boolean;
  correlationId?: string;
  idempotencyKey?: string;
};

export interface SessionExpiredEvent {
  readonly eventId: string;
  readonly occurredAt: string;
  readonly reason:
    "refresh-rejected" | "refresh-network-failure" | "session-revoked";
  readonly error: AppError;
}

export interface NotrelixClientConfig {
  baseUrl: string;
  fetchImpl?: typeof fetch;
  createCorrelationId?: () => string;
  clock?: { now(): Date };
  onSessionExpired?: (event: SessionExpiredEvent) => void;
}

export function createNotrelixClient(config: NotrelixClientConfig) {
  const customBaseUrl = config.baseUrl;
  const fetchImpl = config.fetchImpl ?? globalThis.fetch.bind(globalThis);
  const createCorrelationId =
    config.createCorrelationId ?? generateCorrelationId;
  const clock = config.clock ?? { now: () => new Date() };
  const onSessionExpired = config.onSessionExpired;

  // Instance-scoped single-flight refresh promise closure
  let refreshPromise: Promise<void> | null = null;

  async function refreshOnce(): Promise<void> {
    if (!refreshPromise) {
      refreshPromise = (async () => {
        const correlationId = createCorrelationId();
        let refreshResponse: Response;
        try {
          refreshResponse = await fetchImpl(
            `${customBaseUrl}${endpoints.auth.refresh}`,
            {
              method: "POST",
              credentials: "include",
              headers: {
                "Content-Type": "application/json",
                "X-Correlation-ID": correlationId,
              },
            },
          );
        } catch (netErr) {
          const appErr = new AppError({
            kind: "network",
            message: "Network error during auth refresh.",
            correlationId,
            cause: netErr,
          });
          if (onSessionExpired) {
            onSessionExpired({
              eventId: createCorrelationId(),
              occurredAt: clock.now().toISOString(),
              reason: "refresh-network-failure",
              error: appErr,
            });
          }
          throw appErr;
        }

        if (!refreshResponse.ok) {
          const appErr = new AppError({
            kind: "auth",
            status: refreshResponse.status,
            message: "Session expired. Please sign in again.",
            correlationId,
          });
          if (onSessionExpired) {
            onSessionExpired({
              eventId: createCorrelationId(),
              occurredAt: clock.now().toISOString(),
              reason: "refresh-rejected",
              error: appErr,
            });
          }
          throw appErr;
        }
      })().finally(() => {
        refreshPromise = null;
      });
    }
    return refreshPromise;
  }

  async function apiFetch<TResponse>(
    url: string,
    options: RequestInit & ApiRequestOptions = {},
    retry = true,
  ): Promise<TResponse> {
    const correlationId = options.correlationId || createCorrelationId();

    const headers: HeadersInit = {
      "Content-Type": "application/json",
      "X-Correlation-ID": correlationId,
      ...(options.headers as Record<string, string>),
    };

    if (options.idempotencyKey) {
      headers["Idempotency-Key"] = options.idempotencyKey;
    }

    const csrfToken = getCsrfToken();
    if (
      csrfToken &&
      options.method &&
      ["POST", "PUT", "PATCH", "DELETE"].includes(options.method.toUpperCase())
    ) {
      headers["X-XSRF-TOKEN"] = csrfToken;
    }

    let response: Response;
    try {
      response = await fetchImpl(`${customBaseUrl}${url}`, {
        credentials: "include",
        ...options,
        headers,
      });
    } catch (error) {
      const isAbort = (error as { name?: string }).name === "AbortError";
      console.log("FAILED URL:", url, options.method);
      throw new AppError({
        kind: isAbort ? "aborted" : "network",
        message: isAbort
          ? "Request was aborted."
          : "Network error. Please check your internet connection.",
        correlationId,
        cause: error,
      });
    }

    // Handle 401 Unauthorized (Single-flight auth refresh)
    if (
      response.status === 401 &&
      retry &&
      !options.skipAuthRefresh &&
      url !== endpoints.auth.refresh &&
      url !== endpoints.auth.login &&
      url !== endpoints.auth.register
    ) {
      await refreshOnce();
      // Single retry attempt after refresh
      return await apiFetch<TResponse>(
        url,
        { ...options, skipAuthRefresh: true, correlationId },
        false,
      );
    }

    // Handle 204 No Content
    if (response.status === 204) {
      return null as unknown as TResponse;
    }

    // Parse JSON safely
    const text = await response.text();
    let data: unknown = null;
    if (text) {
      try {
        data = JSON.parse(text);
      } catch (parseError) {
        if (!response.ok) {
          console.log("FAILED URL:", url, options.method);
          throw new AppError({
            kind: mapStatusToKind(response.status),
            status: response.status,
            message: text || "Request failed",
            correlationId,
          });
        }
        console.log("FAILED URL:", url, options.method);
        throw new AppError({
          kind: "server",
          status: response.status,
          code: "parse_error",
          message: "Failed to parse server response.",
          correlationId,
          cause: parseError,
        });
      }
    }

    // Handle non-2xx responses
    if (!response.ok) {
      const message = extractErrorMessage(data);
      const kind = mapStatusToKind(response.status);
      const validationErrors =
        kind === "validation"
          ? (((data as Record<string, unknown>)?.errors ||
              (data as Record<string, unknown>)?.validationErrors) as
              Record<string, string[]> | undefined)
          : undefined;

      console.log("FAILED URL:", url, options.method);
      throw new AppError({
        kind,
        status: response.status,
        message,
        details: data,
        validationErrors,
        correlationId,
      });
    }

    return (data ?? null) as TResponse;
  }

  return {
    api: {
      get<TResponse>(
        url: string,
        options?: ApiRequestOptions,
      ): Promise<TResponse> {
        return apiFetch<TResponse>(url, { method: "GET", ...options });
      },
      post<TResponse, TBody = unknown>(
        url: string,
        body?: TBody,
        options?: ApiRequestOptions,
      ): Promise<TResponse> {
        const init: RequestInit & ApiRequestOptions = {
          method: "POST",
          ...options,
        };
        if (body !== undefined) init.body = JSON.stringify(body);
        return apiFetch<TResponse>(url, init);
      },
      put<TResponse, TBody = unknown>(
        url: string,
        body?: TBody,
        options?: ApiRequestOptions,
      ): Promise<TResponse> {
        const init: RequestInit & ApiRequestOptions = {
          method: "PUT",
          ...options,
        };
        if (body !== undefined) init.body = JSON.stringify(body);
        return apiFetch<TResponse>(url, init);
      },
      patch<TResponse, TBody = unknown>(
        url: string,
        body?: TBody,
        options?: ApiRequestOptions,
      ): Promise<TResponse> {
        const init: RequestInit & ApiRequestOptions = {
          method: "PATCH",
          ...options,
        };
        if (body !== undefined) init.body = JSON.stringify(body);
        return apiFetch<TResponse>(url, init);
      },
      delete<TResponse>(
        url: string,
        options?: ApiRequestOptions,
      ): Promise<TResponse> {
        return apiFetch<TResponse>(url, { method: "DELETE", ...options });
      },
    },
    endpoints,
  };
}

function extractErrorMessage(data: unknown): string {
  if (!data || typeof data !== "object") return "Request failed";
  const d = data as Record<string, unknown>;
  if (typeof d.message === "string") return d.message;
  if (typeof d.detail === "string") return d.detail;
  if (Array.isArray(d.errors) && d.errors.length > 0) {
    return String(d.errors[0]);
  }
  return "Request failed";
}

export type NotrelixClient = ReturnType<typeof createNotrelixClient>;
