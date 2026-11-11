/**
 * Shared helpers of the Chat mock tests: a store per persona, a call through
 * the mock fetch (the same path the app takes), and the ids of the seed world.
 */

import { MockStore } from "../state/mock-store";
import { defaultConfig, type MockPersona } from "../config/mock-config";
import { createMockFetch } from "../transport/create-mock-fetch";
import { mockIds } from "../state/mock-ids";
import { CHAT_SEED_IDS } from "../contexts/chat/chat-world";

export const WORKSPACE_ID = mockIds.workspaces.primary;
export const SECONDARY_WORKSPACE_ID = mockIds.workspaces.secondary;
export const SPACES = CHAT_SEED_IDS.spaces;
export const GROUPS = CHAT_SEED_IDS.groups;
export const USERS = mockIds.users;

export function storeAs(persona: MockPersona = "owner"): MockStore {
  return new MockStore({ ...defaultConfig, persona });
}

/**
 * Acts as another persona on the same store, so one world holds the writes of several people. Same as the
 * forms and saved-filters tests: the persona is the config field the current user is read from.
 */
export function actAs(store: MockStore, persona: MockPersona): MockStore {
  (store as unknown as { config: { persona: MockPersona } }).config.persona =
    persona;
  return store;
}

export interface ChatCall {
  readonly status: number;
  readonly body: any;
}

export interface CallOptions {
  readonly body?: unknown;
  readonly headers?: Record<string, string>;
  /** Query string without the leading "?". */
  readonly query?: string;
  readonly workspaceId?: string;
  /** The Idempotency-Key of a create; sent unless `headers` overrides it. */
  readonly idempotencyKey?: string;
}

/** Calls a Chat endpoint through the mock fetch. `path` is relative to `/workspaces/:id/chat`. */
export async function chat(
  store: MockStore,
  method: string,
  path: string,
  options: CallOptions = {},
): Promise<ChatCall> {
  const workspaceId = options.workspaceId ?? WORKSPACE_ID;
  const query = options.query ? `?${options.query}` : "";
  const url = `http://localhost:8000/api/v1/workspaces/${workspaceId}/chat${path}${query}`;
  const headers: Record<string, string> = { ...(options.headers ?? {}) };
  if (options.idempotencyKey !== undefined) {
    headers["Idempotency-Key"] = options.idempotencyKey;
  }
  if (options.body !== undefined) headers["Content-Type"] = "application/json";
  const response = await createMockFetch(store)(url, {
    method,
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });
  const text = await response.text();
  return { status: response.status, body: text ? JSON.parse(text) : null };
}

/** A fresh Idempotency-Key (8 to 128 characters). */
export function key(label: string): string {
  return `chat-test-key-${label}`;
}
