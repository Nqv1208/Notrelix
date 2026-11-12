/**
 * Who may see and change a Chat space, the problem answers the backend gives,
 * and the views the wire carries. The mock mirrors ChannelAccess and the
 * workspace role baseline (Governance) for the Chat actions.
 *
 * Owner and Admin do not see a private channel by role (CHT-004): a private
 * channel the caller is not in is the same NotFound as a missing one.
 */

import type { MockStore } from "../../state/mock-store";
import type { MockHttpResult } from "../../transport/create-response";
import type { MockMembershipRecord } from "../../state/records";
import type { MockChatState } from "./chat-state";
import {
  CHAT_ERROR,
  CHAT_LIMITS,
  CHAT_REACTION_PALETTE,
  type ChatAttachmentRecord,
  type ChatGroupRecord,
  type ChatMembershipRecord,
  type ChatMessageRecord,
  type ChatNotificationLevel,
  type ChatPinRecord,
  type ChatSpaceRecord,
  type ChatWorkspaceRole,
} from "./chat-model";
import { mentionCountIn, mentionViewsOf } from "./chat-mention-views";
import type { MentionNotice } from "./chat-mentions";

// ─── Problems ────────────────────────────────────────────────────────────────

export interface ChatProblemBody {
  type: string;
  title: string;
  status: number;
  detail: string;
  errorCode: string;
  /** The stable rule behind a business-rule problem (additive to errorCode). */
  ruleCode?: string;
  /** A 429 only: whole seconds to wait, and what was limited. */
  retryAfter?: number;
  policy?: string;
  scope?: string;
  limit?: number;
  slowModeSeconds?: number;
}

/** Thrown inside a handler to answer with a problem; `guarded` turns it into the response. */
export class ChatFailure extends Error {
  constructor(readonly result: MockHttpResult<ChatProblemBody>) {
    super(result.body?.detail ?? "Chat request failed");
    this.name = "ChatFailure";
  }
}

export function fail(
  status: number,
  errorCode: string,
  title: string,
  detail: string,
): never {
  throw new ChatFailure({
    status,
    body: {
      type: `https://notrelix.dev/problems/${errorCode}`,
      title,
      status,
      detail,
      errorCode,
    },
  });
}

/** A domain rule the backend answers with the generic 400: errorCode business_rule.violation, the rule in ruleCode. */
export function ruleProblem(ruleCode: string, detail: string): never {
  throw new ChatFailure({
    status: 400,
    body: {
      type: `https://notrelix.dev/problems/${CHAT_ERROR.businessRule}`,
      title: "Business rule violation",
      status: 400,
      detail,
      errorCode: CHAT_ERROR.businessRule,
      ruleCode,
    },
  });
}

export function notFoundProblem(): never {
  return fail(404, CHAT_ERROR.notFound, "Not found", "Not found.");
}

export function forbiddenProblem(detail: string): never {
  return fail(403, CHAT_ERROR.forbidden, "Forbidden", detail);
}

export function validationProblem(detail: string): never {
  return fail(400, CHAT_ERROR.validation, "Validation failed", detail);
}

export function archivedProblem(): never {
  return fail(
    409,
    CHAT_ERROR.channelArchived,
    "Channel archived",
    "The channel is archived and read-only.",
  );
}

/** Runs a handler body; a ChatFailure becomes its response, anything else is a mock bug and rethrown. */
export async function guarded(
  run: () => Promise<MockHttpResult<unknown>>,
): Promise<MockHttpResult<unknown>> {
  try {
    return await run();
  } catch (error) {
    if (error instanceof ChatFailure) return error.result;
    throw error;
  }
}

// ─── Caller ──────────────────────────────────────────────────────────────────

export interface ChatCaller {
  readonly userId: string;
  readonly workspaceId: string;
  readonly role: ChatWorkspaceRole;
}

const WORKSPACE_ROLE: Record<MockMembershipRecord["role"], ChatWorkspaceRole> =
  {
    owner: "Owner",
    admin: "Admin",
    member: "Member",
    guest: "Guest",
  };

/** The caller in a Workspace; a person outside it gets the NotFound of a missing Workspace. */
export function callerIn(store: MockStore, workspaceId: string): ChatCaller {
  if (!store.isCurrentMemberOf(workspaceId)) notFoundProblem();
  const userId = store.getCurrentUser().id;
  const membership = store
    .getWorkspaceMembers(workspaceId)
    .find((row) => row.userId === userId);
  if (!membership) notFoundProblem();
  return { userId, workspaceId, role: WORKSPACE_ROLE[membership.role] };
}

// ─── Workspace permission (the baseline cells the Chat actions use) ──────────

export type ChatAction =
  | "viewWorkspace"
  | "messagePost"
  | "messagePin"
  | "messageReact"
  | "mentionBroadcast"
  | "channelCreate"
  | "channelManage"
  | "groupManage"
  | "memberInvite"
  | "messageModerate";

/** The roles the baseline denies each Chat action (WorkspaceRoleBaseline, Chat section). */
const DENIED_ROLES: Record<ChatAction, readonly ChatWorkspaceRole[]> = {
  viewWorkspace: [],
  messagePost: [],
  messagePin: ["Guest"],
  messageReact: [],
  mentionBroadcast: ["Member", "Guest"],
  channelCreate: ["Guest"],
  channelManage: ["Guest"],
  groupManage: ["Member", "Guest"],
  memberInvite: ["Guest"],
  messageModerate: ["Member", "Guest"],
};

/** Whether the Workspace role holds the Chat action by default (the baseline cell, not an override). */
export function roleHolds(
  role: ChatWorkspaceRole,
  action: ChatAction,
): boolean {
  return !DENIED_ROLES[action].includes(role);
}

/** The pipeline's permission check: runs before the handler looks at the channel. */
export function requireAction(caller: ChatCaller, action: ChatAction): void {
  if (DENIED_ROLES[action].includes(caller.role)) {
    forbiddenProblem("Your role does not allow this Chat action.");
  }
}

// ─── Spaces and visibility ───────────────────────────────────────────────────

/** A live (not deleted) space of the Workspace, or the NotFound of a missing one. */
export function liveSpace(
  state: MockChatState,
  workspaceId: string,
  spaceId: string,
): ChatSpaceRecord {
  const space = state.spaces.get(spaceId);
  if (!space || space.workspaceId !== workspaceId || space.deletedAt !== null) {
    notFoundProblem();
  }
  return space;
}

/** A live channel: a direct or group conversation is the same NotFound as a missing channel (backend LoadChannelAsync). */
export function liveChannel(
  state: MockChatState,
  workspaceId: string,
  spaceId: string,
): ChatSpaceRecord {
  const space = liveSpace(state, workspaceId, spaceId);
  if (isConversationKind(space.kind)) notFoundProblem();
  return space;
}

export function isConversationKind(kind: ChatSpaceRecord["kind"]): boolean {
  return kind === "direct" || kind === "group";
}

/** A private channel or a conversation is seen by its members; a Guest sees only the spaces they were added to. */
export function requireVisible(
  state: MockChatState,
  space: ChatSpaceRecord,
  caller: ChatCaller,
): void {
  const mustBeMember =
    space.kind !== "channel_public" || caller.role === "Guest";
  if (mustBeMember && !state.activeMembershipOf(space.id, caller.userId)) {
    notFoundProblem();
  }
}

/** Renaming, archiving, moving and removing others: a channel manager, a Workspace Owner or Admin. */
export function requireManager(
  state: MockChatState,
  space: ChatSpaceRecord,
  caller: ChatCaller,
): void {
  if (state.activeMembershipOf(space.id, caller.userId)?.role === "manager") {
    return;
  }
  if (caller.role === "Owner" || caller.role === "Admin") return;
  forbiddenProblem(
    "Only a channel manager, a Workspace Owner or Admin can change this channel.",
  );
}

/** Channel names are unique per Workspace among live, unarchived channels, ignoring case. */
export function requireUniqueChannelName(
  state: MockChatState,
  workspaceId: string,
  name: string,
  exceptSpaceId: string | null,
): void {
  const key = name.trim().toLowerCase();
  const clash = state
    .spacesOfWorkspace(workspaceId)
    .some(
      (space) =>
        space.deletedAt === null &&
        space.archivedAt === null &&
        space.id !== exceptSpaceId &&
        space.name.toLowerCase() === key,
    );
  if (clash) {
    fail(
      409,
      CHAT_ERROR.nameConflict,
      "Name conflict",
      "A channel with this name already exists in the Workspace.",
    );
  }
}

/** Group names are unique per Workspace, ignoring case. */
export function requireUniqueGroupName(
  state: MockChatState,
  workspaceId: string,
  name: string,
  exceptGroupId: string | null,
): void {
  const key = name.trim().toLowerCase();
  const clash = state
    .groupsOfWorkspace(workspaceId)
    .some(
      (group) => group.id !== exceptGroupId && group.name.toLowerCase() === key,
    );
  if (clash) {
    fail(
      409,
      CHAT_ERROR.nameConflict,
      "Name conflict",
      "A channel group with this name already exists in the Workspace.",
    );
  }
}

/** A group of the Workspace, or the NotFound of a missing one. */
export function groupIn(
  state: MockChatState,
  workspaceId: string,
  groupId: string,
): ChatGroupRecord {
  const group = state.groups.get(groupId);
  if (!group || group.workspaceId !== workspaceId) notFoundProblem();
  return group;
}

/** The position after the last live channel (the backend's NextChannelPositionAsync). */
export function nextChannelPosition(
  state: MockChatState,
  workspaceId: string,
): number {
  const positions = state
    .spacesOfWorkspace(workspaceId)
    .filter((space) => space.deletedAt === null)
    .map((space) => space.position);
  return positions.length === 0 ? 1 : Math.max(...positions) + 1;
}

export function nextGroupPosition(
  state: MockChatState,
  workspaceId: string,
): number {
  const positions = state
    .groupsOfWorkspace(workspaceId)
    .map((group) => group.position);
  return positions.length === 0 ? 1 : Math.max(...positions) + 1;
}

// ─── Notification levels and unread ──────────────────────────────────────────

/** The level in force at `now`: a timed mute whose end has passed is over (Membership.EffectiveLevel). */
export function effectiveLevel(
  stored: ChatNotificationLevel,
  mutedUntil: string | null,
  now: Date,
): ChatNotificationLevel {
  if (
    stored === "none" &&
    mutedUntil !== null &&
    Date.parse(mutedUntil) <= now.getTime()
  ) {
    return "all";
  }
  return stored;
}

/** The unread messages of a person in a space: other people's user messages after the boundary, not deleted. */
export function unreadIn(
  state: MockChatState,
  spaceId: string,
  userId: string,
  readSequence: number,
): { count: number; first: number | null } {
  let count = 0;
  let first: number | null = null;
  for (const message of state.messagesOf(spaceId)) {
    if (
      message.sequence > readSequence &&
      message.authorId !== userId &&
      message.kind === "user" &&
      message.deletedAt === null
    ) {
      count += 1;
      first ??= message.sequence;
    }
  }
  return { count, first };
}

export interface ChatSpaceUnreadRow {
  spaceId: string;
  name: string;
  readSequence: number;
  lastSequence: number;
  unreadCount: number;
  firstUnreadSequence: number | null;
  mentionCount: number;
  notificationLevel: ChatNotificationLevel;
  muted: boolean;
}

export interface ChatUnreadSummary {
  spaces: ChatSpaceUnreadRow[];
  totalUnread: number;
  totalMentions: number;
}

/** OrdinalIgnoreCase, as the backend orders the unread rows by name. */
export function compareOrdinalIgnoreCase(a: string, b: string): number {
  const x = a.toUpperCase();
  const y = b.toUpperCase();
  return x < y ? -1 : x > y ? 1 : 0;
}

export function unreadRow(
  state: MockChatState,
  space: ChatSpaceRecord,
  membership: ChatMembershipRecord,
  now: Date,
): ChatSpaceUnreadRow {
  const level = effectiveLevel(
    membership.notificationLevel,
    membership.mutedUntil,
    now,
  );
  const unread = unreadIn(
    state,
    space.id,
    membership.userId,
    membership.readSequence,
  );
  return {
    spaceId: space.id,
    name: space.name,
    readSequence: membership.readSequence,
    lastSequence: space.lastSequence,
    unreadCount: unread.count,
    firstUnreadSequence: unread.first,
    mentionCount: mentionCountIn(
      state,
      space.id,
      membership.userId,
      membership.readSequence,
      membership.joinedAt,
    ),
    notificationLevel: level,
    muted: level === "none",
  };
}

/** One row per live space the person is an active member of, ordered by name, then id. */
export function unreadSummaryOf(
  state: MockChatState,
  workspaceId: string,
  userId: string,
  now: Date,
): ChatUnreadSummary {
  const rows: ChatSpaceUnreadRow[] = [];
  for (const membership of state.memberships.values()) {
    if (
      membership.userId !== userId ||
      membership.workspaceId !== workspaceId ||
      membership.leftAt !== null
    ) {
      continue;
    }
    const space = state.spaces.get(membership.spaceId);
    if (!space || space.deletedAt !== null) continue;
    rows.push(unreadRow(state, space, membership, now));
  }
  rows.sort(
    (a, b) =>
      compareOrdinalIgnoreCase(a.name, b.name) ||
      (a.spaceId < b.spaceId ? -1 : a.spaceId > b.spaceId ? 1 : 0),
  );
  const unmuted = rows.filter((row) => !row.muted);
  return {
    spaces: rows,
    totalUnread: unmuted.reduce((sum, row) => sum + row.unreadCount, 0),
    totalMentions: unmuted.reduce((sum, row) => sum + row.mentionCount, 0),
  };
}

// ─── Wire views ──────────────────────────────────────────────────────────────

/** ChannelDto: the membership fields are the viewer's own. */
export function channelView(
  state: MockChatState,
  space: ChatSpaceRecord,
  viewerUserId: string,
) {
  const mine = state.activeMembershipOf(space.id, viewerUserId);
  return {
    id: space.id,
    name: space.name,
    topic: space.topic,
    kind: space.kind,
    groupId: space.groupId,
    position: space.position,
    isArchived: space.archivedAt !== null,
    isMember: mine !== undefined,
    role: mine ? mine.role : null,
    notificationLevel: mine ? mine.notificationLevel : null,
    version: space.version,
    createdAt: space.createdAt,
    slowModeSeconds: space.slowModeSeconds ?? 0,
  };
}

/**
 * The reaction summary of a message as the viewer sees it (backend ReactionSummaries): per palette kind, the count and whether the
 * viewer is among the reactors, in palette order. A kind nobody reacted with is absent.
 */
export function reactionSummaryOf(
  state: MockChatState,
  messageId: string,
  viewerUserId: string,
) {
  return CHAT_REACTION_PALETTE.flatMap((kind) => {
    const rows = state
      .reactionsOfMessage(messageId)
      .filter((row) => row.kind === kind);
    if (rows.length === 0) return [];
    return [
      {
        emoji: kind,
        count: rows.length,
        reactedByMe: rows.some((row) => row.userId === viewerUserId),
      },
    ];
  });
}

/** MessageAttachmentDto: the metadata of a file and the path that streams its bytes (a permission-checked request). */
export function attachmentView(row: ChatAttachmentRecord) {
  return {
    id: row.id,
    messageId: row.messageId,
    fileName: row.filename,
    sizeBytes: row.sizeBytes,
    contentType: row.contentType,
    uploadedBy: row.uploadedBy,
    createdAt: row.createdAt,
    downloadPath: `/api/v1/workspaces/${row.workspaceId}/chat/messages/${row.messageId}/attachments/${row.id}/content`,
  };
}

/**
 * MessageDto: a tombstone keeps its author, time and sequence, with null content, the reaction summary of the viewer and the mentions the
 * viewer may see. `mentionNotices` is what a send or an edit did not do for the sender; every other view leaves it empty.
 */
export function messageView(
  state: MockChatState,
  viewerUserId: string,
  message: ChatMessageRecord,
  mentionNotices: readonly MentionNotice[] = [],
) {
  return {
    id: message.id,
    spaceId: message.spaceId,
    sequence: message.sequence,
    kind: message.kind,
    authorId: message.authorId,
    content: message.content,
    clientMessageId: message.clientMessageId,
    replyToId: message.replyToId,
    threadRootId: message.threadRootId,
    threadSequence: message.threadSequence,
    alsoSentToChannel: message.alsoSentToChannel,
    edited: message.editedAt !== null,
    deleted: message.deletedAt !== null,
    removedByModerator:
      message.deletedAt !== null &&
      message.deletedBy !== null &&
      message.deletedBy !== message.authorId,
    createdAt: message.createdAt,
    editedAt: message.editedAt,
    deletedAt: message.deletedAt,
    version: message.version,
    reactions: reactionSummaryOf(state, message.id, viewerUserId),
    attachments: state.attachmentsOfMessage(message.id).map(attachmentView),
    mentions: mentionViewsOf(
      state,
      message.workspaceId,
      viewerUserId,
      message.id,
    ),
    mentionNotices: [...mentionNotices],
  };
}

/** ThreadSummaryDto: the reply count and last reply of the root's thread, and the viewer's follow state. */
export function threadSummaryView(
  state: MockChatState,
  root: ChatMessageRecord,
  viewerUserId: string,
  now: Date,
) {
  const thread = state.threads.get(root.id);
  const follower = state.threadFollowerOf(root.id, viewerUserId);
  const replyCount = thread?.replyCount ?? 0;
  const inactiveMs = CHAT_LIMITS.threadInactivityDays * 24 * 60 * 60 * 1000;
  return {
    rootMessageId: root.id,
    spaceId: root.spaceId,
    replyCount,
    lastReplyAt: thread?.lastReplyAt ?? null,
    // The archive sweep is not in the mock: an inactive thread is archived by the derived rule only.
    archived: thread
      ? now.getTime() - Date.parse(thread.lastReplyAt) >= inactiveMs
      : false,
    following: follower !== undefined,
    muted: follower?.muted ?? false,
    unreadReplies: follower
      ? Math.max(0, replyCount - follower.readReplySequence)
      : 0,
  };
}

/** PinDto: the pinned message as readers see it, with who pinned it and when. */
export function pinView(
  state: MockChatState,
  pin: ChatPinRecord,
  viewerUserId: string,
) {
  return {
    message: messageView(
      state,
      viewerUserId,
      state.messages.get(pin.messageId)!,
    ),
    pinnedBy: pin.pinnedBy,
    pinnedAt: pin.pinnedAt,
  };
}

export function groupView(group: ChatGroupRecord) {
  return {
    id: group.id,
    name: group.name,
    position: group.position,
    version: group.version,
  };
}

/** The 412 of a stale expectedVersion (the backend's PreconditionFailedException). */
export function requireVersion(expected: number, current: number): void {
  if (expected !== current) {
    fail(
      412,
      CHAT_ERROR.precondition,
      "Precondition failed",
      "The item changed since it was read. Reload it and try again.",
    );
  }
}

/** A required positive-integer version from the body or the query. */
export function parseVersion(value: unknown, name: string): number {
  const parsed = typeof value === "string" ? Number(value) : value;
  if (typeof parsed !== "number" || !Number.isInteger(parsed) || parsed < 0) {
    validationProblem(`${name} is required.`);
  }
  return parsed as number;
}

/** The Idempotency-Key header: 8 to 128 characters. */
export function requireIdempotencyKey(headers: Headers): string {
  const key = headers.get("Idempotency-Key");
  if (key === null || key.length < 8 || key.length > 128) {
    validationProblem("Idempotency-Key is required (8 to 128 characters).");
  }
  return key;
}

// ─── Validation of the request bodies (the backend's validators) ─────────────

/** A required text: trimmed, 1 to `max` characters. */
export function requiredText(
  value: unknown,
  label: string,
  max: number,
): string {
  if (typeof value !== "string" || value.trim().length === 0) {
    validationProblem(`${label} is required.`);
  }
  const trimmed = (value as string).trim();
  if (trimmed.length > max) {
    validationProblem(`${label} has at most ${max} characters.`);
  }
  return trimmed;
}

/** An optional text: null or blank clears it; otherwise trimmed and at most `max` characters. */
export function optionalText(
  value: unknown,
  label: string,
  max: number,
): string | null {
  if (value === undefined || value === null) return null;
  if (typeof value !== "string") validationProblem(`${label} must be text.`);
  const trimmed = (value as string).trim();
  if (trimmed.length === 0) return null;
  if (trimmed.length > max) {
    validationProblem(`${label} has at most ${max} characters.`);
  }
  return trimmed;
}

/** A finite sidebar position. */
export function requiredPosition(value: unknown): number {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    validationProblem("position must be a finite number.");
  }
  return value as number;
}

/** A query value read as a whole number, or undefined when absent. */
export function optionalInteger(
  value: string | undefined,
  label: string,
): number | undefined {
  if (value === undefined || value === "") return undefined;
  const parsed = Number(value);
  if (!Number.isInteger(parsed))
    validationProblem(`${label} must be a whole number.`);
  return parsed;
}
