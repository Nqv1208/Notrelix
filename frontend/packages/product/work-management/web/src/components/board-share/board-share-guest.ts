import {
  fillBoardShareCopy as fill,
  type BoardShareCopy,
} from "./board-share.copy";

/** A pending resource invitation of this Board (`Workspaces.Invitations.ListResourceInvitations`). */
export interface PendingGuestInvitation {
  readonly id: string;
  readonly email: string;
  /** Whether the signed-in person sent it. */
  readonly issuedByYou: boolean;
  /** Whether the signed-in person may resend or revoke it: the issuer, or a workspace Owner or Admin. */
  readonly canManage: boolean;
  readonly invitedByName: string;
  /** ISO instant the invitation expires. */
  readonly expiresAt: string;
}

export const EMAIL_SHAPE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * What the add field makes of the text typed when guests may be invited (UC-WM-A19): a well-formed email no
 * member matches is offered as a Guest invitation (Viewer, GOV-030); text with an "@" that is no email is
 * invalid; anything else is a search for a member.
 */
export function guestOffer(
  query: string,
  memberMatches: number,
): { kind: "email"; email: string } | { kind: "invalid" } | null {
  const text = query.trim();
  if (memberMatches > 0 || !text.includes("@")) return null;
  return EMAIL_SHAPE.test(text)
    ? { kind: "email", email: text }
    : { kind: "invalid" };
}

/**
 * The notice after an invitation was refused while the email already has a pending one: it names the person who
 * sent it (a reader who may not see names reads "Someone else", Q12). Null when no pending invitation matches,
 * so the caller falls back to its generic failure.
 */
export function guestInviteConflict(
  email: string,
  pending: readonly PendingGuestInvitation[],
  copy: BoardShareCopy,
): { title: string; body: string } | null {
  const wanted = email.trim().toLowerCase();
  const row = pending.find(
    (candidate) => candidate.email.toLowerCase() === wanted,
  );
  if (!row) return null;
  return {
    title: copy.add.inviteFailed,
    body: fill(copy.add.inviteConflict, {
      name: row.invitedByName || copy.add.someoneElse,
      email: row.email,
    }),
  };
}

/** The invitation's expiry as day/month/year, the way the history writes dates. */
export function formatInviteDate(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()}`;
}
