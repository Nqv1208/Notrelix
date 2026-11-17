import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
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

  it("draws every loaded row, cheap off-screen, each under its stable key", async () => {
    render(<ChatMessageList {...listProps(500)} />);
    const log = screen.getByRole("log");
    await waitFor(() =>
      expect(log.querySelectorAll("li").length).toBeGreaterThan(0),
    );
    const items = Array.from(log.querySelectorAll<HTMLElement>("li"));
    expect(items).toHaveLength(listProps(500).rows.length);
    for (const item of items.slice(0, 5)) {
      expect(item.dataset.rowKey).toBeTruthy();
      expect(item.style.contentVisibility).toBe("auto");
      expect(item.style.containIntrinsicSize).toContain("auto");
    }
    // A list may hold only list items (axe list rule): no row drops its role.
    expect(log.querySelectorAll('li[role="presentation"]')).toHaveLength(0);
  });

  it("puts the reader's row back at the same offset when an older page lands above it", async () => {
    const tops = new Map<string, number>();
    vi.spyOn(HTMLElement.prototype, "offsetTop", "get").mockImplementation(
      function (this: HTMLElement) {
        return tops.get(this.dataset.rowKey ?? "") ?? 0;
      },
    );
    const layout = (rows: ReturnType<typeof listProps>["rows"]) => {
      // 50 px rows, the first one starting at 0.
      rows.forEach((row, i) => tops.set(row.key, i * 50));
    };
    const first = listProps(60);
    const recent = { ...first, rows: first.rows.slice(30) };
    layout(recent.rows);
    const { rerender } = render(<ChatMessageList {...recent} />);
    const log = screen.getByRole("log");
    await waitFor(() => expect(log.querySelector("li")).not.toBeNull());
    log.scrollTop = 100;
    fireEvent.scroll(log);
    layout(first.rows);
    rerender(<ChatMessageList {...first} />);
    // Thirty rows of 50 px were added above: the same row stays 100 px under the top edge.
    await waitFor(() => expect(log.scrollTop).toBe(30 * 50 + 100));
  });
});
