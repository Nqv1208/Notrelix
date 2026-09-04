import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  resourceCommentsDefaultScenario,
  resourceCommentsEmptyScenario,
} from "../../../verification/collaboration-ui-fixtures";
import { ResourceCommentsSurface } from "../resource-comments-surface";

describe("collaboration web pure surface", () => {
  it("renders comments from deterministic fixtures", () => {
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

  it("routes comment creation and deletion through injected callbacks", () => {
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
    fireEvent.click(
      screen.getByRole("button", { name: "Delete comment comment-2" }),
    );

    expect(onCreateComment).toHaveBeenCalledWith("Ready for review");
    expect(onDeleteComment).toHaveBeenCalledWith("comment-2");
  });

  it("renders the empty comments state without query providers", () => {
    renderPureUi(
      <ResourceCommentsSurface
        comments={resourceCommentsEmptyScenario()}
        currentUserId="current-user"
      />,
    );

    expect(screen.getByText("No comments yet.")).toBeTruthy();
  });
});
