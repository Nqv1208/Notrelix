/**
 * Retention and export rules of the mock (UC-CHT-H05, H06), mirroring the backend ChatRetentionApplier and ChatExportBuilder.
 *
 * Time is the mock's virtual clock (the store clock moved forward by the write ticks): nothing reads the wall clock. A test moves
 * it with `state.advanceVirtualSeconds` and then calls `runRetention`, which is the Chat.Retention.Apply job of the backend.
 */

import { announcePinChanged } from "./chat-realtime";
import type { ChatExportJobRecord, ChatSpaceRecord } from "./chat-model";
import type { MockChatState } from "./chat-state";

export const RETENTION_MIN_DAYS = 7;
export const RETENTION_MAX_DAYS = 3650;
/** ChatLimits.ExportMaxMessages and ExportLifetimeHours. */
export const EXPORT_MAX_MESSAGES = 50_000;
export const EXPORT_LIFETIME_HOURS = 24;
export const EXPORT_FORMAT = "notrelix.chat-export.v1";
const EMPTY_ID = "00000000-0000-0000-0000-000000000000";

/** The virtual now as an ISO string. */
export function virtualNowIso(state: MockChatState): string {
  return state.store().getClock().offsetSeconds(state.virtualSeconds());
}

export interface RetentionRun {
  readonly spaces: number;
  readonly messages: number;
  readonly threads: number;
}

/** Drops everything a message owns (pin, reactions, mentions, files) without announcing it: the batch has one frame per space. */
function clearOwned(state: MockChatState, messageId: string): void {
  state.pins.delete(messageId);
  for (const row of state.reactionsOfMessage(messageId)) {
    state.reactions.delete(
      state.reactionKey(row.messageId, row.userId, row.kind),
    );
  }
  state.mentions.delete(messageId);
  for (const row of state.attachmentsOfMessage(messageId)) {
    state.attachments.delete(row.id);
  }
}

/**
 * The retention job for one Workspace: tombstones or purges the user messages past the window in every space that is not deleted
 * (a deleted space is inside its recovery window and is skipped). One `retention` frame per changed space. A Workspace without a
 * window changes nothing. Purge removes a thread as a whole, and only when its last reply is past the window.
 */
export function runRetention(
  state: MockChatState,
  workspaceId: string,
): RetentionRun {
  const policy = state.retentionPolicies.get(workspaceId);
  if (!policy || policy.retentionDays === null) {
    return { spaces: 0, messages: 0, threads: 0 };
  }
  const now = virtualNowIso(state);
  const cutoff = state
    .store()
    .getClock()
    .offsetSeconds(state.virtualSeconds() - policy.retentionDays * 86_400);
  let spaces = 0;
  let messages = 0;
  let threads = 0;
  for (const space of state.spacesOfWorkspace(workspaceId)) {
    if (space.deletedAt !== null) continue;
    let changed = 0;
    if (policy.mode === "tombstone") {
      for (const message of state.messagesOf(space.id)) {
        if (
          message.kind !== "user" ||
          message.deletedAt !== null ||
          message.createdAt >= cutoff
        ) {
          continue;
        }
        message.content = null;
        message.deletedAt = now;
        message.deletedBy = null;
        message.version += 1;
        clearOwned(state, message.id);
        changed += 1;
      }
    } else {
      const removed = new Set<string>();
      for (const message of state.messagesOf(space.id)) {
        if (message.kind !== "user" || message.threadRootId !== null) continue;
        const thread = state.threads.get(message.id);
        if (thread) {
          if (thread.lastReplyAt >= cutoff) continue;
        } else if (message.createdAt >= cutoff) {
          continue;
        }
        removed.add(message.id);
        if (thread) {
          threads += 1;
          for (const reply of state.messagesOf(space.id)) {
            if (reply.threadRootId === message.id) removed.add(reply.id);
          }
          state.threads.delete(message.id);
          for (const key of [...state.threadFollowers.keys()]) {
            if (key.startsWith(`${message.id}|`)) {
              state.threadFollowers.delete(key);
            }
          }
        }
      }
      for (const id of removed) {
        clearOwned(state, id);
        state.messages.delete(id);
      }
      const ids = state.messageIdsBySpace.get(space.id);
      if (ids) {
        state.messageIdsBySpace.set(
          space.id,
          ids.filter((id) => !removed.has(id)),
        );
      }
      changed = removed.size;
    }
    if (changed > 0) {
      spaces += 1;
      messages += changed;
      announcePinChanged(state, space, EMPTY_ID, "retention");
    }
  }
  return { spaces, messages, threads };
}

/** A space the requester sees (backend ChatExportBuilder): a public channel, or a private space they are an active member of. */
function visibleTo(
  state: MockChatState,
  space: ChatSpaceRecord,
  requesterId: string,
): boolean {
  if (space.kind === "channel_public") return true;
  return state.activeMembershipOf(space.id, requesterId) !== undefined;
}

/**
 * Runs a queued export (the backend worker): the subject's own user messages in the spaces the requester sees, inside the range
 * (from inclusive, to exclusive). Tombstones are counted, not listed; spaces the requester cannot see are counted, never named.
 * Over the message limit the job fails with `export-too-large`.
 */
export function runExport(
  state: MockChatState,
  job: ChatExportJobRecord,
): void {
  if (job.status !== "queued" && job.status !== "running") return;
  const now = virtualNowIso(state);
  const inRange = (at: string) =>
    (job.rangeFrom === null || Date.parse(at) >= Date.parse(job.rangeFrom)) &&
    (job.rangeTo === null || Date.parse(at) < Date.parse(job.rangeTo));
  const spaces: { id: string; name: string | null; kind: string }[] = [];
  const rows: {
    spaceId: string;
    id: string;
    sentAt: string;
    content: string;
  }[] = [];
  let excluded = 0;
  let tombstones = 0;
  for (const space of state.spacesOfWorkspace(job.workspaceId)) {
    if (space.deletedAt !== null) continue;
    const mine = state
      .messagesOf(space.id)
      .filter((m) => m.authorId === job.subjectUserId && m.kind === "user");
    const involved =
      mine.length > 0 ||
      state.activeMembershipOf(space.id, job.subjectUserId) !== undefined;
    if (!involved) continue;
    if (!visibleTo(state, space, job.requestedBy)) {
      excluded += 1;
      continue;
    }
    spaces.push({ id: space.id, name: space.name ?? null, kind: space.kind });
    for (const message of mine) {
      if (!inRange(message.createdAt)) continue;
      if (message.deletedAt !== null || message.content === null) {
        tombstones += 1;
        continue;
      }
      rows.push({
        spaceId: space.id,
        id: message.id,
        sentAt: message.createdAt,
        content: message.content,
      });
    }
  }
  if (rows.length > EXPORT_MAX_MESSAGES) {
    job.status = "failed";
    job.failureCode = "export-too-large";
    job.completedAt = now;
    return;
  }
  const file = JSON.stringify({
    format: EXPORT_FORMAT,
    generatedAt: now,
    subjectUserId: job.subjectUserId,
    memberships: spaces,
    messages: rows,
    summary: {
      messages: rows.length,
      spaces: spaces.length,
      spacesNotVisibleToRequester: excluded,
      removedMessagesOmitted: tombstones,
    },
  });
  job.status = "ready";
  job.messageCount = rows.length;
  job.spaceCount = spaces.length;
  job.excludedSpaceCount = excluded;
  job.tombstoneCount = tombstones;
  job.file = file;
  job.sizeBytes = new TextEncoder().encode(file).byteLength;
  job.completedAt = now;
  job.expiresAt = state
    .store()
    .getClock()
    .offsetSeconds(state.virtualSeconds() + EXPORT_LIFETIME_HOURS * 3600);
}

/** The sweep: a ready export past its expiry loses its file and reads as expired. */
export function sweepExport(
  state: MockChatState,
  job: ChatExportJobRecord,
): void {
  if (
    job.status === "ready" &&
    job.expiresAt !== null &&
    virtualNowIso(state) >= job.expiresAt
  ) {
    job.status = "expired";
    job.file = null;
  }
}
