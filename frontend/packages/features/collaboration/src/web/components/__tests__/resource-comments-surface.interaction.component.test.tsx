import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import { comment } from "../../../verification/collaboration-ui-fixtures";
import {
  resourceCommentsDefaultScenario,
  resourceCommentsEmptyScenario,
} from "../../../verification/collaboration-ui-fixtures";
import { ResourceCommentsSurface } from "../resource-comments-surface";

describe("collaboration web pure surface", () => {
  it("FUI[collaboration.comments:render] renders comments from deterministic fixtures", () => {
    renderPureUi(
      <ResourceCommentsSurface
        comments={resourceCommentsDefaultScenario()}
        currentUserId="current-user"
      />,
    );

    expect(
      screen.getByText("Can we link this to the launch checklist?"),
    ).toBeTruthy();
  });

  it("FUI[collaboration.comments:submit] routes comment creation and deletion through injected callbacks", () => {
    const onCreateComment = vi.fn();
    const onDeleteComment = vi.fn();

    renderPureUi(
      <ResourceCommentsSurface
        comments={resourceCommentsDefaultScenario()}
        currentUserId="current-user"
        onCreateComment={onCreateComment}
        onDeleteComment={onDeleteComment}
      />,
    );

    fireEvent.change(screen.getByLabelText("New comment"), {
      target: { value: "Ready for review" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Send comment" }));
    const remove = screen.getByRole("button", {
      name: 'Delete comment by You: "Resolved after adding the owner evidence…"',
    });
    // Hover-revealed actions must also appear on keyboard focus and touch.
    expect(remove.className).toContain("focus-visible:opacity-100");
    expect(remove.className).toContain("pointer-coarse:opacity-100");
    fireEvent.click(remove);

    expect(onCreateComment).toHaveBeenCalledWith("Ready for review");
    expect(onDeleteComment).toHaveBeenCalledWith("comment-2");
  });

  it("names each delete action after its comment, never its id", () => {
    renderPureUi(
      <ResourceCommentsSurface
        comments={[
          comment({
            id: "comment-a",
            authorId: "current-user",
            authorName: "You",
            body: "First note",
          }),
          comment({
            id: "comment-b",
            authorId: "current-user",
            authorName: "You",
            body: "Second note that is long enough to be truncated in the accessible name of its action",
          }),
        ]}
        currentUserId="current-user"
      />,
    );

    const names = screen
      .getAllByRole("button", { name: /^Delete comment/ })
      .map((button) => button.getAttribute("aria-label"));

    expect(names).toEqual([
      'Delete comment by You: "First note"',
      'Delete comment by You: "Second note that is long enough to be tr…"',
    ]);
    expect(new Set(names).size).toBe(2);
    expect(names.join(" ")).not.toMatch(/comment-[ab]/);
  });

  it("FUI[collaboration.comments:empty] renders the empty comments state without query providers", () => {
    renderPureUi(
      <ResourceCommentsSurface
        comments={resourceCommentsEmptyScenario()}
        currentUserId="current-user"
      />,
    );

    expect(screen.getByText("No comments yet.")).toBeTruthy();
  });
});
