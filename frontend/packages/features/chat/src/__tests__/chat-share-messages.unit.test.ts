import { describe, expect, it } from "vitest";
import type { InfiniteData } from "@tanstack/react-query";
import { shareMessages } from "../web/query/create-chat-hooks";
import type {
  ChatMessage,
  ChatMessageCursor,
  ChatMessagePage,
} from "../core/types/chat";

function msg(sequence: number, content = `m${sequence}`): ChatMessage {
  return { id: `m-${sequence}`, sequence, content } as unknown as ChatMessage;
}
function data(...pages: ChatMessage[][]) {
  return {
    pages: pages.map((items) => ({ items, hasOlder: false, hasNewer: false })),
    pageParams: pages.map(() => ({})),
  } as unknown as InfiniteData<ChatMessagePage, ChatMessageCursor>;
}
const items = (d: unknown, page: number) =>
  (d as InfiniteData<ChatMessagePage>).pages[page]!.items;

describe("shareMessages", () => {
  it("keeps every loaded message when an older page is prepended", () => {
    const before = data([msg(3), msg(4)]);
    const kept = items(before, 0);
    const after = shareMessages(
      before,
      data([msg(1), msg(2)], [msg(3), msg(4)]),
    );
    expect(items(after, 1)[0]).toBe(kept[0]);
    expect(items(after, 1)[1]).toBe(kept[1]);
  });

  it("takes the new object of a message that changed", () => {
    const before = data([msg(1), msg(2)]);
    const after = shareMessages(before, data([msg(1), msg(2, "edited")]));
    expect(items(after, 0)[0]).toBe(items(before, 0)[0]);
    expect(items(after, 0)[1]).not.toBe(items(before, 0)[1]);
    expect(items(after, 0)[1]!.content).toBe("edited");
  });

  it("returns the new data when nothing was loaded before", () => {
    const next = data([msg(1)]);
    expect(shareMessages(undefined, next)).toBe(next);
  });
});
