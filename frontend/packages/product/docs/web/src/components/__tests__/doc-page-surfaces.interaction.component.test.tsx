import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  docsCommentsDefaultScenario,
  docsPageScreenDefaultScenario,
  docsPageScreenEmptyScenario,
  docsPageTreeDefaultScenario,
} from "../../verification/docs-ui-fixtures";
import {
  DocCommentsSurface,
  DocPageHeaderSurface,
  DocPageScreenSurface,
  DocPageTreeSurface,
} from "../doc-page-surfaces";

describe("docs web pure surfaces", () => {
  it("renders the composed document screen from deterministic fixture data", () => {
    renderPureUi(<DocPageScreenSurface {...docsPageScreenDefaultScenario()} />);

    expect(
      screen.getByRole("heading", { name: "Operating plan" }),
    ).toBeTruthy();
    expect(screen.getByDisplayValue("Operating plan")).toBeTruthy();
    expect(screen.getByText("Migration risks")).toBeTruthy();
    expect(
      screen.getByText("Can we link this to the launch checklist?"),
    ).toBeTruthy();
  });

  it("routes page tree create actions through injected callbacks", () => {
    const onAddPage = vi.fn();

    renderPureUi(
      <DocPageTreeSurface
        pages={docsPageTreeDefaultScenario()}
        workspaceId="workspace-docs"
        currentPageId="page-operating-plan"
        callbacks={{ onAddPage }}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Create root page" }));
    fireEvent.click(
      screen.getByRole("button", { name: "Add page under Operating plan" }),
    );

    expect(onAddPage).toHaveBeenNthCalledWith(1, null);
    expect(onAddPage).toHaveBeenNthCalledWith(2, "page-operating-plan");
  });

  it("routes header favorite/share/actions through injected callbacks", () => {
    const onToggleFavorite = vi.fn();
    const onShare = vi.fn();
    const onAction = vi.fn();

    renderPureUi(
      <DocPageHeaderSurface
        pageTitle="Operating plan"
        breadcrumbs={[
          { id: "page-operating-plan", title: "Operating plan", icon: "📘" },
        ]}
        isFavorited={false}
        callbacks={{ onToggleFavorite, onShare, onAction }}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Favorite" }));
    fireEvent.click(screen.getByRole("button", { name: "Share" }));
    fireEvent.click(screen.getByRole("button", { name: "Document actions" }));
    fireEvent.click(screen.getByRole("button", { name: "Export" }));

    expect(onToggleFavorite).toHaveBeenCalledTimes(1);
    expect(onShare).toHaveBeenCalledTimes(1);
    expect(onAction).toHaveBeenCalledWith("Export");
  });

  it("routes comment composition and deletion through injected callbacks", () => {
    const onCreateComment = vi.fn();
    const onDeleteComment = vi.fn();

    renderPureUi(
      <DocCommentsSurface
        comments={docsCommentsDefaultScenario()}
        callbacks={{ onCreateComment, onDeleteComment }}
      />,
    );

    fireEvent.change(screen.getByRole("textbox", { name: "New comment" }), {
      target: { value: "Ready for review" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Send comment" }));
    fireEvent.click(
      screen.getByRole("button", { name: "Delete comment comment-1" }),
    );

    expect(onCreateComment).toHaveBeenCalledWith("Ready for review");
    expect(onDeleteComment).toHaveBeenCalledWith("comment-1");
  });

  it("renders the empty document state without state/query providers", () => {
    renderPureUi(<DocPageScreenSurface {...docsPageScreenEmptyScenario()} />);

    expect(screen.getByText("No pages yet")).toBeTruthy();
    expect(
      screen.getByText(
        "Start writing with a heading, paragraph, checklist, or callout.",
      ),
    ).toBeTruthy();
    expect(screen.getByText("No comments yet")).toBeTruthy();
    expect(screen.getByText("No history yet")).toBeTruthy();
  });
});
