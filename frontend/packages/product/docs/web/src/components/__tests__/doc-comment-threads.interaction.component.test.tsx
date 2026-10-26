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
  },
  {
    id: "done",
    author: AUTHOR,
    quote: "settled text",
    anchored: true,
    body: "Already handled",
    when: "today",
    resolved: true,
  },
];

function panel(callbacks = {}) {
  return renderPureUi(
    <DocSidePanelSurface
      copy={docsCopy}
      tab="comments"
      tasks={[]}
      relatedDocs={[]}
      toc={TOC}
      comments={THREADS}
      canComment
      callbacks={callbacks}
    />,
  );
}

describe("docs comment threads", () => {
  it("FUI[docs.page.side-panel:comment-resolve] hides resolved threads until Show resolved is on, and counts the open ones", () => {
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
      name: docsCopy.comments.resolveThread,
    });
    expect(buttons).toHaveLength(1);
    fireEvent.click(buttons[0]!);
    expect(onCommentResolve).toHaveBeenCalledWith("open");
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
});
