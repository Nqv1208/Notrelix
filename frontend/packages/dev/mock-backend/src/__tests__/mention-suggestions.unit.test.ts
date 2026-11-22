import { describe, expect, it } from "vitest";
import { defaultConfig } from "../config/mock-config";
import { mockIds } from "../state/mock-ids";
import {
  itemMentionSuggestions,
  mentionLimit,
} from "../state/mock-mention-suggestions";
import { peopleDirectory } from "../state/mock-people-directory";
import { MockStore } from "../state/mock-store";

describe("mention suggestions in the mock world", () => {
  it("offers only people on the board, never the caller or someone who cannot see it", () => {
    const store = new MockStore({ ...defaultConfig });
    const directory = peopleDirectory(store, mockIds.boards.erp, false)!;
    const outsider = directory.people.find((p) => !p.onBoard)!;
    const offered = itemMentionSuggestions(store, mockIds.boards.erp, "", 20);
    const ids = offered.map((p) => p.userId);
    expect(ids).not.toContain(outsider.userId);
    expect(ids).not.toContain(store.getCurrentUser().id);
    expect(ids.length).toBeGreaterThan(0);
  });

  it("matches the typed text without accents and caps the count", () => {
    const store = new MockStore({ ...defaultConfig });
    const all = itemMentionSuggestions(store, mockIds.boards.erp, "", 20);
    const first = all[0]!;
    const typed = first.name
      .slice(0, 2)
      .normalize("NFD")
      .replace(/\p{M}/gu, "");
    expect(
      itemMentionSuggestions(store, mockIds.boards.erp, typed, 20).map(
        (p) => p.userId,
      ),
    ).toContain(first.userId);
    expect(
      itemMentionSuggestions(store, mockIds.boards.erp, "", 1),
    ).toHaveLength(1);
  });

  it("limits like the server: default 8, at most 20", () => {
    expect(mentionLimit(undefined)).toBe(8);
    expect(mentionLimit("500")).toBe(20);
    expect(mentionLimit("3")).toBe(3);
    expect(mentionLimit("x")).toBe(8);
  });
});
