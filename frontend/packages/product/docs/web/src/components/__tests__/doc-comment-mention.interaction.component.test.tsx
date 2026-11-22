import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import { COMMENTS, TOC, docsCopy } from "../../verification/docs-ui-fixtures";
import type {
  DocCommentView,
  DocMentionPeople,
  DocPageCallbacks,
} from "../doc-page-models";
import { DocSidePanelSurface } from "../doc-page-surfaces";

const AUTHOR = COMMENTS[0]!.author;
const THREAD: DocCommentView = {
  id: "open",
  author: AUTHOR,
  quote: null,
  body: "Still open",
  when: "today",
  canResolve: true,
};
const BINH = { id: "u-binh", name: "Bình Trần", initials: "BT", tone: "a" };
const ok = (people = [BINH]): DocMentionPeople => ({
  people: people as DocMentionPeople["people"],
  status: "success",
});

function panel(
  people: DocMentionPeople,
  callbacks: DocPageCallbacks = {},
  pendingQuote: string | null = null,
) {
  renderPureUi(
    <DocSidePanelSurface
      copy={docsCopy}
      tab="comments"
      tasks={[]}
      relatedDocs={[]}
      toc={TOC}
      comments={[THREAD]}
      pendingQuote={pendingQuote}
      canComment
      mentionPeople={people}
      callbacks={callbacks}
    />,
  );
}

const composer = () =>
  screen.getByRole("textbox", { name: docsCopy.comments.compose });
const reply = () => {
  fireEvent.click(
    screen.getByRole("button", { name: docsCopy.comments.replyAction }),
  );
  return screen.getByRole("textbox", { name: docsCopy.comments.reply });
};
const type = (field: HTMLElement, text: string) =>
  fireEvent.change(field, {
    target: { value: text, selectionStart: text.length },
  });

describe.each([
  ["page composer", composer],
  ["reply field", reply],
])("comment @mention list in the %s (server read)", (_name, field) => {
  const m = docsCopy.mention;

  it("lists only the people the host returned", () => {
    panel(ok());
    type(field(), "Hi @b");
    expect(
      screen.getAllByRole("option").map((option) => option.textContent),
    ).toEqual(["BTBình Trần"]);
    expect(document.body.textContent).not.toContain("Ân Nguyễn");
  });

  it("says it is loading", () => {
    panel({ people: [], status: "loading" });
    type(field(), "@");
    expect(screen.getByRole("status").textContent).toBe(m.peopleLoading);
  });

  it("says nobody can be mentioned", () => {
    panel(ok([]));
    type(field(), "@zz");
    expect(screen.getByRole("status").textContent).toBe(m.peopleEmpty);
  });

  it("says the read failed and retries it", () => {
    const onRetry = vi.fn();
    panel({ people: [], status: "error", onRetry });
    type(field(), "@");
    expect(screen.getByRole("alert").textContent).toContain(m.peopleError);
    fireEvent.click(screen.getByRole("button", { name: m.peopleRetry }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it("picks with Enter without sending, then sends the id", () => {
    const onCommentCreate = vi.fn();
    panel(ok(), { onCommentCreate });
    const input = field() as HTMLTextAreaElement;
    type(input, "Hi @b");
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onCommentCreate).not.toHaveBeenCalled();
    expect(input.value).toBe("Hi @Bình Trần ");
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onCommentCreate).toHaveBeenCalledTimes(1);
    expect(onCommentCreate.mock.calls[0]?.[0]).toBe("Hi @Bình Trần");
    expect(onCommentCreate.mock.calls[0]?.[2]).toEqual(["u-binh"]);
  });

  it("does not send a person whose name was deleted from the text", () => {
    const onCommentCreate = vi.fn();
    panel(ok(), { onCommentCreate });
    const input = field();
    type(input, "Hi @b");
    fireEvent.keyDown(input, { key: "Tab" });
    type(input, "Hi there");
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onCommentCreate.mock.calls[0]).not.toContain("u-binh");
    expect(onCommentCreate.mock.calls[0]?.[2]).toBeUndefined();
  });

  it("reports the typed query to the host and Escape only closes the list", () => {
    const onCommentMentionQuery = vi.fn();
    const onCommentCreate = vi.fn();
    panel(ok(), { onCommentMentionQuery, onCommentCreate });
    const input = field();
    type(input, "Hi @bi");
    expect(onCommentMentionQuery).toHaveBeenLastCalledWith("bi");
    fireEvent.keyDown(input, { key: "Escape" });
    expect(screen.queryByRole("listbox")).toBeNull();
    expect(onCommentMentionQuery).toHaveBeenLastCalledWith(null);
    expect(onCommentCreate).not.toHaveBeenCalled();
  });
});

describe("page comment @mention", () => {
  it("sends the id with the reply's parent", () => {
    const onCommentCreate = vi.fn();
    panel(ok(), { onCommentCreate });
    const input = reply();
    type(input, "@b");
    fireEvent.keyDown(input, { key: "Enter" });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onCommentCreate).toHaveBeenCalledWith("@Bình Trần", "open", [
      "u-binh",
    ]);
  });

  it("sends the id of a comment written on a selection", () => {
    const onCommentCreate = vi.fn();
    panel(ok(), { onCommentCreate }, "selected words");
    const input = composer();
    type(input, "@b");
    fireEvent.keyDown(input, { key: "Enter" });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onCommentCreate).toHaveBeenCalledWith("@Bình Trần", undefined, [
      "u-binh",
    ]);
  });

  it("still sends with Enter when no list is open", () => {
    const onCommentCreate = vi.fn();
    panel(ok(), { onCommentCreate });
    const input = composer();
    type(input, "plain");
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onCommentCreate).toHaveBeenCalledWith("plain");
  });
});
