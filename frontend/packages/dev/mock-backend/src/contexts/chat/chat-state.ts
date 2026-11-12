/**
 * The Chat state of the mock: spaces (channels), channel groups, memberships,
 * messages, the idempotent replays and the realtime counters. Plain maps with
 * the lookups the handlers need; the rules live in the handlers.
 *
 * Ids are GUID-shaped (the backend's `Guid`), so a realtime stream key can use
 * the `N` form (dashes removed) exactly as the backend stamps it.
 */

import type { MockStore } from "../../state/mock-store";
import type { ChatPermissionEntryRecord } from "./chat-permissions";
import type {
  ChatAttachmentRecord,
  ChatGroupRecord,
  ChatMembershipRecord,
  ChatMentionRecord,
  ChatMessageRecord,
  ChatPinRecord,
  ChatReactionRecord,
  ChatSpaceRecord,
  ChatThreadFollowerRecord,
  ChatThreadRecord,
} from "./chat-model";

export type ChatIdKind =
  "space" | "group" | "message" | "pin" | "reaction" | "attachment";

/** The tag of each id family: a GUID whose first group names the family. */
const ID_TAG: Record<ChatIdKind, string> = {
  space: "c1000000",
  group: "c2000000",
  message: "c3000000",
  pin: "c4000000",
  reaction: "c5000000",
  attachment: "c6000000",
};

/** A deterministic GUID-shaped id of a family, numbered from 1. */
export function chatGuid(kind: ChatIdKind, n: number): string {
  return `${ID_TAG[kind]}-0000-4000-8000-${String(n).padStart(12, "0")}`;
}

/** The `N` form of a GUID-shaped id (what `Guid.ToString("N")` prints). Mock user ids pass through. */
export function streamIdOf(id: string): string {
  return id.replace(/-/g, "");
}

export class MockChatState {
  readonly spaces = new Map<string, ChatSpaceRecord>();
  readonly groups = new Map<string, ChatGroupRecord>();
  /** Keyed by `${spaceId}|${userId}`: one row per person and space, reused after a leave. */
  readonly memberships = new Map<string, ChatMembershipRecord>();
  readonly messages = new Map<string, ChatMessageRecord>();
  /** Message ids of each space in sequence order (a message never changes its place). */
  readonly messageIdsBySpace = new Map<string, string[]>();
  /** Stored answers of creates that carry an Idempotency-Key, by scope and key. */
  readonly idempotentReplies = new Map<
    string,
    { fingerprint: string; status: number; body: unknown }
  >();
  /** Threads by root message id: a root has at most one thread, created by its first reply. */
  readonly threads = new Map<string, ChatThreadRecord>();
  /** Follow states keyed by `${rootMessageId}|${userId}`. */
  readonly threadFollowers = new Map<string, ChatThreadFollowerRecord>();
  /** Pins keyed by the pinned message id: a message has at most one pin. */
  readonly pins = new Map<string, ChatPinRecord>();
  /** Reactions keyed by `${messageId}|${userId}|${kind}`: one per message, person and kind. */
  readonly reactions = new Map<string, ChatReactionRecord>();
  /** Files on messages by attachment id (Collaboration attachments with target kind chat.message). */
  readonly attachments = new Map<string, ChatAttachmentRecord>();
  /** The stored mentions of each message, in text order; a message with none has no entry. */
  readonly mentions = new Map<string, ChatMentionRecord[]>();
  /** The permission overrides of each channel, in enum order; a channel with none has no entry (UC-CHT-H01). */
  readonly permissionOverrides = new Map<
    string,
    readonly ChatPermissionEntryRecord[]
  >();
  /** The direct conversation of each pair, keyed by `${workspaceId}|${low}|${high}` (backend DirectPair). */
  readonly directPairs = new Map<string, string>();
  /** Blocks keyed by `${workspaceId}|${blockerId}|${blockedId}` (backend PersonBlock). */
  readonly blocks = new Set<string>();

  private readonly streamVersions = new Map<string, number>();
  private readonly idCounters: Record<ChatIdKind, number> = {
    space: 0,
    group: 0,
    message: 0,
    pin: 0,
    reaction: 0,
    attachment: 0,
  };
  private writeTick = 0;
  private eventCounter = 0;

  constructor(private readonly storeOf: () => MockStore) {}

  /** Drops every row and counter; the store seeds the world again after a reset. */
  clear(): void {
    this.spaces.clear();
    this.groups.clear();
    this.memberships.clear();
    this.messages.clear();
    this.messageIdsBySpace.clear();
    this.idempotentReplies.clear();
    this.threads.clear();
    this.threadFollowers.clear();
    this.pins.clear();
    this.reactions.clear();
    this.attachments.clear();
    this.mentions.clear();
    this.directPairs.clear();
    this.permissionOverrides.clear();
    this.blocks.clear();
    this.streamVersions.clear();
    this.idCounters.space = 0;
    this.idCounters.group = 0;
    this.idCounters.message = 0;
    this.idCounters.pin = 0;
    this.idCounters.reaction = 0;
    this.idCounters.attachment = 0;
    this.writeTick = 0;
    this.eventCounter = 0;
  }

  nextId(kind: ChatIdKind): string {
    this.idCounters[kind] += 1;
    return chatGuid(kind, this.idCounters[kind]);
  }

  /** Marks the ids up to `count` of a family as taken (the seed numbers its rows itself). */
  reserveIds(kind: ChatIdKind, count: number): void {
    this.idCounters[kind] = Math.max(this.idCounters[kind], count);
  }

  /** Stamp of a write: the mock clock moved forward one second per write, so the order is stable. */
  stampWrite(): string {
    this.writeTick += 1;
    return this.storeOf().getClock().offsetSeconds(this.writeTick);
  }

  /** The current instant of the mock clock. */
  nowDate(): Date {
    return this.storeOf().getClock().now();
  }

  nextEventId(): string {
    this.eventCounter += 1;
    return `chat-evt-${String(this.eventCounter).padStart(8, "0")}`;
  }

  /** The next per-stream version: gap-free, one per frame of one stream key. */
  nextStreamVersion(streamKey: string): number {
    const next = (this.streamVersions.get(streamKey) ?? 0) + 1;
    this.streamVersions.set(streamKey, next);
    return next;
  }

  membershipKey(spaceId: string, userId: string): string {
    return `${spaceId}|${userId}`;
  }

  membershipOf(
    spaceId: string,
    userId: string,
  ): ChatMembershipRecord | undefined {
    return this.memberships.get(this.membershipKey(spaceId, userId));
  }

  /** The active membership of a person in a space, or undefined. */
  activeMembershipOf(
    spaceId: string,
    userId: string,
  ): ChatMembershipRecord | undefined {
    const row = this.membershipOf(spaceId, userId);
    return row && row.leftAt === null ? row : undefined;
  }

  membershipsOfSpace(spaceId: string): ChatMembershipRecord[] {
    return Array.from(this.memberships.values()).filter(
      (row) => row.spaceId === spaceId,
    );
  }

  /** Every space row of a Workspace, deleted ones included (callers filter). */
  spacesOfWorkspace(workspaceId: string): ChatSpaceRecord[] {
    return Array.from(this.spaces.values()).filter(
      (space) => space.workspaceId === workspaceId,
    );
  }

  groupsOfWorkspace(workspaceId: string): ChatGroupRecord[] {
    return Array.from(this.groups.values()).filter(
      (group) => group.workspaceId === workspaceId,
    );
  }

  /** The messages of a space in ascending sequence order. */
  messagesOf(spaceId: string): ChatMessageRecord[] {
    const ids = this.messageIdsBySpace.get(spaceId) ?? [];
    return ids.map((id) => this.messages.get(id)!);
  }

  threadFollowerOf(
    rootMessageId: string,
    userId: string,
  ): ChatThreadFollowerRecord | undefined {
    return this.threadFollowers.get(`${rootMessageId}|${userId}`);
  }

  /** The pins of a space, newest pin first (pin time, then pin id: the order of ListPins). */
  pinsOfSpace(spaceId: string): ChatPinRecord[] {
    return Array.from(this.pins.values())
      .filter((pin) => pin.spaceId === spaceId)
      .sort(
        (a, b) =>
          Date.parse(b.pinnedAt) - Date.parse(a.pinnedAt) ||
          (streamIdOf(a.id) < streamIdOf(b.id)
            ? 1
            : streamIdOf(a.id) > streamIdOf(b.id)
              ? -1
              : 0),
      );
  }

  /** The key of a pair: the two people in ordinal order, so either side finds the same conversation. */
  pairKey(workspaceId: string, a: string, b: string): string {
    const [low, high] = a < b ? [a, b] : [b, a];
    return `${workspaceId}|${low}|${high}`;
  }

  /** True when either person has blocked the other in the Workspace. */
  isBlockedBetween(workspaceId: string, a: string, b: string): boolean {
    return (
      this.blocks.has(`${workspaceId}|${a}|${b}`) ||
      this.blocks.has(`${workspaceId}|${b}|${a}`)
    );
  }

  reactionKey(messageId: string, userId: string, kind: string): string {
    return `${messageId}|${userId}|${kind}`;
  }

  /** The reactions of one message, in the order they were made (the order of ListReactors). */
  reactionsOfMessage(messageId: string): ChatReactionRecord[] {
    return Array.from(this.reactions.values())
      .filter((row) => row.messageId === messageId)
      .sort(
        (a, b) =>
          Date.parse(a.createdAt) - Date.parse(b.createdAt) ||
          (streamIdOf(a.id) < streamIdOf(b.id)
            ? -1
            : streamIdOf(a.id) > streamIdOf(b.id)
              ? 1
              : 0),
      );
  }

  /** The files of one message, oldest first (the order of the file list). */
  attachmentsOfMessage(messageId: string): ChatAttachmentRecord[] {
    return Array.from(this.attachments.values())
      .filter((row) => row.messageId === messageId)
      .sort(
        (a, b) =>
          Date.parse(a.createdAt) - Date.parse(b.createdAt) ||
          (streamIdOf(a.id) < streamIdOf(b.id)
            ? -1
            : streamIdOf(a.id) > streamIdOf(b.id)
              ? 1
              : 0),
      );
  }

  /** Appends a message to its space's order; the sequence is the caller's (already allocated). */
  appendMessage(message: ChatMessageRecord): void {
    this.messages.set(message.id, message);
    const ids = this.messageIdsBySpace.get(message.spaceId) ?? [];
    ids.push(message.id);
    this.messageIdsBySpace.set(message.spaceId, ids);
  }

  store(): MockStore {
    return this.storeOf();
  }
}
