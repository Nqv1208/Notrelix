/**
 * Channel permission overrides (UC-CHT-H01) and the capabilities the channel view carries. The mock mirrors the backend's
 * ChannelPermissionResolver, ChannelPermissionChecks and ChannelPermissionUpdates: the Workspace role's default decides first (the
 * baseline of chat-access), then a deny of the person's subject for the action turns it off; an allow never exceeds the role, and a
 * deny beats an allow of the same pair. A Workspace Owner or Admin is never a subject, and a conversation has no overrides.
 */

import {
  channelView,
  fail,
  forbiddenProblem,
  isConversationKind,
  roleHolds,
  type ChatAction,
  type ChatCaller,
} from "./chat-access";
import { linkViewOf } from "./chat-links";
import type { ChatSpaceRecord, ChatWorkspaceRole } from "./chat-model";
import type { MockChatState } from "./chat-state";

export type ChatPermissionSubject = "manager" | "member" | "guest";
export type ChatChannelAction =
  "post" | "react" | "attach" | "pin" | "broadcast" | "invite";
export type ChatPermissionEffect = "allow" | "deny";

export interface ChatPermissionEntryRecord {
  readonly subject: ChatPermissionSubject;
  readonly action: ChatChannelAction;
  readonly effect: ChatPermissionEffect;
}

/** Enum order of the backend (the stored and answered order). */
export const PERMISSION_SUBJECTS: readonly ChatPermissionSubject[] = [
  "manager",
  "member",
  "guest",
];
export const CHANNEL_ACTIONS: readonly ChatChannelAction[] = [
  "post",
  "react",
  "attach",
  "pin",
  "broadcast",
  "invite",
];

/** The most overrides one channel holds (ChatLimits.PermissionOverridesMax): 3 roles by 6 actions. */
export const PERMISSION_OVERRIDES_MAX = 18;

/** The Workspace action each channel action is checked against by default (attach follows post). */
const BASELINE_ACTION: Record<ChatChannelAction, ChatAction> = {
  post: "messagePost",
  attach: "messagePost",
  react: "messageReact",
  pin: "messagePin",
  broadcast: "mentionBroadcast",
  invite: "memberInvite",
};

const ACTION_VERB: Record<ChatChannelAction, string> = {
  post: "post",
  react: "react",
  attach: "attach files",
  pin: "pin messages",
  broadcast: "mention everyone",
  invite: "invite people",
};

function isWorkspaceAdmin(role: ChatWorkspaceRole): boolean {
  return role === "Owner" || role === "Admin";
}

export function permissionEntriesOf(
  state: MockChatState,
  spaceId: string,
): readonly ChatPermissionEntryRecord[] {
  return state.permissionOverrides.get(spaceId) ?? [];
}

/** The subject an override addresses for the caller, or null for a Workspace Owner or Admin. */
function subjectOf(
  isManager: boolean,
  role: ChatWorkspaceRole,
): ChatPermissionSubject | null {
  if (isWorkspaceAdmin(role)) return null;
  if (isManager) return "manager";
  return role === "Guest" ? "guest" : "member";
}

function overridesAllow(
  entries: readonly ChatPermissionEntryRecord[],
  action: ChatChannelAction,
  isManager: boolean,
  role: ChatWorkspaceRole,
): boolean {
  const subject = subjectOf(isManager, role);
  if (subject === null) return true;
  return !entries.some(
    (entry) =>
      entry.subject === subject &&
      entry.action === action &&
      entry.effect === "deny",
  );
}

/** Whether the channel's overrides leave the action to the caller (the role's own default is checked by requireAction). */
export function allowsAction(
  state: MockChatState,
  space: ChatSpaceRecord,
  caller: ChatCaller,
  action: ChatChannelAction,
): boolean {
  if (isConversationKind(space.kind) || isWorkspaceAdmin(caller.role)) {
    return true;
  }
  const isManager =
    state.activeMembershipOf(space.id, caller.userId)?.role === "manager";
  return overridesAllow(
    permissionEntriesOf(state, space.id),
    action,
    isManager,
    caller.role,
  );
}

/** The 403 of an action the channel's overrides turn off (backend ChannelPermissionChecks.RequireAsync). */
export function requireChannelAction(
  state: MockChatState,
  space: ChatSpaceRecord,
  caller: ChatCaller,
  action: ChatChannelAction,
): void {
  if (!allowsAction(state, space, caller, action)) {
    forbiddenProblem(
      `Your role cannot ${ACTION_VERB[action]} in this channel.`,
    );
  }
}

/** ChannelCapabilitiesDto: what the caller can do in the channel, from the role default and the overrides. */
export function capabilitiesOf(
  state: MockChatState,
  space: ChatSpaceRecord,
  caller: ChatCaller,
) {
  const membership = state.activeMembershipOf(space.id, caller.userId);
  const isMember = membership !== undefined;
  const isManager = membership?.role === "manager";
  const archived = space.archivedAt !== null;
  const admin = isWorkspaceAdmin(caller.role);
  const live = isMember && !archived;
  const conversation = isConversationKind(space.kind);
  const entries = permissionEntriesOf(state, space.id);
  const can = (action: ChatChannelAction): boolean =>
    live &&
    roleHolds(caller.role, BASELINE_ACTION[action]) &&
    (conversation || overridesAllow(entries, action, isManager, caller.role));
  return {
    canPost: can("post"),
    canReact: can("react"),
    canAttach: can("attach"),
    canPin: can("pin"),
    canBroadcast: can("broadcast"),
    canInvite: can("invite"),
    canModerate:
      !archived && (isMember || space.kind === "channel_public") && admin,
    canManagePermissions: !conversation && !archived && (isManager || admin),
  };
}

/** ChannelDto with the caller's capabilities. */
export function channelViewOf(
  state: MockChatState,
  space: ChatSpaceRecord,
  caller: ChatCaller,
) {
  return {
    ...channelView(state, space, caller.userId),
    capabilities: capabilitiesOf(state, space, caller),
    link: linkViewOf(state, space),
  };
}

/** ChannelPermissionsDto: the whole set in enum order, and the channel's version. */
export function permissionsView(state: MockChatState, space: ChatSpaceRecord) {
  return {
    channelId: space.id,
    entries: permissionEntriesOf(state, space.id).map((entry) => ({
      ...entry,
    })),
    version: space.version,
  };
}

/** One entry of a request body, or the 400 of a value the validator does not accept. */
export function parseEntry(
  value: unknown,
  validation: (detail: string) => never,
): ChatPermissionEntryRecord {
  const row = (value ?? {}) as Record<string, unknown>;
  const subject = PERMISSION_SUBJECTS.find((item) => item === row.subject);
  const action = CHANNEL_ACTIONS.find((item) => item === row.action);
  const effect = (["allow", "deny"] as const).find(
    (item) => item === row.effect,
  );
  if (!subject) validation("subject must be manager, member or guest.");
  if (!action) {
    validation("action must be post, react, attach, pin, broadcast or invite.");
  }
  if (!effect) validation("effect must be allow or deny.");
  return { subject: subject!, action: action!, effect: effect! };
}

/**
 * One entry per (subject, action), a deny beating an allow of the same pair, in enum order; a manager denied inviting is the 422
 * permission-lockout (backend ChannelPermissionResolver.Normalize).
 */
export function normalizeEntries(
  requested: readonly ChatPermissionEntryRecord[],
): ChatPermissionEntryRecord[] {
  const byKey = new Map<string, ChatPermissionEntryRecord>();
  for (const entry of requested) {
    const key = `${entry.subject}|${entry.action}`;
    const known = byKey.get(key);
    if (!known || entry.effect === "deny") byKey.set(key, entry);
  }
  const result = [...byKey.values()].sort(
    (a, b) =>
      PERMISSION_SUBJECTS.indexOf(a.subject) -
        PERMISSION_SUBJECTS.indexOf(b.subject) ||
      CHANNEL_ACTIONS.indexOf(a.action) - CHANNEL_ACTIONS.indexOf(b.action),
  );
  if (
    result.some(
      (entry) =>
        entry.subject === "manager" &&
        entry.action === "invite" &&
        entry.effect === "deny",
    )
  ) {
    unprocessable(
      "permission-lockout",
      "Permission lockout",
      "A channel manager cannot be denied inviting people.",
    );
  }
  return result;
}

/** An allow may not exceed the role it names, nor what the caller holds (backend ChannelPermissionUpdates). */
export function requireAllowsWithinRole(
  requested: readonly ChatPermissionEntryRecord[],
  caller: ChatCaller,
): void {
  for (const entry of requested) {
    if (entry.effect !== "allow") continue;
    const subjectRole: ChatWorkspaceRole =
      entry.subject === "guest" ? "Guest" : "Member";
    if (!roleHolds(subjectRole, BASELINE_ACTION[entry.action])) {
      unprocessable(
        "permission-exceeds-role",
        "Permission exceeds role",
        `The ${entry.subject} role cannot be allowed to ${entry.action} here.`,
      );
    }
    if (!roleHolds(caller.role, BASELINE_ACTION[entry.action])) {
      unprocessable(
        "permission-exceeds-role",
        "Permission exceeds role",
        `You cannot grant ${entry.action}, which you do not hold yourself.`,
      );
    }
  }
}

function unprocessable(code: string, title: string, detail: string): never {
  // The backend answers these two rules with 422 and the rule code as the errorCode.
  return fail(422, code, title, detail);
}
