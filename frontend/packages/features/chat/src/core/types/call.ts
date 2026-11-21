/** CallDto and CallJoinDto (`Chat.Calls.*`): a one-to-one or group call of a space and the ticket into its media room. */
export type ChatCallKind = "voice" | "video";
export type ChatCallState = "ringing" | "active" | "ended";
export type ChatCallEndReason =
  "declined" | "missed" | "cancelled" | "busy" | "failed" | "completed";

export interface ChatCallParticipant {
  readonly personId: string;
  readonly joinedAt: string;
  readonly leftAt: string | null;
  readonly audioOn: boolean;
  readonly videoOn: boolean;
  readonly sharingScreen: boolean;
}

export interface ChatCall {
  readonly id: string;
  readonly spaceId: string;
  readonly kind: ChatCallKind;
  readonly state: ChatCallState;
  readonly endReason: ChatCallEndReason | null;
  readonly initiatorId: string;
  readonly startedAt: string;
  readonly answeredAt: string | null;
  readonly endedAt: string | null;
  readonly region: string;
  readonly version: number;
  readonly participants: readonly ChatCallParticipant[];
}

/** A short-lived media ticket for one room and one person; never logged, never stored. */
export interface ChatCallJoinToken {
  readonly url: string;
  readonly token: string;
  readonly expiresAt: string;
}

export interface ChatCallJoin {
  readonly call: ChatCall;
  readonly joinToken: ChatCallJoinToken | null;
}
