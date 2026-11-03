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
 * Plan: 01-FREEZE-SPEC.md §FZ-S13, 04-TRANSPORT-PROTOCOL.md
 */

import type { MockStore } from "../state/mock-store";
import { normalizeMockRequest } from "./normalize-request";
import { buildOperationRegistry } from "../operations/build-registry";
import { MockOperationRegistry } from "../operations/operation-registry";
import type { MockBackendConfig } from "../config/mock-config";
import {
  serverError,
  unauthorized,
  conflict,
  validationError,
  createMockResponse,
} from "./create-response";

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

async function applyLatency(
  latency: MockBackendConfig["latency"],
): Promise<void> {
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

// ─── Scripted write (design states) ──────────────────────────────────────────

/**
 * A browser test can script the next create of an item through `globalThis.__notrelixMockCreate`:
 * "hold" (the request waits until the flag is cleared, then goes through) or "fail-once" (the next
 * request fails like a lost connection and the flag clears). Dev only: the mock never ships.
 */
type CreateScriptHost = { __notrelixMockCreate?: "hold" | "fail-once" };

async function applyCreateScript(request: {
  method: string;
  normalizedPathname: string;
}): Promise<void> {
  if (
    request.method !== "POST" ||
    !request.normalizedPathname.endsWith("/items")
  )
    return;
  const host = globalThis as CreateScriptHost;
  if (host.__notrelixMockCreate === "fail-once") {
    delete host.__notrelixMockCreate;
    throw new TypeError("[MockFetch] Scripted network failure");
  }
  while (host.__notrelixMockCreate === "hold")
    await new Promise<void>((resolve) => setTimeout(resolve, 50));
}

// ─── Factory ─────────────────────────────────────────────────────────────────

export function createMockFetch(store: MockStore): typeof fetch {
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
      const refused = conflict("Simulated conflict");
      // The producer names who saved first on a refused stale view write (changedByName).
      return createMockResponse<object>(
        request.normalizedPathname.includes("/views")
          ? {
              ...refused,
              body: { ...refused.body, changedByName: "Hà Nguyễn" },
            }
          : refused,
      );
    }
    if (fault === "500") {
      return createMockResponse(serverError("Simulated server error"));
    }
    if (fault === "validation") {
      return createMockResponse(
        validationError({ _base: ["Simulated validation error"] }),
      );
    }

    await applyCreateScript(request);

    // ── Apply latency ───────────────────────────────────────────────────────
    await applyLatency(config.latency);

    // ── Dispatch to registry ────────────────────────────────────────────────
    return registry.dispatch(request, store);
  };
}
