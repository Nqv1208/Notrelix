import { describe, expect, it } from "vitest";
import { defaultConfig } from "../config/mock-config";
import {
  buildHomeFeed,
  markAllInboxRead,
  markInbox,
} from "../state/mock-work-feed";
import { MockStore } from "../state/mock-store";

function world() {
  const store = new MockStore({ ...defaultConfig });
  const entry = store.feed.inbox.find((record) => record.mentionId);
  expect(entry, "the seeded mention entry").toBeDefined();
  return { store, entry: entry! };
}

const mentionIds = (store: MockStore) =>
  buildHomeFeed(store)
    .replies.filter((reply) => reply.kind === "mention")
    .map((reply) => reply.id);

describe("a mention in the mock world answers like the producer", () => {
  it("lists the mention on Home under its notification's id", () => {
    const { store, entry } = world();
    expect(mentionIds(store)).toEqual([entry.id]);
  });

  it("reading the notification takes the mention off Home, reopening it brings it back", () => {
    const { store, entry } = world();
    expect(markInbox(store, entry.id, { type: "handled" })).toBe("ok");
    expect(mentionIds(store)).toEqual([]);
    markInbox(store, entry.id, {
      type: "snooze",
      until: "2099-01-01T00:00:00Z",
    });
    expect(mentionIds(store)).toEqual([entry.id]);
  });

  it("marking the inbox read handles the mention too", () => {
    const { store } = world();
    markAllInboxRead(store);
    expect(mentionIds(store)).toEqual([]);
  });

  it("commenting on the mentioning thread clears the need to reply", () => {
    const { store, entry } = world();
    expect(entry.need).toBe("reply");
    store.createPageComment(entry.docId!, store.getCurrentUser().id, "Done.");
    expect(entry.need).toBeNull();
  });
});
