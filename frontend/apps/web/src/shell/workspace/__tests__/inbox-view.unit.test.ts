import { describe, expect, it } from "vitest";
import { workspaceInboxCopy } from "@notrelix/features-workspace/web";
import { toInboxItems, type InboxNotification } from "../inbox-view";

const copy = workspaceInboxCopy("vi");
const NOW = new Date(2026, 9, 12, 10, 40).getTime();
const at = (day: number, hour: number, minute: number) =>
  new Date(2026, 9, day, hour, minute).toISOString();

const notifications: InboxNotification[] = [
  {
    id: "older",
    title: "Notification 1",
    message: "You were mentioned in a card.",
    isRead: true,
    createdAt: at(11, 17, 10),
  },
  {
    id: "newer",
    title: "Notification 0",
    body: "A comment on your page.",
    isRead: false,
    createdAt: at(12, 10, 28),
    type: "comment",
  },
];

describe("toInboxItems", () => {
  it("orders newest first, reads the text from message or body and bolds the title", () => {
    const items = toInboxItems(notifications, copy, NOW);
    expect(items.map((item) => item.id)).toEqual(["newer", "older"]);
    expect(items[0]!.segments).toEqual([
      { text: "Notification 0", strong: true },
      { text: " A comment on your page." },
    ]);
    expect(items[1]!.segments[1]).toEqual({
      text: " You were mentioned in a card.",
    });
  });

  it("states only what the read carries: unread, time, a kind from the type, nothing else", () => {
    const [newer, older] = toInboxItems(notifications, copy, NOW);
    expect(newer).toMatchObject({
      unread: true,
      kind: "comment",
      person: null,
      context: null,
      need: null,
      following: false,
      bundle: null,
      href: null,
    });
    expect(older).toMatchObject({ unread: false, kind: "system" });
    expect(newer!.detail).toMatchObject({
      crumb: null,
      card: null,
      comment: null,
      replyTo: null,
      openHref: null,
    });
  });

  it("writes the reading pane's time with the day and the clock time", () => {
    const [newer, older] = toInboxItems(notifications, copy, NOW);
    expect(newer!.detail!.event.when).toBe("Hôm nay 10:28");
    expect(older!.detail!.event.when).toBe("Hôm qua 17:10");
  });

  it("leaves the time empty for an unreadable date and a missing text out", () => {
    const [item] = toInboxItems(
      [{ id: "x", title: "Only a title", isRead: false, createdAt: "nope" }],
      copy,
      NOW,
    );
    expect(item!.segments).toEqual([{ text: "Only a title", strong: true }]);
    expect(item!.detail!.event.when).toBe("");
  });

  it("maps every notification type to a kind and an unknown one to system", () => {
    const kinds = [
      "mention",
      "assignment",
      "comment",
      "status_change",
      "invitation",
      "system",
      "weird",
    ].map(
      (type) =>
        toInboxItems(
          [
            {
              id: type,
              title: type,
              isRead: true,
              createdAt: at(12, 9, 0),
              type,
            },
          ],
          copy,
          NOW,
        )[0]!.kind,
    );
    expect(kinds).toEqual([
      "mention",
      "assign",
      "comment",
      "change",
      "invite",
      "system",
      "system",
    ]);
  });

  it("opens the item through the notification's own link, and offers none without one", () => {
    const [withLink, without] = toInboxItems(
      [
        {
          id: "a",
          title: "Có liên kết",
          isRead: false,
          createdAt: at(12, 10, 28),
          link: "/workspaces/w/boards/b/items/i",
        },
        { id: "b", title: "Không có", isRead: false, createdAt: at(12, 9, 0) },
      ],
      copy,
      NOW,
    );
    expect(withLink!.detail!.openHref).toBe("/workspaces/w/boards/b/items/i");
    expect(without!.detail!.openHref).toBeNull();
  });
});
