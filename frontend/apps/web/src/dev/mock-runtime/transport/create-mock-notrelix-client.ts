import { endpoints, type ApiRequestOptions, type NotrelixClient } from "@notrelix/contracts";
import { AppError } from "@notrelix/kernel";
import type { MockRuntimeConfig } from "../config/mock-runtime-config";
import type { MockStore } from "../state/mock-store";
import type { MockHandler } from "./mock-handler";
import { createMockHandlerRegistry } from "./mock-handler-registry";
import type { MockRequest } from "./mock-request";
import type { MockRequestJournal } from "./mock-request-journal";
import { throwConfiguredMockError } from "../errors/mock-error-policy";

async function waitForLatency(ms: number, signal?: AbortSignal): Promise<void> {
  if (signal?.aborted) throw new AppError({ kind: "aborted", message: "Request was aborted." });
  if (ms === 0) return;
  await new Promise<void>((resolve, reject) => {
    const timeout = setTimeout(resolve, ms);
    signal?.addEventListener("abort", () => {
      clearTimeout(timeout);
      reject(new AppError({ kind: "aborted", message: "Request was aborted." }));
    }, { once: true });
  });
}

export function createMockNotrelixClient(input: {
  readonly config: MockRuntimeConfig;
  readonly store: MockStore;
  readonly handlers: readonly MockHandler[];
  readonly journal: MockRequestJournal;
  readonly now: () => Date;
}): NotrelixClient {
  const registry = createMockHandlerRegistry(input.handlers);
  const dispatch = async <T>(request: MockRequest): Promise<T> => {
    await waitForLatency(input.config.latencyMs, request.options?.signal);
    throwConfiguredMockError(input.config.scenario, request);
    return registry.dispatch(request, {
      store: input.store,
      journal: input.journal,
      now: input.now,
    }) as Promise<T>;
  };

  return {
    endpoints,
    api: {
      get: <T>(url: string, options?: ApiRequestOptions) => dispatch<T>({ method: "GET", url, options }),
      post: <T, TBody = unknown>(url: string, body?: TBody, options?: ApiRequestOptions) => dispatch<T>({ method: "POST", url, body, options }),
      put: <T, TBody = unknown>(url: string, body?: TBody, options?: ApiRequestOptions) => dispatch<T>({ method: "PUT", url, body, options }),
      patch: <T, TBody = unknown>(url: string, body?: TBody, options?: ApiRequestOptions) => dispatch<T>({ method: "PATCH", url, body, options }),
      delete: <T>(url: string, options?: ApiRequestOptions) => dispatch<T>({ method: "DELETE", url, options }),
    },
  };
}
