/**
 * The missed-mention digest of the mock (UC-CHT-H09), mirroring the backend ChatDigestComposer: one "email" per person and window for
 * the unread chat notifications of the latest completed window, queued on `state.digestOutbox` (no real mail). Time is the mock's
 * virtual clock; a test moves it with `state.advanceVirtualSeconds` and calls `runDigest`, which is the Chat.Notifications.Digest job.
 *
 * Who gets one: a person who turned the digest e-mail on (the Collaboration preference matrix, off by default) and whose own
 * in-app cell for the item's type is on; never a Guest. What is left out, checked at send: an item already read (by the message
 * read boundary or in the Inbox), a muted space or one at the Mentions level for what it does not cover, a space the person left or
 * that is gone or archived, a deleted message, and the person's own messages. At most `maxItems` lines, the rest as "and N more".
 */

import { effectiveLevel } from "./chat-access";
import type { ChatDigestMail } from "./chat-model";
import type { ChatNotificationReason } from "./chat-notifications";
import { virtualNowIso } from "./chat-retention";
import type { MockChatState } from "./chat-state";

export const DIGEST_WINDOW_SECONDS = 86_400;
export const DIGEST_MAX_ITEMS = 10;

const REASON_PREFIX = /^chat-(mention|direct-message|thread-reply|broadcast)-/;

/** The reason and message of a chat inbox entry (`chat-{reason}-{messageId}-{recipient}`), or null for any other entry. */
function parseChatEntry(
  id: string,
  recipientId: string,
): { reason: ChatNotificationReason; messageId: string } | null {
  const match = REASON_PREFIX.exec(id);
  const suffix = `-${recipientId}`;
  if (!match || !id.endsWith(suffix)) return null;
  const messageId = id.slice(match[0].length, id.length - suffix.length);
  return messageId.length > 0
    ? { reason: match[1] as ChatNotificationReason, messageId }
    : null;
}

export interface ChatDigestRun {
  /** The digests queued by this pass. */
  readonly queued: number;
}

/** The ISO instant `ms` on the mock's virtual clock (never the wall clock). */
function isoAt(state: MockChatState, nowMs: number, ms: number): string {
  return state
    .store()
    .getClock()
    .offsetSeconds(state.virtualSeconds() + (ms - nowMs) / 1000);
}

/** yyyyMMddHHmm of an ISO instant (the backend window key). */
function windowKeyOf(iso: string): string {
  return iso.replace(/[-:T]/g, "").slice(0, 12);
}

export function runDigest(
  state: MockChatState,
  workspaceId: string,
  options: { windowSeconds?: number; maxItems?: number } = {},
): ChatDigestRun {
  const windowMs = (options.windowSeconds ?? DIGEST_WINDOW_SECONDS) * 1000;
  const maxItems = options.maxItems ?? DIGEST_MAX_ITEMS;
  const store = state.store();
  const nowMs = Date.parse(virtualNowIso(state));
  const end = Math.floor(nowMs / windowMs) * windowMs;
  const start = end - windowMs;
  const windowKey = windowKeyOf(isoAt(state, nowMs, start));
  const members = store.getWorkspaceMembers(workspaceId);

  const byPerson = new Map<
    string,
    {
      reason: ChatNotificationReason;
      messageId: string;
      id: string;
      at: string;
    }[]
  >();
  for (const entry of store.feed.inbox) {
    if (!entry.chatSpaceId || !entry.recipientId) continue;
    const parsed = parseChatEntry(entry.id, entry.recipientId);
    if (!parsed) continue;
    const at = Date.parse(entry.createdAt);
    if (!entry.unread || entry.done || at < start || at >= end) continue;
    const list = byPerson.get(entry.recipientId) ?? [];
    list.push({
      reason: parsed.reason,
      messageId: parsed.messageId,
      id: entry.id,
      at: entry.createdAt,
    });
    byPerson.set(entry.recipientId, list);
  }

  let queued = 0;
  for (const [userId, entries] of byPerson) {
    const sentKey = `${workspaceId}|${userId}|${windowKey}`;
    if (state.digestSent.has(sentKey)) continue;
    const member = members.find((row) => row.userId === userId);
    if (!member || member.role === "guest") continue;
    const set = store.accountPages.notificationsOf(userId, workspaceId);
    if (!set.rows.digest.email) continue;

    const lines: (ChatDigestMail["lines"][number] & { at: string })[] = [];
    for (const entry of entries) {
      const inboxEntry = store.feed.inbox.find((row) => row.id === entry.id)!;
      const messageId = entry.messageId;
      const message = state.messages.get(messageId);
      const space = message ? state.spaces.get(message.spaceId) : undefined;
      if (
        !message ||
        !space ||
        message.deletedAt !== null ||
        message.kind !== "user" ||
        message.authorId === userId ||
        space.workspaceId !== workspaceId ||
        space.deletedAt !== null ||
        space.archivedAt !== null
      ) {
        continue;
      }
      const membership = state.activeMembershipOf(space.id, userId);
      if (!membership) continue;
      const level = effectiveLevel(
        membership.notificationLevel,
        membership.mutedUntil,
        state.nowDate(),
      );
      if (level === "none") continue;
      // At the Mentions level a plain direct message is not covered (the rule that gave the notification).
      if (level === "mentions" && entry.reason === "direct-message") continue;
      if (message.sequence <= membership.readSequence) continue;
      const inAppCell =
        entry.reason === "thread-reply" ? set.rows.watching : set.rows.mention;
      if (!inAppCell.inApp) continue;
      lines.push({
        spaceId: space.id,
        spaceName: space.name,
        messageId,
        reason: entry.reason,
        excerpt: inboxEntry.excerpt,
        path: `/workspaces/${workspaceId}/chat/${space.id}`,
        at: entry.at,
      });
    }
    if (lines.length === 0) continue;
    lines.sort((a, b) => b.at.localeCompare(a.at));
    state.digestSent.add(sentKey);
    state.digestOutbox.push({
      workspaceId,
      userId,
      windowKey,
      subject:
        lines.length === 1
          ? "You missed 1 chat message"
          : `You missed ${lines.length} chat messages`,
      lines: lines.slice(0, maxItems).map(({ at: _at, ...line }) => line),
      more: Math.max(0, lines.length - maxItems),
      createdAt: isoAt(state, nowMs, nowMs),
    });
    queued += 1;
  }
  return { queued };
}
