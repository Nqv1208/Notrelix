/**
 * Calls of a space, held in the mock like the backend holds them (Call aggregate, SPEC-CHAT-MEDIA-CALLS): ringing, active,
 * ended; a version that moves with every change; one call system message when a call ends; the chat.call.v1 frame on the
 * channel stream and, while it rings, on each other member's per-person stream. The media plane does not exist here: the join
 * token carries a `mock://` address the client connects without any network.
 */

import type { MockStore } from "../../state/mock-store";
import { publishMockScopeFrame } from "../../realtime/mock-realtime-transport";
import { streamIdOf } from "./chat-state";
import type { MockChatState } from "./chat-state";
import type { ChatSpaceRecord } from "./chat-model";
import { CHAT_ERROR } from "./chat-model";
import {
  fail,
  forbiddenProblem,
  notFoundProblem,
  ruleProblem,
} from "./chat-access";
import { appendSystemMessage } from "./chat-writes";

export type MockCallKind = "voice" | "video";
export type MockCallState = "ringing" | "active" | "ended";
export type MockCallEndReason =
  "declined" | "missed" | "cancelled" | "busy" | "failed" | "completed";

export const CHAT_CALL_CONTRACT = "chat.call.v1";

export interface MockCallParticipant {
  readonly personId: string;
  readonly joinedAt: string;
  leftAt: string | null;
  audioOn: boolean;
  videoOn: boolean;
  sharingScreen: boolean;
}

export interface MockCallRecord {
  readonly id: string;
  readonly workspaceId: string;
  readonly spaceId: string;
  readonly kind: MockCallKind;
  state: MockCallState;
  endReason: MockCallEndReason | null;
  readonly initiatorId: string;
  readonly startedAt: string;
  answeredAt: string | null;
  endedAt: string | null;
  version: number;
  readonly participants: MockCallParticipant[];
}

export const CALL_RULE = {
  ended: "call-ended",
  stateInvalid: "call-state-invalid",
  alreadyActive: "call-already-active",
  busy: "call-busy",
  notParticipant: "call-not-participant",
  mediaNotAllowed: "call-media-not-allowed",
} as const;

const CALL_CAP = { voice: 25, video: 12 } as const;

function callConflict(code: string, title: string, detail: string): never {
  return fail(409, code, title, detail);
}

export function callView(call: MockCallRecord) {
  return {
    id: call.id,
    spaceId: call.spaceId,
    kind: call.kind,
    state: call.state,
    endReason: call.endReason,
    initiatorId: call.initiatorId,
    startedAt: call.startedAt,
    answeredAt: call.answeredAt,
    endedAt: call.endedAt,
    region: "ap-southeast-1",
    version: call.version,
    participants: [...call.participants]
      .sort((a, b) => a.joinedAt.localeCompare(b.joinedAt))
      .map((p) => ({
        personId: p.personId,
        joinedAt: p.joinedAt,
        leftAt: p.leftAt,
        audioOn: p.audioOn,
        videoOn: p.videoOn,
        sharingScreen: p.sharingScreen,
      })),
  };
}

function joinView(call: MockCallRecord, personId: string, issue: boolean) {
  const token =
    issue && isJoined(call, personId)
      ? {
          url: "mock://sfu.invalid",
          token: `mock-token-${call.id}-${personId}`,
          expiresAt: new Date(
            Date.parse(call.startedAt) + 60_000,
          ).toISOString(),
        }
      : null;
  return { call: callView(call), joinToken: token };
}

export function isJoined(call: MockCallRecord, personId: string): boolean {
  return call.participants.some(
    (p) => p.personId === personId && p.leftAt === null,
  );
}

const live = (call: MockCallRecord) => call.state !== "ended";

/** The live call of a space (ringing or active), if any. */
export function liveCallOf(
  state: MockChatState,
  spaceId: string,
): MockCallRecord | undefined {
  return Array.from(state.calls.values()).find(
    (call) => call.spaceId === spaceId && live(call),
  );
}

/** A person is busy while they sit in another live call. */
function inOtherLiveCall(
  state: MockChatState,
  personId: string,
  exceptId?: string,
): boolean {
  return Array.from(state.calls.values()).some(
    (call) => live(call) && call.id !== exceptId && isJoined(call, personId),
  );
}

// ─── Frames ──────────────────────────────────────────────────────────────────

function callPayload(call: MockCallRecord) {
  return {
    callId: call.id,
    spaceId: call.spaceId,
    state: call.state,
    kind: call.kind,
    initiatorId: call.initiatorId,
    ...(call.endReason ? { endReason: call.endReason } : {}),
    participants: call.participants.map((p) => p.personId),
    participantsOmitted: false,
    version: call.version,
  };
}

/** The channel frame of a call change and, while it rings, the ring on each other member's own stream. */
export function announceCall(
  state: MockChatState,
  space: ChatSpaceRecord,
  call: MockCallRecord,
  rings: boolean,
): void {
  const occurredAt = state.store().getClock().isoNow();
  const frameOf = (
    subscriptionId: string,
    streamKey: string,
    resourceKind: string,
    resourceId: string,
  ) => {
    const eventId = state.nextEventId();
    return {
      schemaVersion: 1,
      eventId,
      eventType: "chat.call.changed",
      changeKind: "chat.call.changed",
      subscriptionId,
      streamKey,
      streamVersion: state.nextStreamVersion(streamKey),
      resourceKind,
      resourceId,
      aggregateId: resourceId,
      workspaceId: space.workspaceId,
      correlationId: eventId,
      timestamp: occurredAt,
      payloadContract: CHAT_CALL_CONTRACT,
      payload: callPayload(call) as Readonly<Record<string, unknown>>,
    };
  };
  publishMockScopeFrame(
    frameOf(
      `channel:${space.id}`,
      `chat:Channel:${streamIdOf(space.id)}`,
      "Channel",
      space.id,
    ),
  );
  if (!rings || call.state !== "ringing") return;
  for (const member of state.membershipsOfSpace(space.id)) {
    if (member.leftAt !== null || member.userId === call.initiatorId) continue;
    publishMockScopeFrame(
      frameOf(
        `user:${member.userId}`,
        `user:notifications:${streamIdOf(member.userId)}`,
        "notifications",
        member.userId,
      ),
    );
  }
}

// ─── Lines of the timeline (the backend's English text) ─────────────────────

function lineOf(call: MockCallRecord): string {
  const kind = call.kind === "video" ? "video call" : "voice call";
  switch (call.endReason) {
    case "completed": {
      const seconds = Math.max(
        0,
        Math.floor(
          (Date.parse(call.endedAt ?? call.startedAt) -
            Date.parse(call.answeredAt ?? call.startedAt)) /
            1000,
        ),
      );
      const text =
        seconds >= 3600
          ? `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`
          : seconds >= 60
            ? `${Math.floor(seconds / 60)}m ${seconds % 60}s`
            : `${seconds}s`;
      return `${kind} ended, lasted ${text}`;
    }
    case "missed":
      return `missed ${kind}`;
    case "declined":
      return `${kind} declined`;
    case "cancelled":
      return `${kind} cancelled`;
    case "busy":
      return `${kind} not answered, the other person is in a call`;
    default:
      return `${kind} failed`;
  }
}

function finish(
  state: MockChatState,
  space: ChatSpaceRecord,
  call: MockCallRecord,
  reason: MockCallEndReason,
  actorId: string,
): void {
  const now = state.stampWrite();
  call.state = "ended";
  call.endReason = reason;
  call.endedAt = now;
  call.version += 1;
  for (const p of call.participants) p.leftAt ??= now;
  appendSystemMessage(state, space, actorId, lineOf(call));
  announceCall(state, space, call, false);
}

// ─── Access ──────────────────────────────────────────────────────────────────

export function callIn(
  state: MockChatState,
  workspaceId: string,
  callId: string,
  userId: string,
): { call: MockCallRecord; space: ChatSpaceRecord } {
  const call = state.calls.get(callId);
  if (!call || call.workspaceId !== workspaceId) notFoundProblem();
  const space = state.spaces.get(call.spaceId);
  if (!space || space.deletedAt !== null) notFoundProblem();
  if (!state.activeMembershipOf(space.id, userId)) notFoundProblem();
  return { call, space };
}

function requireVersion(call: MockCallRecord, expected: number): void {
  if (call.version !== expected) {
    fail(
      412,
      CHAT_ERROR.precondition,
      "Precondition failed",
      "The call changed since it was read. Reload it and try again.",
    );
  }
}

function requireLive(call: MockCallRecord): void {
  if (call.state === "ended") {
    callConflict(CALL_RULE.ended, "Call ended", "The call has ended.");
  }
}

function requireMayTakePart(
  store: MockStore,
  state: MockChatState,
  space: ChatSpaceRecord,
  userId: string,
): string | null {
  if (space.archivedAt !== null) {
    fail(
      409,
      CHAT_ERROR.channelArchived,
      "Channel archived",
      "The channel is archived and read-only.",
    );
  }
  const role = store
    .getWorkspaceMembers(space.workspaceId)
    .find((m) => m.userId === userId)?.role;
  if (role === "guest")
    forbiddenProblem("A Guest does not take part in calls.");
  if (space.kind !== "direct") return null;
  const other = state
    .membershipsOfSpace(space.id)
    .find((m) => m.leftAt === null && m.userId !== userId)?.userId;
  if (!other || state.isBlockedBetween(space.workspaceId, userId, other)) {
    notFoundProblem();
  }
  return other;
}

function seat(call: MockCallRecord, personId: string, now: string): void {
  const known = call.participants.find((p) => p.personId === personId);
  if (known) {
    known.leftAt = null;
    return;
  }
  call.participants.push({
    personId,
    joinedAt: now,
    leftAt: null,
    audioOn: true,
    videoOn: call.kind === "video",
    sharingScreen: false,
  });
}

// ─── Commands ────────────────────────────────────────────────────────────────

export function startCall(
  store: MockStore,
  workspaceId: string,
  spaceId: string,
  userId: string,
  kind: MockCallKind,
): { status: number; body: unknown } {
  const state = store.chat;
  const space = state.spaces.get(spaceId);
  if (
    !space ||
    space.workspaceId !== workspaceId ||
    space.deletedAt !== null ||
    !state.activeMembershipOf(space.id, userId)
  ) {
    notFoundProblem();
  }
  const other = requireMayTakePart(store, state, space, userId);
  if (liveCallOf(state, space.id)) {
    callConflict(
      CALL_RULE.alreadyActive,
      "Call already active",
      "The conversation already has a call.",
    );
  }
  if (inOtherLiveCall(state, userId)) {
    callConflict(
      CALL_RULE.busy,
      "Call busy",
      "You are already in another call.",
    );
  }
  const now = state.stampWrite();
  const call: MockCallRecord = {
    id: state.nextCallId(),
    workspaceId,
    spaceId,
    kind,
    state: "ringing",
    endReason: null,
    initiatorId: userId,
    startedAt: now,
    answeredAt: null,
    endedAt: null,
    version: 1,
    participants: [],
  };
  seat(call, userId, now);
  state.calls.set(call.id, call);
  if (other && inOtherLiveCall(state, other)) {
    // The other person is in a call: stored as ended busy, no ring, no token.
    finish(state, space, call, "busy", userId);
    return { status: 201, body: joinView(call, userId, false) };
  }
  announceCall(state, space, call, true);
  return { status: 201, body: joinView(call, userId, true) };
}

export function answerCall(
  store: MockStore,
  workspaceId: string,
  callId: string,
  userId: string,
  expectedVersion: number,
): { status: number; body: unknown } {
  const state = store.chat;
  const { call, space } = callIn(state, workspaceId, callId, userId);
  requireVersion(call, expectedVersion);
  requireMayTakePart(store, state, space, userId);
  if (call.state === "active" && isJoined(call, userId)) {
    return { status: 200, body: joinView(call, userId, true) };
  }
  requireLive(call);
  if (call.initiatorId === userId) {
    callConflict(
      CALL_RULE.stateInvalid,
      "Call state invalid",
      "Your own call cannot be answered.",
    );
  }
  if (inOtherLiveCall(state, userId, call.id)) {
    callConflict(
      CALL_RULE.busy,
      "Call busy",
      "You are already in another call.",
    );
  }
  if (
    call.participants.filter((p) => p.leftAt === null).length >=
    CALL_CAP[call.kind]
  ) {
    ruleProblem("call-full", "The call is full.");
  }
  const now = state.stampWrite();
  seat(call, userId, now);
  call.state = "active";
  call.answeredAt ??= now;
  call.version += 1;
  announceCall(state, space, call, false);
  return { status: 200, body: joinView(call, userId, true) };
}

export function declineCall(
  store: MockStore,
  workspaceId: string,
  callId: string,
  userId: string,
  expectedVersion: number,
): { status: number; body: unknown } {
  const state = store.chat;
  const { call, space } = callIn(state, workspaceId, callId, userId);
  requireVersion(call, expectedVersion);
  requireLive(call);
  if (call.state !== "ringing" || call.initiatorId === userId) {
    callConflict(
      CALL_RULE.stateInvalid,
      "Call state invalid",
      "Only a ringing call of someone else can be declined.",
    );
  }
  if (space.kind === "direct") finish(state, space, call, "declined", userId);
  return { status: 200, body: callView(call) };
}

export function cancelCall(
  store: MockStore,
  workspaceId: string,
  callId: string,
  userId: string,
  expectedVersion: number,
): { status: number; body: unknown } {
  const state = store.chat;
  const { call, space } = callIn(state, workspaceId, callId, userId);
  requireVersion(call, expectedVersion);
  if (call.initiatorId !== userId) {
    fail(
      403,
      CALL_RULE.notParticipant,
      "Forbidden",
      "Only the starter cancels a call.",
    );
  }
  requireLive(call);
  if (call.state !== "ringing") {
    callConflict(
      CALL_RULE.stateInvalid,
      "Call state invalid",
      "Only a ringing call can be cancelled.",
    );
  }
  finish(state, space, call, "cancelled", userId);
  return { status: 200, body: callView(call) };
}

export function endCall(
  store: MockStore,
  workspaceId: string,
  callId: string,
  userId: string,
  expectedVersion: number,
): { status: number; body: unknown } {
  const state = store.chat;
  const { call, space } = callIn(state, workspaceId, callId, userId);
  requireVersion(call, expectedVersion);
  requireLive(call);
  if (call.state !== "active") {
    callConflict(
      CALL_RULE.stateInvalid,
      "Call state invalid",
      "A ringing call is cancelled or declined instead.",
    );
  }
  if (space.kind !== "direct" && call.initiatorId !== userId) {
    fail(
      403,
      CALL_RULE.notParticipant,
      "Forbidden",
      "Only the starter ends a group call.",
    );
  }
  finish(state, space, call, "completed", userId);
  return { status: 200, body: callView(call) };
}

export function joinCall(
  store: MockStore,
  workspaceId: string,
  callId: string,
  userId: string,
  expectedVersion: number,
): { status: number; body: unknown } {
  const state = store.chat;
  const { call, space } = callIn(state, workspaceId, callId, userId);
  requireVersion(call, expectedVersion);
  requireMayTakePart(store, state, space, userId);
  if (isJoined(call, userId) && call.state === "active") {
    return { status: 200, body: joinView(call, userId, true) };
  }
  requireLive(call);
  if (space.kind === "direct" || call.state !== "active") {
    callConflict(
      CALL_RULE.stateInvalid,
      "Call state invalid",
      "Only a live group or channel call is joined.",
    );
  }
  if (inOtherLiveCall(state, userId, call.id)) {
    callConflict(
      CALL_RULE.busy,
      "Call busy",
      "You are already in another call.",
    );
  }
  seat(call, userId, state.stampWrite());
  call.version += 1;
  announceCall(state, space, call, false);
  return { status: 200, body: joinView(call, userId, true) };
}

export function leaveCall(
  store: MockStore,
  workspaceId: string,
  callId: string,
  userId: string,
  expectedVersion: number,
): { status: number; body: unknown } {
  const state = store.chat;
  const { call, space } = callIn(state, workspaceId, callId, userId);
  requireVersion(call, expectedVersion);
  requireLive(call);
  if (!isJoined(call, userId) || call.state !== "active") {
    return { status: 200, body: callView(call) };
  }
  if (space.kind === "direct") {
    finish(state, space, call, "completed", userId);
  } else {
    const me = call.participants.find((p) => p.personId === userId)!;
    me.leftAt = state.stampWrite();
    call.version += 1;
    announceCall(state, space, call, false);
  }
  return { status: 200, body: callView(call) };
}

export function updateCallMedia(
  store: MockStore,
  workspaceId: string,
  callId: string,
  userId: string,
  expectedVersion: number,
  media: { audioOn: boolean; videoOn: boolean; sharingScreen: boolean },
): { status: number; body: unknown } {
  const state = store.chat;
  const { call, space } = callIn(state, workspaceId, callId, userId);
  requireVersion(call, expectedVersion);
  requireLive(call);
  const me = call.participants.find(
    (p) => p.personId === userId && p.leftAt === null,
  );
  if (!me) {
    fail(
      403,
      CALL_RULE.notParticipant,
      "Forbidden",
      "You are not in the call.",
    );
  }
  if (call.kind === "voice" && (media.videoOn || media.sharingScreen)) {
    fail(
      400,
      CALL_RULE.mediaNotAllowed,
      "Media not allowed",
      "A voice call carries no camera.",
    );
  }
  if (
    me.audioOn !== media.audioOn ||
    me.videoOn !== media.videoOn ||
    me.sharingScreen !== media.sharingScreen
  ) {
    me.audioOn = media.audioOn;
    me.videoOn = media.videoOn;
    me.sharingScreen = media.sharingScreen;
    call.version += 1;
    announceCall(state, space, call, false);
  }
  return { status: 200, body: callView(call) };
}

// ─── Dev only: the other person of a call, for browser checks ───────────────

/** Another member starts a call in the space (it rings the current person); null when the space is unknown. */
export function ringAsOther(
  store: MockStore,
  spaceId: string,
  personId: string,
  kind: MockCallKind,
): string | null {
  const state = store.chat;
  const space = state.spaces.get(spaceId);
  if (!space) return null;
  const { body } = startCall(store, space.workspaceId, spaceId, personId, kind);
  return (body as { call: { id: string } }).call.id;
}

/** The other person acts on a call: answer, decline, cancel or end. True when it applied. */
export function actAsOther(
  store: MockStore,
  callId: string,
  personId: string,
  action: "answer" | "decline" | "cancel" | "end",
): boolean {
  const call = store.chat.calls.get(callId);
  if (!call) return false;
  const run = {
    answer: answerCall,
    decline: declineCall,
    cancel: cancelCall,
    end: endCall,
  }[action];
  run(store, call.workspaceId, callId, personId, call.version);
  return true;
}

/** The ring runs out: a ringing call nobody answered ends as missed (the backend sweep). */
export function missCall(store: MockStore, callId: string): boolean {
  const state = store.chat;
  const call = state.calls.get(callId);
  if (!call || call.state !== "ringing") return false;
  const space = state.spaces.get(call.spaceId);
  if (!space) return false;
  finish(state, space, call, "missed", call.initiatorId);
  return true;
}
