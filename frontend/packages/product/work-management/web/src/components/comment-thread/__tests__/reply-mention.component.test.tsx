import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { itemPageCopy } from "../../item-page/item-page.copy";
import {
  WmUiPortsProvider,
  type MentionSuggestionsView,
  type WmUiPorts,
} from "../../shared/wm-ui-ports";
import { threadEn } from "../comment-thread.copy";
import { ReplyBox } from "../reply-box";

const pageCopy = itemPageCopy("en");
const copy = pageCopy.composer;
const BINH = { userId: "u-binh", name: "Bình Trần", avatarUrl: null };

function show(
  view: MentionSuggestionsView,
  onCreateUpdate: (input: unknown, options?: any) => void = vi.fn(),
) {
  const calls: (string | null)[] = [];
  const ports: WmUiPorts = {
    useCardImageUrl: () => null,
    useRelationTarget: () => ({
      groups: [],
      isLoading: false,
      error: null,
      refetch: () => undefined,
    }),
    useMentionSuggestions: (workspaceId, itemId, query) => {
      calls.push(`${workspaceId}/${itemId}/${query}`);
      return view;
    },
  };
  renderPureUi(
    <WmUiPortsProvider ports={ports}>
      <ReplyBox
        cardId="item-1"
        workspaceId="ws-1"
        parentCommentId="c-1"
        authorName="Ada"
        copy={threadEn}
        mentionCopy={pageCopy}
        onCreateUpdate={onCreateUpdate as never}
        onClose={vi.fn()}
      />
    </WmUiPortsProvider>,
  );
  const field = screen.getByRole("textbox") as HTMLTextAreaElement;
  const type = (text: string) =>
    fireEvent.change(field, {
      target: { value: text, selectionStart: text.length },
    });
  return { calls, field, type, onCreateUpdate };
}

const ok = (people = [BINH]): MentionSuggestionsView => ({
  people,
  status: "success",
  retry: vi.fn(),
});

describe("reply @mention list (server read)", () => {
  it("offers only the people the server returned", () => {
    const { calls, type } = show(ok());
    type("Hi @b");
    expect(calls.at(-1)).toBe("ws-1/item-1/b");
    expect(
      screen.getAllByRole("option").map((option) => option.textContent),
    ).toEqual(["BTBình Trần"]);
    expect(document.body.textContent).not.toContain("Ân Nguyễn");
  });

  it("says it is loading", () => {
    const { type } = show({ people: [], status: "loading", retry: vi.fn() });
    type("@");
    expect(screen.getByRole("status").textContent).toBe(copy.mentionLoading);
  });

  it("says there is nobody to mention", () => {
    const { type } = show(ok([]));
    type("@zz");
    expect(screen.getByRole("status").textContent).toBe(copy.mentionEmpty);
  });

  it("says the read failed and retries it", () => {
    const retry = vi.fn();
    const { type } = show({ people: [], status: "error", retry });
    type("@");
    expect(screen.getByRole("alert").textContent).toContain(copy.mentionError);
    fireEvent.click(screen.getByRole("button", { name: copy.mentionRetry }));
    expect(retry).toHaveBeenCalledTimes(1);
  });

  it("picks with Enter without sending, then sends the id with the parent", () => {
    const onCreateUpdate = vi.fn();
    const { type, field } = show(ok(), onCreateUpdate);
    type("Hi @b");
    // Plain Enter picks and sends nothing; Ctrl+Enter is the send chord.
    fireEvent.keyDown(field, { key: "Enter" });
    expect(onCreateUpdate).not.toHaveBeenCalled();
    expect(field.value).toBe("Hi @Bình Trần ");
    fireEvent.keyDown(field, { key: "Enter", ctrlKey: true });
    expect(onCreateUpdate).toHaveBeenCalledWith(
      expect.objectContaining({
        cardId: "item-1",
        parentCommentId: "c-1",
        mentionUserIds: ["u-binh"],
        body: "Hi @Bình Trần",
      }),
      expect.anything(),
    );
  });

  it("does not send a person whose name was deleted from the text", () => {
    const onCreateUpdate = vi.fn();
    const { type, field } = show(ok(), onCreateUpdate);
    type("Hi @b");
    fireEvent.keyDown(field, { key: "Enter" });
    type("Hi there");
    fireEvent.keyDown(field, { key: "Enter", ctrlKey: true });
    expect(onCreateUpdate.mock.calls[0]?.[0]).toMatchObject({
      mentionUserIds: [],
    });
  });

  it("keeps the text and the people when the post fails", () => {
    const onCreateUpdate = vi.fn((_input, options) => options?.onSettled?.());
    const { type, field } = show(ok(), onCreateUpdate);
    type("Hi @b");
    fireEvent.keyDown(field, { key: "Enter" });
    fireEvent.keyDown(field, { key: "Enter", ctrlKey: true });
    expect(field.value).toBe("Hi @Bình Trần ");
    fireEvent.keyDown(field, { key: "Enter", ctrlKey: true });
    expect(onCreateUpdate).toHaveBeenCalledTimes(2);
    expect(onCreateUpdate.mock.calls[1]?.[0]).toMatchObject({
      mentionUserIds: ["u-binh"],
    });
  });
});
