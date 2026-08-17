/**
 * Converts MockHttpResult<T> into a real fetch Response.
 *
 * 204 → no body.
 * All other statuses → JSON body with Content-Type: application/json.
 *
 * Plan: 04-TRANSPORT-PROTOCOL.md §Responses, §Protocol fidelity
 */

export interface MockHttpResult<T = unknown> {
  readonly status: number;
  readonly headers?: HeadersInit;
  readonly body?: T;
}

export function createMockResponse<T>(result: MockHttpResult<T>): Response {
  if (result.status === 204) {
    return new Response(null, {
      status: 204,
      headers: result.headers,
    });
  }

  const bodyStr = result.body !== undefined ? JSON.stringify(result.body) : null;
  const contentType: Record<string, string> =
    bodyStr !== null ? { "Content-Type": "application/json" } : {};
  const headers: Record<string, string> = {
    ...contentType,
    ...((result.headers as Record<string, string>) ?? {}),
  };

  return new Response(bodyStr, { status: result.status, headers });
}

// ─── Convenience result builders ────────────────────────────────────────────

export function ok<T>(body: T, status = 200): MockHttpResult<T> {
  return { status, body };
}

export function created<T>(body: T): MockHttpResult<T> {
  return { status: 201, body };
}

export function noContent(): MockHttpResult<never> {
  return { status: 204 };
}

export function notFound(
  message = "Not found",
): MockHttpResult<{ message: string; code: string }> {
  return { status: 404, body: { message, code: "NOT_FOUND" } };
}

export function unauthorized(): MockHttpResult<{ message: string; code: string }> {
  return { status: 401, body: { message: "Unauthorized", code: "UNAUTHORIZED" } };
}

export function forbidden(): MockHttpResult<{ message: string; code: string }> {
  return { status: 403, body: { message: "Forbidden", code: "FORBIDDEN" } };
}

export function conflict(
  message = "Conflict",
): MockHttpResult<{ message: string; code: string }> {
  return { status: 409, body: { message, code: "CONFLICT" } };
}

export function validationError(
  fields: Record<string, string[]>,
): MockHttpResult<{ message: string; code: string; errors: Record<string, string[]> }> {
  return {
    status: 422,
    body: { message: "Validation failed", code: "VALIDATION_ERROR", errors: fields },
  };
}

export function serverError(
  message = "Internal Server Error",
): MockHttpResult<{ message: string; code: string }> {
  return { status: 500, body: { message, code: "SERVER_ERROR" } };
}
