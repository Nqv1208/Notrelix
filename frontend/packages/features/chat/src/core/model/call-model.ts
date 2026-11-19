/** Framework-neutral rules of a call: the screen state, the timer and the stored call line of the timeline. */
import type { ChatCall, ChatCallEndReason, ChatCallKind } from "../types/call";

/** What the person sees of a call from where they stand. */
export type ChatCallPhase =
  | "outgoing-ringing"
  | "incoming-ringing"
  | "connecting"
  | "connected"
  | "ended";

export function callPhaseOf(
  call: ChatCall,
  viewerId: string,
  connected: boolean,
): ChatCallPhase {
  if (call.state === "ended") return "ended";
  if (call.state === "ringing")
    return call.initiatorId === viewerId
      ? "outgoing-ringing"
      : "incoming-ringing";
  return connected ? "connected" : "connecting";
}

/** The other person of a one-to-one call (the first other participant, else the starter when the viewer is not it). */
export function counterpartOf(
  call: ChatCall,
  viewerId: string,
  memberIds: readonly string[] = [],
): string | null {
  if (call.initiatorId !== viewerId) return call.initiatorId;
  const joined = call.participants.find((p) => p.personId !== viewerId);
  return joined?.personId ?? memberIds.find((id) => id !== viewerId) ?? null;
}

/** The most people one group or channel call holds: 25 in a voice call, 12 in a video call (the server enforces it; `call-full`). */
export const CHAT_CALL_CAP: Readonly<Record<ChatCallKind, number>> = {
  voice: 25,
  video: 12,
};

/** The people in the call now (a person who left stays in the list with a `leftAt`). */
export function joinedParticipants(call: ChatCall) {
  return call.participants.filter((p) => p.leftAt === null);
}

/** Whether the call has no seat left. */
export function isCallFull(call: ChatCall): boolean {
  return joinedParticipants(call).length >= CHAT_CALL_CAP[call.kind];
}

/** `m:ss`, or `h:mm:ss` from one hour; a negative or fractional input counts as whole seconds from zero. */
export function formatCallTimer(totalSeconds: number): string {
  const seconds = Math.max(0, Math.floor(totalSeconds));
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = seconds % 60;
  const two = (n: number) => String(n).padStart(2, "0");
  return h > 0 ? `${h}:${two(m)}:${two(s)}` : `${m}:${two(s)}`;
}

export function elapsedSeconds(answeredAt: string | null, now: number): number {
  if (!answeredAt) return 0;
  const start = Date.parse(answeredAt);
  return Number.isNaN(start) ? 0 : Math.max(0, (now - start) / 1000);
}

/** The call line the server stores in the timeline (English), taken apart so the copy files can say it in the viewer's language. */
export interface StoredCallLine {
  readonly kind: ChatCallKind;
  readonly outcome: ChatCallEndReason;
  /** `3m 2s`, for a completed call only. */
  readonly duration: string | null;
}

const CALL_LINE =
  /^(voice|video) call (ended, lasted (.+)|declined|cancelled|failed|not answered, the other person is in a call)$|^missed (voice|video) call$/;

export function parseStoredCallLine(
  text: string | null,
): StoredCallLine | null {
  const match = text ? CALL_LINE.exec(text) : null;
  if (!match) return null;
  if (match[4])
    return {
      kind: match[4] as ChatCallKind,
      outcome: "missed",
      duration: null,
    };
  const kind = match[1] as ChatCallKind;
  const tail = match[2] ?? "";
  if (tail.startsWith("ended"))
    return { kind, outcome: "completed", duration: match[3] ?? null };
  const outcome: ChatCallEndReason = tail.startsWith("not answered")
    ? "busy"
    : (tail as ChatCallEndReason);
  return { kind, outcome, duration: null };
}

/** A ring this person has not answered yet (from a per-person call frame, until the call leaves `ringing`). */
export interface ChatIncomingCall {
  readonly callId: string;
  readonly spaceId: string;
  readonly kind: ChatCallKind;
  readonly initiatorId: string;
  readonly version: number;
  /** When the frame arrived, so the ring can stop by itself when the call is missed. */
  readonly receivedAt: string;
}
