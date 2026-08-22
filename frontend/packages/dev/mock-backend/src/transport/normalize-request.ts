/**
 * Normalizes the polymorphic fetch() input into a single structured request object.
 *
 * Plan: 04-TRANSPORT-PROTOCOL.md §MockFetch
 */

export interface NormalizedMockRequest {
  readonly method: string;
  readonly url: URL;
  readonly pathname: string;
  /** pathname with /api/v1 prefix stripped */
  readonly normalizedPathname: string;
  readonly searchParams: URLSearchParams;
  readonly headers: Headers;
  readonly bodyText: string | null;
  readonly jsonBody: unknown;
  readonly signal: AbortSignal | null;
}

const API_V1_PREFIX = "/api/v1";

export function normalizeMockRequest(
  input: RequestInfo | URL,
  init?: RequestInit,
): NormalizedMockRequest {
  const urlStr =
    typeof input === "string"
      ? input
      : input instanceof URL
        ? input.toString()
        : (input as Request).url;

  const method = (
    init?.method ?? (input instanceof Request ? input.method : "GET")
  ).toUpperCase();

  const url = new URL(urlStr, "http://localhost");
  const pathname = url.pathname;
  const normalizedPathname = pathname.startsWith(API_V1_PREFIX)
    ? pathname.slice(API_V1_PREFIX.length)
    : pathname;

  const headers =
    init?.headers instanceof Headers
      ? init.headers
      : new Headers((init?.headers as Record<string, string>) ?? {});

  let bodyText: string | null = null;
  let jsonBody: unknown = null;

  if (init?.body != null) {
    if (typeof init.body === "string") {
      bodyText = init.body;
    } else if (init.body instanceof URLSearchParams) {
      bodyText = init.body.toString();
    } else {
      try {
        bodyText = JSON.stringify(init.body);
      } catch {
        bodyText = String(init.body);
      }
    }

    if (bodyText) {
      try {
        jsonBody = JSON.parse(bodyText);
      } catch {
        jsonBody = null;
      }
    }
  }

  const signal =
    init?.signal ?? (input instanceof Request ? input.signal : null) ?? null;

  return {
    method,
    url,
    pathname,
    normalizedPathname,
    searchParams: url.searchParams,
    headers,
    bodyText,
    jsonBody,
    signal,
  };
}
