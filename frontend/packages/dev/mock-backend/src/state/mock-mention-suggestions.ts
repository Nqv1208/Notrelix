/**
 * The people a page or an item can @mention (gap `CTR-GAP-COL-MENTION-SUGGESTIONS`): only people who can view
 * the target, the caller left out, matched by name, at most `limit`. The mock deems an item's viewers the
 * people on its board and a page's viewers the workspace's people.
 */
import type { MockStore } from "./mock-store";
import { peopleDirectory } from "./mock-people-directory";

export interface MentionSuggestionDto {
  readonly userId: string;
  readonly name: string;
  readonly avatarUrl: string | null;
}

const fold = (value: string) =>
  value
    .normalize("NFD")
    .replace(/\p{M}/gu, "")
    .replace(/đ/g, "d")
    .toLowerCase()
    .trim();

function match(
  store: MockStore,
  viewerIds: readonly string[],
  query: string,
  limit: number,
): MentionSuggestionDto[] {
  const callerId = store.getCurrentUser().id;
  const needle = fold(query);
  return viewerIds
    .filter((userId) => userId !== callerId)
    .flatMap((userId) => {
      const user = store.getUser(userId);
      return user && (needle === "" || fold(user.name).includes(needle))
        ? [{ userId, name: user.name, avatarUrl: user.avatarUrl ?? null }]
        : [];
    })
    .slice(0, limit);
}

/** The limit the server applies: default 8, at most 20. */
export function mentionLimit(raw: string | undefined): number {
  const parsed = Number(raw);
  return Number.isInteger(parsed) && parsed > 0 ? Math.min(parsed, 20) : 8;
}

export function itemMentionSuggestions(
  store: MockStore,
  boardId: string,
  query: string,
  limit: number,
): MentionSuggestionDto[] {
  const viewers = (peopleDirectory(store, boardId, false)?.people ?? [])
    .filter((person) => person.onBoard)
    .map((person) => person.userId);
  return match(store, viewers, query, limit);
}

export function pageMentionSuggestions(
  store: MockStore,
  query: string,
  limit: number,
): MentionSuggestionDto[] {
  return match(
    store,
    store.listUsers().map((user) => user.id),
    query,
    limit,
  );
}
