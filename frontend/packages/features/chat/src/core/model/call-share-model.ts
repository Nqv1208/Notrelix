/** Framework-neutral rules of a screen share in a call: who shares, and whether this person may start one. */
import type { ChatCall } from "../types/call";

/** The person who shares their screen now: someone still in the call whose share flag is up. At most one (the server keeps it so). */
export function screenSharerOf(call: ChatCall): string | null {
  if (call.state === "ended") return null;
  return (
    call.participants.find((p) => p.sharingScreen && p.leftAt === null)
      ?.personId ?? null
  );
}

/** A screen may be shared in any live call, voice or video (a voice call still carries no camera). */
export const canShareInCall = (call: ChatCall): boolean =>
  call.state === "active";
