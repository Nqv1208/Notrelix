/**
 * Rate limits and slow mode of the mock (UC-CHT-H08), mirroring the backend IChatRateLimiter and SlowMode.
 *
 * Time here is the mock's virtual clock: one second per write (MockChatState.stampWrite) plus the seconds a test moves it
 * forward with `advanceVirtualSeconds`. Nothing reads the wall clock. A counted call is an attempt (the sliding window also
 * counts the ones it refuses), per person and per space, exactly as the backend counts them.
 */

import { CHAT_ERROR, type ChatSpaceRecord } from "./chat-model";
import { ChatFailure, type ChatProblemBody } from "./chat-access";
import type { MockChatState } from "./chat-state";

export type ChatLimitPolicy =
  | "send"
  | "react"
  | "thread-reply"
  | "upload"
  | "broadcast"
  | "export"
  | "search";

interface Limit {
  readonly person: number;
  readonly space: number;
  readonly windowSeconds: number;
}

/** The backend defaults (ChatRateLimitOptions). */
export const CHAT_LIMIT_DEFAULTS: Readonly<Record<ChatLimitPolicy, Limit>> = {
  send: { person: 30, space: 300, windowSeconds: 60 },
  react: { person: 60, space: 600, windowSeconds: 60 },
  "thread-reply": { person: 30, space: 300, windowSeconds: 60 },
  upload: { person: 10, space: 100, windowSeconds: 60 },
  broadcast: { person: 3, space: 10, windowSeconds: 60 },
  /** The export counts per person and per Workspace (the workspace id travels where the space id does). */
  export: { person: 5, space: 20, windowSeconds: 3600 },
  /** A message search counts per person and per Workspace (the workspace id travels where the space id does). */
  search: { person: 30, space: 300, windowSeconds: 60 },
};

function tooMany(
  detail: string,
  ruleCode: string,
  retryAfter: number,
  extra: Partial<ChatProblemBody>,
): never {
  throw new ChatFailure({
    status: 429,
    headers: { "Retry-After": String(retryAfter) },
    body: {
      type: "https://notrelix.dev/problems/rate_limit-exceeded",
      title: "Too many requests",
      status: 429,
      detail,
      errorCode: CHAT_ERROR.rateLimited,
      ruleCode,
      retryAfter,
      ...extra,
    },
  });
}

/** Counts one attempt of the caller and of the space; over a limit it is the 429 with the seconds to wait. */
export function requireWithinLimit(
  state: MockChatState,
  policy: ChatLimitPolicy,
  spaceId: string,
  userId: string,
): void {
  const limit = state.limitOverrides.get(policy) ?? CHAT_LIMIT_DEFAULTS[policy];
  const now = state.virtualSeconds();
  const scopes = [
    { scope: "person", id: userId, max: limit.person },
    { scope: "space", id: spaceId, max: limit.space },
  ] as const;
  let refused: { scope: string; max: number; wait: number } | null = null;
  for (const entry of scopes) {
    const key = `${policy}|${entry.scope}|${entry.id}`;
    const live = (state.limitHits.get(key) ?? []).filter(
      (at) => at > now - limit.windowSeconds,
    );
    live.push(now);
    state.limitHits.set(key, live);
    if (live.length > entry.max && refused === null) {
      const oldest = live[live.length - 1 - entry.max]!;
      refused = {
        scope: entry.scope,
        max: entry.max,
        wait: Math.max(1, oldest + limit.windowSeconds - now),
      };
    }
  }
  if (refused) {
    tooMany(
      "Too many requests. Try again shortly.",
      "rate-limited",
      refused.wait,
      { policy: `chat.${policy}`, scope: refused.scope, limit: refused.max },
    );
  }
}

/** The channel's slow mode: nobody is exempt; the wait runs from the caller's own last message in the space. */
export function requireSlowModeClear(
  state: MockChatState,
  space: ChatSpaceRecord,
  userId: string,
): void {
  const seconds = space.slowModeSeconds ?? 0;
  if (seconds <= 0) return;
  let last: string | null = null;
  for (const message of state.messagesOf(space.id)) {
    if (message.authorId !== userId || message.kind !== "user") continue;
    if (last === null || message.createdAt > last) last = message.createdAt;
  }
  if (last === null) return;
  const nowIso = state.store().getClock().offsetSeconds(state.virtualSeconds());
  const wait = Math.ceil(
    (Date.parse(last) + seconds * 1000 - Date.parse(nowIso)) / 1000,
  );
  if (wait <= 0) return;
  tooMany(
    `This channel is in slow mode. Wait ${wait} seconds to write again.`,
    "slow-mode",
    wait,
    { slowModeSeconds: seconds },
  );
}
