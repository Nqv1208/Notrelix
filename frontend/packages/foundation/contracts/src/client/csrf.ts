/**
 * Browser CSRF transport per ADR-005 (cross-origin bootstrap protocol).
 *
 * The client NEVER reads the API host cookie. The token is obtained from the
 * canonical bootstrap response body (GET auth/csrf), kept in instance-scoped
 * memory only, and echoed in the `X-CSRF-Token` header on every unsafe
 * browser request that relies on ambient cookie credentials.
 *
 * Legacy conventions (`XSRF-TOKEN`, `X-XSRF-TOKEN`, `<meta name="csrf-token">`,
 * localStorage/sessionStorage persistence) are removed and forbidden.
 */

export const CSRF_HEADER = "X-CSRF-Token";

export type CsrfBootstrapResponse = {
  token: string;
};

export interface CsrfProviderDeps {
  fetchImpl: typeof fetch;
  baseUrl: string;
  bootstrapPath: string;
  createCorrelationId: () => string;
}

export interface CsrfProvider {
  /**
   * Returns the in-memory CSRF token, bootstrapping exactly once when absent.
   * Concurrent callers share the same in-flight bootstrap promise.
   */
  ensureCsrfToken(): Promise<string>;
  /** Clears the in-memory token so the next unsafe request re-bootstraps. */
  clearToken(): void;
}

export function createCsrfProvider(deps: CsrfProviderDeps): CsrfProvider {
  let csrfToken: string | null = null;
  let csrfBootstrapPromise: Promise<string> | null = null;

  async function bootstrap(): Promise<string> {
    const correlationId = deps.createCorrelationId();

    const response = await deps.fetchImpl(
      `${deps.baseUrl}${deps.bootstrapPath}`,
      {
        method: "GET",
        credentials: "include",
        headers: { "X-Correlation-ID": correlationId },
      },
    );

    if (!response.ok) {
      throw new Error(
        `CSRF bootstrap failed with status ${response.status}.`,
      );
    }

    const body = (await response.json()) as Partial<CsrfBootstrapResponse>;

    if (!body || typeof body.token !== "string" || body.token.length === 0) {
      throw new Error("CSRF bootstrap returned an invalid response body.");
    }

    csrfToken = body.token;
    return csrfToken;
  }

  return {
    ensureCsrfToken(): Promise<string> {
      if (csrfToken !== null) {
        return Promise.resolve(csrfToken);
      }

      // Single-flight: concurrent unsafe requests share one bootstrap.
      if (csrfBootstrapPromise === null) {
        csrfBootstrapPromise = bootstrap().finally(() => {
          csrfBootstrapPromise = null;
        });
      }

      return csrfBootstrapPromise;
    },

    clearToken(): void {
      csrfToken = null;
      csrfBootstrapPromise = null;
    },
  };
}
