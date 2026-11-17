import { describe, expect, it, vi } from "vitest";
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

const TABBABLE =
  'a[href],button,input,textarea,select,[tabindex]:not([tabindex="-1"])';

describe("ChatMessageList: keyboard", () => {
  it("is one Tab stop however long the history, and the arrow keys, Enter and Escape move through rows", async () => {
    render(
      <div>
        <ChatMessageList
          {...listProps(500)}
          onReply={vi.fn()}
          onReact={vi.fn()}
        />
        <button type="button">next region</button>
      </div>,
    );
    const log = screen.getByRole("log");
    await waitFor(() => expect(log.querySelector("li")).not.toBeNull());
    // The scroller is the only stop inside the list: no row and no control in a row is reachable with Tab.
    await waitFor(() =>
      expect(
        Array.from(log.querySelectorAll(TABBABLE)).filter(
          (el) => el.getAttribute("tabindex") !== "-1",
        ),
      ).toHaveLength(0),
    );
    expect(log.getAttribute("tabindex")).toBe("0");
    expect(log.querySelectorAll("button").length).toBeGreaterThan(0);

    log.focus();
    fireEvent.keyDown(log, { key: "ArrowUp" });
    const newest = log.querySelector<HTMLElement>('[data-message-id="m-500"]');
    expect(document.activeElement).toBe(newest);
    fireEvent.keyDown(newest!, { key: "ArrowUp" });
    expect(document.activeElement).toBe(
      log.querySelector('[data-message-id="m-499"]'),
    );
    fireEvent.keyDown(document.activeElement!, { key: "Home" });
    expect(document.activeElement).toBe(
      log.querySelector('[data-message-id="m-1"]'),
    );
    fireEvent.keyDown(document.activeElement!, { key: "End" });
    expect(document.activeElement).toBe(newest);

    fireEvent.keyDown(newest!, { key: "Enter" });
    const inside = document.activeElement as HTMLElement;
    expect(inside).not.toBe(newest);
    expect(newest!.contains(inside)).toBe(true);
    fireEvent.keyDown(inside, { key: "Escape" });
    expect(document.activeElement).toBe(newest);
  });
});
