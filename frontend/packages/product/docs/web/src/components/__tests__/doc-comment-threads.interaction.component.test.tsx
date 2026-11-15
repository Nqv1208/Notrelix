import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import { COMMENTS, TOC, docsCopy } from "../../verification/docs-ui-fixtures";
import type { DocCommentView } from "../doc-page-models";
import { DocSidePanelSurface } from "../doc-page-surfaces";

const AUTHOR = COMMENTS[0]!.author;

const THREADS: readonly DocCommentView[] = [
  {
    id: "open",
    author: AUTHOR,
    quote: null,
    body: "Still open",
    when: "today",
    mine: true,
    canResolve: true,
  },
  {
    id: "done",
    author: AUTHOR,
    quote: "settled text",
    anchored: true,
    body: "Already handled",
    when: "today",
    resolved: true,
    canResolve: true,
  },
];

function panel(callbacks = {}, comments = THREADS) {
  return renderPureUi(
    <DocSidePanelSurface
      copy={docsCopy}
      tab="comments"
      tasks={[]}
      relatedDocs={[]}
      toc={TOC}
      comments={comments}
      canComment
      callbacks={callbacks}
    />,
  );
}

describe("docs comment threads", () => {
  it("FUI[docs.page.side-panel:comment-resolved-hidden] hides resolved threads until Show resolved is on, and counts the open ones", () => {
    panel();
    expect(screen.getByText("Still open")).toBeTruthy();
    expect(screen.queryByText("Already handled")).toBeNull();
    expect(
      screen.getByRole("tab", { name: `${docsCopy.panel.comments} 1` }),
    ).toBeTruthy();
    fireEvent.click(
      screen.getByRole("checkbox", { name: /Hiện bình luận đã xử lý/ }),
    );
    expect(screen.getByText("Already handled")).toBeTruthy();
  });

  it("FUI[docs.page.side-panel:comment-resolve] resolves an open thread and offers no resolve on a resolved one", () => {
    const onCommentResolve = vi.fn();
    panel({ onCommentResolve });
    fireEvent.click(
      screen.getByRole("checkbox", { name: /Hiện bình luận đã xử lý/ }),
    );
    const buttons = screen.getAllByRole("button", {
      name: docsCopy.comments.resolveAction,
    });
    expect(buttons).toHaveLength(1);
    fireEvent.click(buttons[0]!);
    expect(onCommentResolve).toHaveBeenCalledWith("open");
  });

  it("FUI[docs.page.side-panel:comment-reopen] offers Reopen only on a resolved thread and reports it", () => {
    const onCommentReopen = vi.fn();
    panel({ onCommentReopen });
    expect(
      screen.queryByRole("button", { name: docsCopy.comments.reopenAction }),
    ).toBeNull();
    fireEvent.click(
      screen.getByRole("checkbox", { name: /Hiện bình luận đã xử lý/ }),
    );
    const buttons = screen.getAllByRole("button", {
      name: docsCopy.comments.reopenAction,
    });
    expect(buttons).toHaveLength(1);
    fireEvent.click(buttons[0]!);
    expect(onCommentReopen).toHaveBeenCalledWith("done");
  });

  it("FUI[docs.page.side-panel:comment-delete] asks before deleting and only the author is offered Delete", () => {
    const onCommentDelete = vi.fn();
    panel({ onCommentDelete });
    fireEvent.click(
      screen.getByRole("checkbox", { name: /Hiện bình luận đã xử lý/ }),
    );
    expect(
      screen.getAllByRole("button", { name: docsCopy.comments.delete }),
    ).toHaveLength(1);
    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.comments.delete }),
    );
    expect(onCommentDelete).not.toHaveBeenCalled();
    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.comments.deleteKeep }),
    );
    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.comments.delete }),
    );
    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.comments.delete }),
    );
    expect(onCommentDelete).toHaveBeenCalledWith("open");
  });

  it("FUI[docs.page.side-panel:comment-reply] shows replies under their thread and sends one with its thread starter", () => {
    const onCommentCreate = vi.fn();
    panel({ onCommentCreate }, [
      {
        ...THREADS[0]!,
        replies: [{ ...THREADS[0]!, id: "r1", body: "Answered", mine: false }],
      },
    ]);
    expect(screen.getByText("Answered")).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: docsCopy.comments.replyAction }),
    );
    const field = screen.getByRole("textbox", {
      name: docsCopy.comments.reply,
    });
    fireEvent.change(field, { target: { value: "Thanks" } });
    fireEvent.keyDown(field, { key: "Enter" });
    expect(onCommentCreate).toHaveBeenCalledWith("Thanks", "open");
  });

  it("FUI[docs.page.side-panel:comment-compose-limits] refuses a comment over the limit and tells a viewer they cannot comment", () => {
    const onCommentCreate = vi.fn();
    const view = panel({ onCommentCreate });
    const field = screen.getByRole("textbox", {
      name: docsCopy.comments.compose,
    });
    fireEvent.change(field, { target: { value: "x".repeat(10_012) } });
    expect(screen.getByRole("alert").textContent).toContain("12");
    expect(
      (
        screen.getByRole("button", {
          name: docsCopy.comments.send,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    fireEvent.keyDown(field, { key: "Enter" });
    expect(onCommentCreate).not.toHaveBeenCalled();
    view.unmount();
    renderPureUi(
      <DocSidePanelSurface
        copy={docsCopy}
        tab="comments"
        tasks={[]}
        relatedDocs={[]}
        toc={TOC}
        comments={[]}
        callbacks={{}}
      />,
    );
    expect(screen.getByText(docsCopy.comments.denied)).toBeTruthy();
    expect(screen.queryByRole("textbox")).toBeNull();
    expect(screen.getByText(docsCopy.comments.emptyTitle)).toBeTruthy();
  });

  it("offers resolve and reopen only to the thread starter or an editor", () => {
    // Someone else's threads, viewer cannot edit the page: nothing to resolve.
    panel({}, [
      { ...THREADS[0]!, mine: false, canResolve: false },
      { ...THREADS[1]!, canResolve: false },
    ]);
    fireEvent.click(
      screen.getByRole("checkbox", { name: /Hiện bình luận đã xử lý/ }),
    );
    expect(
      screen.queryByRole("button", { name: docsCopy.comments.resolveAction }),
    ).toBeNull();
    expect(
      screen.queryByRole("button", { name: docsCopy.comments.reopenAction }),
    ).toBeNull();
    expect(
      screen.queryByRole("button", { name: docsCopy.comments.delete }),
    ).toBeNull();
  });
});
