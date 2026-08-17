/**
 * createMockFetch — production API transport seam for development.
 *
 * Architecture:
 *   1. Normalize polymorphic fetch() input (NormalizedMockRequest)
 *   2. Apply fault profile for the operation (config.faultProfile)
 *   3. Apply simulated network latency (config.latency)
 *   4. Dispatch to typed operation registry → context handler → MockHttpResult
 *   5. Convert MockHttpResult → real fetch Response
 *
 * The old monolithic if/else chain has been replaced by the typed operation registry.
 * MockUnhandledOperationError is re-exported for backwards-compat.
 *
 * Plan: 04-TRANSPORT-PROTOCOL.md §MockFetch, §Latency, §Fault profiles
 *       06-HANDLERS-PROJECTIONS.md §Context layout
 */

import { globalMockStore, MockStore } from "../state/mock-store";
import { normalizeMockRequest } from "./normalize-request";
import { MockUnhandledOperationError } from "./route-matcher";
import { buildOperationRegistry } from "../operations/build-registry";
import { MockOperationRegistry } from "../operations/operation-registry";
import type { MockBackendConfig } from "../config/mock-config";
import { serverError, unauthorized, conflict, validationError, createMockResponse } from "./create-response";

// Re-export for backwards-compat with any external consumers
export { MockUnhandledOperationError } from "./route-matcher";

// ─── Singleton registry (built once per process) ──────────────────────────────
let _registry: MockOperationRegistry | null = null;

function getRegistry(): MockOperationRegistry {
  if (!_registry) {
    _registry = buildOperationRegistry();
  }
  return _registry;
}

// ─── Latency simulation ───────────────────────────────────────────────────────

async function applyLatency(latency: MockBackendConfig["latency"]): Promise<void> {
  const delayMs: Record<string, number> = {
    instant: 0,
    fast: 80,
    normal: 350,
    slow: 1400,
  };
  const delay = delayMs[latency] ?? 0;
  if (delay > 0) {
    await new Promise<void>((resolve) => setTimeout(resolve, delay));
  }
}

// ─── Factory ─────────────────────────────────────────────────────────────────

export function createMockFetch(store: MockStore = globalMockStore): typeof fetch {
  const registry = getRegistry();

  return async function mockFetch(
    input: RequestInfo | URL,
    init?: RequestInit,
  ): Promise<Response> {
    const request = normalizeMockRequest(input, init);
    const config = store.getConfig();

    // ── Apply fault profile ─────────────────────────────────────────────────
    const fault =
      config.faultProfile[request.normalizedPathname] ??
      config.faultProfile["*"];

    if (fault === "network") {
      throw new TypeError("[MockFetch] Simulated network failure");
    }
    if (fault === "401") {
      return createMockResponse(unauthorized());
    }
    if (fault === "409") {
      return createMockResponse(conflict("Simulated conflict"));
    }
    if (fault === "500") {
      return createMockResponse(serverError("Simulated server error"));
    }
    if (fault === "validation") {
      return createMockResponse(
        validationError({ _base: ["Simulated validation error"] }),
      );
    }

    // ── Apply latency ───────────────────────────────────────────────────────
    await applyLatency(config.latency);

    // ── Dispatch to registry ────────────────────────────────────────────────
    return registry.dispatch(request, store);
  };
}

export const mockFetch: typeof fetch = createMockFetch();
