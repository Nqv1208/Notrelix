/**
 * View rules of direct and group conversations (framework neutral). A direct
 * conversation is named after the other person, a group after its name or, when
 * it has none, after its other participants.
 */

import type {
  ChatConversation,
  ChatConversationParticipant,
} from "../types/chat";
import { CHAT_LIMITS } from "../types/chat";

/** The participants other than the viewer, in the order the server lists them. */
export function otherParticipants(
  conversation: ChatConversation,
  viewerId: string,
): readonly ChatConversationParticipant[] {
  return conversation.participants.filter((entry) => entry.userId !== viewerId);
}

/**
 * The title of a conversation: the other person for a direct one; the group name for a group, else the
 * other participants' names joined by commas. `nameOf` gives the member name (the host supplies it).
 */
export function conversationTitle(
  conversation: ChatConversation,
  viewerId: string,
  nameOf: (userId: string) => string,
): string {
  const others = otherParticipants(conversation, viewerId).map((entry) =>
    nameOf(entry.userId),
  );
  if (conversation.kind === "group" && conversation.name) {
    return conversation.name;
  }
  return others.join(", ");
}

/** The other person of a direct conversation, or null for a group. */
export function directCounterpart(
  conversation: ChatConversation,
  viewerId: string,
): string | null {
  if (conversation.kind !== "direct") return null;
  return otherParticipants(conversation, viewerId)[0]?.userId ?? null;
}

/** The newest activity first; equal times keep a stable order by id. */
export function sortConversations(
  conversations: readonly ChatConversation[],
): ChatConversation[] {
  return [...conversations].sort(
    (a, b) =>
      Date.parse(b.lastActivityAt) - Date.parse(a.lastActivityAt) ||
      (a.id < b.id ? -1 : a.id > b.id ? 1 : 0),
  );
}

/** Puts a conversation into a list, replacing the same id, newest activity first. */
export function upsertConversation(
  list: readonly ChatConversation[] | undefined,
  conversation: ChatConversation,
): ChatConversation[] {
  const rest = (list ?? []).filter((entry) => entry.id !== conversation.id);
  return sortConversations([...rest, conversation]);
}

/** How many more people a group of this size can take (the cap counts the caller). */
export function groupRoomLeft(participantCount: number): number {
  return Math.max(0, CHAT_LIMITS.groupCap - participantCount);
}

/** Whether a set of picked people starts a direct conversation, a group, or nothing yet. */
export function startKindOf(pickedCount: number): "none" | "direct" | "group" {
  if (pickedCount <= 0) return "none";
  return pickedCount === 1 ? "direct" : "group";
}
