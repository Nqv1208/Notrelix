import type { CollaborationApiClient } from "./comment.service";

/** What a comment or a page can @mention: a person who can view it (the server decides who). */
export interface MentionSuggestion {
  readonly userId: string;
  readonly name: string;
  readonly avatarUrl: string | null;
}

export type MentionTargetKind = "page" | "item";

export interface MentionEndpoints {
  pages: { mentionSuggestions: (pageId: string) => string };
  boardItems: { mentionSuggestions: (itemId: string) => string };
}

export function createMentionService(
  api: CollaborationApiClient,
  endpoints: MentionEndpoints,
) {
  return {
    /** The people who can view the target and match the typed text, the caller left out; a Guest gets none. */
    suggestions(
      kind: MentionTargetKind,
      targetId: string,
      query: string,
      limit?: number,
    ): Promise<MentionSuggestion[]> {
      const base =
        kind === "page"
          ? endpoints.pages.mentionSuggestions(targetId)
          : endpoints.boardItems.mentionSuggestions(targetId);
      const search = new URLSearchParams({ query });
      if (limit !== undefined) search.set("limit", String(limit));
      return api.get<MentionSuggestion[]>(`${base}?${search.toString()}`);
    },
  };
}
