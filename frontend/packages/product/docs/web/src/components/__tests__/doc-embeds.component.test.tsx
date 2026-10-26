import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { DocLiveTable } from "../doc-article-surface";
import { DocBoardPickerDialogSurface } from "../doc-dialogs-surface";
import type { DocBlockView } from "../doc-page-models";
import { docsCopy } from "../../verification/docs-ui-fixtures";

type Live = Extract<DocBlockView, { kind: "liveTable" }>;

const base: Live = {
  id: "e",
  kind: "liveTable",
  boardId: "b1",
  state: "ready",
  boardName: "Sprint 12",
  filterLabel: "",
  boardHref: "/b/1",
  counts: { todo: 3, doing: 2, review: 0, done: 5 },
  total: 10,
  tasks: [],
};

describe("board embed", () => {
  it("summarises the board by status with an open link", () => {
    renderPureUi(<DocLiveTable block={base} copy={docsCopy} />);
    expect(screen.getByText("Sprint 12")).toBeTruthy();
    expect(
      screen.getByText(/Cần làm 3 · Đang làm 2 · Hoàn thành 5/),
    ).toBeTruthy();
    expect(
      screen.getByRole("link", { name: docsCopy.blocks.openInTable }),
    ).toBeTruthy();
  });

  it("falls back for a board that cannot be read", () => {
    renderPureUi(
      <DocLiveTable
        block={{
          ...base,
          state: "unavailable",
          boardName: "",
          counts: undefined,
        }}
        copy={docsCopy}
      />,
    );
    expect(
      screen.getByText(docsCopy.blocks.boardReference.unavailable),
    ).toBeTruthy();
  });

  it("shows loading, and lets an editor choose when none is set", () => {
    const onOpenBoardPicker = vi.fn();
    const { rerender } = renderPureUi(
      <DocLiveTable block={{ ...base, state: "loading" }} copy={docsCopy} />,
    );
    expect(
      screen.getByText(docsCopy.blocks.boardReference.loading),
    ).toBeTruthy();
    rerender(
      <DocLiveTable
        block={{ ...base, state: "unset", boardId: null }}
        copy={docsCopy}
        readOnly={false}
        callbacks={{ onOpenBoardPicker }}
      />,
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: docsCopy.blocks.boardReference.choose,
      }),
    );
    expect(onOpenBoardPicker).toHaveBeenCalledWith("e");
  });

  it("does not offer to choose in a read-only page", () => {
    renderPureUi(
      <DocLiveTable
        block={{ ...base, state: "unset", boardId: null }}
        copy={docsCopy}
        readOnly
      />,
    );
    expect(
      (
        screen.getByRole("button", {
          name: docsCopy.blocks.boardReference.choose,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });
});

describe("board picker", () => {
  it("filters boards by name and picks one", () => {
    const onPickBoard = vi.fn();
    renderPureUi(
      <DocBoardPickerDialogSurface
        copy={docsCopy}
        blockId="e"
        boards={[
          { id: "b1", title: "Sprint 12" },
          { id: "b2", title: "Marketing" },
        ]}
        callbacks={{ onPickBoard }}
      />,
    );
    fireEvent.change(screen.getByRole("searchbox"), {
      target: { value: "mark" },
    });
    expect(screen.queryByText("Sprint 12")).toBeNull();
    fireEvent.click(screen.getByRole("option", { name: "Marketing" }));
    expect(onPickBoard).toHaveBeenCalledWith("e", "b2");
  });
});
