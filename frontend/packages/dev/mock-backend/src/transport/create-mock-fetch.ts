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
import { injectIncomingMessage } from "../contexts/chat/chat-writes";
import {
  actAsOther,
  liveCallOf,
  missCall,
  ringAsOther,
} from "../contexts/chat/chat-calls";
import {
  notFound,
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

/**
 * A browser test can have another person delete items through `globalThis.__notrelixMockGone` (item ids):
 * the next board read finds them gone (BC-Bulk `bk-gone`). Dev only: the mock never ships.
 */
type GoneScriptHost = { __notrelixMockGone?: string[] };

function applyGoneScript(store: MockStore, request: { method: string }): void {
  const host = globalThis as GoneScriptHost;
  if (request.method !== "GET" || !host.__notrelixMockGone?.length) return;
  const ids = host.__notrelixMockGone;
  delete host.__notrelixMockGone;
  for (const id of ids) store.deleteCard(id);
}

/**
 * Another person saved the form first: question 3 gets a new label, a sixth question is added, questions 2 and
 * 4 swap places. The form's version moves on, so the caller's next write is stale.
 */
function editFormFirst(store: MockStore, formId: string): boolean {
  const form = store.forms.find(formId);
  const [, second, third, fourth] = form?.questions ?? [];
  if (!form || !second || !third || !fourth) return false;
  const secondPosition = second.position;
  second.position = fourth.position;
  fourth.position = secondPosition;
  third.label = `${third.label} cao`;
  form.questions.push({
    ...third,
    placeholder: null,
    helpText: null,
    maxLength: null,
    minValue: null,
    maxValue: null,
    id: "mock-fq-first-phone",
    questionKey: "phone",
    label: "Số điện thoại",
    questionType: "ShortText",
    isRequired: false,
    boardFieldId: null,
    position: "a9",
    options: null,
  });
  form.questions.sort((a, b) => (a.position < b.position ? -1 : 1));
  form.version += 1;
  return true;
}

// ─── Factory ─────────────────────────────────────────────────────────────────

export function createMockFetch(store: MockStore): typeof fetch {
  const registry = getRegistry();
  // Dev only: a browser check makes another member write a Chat message (realtime frames included).
  (
    globalThis as {
      __notrelixMockInjectChat?: (
        spaceId: string,
        authorId: string,
        content: string,
      ) => boolean;
    }
  ).__notrelixMockInjectChat = (spaceId, authorId, content) =>
    injectIncomingMessage(store.chat, spaceId, authorId, content) !== null;

  // Dev only: a browser check plays the other person of a call (ring, answer, decline, cancel, end, ring out).
  (
    globalThis as {
      __notrelixMockCall?: {
        ring: (
          spaceId: string,
          personId: string,
          kind: "voice" | "video",
        ) => string | null;
        act: (
          callId: string,
          personId: string,
          action: "answer" | "decline" | "cancel" | "end" | "join" | "leave",
        ) => boolean;
        miss: (callId: string) => boolean;
        live: (spaceId: string) => string | null;
        peer: (spaceId: string) => string | null;
      };
    }
  ).__notrelixMockCall = {
    ring: (spaceId, personId, kind) =>
      ringAsOther(store, spaceId, personId, kind),
    act: (callId, personId, action) =>
      actAsOther(store, callId, personId, action),
    miss: (callId) => missCall(store, callId),
    live: (spaceId) => liveCallOf(store.chat, spaceId)?.id ?? null,
    peer: (spaceId) =>
      store.chat
        .membershipsOfSpace(spaceId)
        .find(
          (row) =>
            row.leftAt === null && row.userId !== store.getCurrentUser().id,
        )?.userId ?? null,
  };

  // Dev only: a browser check has another person save a form first (WM-FormConflict "Xem bản mới").
  (
    globalThis as {
      __notrelixMockEditFormFirst?: (formId: string) => boolean;
    }
  ).__notrelixMockEditFormFirst = (formId) => editFormFirst(store, formId);

  return async function mockFetch(
    input: RequestInfo | URL,
    init?: RequestInit,
  ): Promise<Response> {
    const request = normalizeMockRequest(input, init);
    const config = store.getConfig();

    // ── Apply fault profile ─────────────────────────────────────────────────
    // A key "PUT /preferences" fails only that method on any path that contains the text.
    const methodFault = Object.entries(config.faultProfile).find(
      ([key]) =>
        key.includes(" ") &&
        key.split(" ")[0] === request.method &&
        request.normalizedPathname.includes(key.slice(key.indexOf(" ") + 1)),
    )?.[1];
    const fault =
      config.faultProfile[request.normalizedPathname] ??
      methodFault ??
      config.faultProfile["*"];

    if (fault === "network") {
      throw new TypeError("[MockFetch] Simulated network failure");
    }
    if (fault === "401") {
      return createMockResponse(unauthorized());
    }
    if (fault === "409") {
      const refused = conflict("Simulated conflict");
      // The producer names who saved first on a refused stale view, saved-filter or field value write (changedByName).
      return createMockResponse<object>(
        request.normalizedPathname.includes("/views") ||
          request.normalizedPathname.includes("/saved-filters") ||
          request.normalizedPathname.includes("/forms/") ||
          request.normalizedPathname.includes("/values/")
          ? {
              ...refused,
              body: { ...refused.body, changedByName: "Hà Nguyễn" },
            }
          : refused,
      );
    }
    if (fault === "404") {
      return createMockResponse(notFound());
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
    applyGoneScript(store, request);

    // ── Apply latency ───────────────────────────────────────────────────────
    await applyLatency(config.latency);

    // ── Dispatch to registry ────────────────────────────────────────────────
    return registry.dispatch(request, store);
  };
}
