import { endpoints } from "../endpoints/endpoints";
import { AppError } from "@notrelix/kernel";
import { mapStatusToKind } from "@notrelix/kernel";
import { configureApiAssetOrigin } from "./asset-url";
import { createCsrfProvider, type CsrfProvider, CSRF_HEADER } from "./csrf";
import { generateCorrelationId } from "@notrelix/kernel";
import {
  createFetchUploadTransport,
  createXhrUploadTransport,
  type UploadProgress,
  type UploadTransport,
} from "./upload-transport";

export type { UploadProgress } from "./upload-transport";

/** A multipart upload: one idempotency key per user action, reused only to retry it. */
export type ApiUploadOptions = {
  /** The same key replays the stored result of the same action; omit it for an operation that takes none. */
  idempotencyKey?: string;
  /** `POST` by default; `PUT` for an operation that replaces a resource. */
  method?: "POST" | "PUT";
  signal?: AbortSignal;
  skipAuthRefresh?: boolean;
  correlationId?: string;
  onProgress?: (progress: UploadProgress) => void;
};

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
  /**
   * Builds the XMLHttpRequest an upload reports progress over. Without it the
   * browser's XMLHttpRequest is used, unless `fetchImpl` is injected (mock
   * backend): then uploads go through that fetch and report no progress.
   */
  createXhr?: () => XMLHttpRequest;
}

const UNSAFE_METHODS = new Set(["POST", "PUT", "PATCH", "DELETE"]);

const CSRF_PROBLEM_TYPE = "csrf-validation-failed";
const CSRF_ERROR_CODE = "security.csrf_validation_failed";

function isCsrfRejection(status: number, body: unknown): boolean {
  if (status !== 403 || !body || typeof body !== "object") return false;

  const problem = body as Record<string, unknown>;
  const type = typeof problem.type === "string" ? problem.type : "";
  const errorCode =
    typeof problem.errorCode === "string" ? problem.errorCode : "";

  return type.includes(CSRF_PROBLEM_TYPE) || errorCode === CSRF_ERROR_CODE;
}

export function createNotrelixClient(config: NotrelixClientConfig) {
  const customBaseUrl = config.baseUrl;
  configureApiAssetOrigin(customBaseUrl);
  const fetchImpl = config.fetchImpl ?? globalThis.fetch.bind(globalThis);
  const createCorrelationId =
    config.createCorrelationId ?? generateCorrelationId;
  const clock = config.clock ?? { now: () => new Date() };
  const onSessionExpired = config.onSessionExpired;

  // Instance-scoped CSRF transport (ADR-005): memory-only token, single-flight
  // bootstrap shared by every unsafe request of this client instance.
  const csrf: CsrfProvider = createCsrfProvider({
    fetchImpl,
    baseUrl: customBaseUrl,
    bootstrapPath: endpoints.auth.csrf,
    createCorrelationId,
  });

  /**
   * Shared CSRF-aware low-level request primitive.
   *
   * Every browser request path — ordinary API calls AND the single-flight
   * auth refresh — goes through this primitive so no path can bypass the
   * canonical CSRF transport (ADR-005 / IAREQ129).
   */
  async function csrfAwareFetch(
    url: string,
    init: RequestInit,
    correlationId: string,
  ): Promise<Response> {
    const headers: Record<string, string> = {
      "Content-Type": "application/json",
      "X-Correlation-ID": correlationId,
      ...(init.headers as Record<string, string> | undefined),
    };

    const method = (init.method ?? "GET").toUpperCase();

    if (UNSAFE_METHODS.has(method)) {
      // Bootstrap is single-flight and memory-cached; safe GETs never trigger it.
      const token = await csrf.ensureCsrfToken();
      headers[CSRF_HEADER] = token;
    }

    return fetchImpl(url, {
      credentials: "include",
      ...init,
      headers,
    });
  }

  const uploadTransport: UploadTransport = (() => {
    if (config.createXhr) return createXhrUploadTransport(config.createXhr);
    if (!config.fetchImpl && typeof XMLHttpRequest !== "undefined") {
      return createXhrUploadTransport(() => new XMLHttpRequest());
    }
    return createFetchUploadTransport(fetchImpl);
  })();

  // Instance-scoped single-flight refresh promise closure
  let refreshPromise: Promise<void> | null = null;

  async function refreshOnce(): Promise<void> {
    if (!refreshPromise) {
      refreshPromise = (async () => {
        const correlationId = createCorrelationId();
        let refreshResponse: Response;
        try {
          refreshResponse = await csrfAwareFetch(
            `${customBaseUrl}${endpoints.auth.refresh}`,
            { method: "POST" },
            correlationId,
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

  async function parseBody(
    response: Response,
    correlationId: string,
  ): Promise<unknown> {
    const text = await response.text();
    if (!text) return null;

    try {
      return JSON.parse(text);
    } catch (parseError) {
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

  const MUTATING_METHODS = new Set(["POST", "PUT", "PATCH", "DELETE"]);

  async function apiFetch<TResponse>(
    url: string,
    options: RequestInit & ApiRequestOptions = {},
    retryAfterRefresh = true,
    retryAfterCsrfRecovery = true,
  ): Promise<TResponse> {
    const correlationId = options.correlationId || createCorrelationId();

    // A write names itself with an Idempotency-Key (the producer requires one on
    // its idempotent operations). The key is made once and travels with the
    // retries below, so a retry replays the write instead of repeating it.
    const namedByCaller =
      Boolean(options.idempotencyKey) ||
      Object.keys((options.headers as Record<string, string>) ?? {}).some(
        (name) => name.toLowerCase() === "idempotency-key",
      );
    if (!namedByCaller && MUTATING_METHODS.has(options.method ?? "")) {
      options = { ...options, idempotencyKey: crypto.randomUUID() };
    }

    const init: RequestInit & ApiRequestOptions = { ...options };
    delete (init as { correlationId?: string }).correlationId;

    // Normalize to a plain record so header merging stays type-safe
    // regardless of the HeadersInit shape the caller provided.
    const headers: Record<string, string> = {
      ...(init.headers as Record<string, string> | undefined),
    };

    if (options.idempotencyKey) {
      headers["Idempotency-Key"] = options.idempotencyKey;
    }

    init.headers = headers;

    let response: Response;
    try {
      response = await csrfAwareFetch(
        `${customBaseUrl}${url}`,
        init,
        correlationId,
      );
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
      retryAfterRefresh &&
      !options.skipAuthRefresh &&
      url !== endpoints.auth.refresh &&
      url !== endpoints.auth.login &&
      url !== endpoints.auth.register
    ) {
      await refreshOnce();
      // Single retry attempt after refresh
      return await apiFetch<TResponse>(
        url,
        { ...options, skipAuthRefresh: true },
        false,
        retryAfterCsrfRecovery,
      );
    }

    // Handle 204 No Content
    if (response.status === 204) {
      return null as unknown as TResponse;
    }

    const data = await parseBody(response, correlationId);

    // Bounded CSRF stale-token recovery (ADR-005 policy):
    // clear memory token → re-bootstrap once → retry the original unsafe
    // request exactly once. The retry flag guarantees no loop.
    if (isCsrfRejection(response.status, data) && retryAfterCsrfRecovery) {
      csrf.clearToken();
      await csrf.ensureCsrfToken();

      return await apiFetch<TResponse>(
        url,
        { ...options },
        retryAfterRefresh,
        false,
      );
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
      const problemCode = (data as Record<string, unknown> | null)?.errorCode;
      throw new AppError({
        kind,
        status: response.status,
        ...(typeof problemCode === "string" ? { code: problemCode } : {}),
        message,
        details: data,
        validationErrors,
        correlationId,
      });
    }

    return (data ?? null) as TResponse;
  }

  function parseProblem(bodyText: string): unknown {
    if (!bodyText) return null;
    try {
      return JSON.parse(bodyText);
    } catch {
      return null;
    }
  }

  /**
   * POST (or PUT) multipart/form-data. Same CSRF, auth-refresh and correlation rules as
   * `apiFetch`; the body is a FormData so no JSON Content-Type is sent (the
   * transport adds the multipart boundary). Retries re-send the same FormData.
   */
  async function apiUpload<TResponse>(
    url: string,
    form: FormData,
    options: ApiUploadOptions,
    retryAfterRefresh = true,
    retryAfterCsrfRecovery = true,
  ): Promise<TResponse> {
    const correlationId = options.correlationId || createCorrelationId();
    const token = await csrf.ensureCsrfToken();
    const headers: Record<string, string> = {
      "X-Correlation-ID": correlationId,
      ...(options.idempotencyKey
        ? { "Idempotency-Key": options.idempotencyKey }
        : {}),
      [CSRF_HEADER]: token,
    };

    let raw;
    try {
      raw = await uploadTransport({
        url: `${customBaseUrl}${url}`,
        method: options.method ?? "POST",
        form,
        headers,
        signal: options.signal,
        onProgress: options.onProgress,
      });
    } catch (error) {
      const isAbort = (error as { name?: string }).name === "AbortError";
      throw new AppError({
        kind: isAbort ? "aborted" : "network",
        message: isAbort
          ? "Request was aborted."
          : "Network error. Please check your internet connection.",
        correlationId,
        cause: error,
      });
    }

    if (raw.status === 401 && retryAfterRefresh && !options.skipAuthRefresh) {
      await refreshOnce();
      return apiUpload<TResponse>(
        url,
        form,
        { ...options, skipAuthRefresh: true },
        false,
        retryAfterCsrfRecovery,
      );
    }

    const data = parseProblem(raw.bodyText);

    if (isCsrfRejection(raw.status, data) && retryAfterCsrfRecovery) {
      csrf.clearToken();
      await csrf.ensureCsrfToken();
      return apiUpload<TResponse>(url, form, options, retryAfterRefresh, false);
    }

    if (raw.status < 200 || raw.status >= 300) {
      const problem = (data ?? {}) as Record<string, unknown>;
      throw new AppError({
        kind: mapStatusToKind(raw.status),
        status: raw.status,
        ...(typeof problem.errorCode === "string"
          ? { code: problem.errorCode }
          : {}),
        message: extractErrorMessage(data),
        details: data,
        correlationId,
      });
    }

    return (data ?? null) as TResponse;
  }

  /** GET a binary body (a file) with the session; the caller saves the Blob. */
  async function apiGetBlob(
    url: string,
    options: ApiRequestOptions = {},
    retryAfterRefresh = true,
  ): Promise<Blob> {
    const correlationId = options.correlationId || createCorrelationId();
    let response: Response;
    try {
      response = await csrfAwareFetch(
        `${customBaseUrl}${url}`,
        {
          method: "GET",
          ...(options.signal ? { signal: options.signal } : {}),
        },
        correlationId,
      );
    } catch (error) {
      const isAbort = (error as { name?: string }).name === "AbortError";
      throw new AppError({
        kind: isAbort ? "aborted" : "network",
        message: isAbort
          ? "Request was aborted."
          : "Network error. Please check your internet connection.",
        correlationId,
        cause: error,
      });
    }
    if (
      response.status === 401 &&
      retryAfterRefresh &&
      !options.skipAuthRefresh
    ) {
      await refreshOnce();
      return apiGetBlob(url, { ...options, skipAuthRefresh: true }, false);
    }
    if (!response.ok) {
      const data = parseProblem(await response.text());
      const problem = (data ?? {}) as Record<string, unknown>;
      throw new AppError({
        kind: mapStatusToKind(response.status),
        status: response.status,
        ...(typeof problem.errorCode === "string"
          ? { code: problem.errorCode }
          : {}),
        message: extractErrorMessage(data),
        details: data,
        correlationId,
      });
    }
    return response.blob();
  }

  return {
    api: {
      upload<TResponse>(
        url: string,
        form: FormData,
        options: ApiUploadOptions,
      ): Promise<TResponse> {
        return apiUpload<TResponse>(url, form, options);
      },
      getBlob(url: string, options?: ApiRequestOptions): Promise<Blob> {
        return apiGetBlob(url, options);
      },
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
