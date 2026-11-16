import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@notrelix/testing";
import { ChatMessageList } from "../web/components/chat-message-list";
import { buildChatRows } from "../core/model/chat-model";
import type { ChatMessage } from "../core/types/chat";
import { en } from "../web/components/chat.copy";

const TZ = "UTC";

function message(sequence: number): ChatMessage {
  return {
    id: `m-${sequence}`,
    spaceId: "space-1",
    sequence,
    kind: "user",
    authorId: "user-other",
    content: `text ${sequence}`,
    clientMessageId: null,
    replyToId: null,
    edited: false,
    deleted: false,
    createdAt: "2026-11-10T09:00:00.000Z",
    editedAt: null,
    deletedAt: null,
    version: 1,
  };
}

function listProps(count: number) {
  const messages = Array.from({ length: count }, (_, i) => message(i + 1));
  return {
    channelName: "general",
    rows: buildChatRows({
      messages,
      pending: [],
      firstUnread: null,
      timeZone: TZ,
    }),
    status: "ready" as const,
    hasOlder: false,
    isLoadingOlder: false,
    hasNewer: false,
    isLoadingNewer: false,
    scrollRequest: null,
    editing: null,
    staleNotice: null,
    viewerId: "user-me",
    authorName: () => "Other person",
    locale: "en",
    timeZone: TZ,
    copy: en,
    onRetryLoad: vi.fn(),
    onOlderNeeded: vi.fn(),
    onNewerNeeded: vi.fn(),
    onAtBottomChange: vi.fn(),
    onJumpLatest: vi.fn(),
    onEditDraftChange: vi.fn(),
    onEditSave: vi.fn(),
    onEditCancel: vi.fn(),
    onRetryPending: vi.fn(),
    onDiscardPending: vi.fn(),
    pendingEntries: [],
  };
}

// jsdom has no layout: the list falls back to normal flow without one, so this gives it a 600 px viewport and 50 px rows.
describe("ChatMessageList: a long history", () => {
  beforeEach(() => {
    vi.spyOn(HTMLElement.prototype, "getBoundingClientRect").mockImplementation(
      function (this: HTMLElement) {
        const height = this.tagName === "LI" ? 50 : 600;
        return {
          x: 0,
          y: 0,
          top: 0,
          left: 0,
          right: 760,
          bottom: height,
          width: 760,
          height,
          toJSON: () => ({}),
        };
      },
    );
    vi.spyOn(HTMLElement.prototype, "offsetHeight", "get").mockReturnValue(600);
    vi.spyOn(HTMLElement.prototype, "offsetWidth", "get").mockReturnValue(760);
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("keeps a few dozen rows in the page and tells assistive technology the full size", () => {
    render(<ChatMessageList {...listProps(500)} />);
    const log = screen.getByRole("log");
    const items = log.querySelectorAll("li");
    expect(items.length).toBeGreaterThan(0);
    expect(items.length).toBeLessThan(80);
    const positioned = log.querySelectorAll("li[aria-setsize]");
    expect(positioned.length).toBeGreaterThan(0);
    expect(Number(positioned[0]!.getAttribute("aria-setsize"))).toBe(
      listProps(500).rows.length,
    );
    // A list may hold only list items (axe list rule): no row drops its role.
    expect(log.querySelectorAll('li[role="presentation"]')).toHaveLength(0);
  });
});
