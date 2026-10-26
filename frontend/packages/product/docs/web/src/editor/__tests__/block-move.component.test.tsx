import { beforeAll, describe, expect, it, vi } from "vitest";
import { act, fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { planBlockWrites } from "@notrelix/docs-state";
import type { Editor } from "@tiptap/core";
import { NodeSelection, TextSelection } from "@tiptap/pm/state";
import { docsCopy } from "../../verification/docs-ui-fixtures";
import { BlockHandle } from "../block-handle";
import { docToDrafts } from "../block-mapping";
import { moveBlock, moveBlockBy } from "../block-move";
import { headlessEditor, paragraph, stubLayout } from "./headless-editor";

beforeAll(stubLayout);

const blocks = () => [
  { ...paragraph("a", "First"), version: 1 },
  { ...paragraph("b", "Second"), version: 2 },
  { ...paragraph("c", "Third"), version: 3 },
];
const order = (editor: Editor) =>
  docToDrafts(editor.getJSON()).map((draft) => draft.id);

function press(editor: Editor, key: string, init: KeyboardEventInit = {}) {
  const event = new KeyboardEvent("keydown", {
    key,
    bubbles: true,
    cancelable: true,
    ...init,
  });
  return Boolean(
    editor.view.someProp("handleKeyDown", (handler) =>
      handler(editor.view, event),
    ),
  );
}

describe("moving blocks", () => {
  it("moves a block to another place keeping its id and the caret in it", () => {
    const editor = headlessEditor(blocks());
    // Caret inside "Second", two characters in.
    editor.view.dispatch(
      editor.state.tr.setSelection(TextSelection.create(editor.state.doc, 10)),
    );
    expect(moveBlock(editor, 1, 0)).toBe(true);
    expect(order(editor)).toEqual(["b", "a", "c"]);
    const { $from } = editor.state.selection;
    expect($from.parent.textContent).toBe("Second");
    expect($from.parentOffset).toBe(2);
    expect(moveBlock(editor, 0, 2)).toBe(true);
    expect(order(editor)).toEqual(["a", "c", "b"]);
  });

  it("does nothing past either end or in a read-only page", () => {
    const editor = headlessEditor(blocks());
    expect(moveBlock(editor, 0, -1)).toBe(false);
    expect(moveBlock(editor, 2, 3)).toBe(false);
    editor.setEditable(false);
    expect(moveBlock(editor, 0, 1)).toBe(false);
    expect(order(editor)).toEqual(["a", "b", "c"]);
  });

  it("moves the block holding the caret with Alt+Shift+Arrow", () => {
    const editor = headlessEditor(blocks());
    editor.view.dispatch(
      editor.state.tr.setSelection(TextSelection.create(editor.state.doc, 9)),
    );
    expect(press(editor, "ArrowDown", { altKey: true, shiftKey: true })).toBe(
      true,
    );
    expect(order(editor)).toEqual(["a", "c", "b"]);
    expect(press(editor, "ArrowUp", { altKey: true, shiftKey: true })).toBe(
      true,
    );
    expect(order(editor)).toEqual(["a", "b", "c"]);
    // Without the modifiers the arrow keys stay the caret's.
    expect(press(editor, "ArrowDown")).toBe(false);
    expect(moveBlockBy(editor, -1)).toBe(true);
  });

  it("is saved as placements after the predecessor with the stored versions", () => {
    const editor = headlessEditor(blocks());
    const stored = docToDrafts(editor.getJSON()).map((draft, index) => ({
      ...draft,
      version: index + 1,
    }));
    moveBlock(editor, 2, 0);
    const target = docToDrafts(editor.getJSON()).map((draft) => ({
      ...draft,
      version: stored.find((entry) => entry.id === draft.id)?.version,
    }));
    const writes = planBlockWrites(stored, target);
    // The producer cannot place a block first: the blocks behind it follow it.
    expect(writes.map((write) => write.id)).toEqual(["a", "b"]);
    expect(writes[0]).toMatchObject({
      id: "a",
      afterId: "c",
      expectedVersion: 1,
    });
    expect(writes[1]).toMatchObject({
      id: "b",
      afterId: "a",
      expectedVersion: 2,
    });
  });
});

describe("block handle", () => {
  it("appears at the hovered block, drags it as a move and announces a keyboard move", async () => {
    vi.stubGlobal("requestAnimationFrame", (run: () => void) => {
      run();
      return 1;
    });
    const editor = headlessEditor(blocks());
    const surface = document.createElement("div");
    surface.append(editor.options.element as HTMLElement);
    document.body.append(surface);
    // jsdom has no layout: the pointer is "over" the second block.
    editor.view.posAtCoords = () => ({ pos: 10, inside: 7 });
    renderPureUi(
      <BlockHandle
        editor={editor}
        copy={docsCopy}
        surface={{ current: surface }}
      />,
    );
    expect(
      screen.queryByRole("button", { name: docsCopy.blockHandle.label }),
    ).toBeNull();
    await act(async () => {
      fireEvent.mouseMove(surface, { clientX: 5, clientY: 5 });
    });
    const grip = screen.getByRole("button", {
      name: docsCopy.blockHandle.label,
    });
    expect(grip.getAttribute("draggable")).toBe("true");

    const transfer = {
      effectAllowed: "",
      setData: vi.fn(),
      setDragImage: vi.fn(),
    };
    await act(async () => {
      fireEvent.dragStart(grip, { dataTransfer: transfer });
    });
    const dragging = (editor.view as unknown as { dragging: { move: boolean } })
      .dragging;
    expect(dragging.move).toBe(true);
    expect(editor.state.selection).toBeInstanceOf(NodeSelection);
    expect(editor.state.selection.from).toBe(7);

    await act(async () => {
      fireEvent.mouseMove(surface, { clientX: 5, clientY: 5 });
    });
    await act(async () => {
      fireEvent.keyDown(
        screen.getByRole("button", { name: docsCopy.blockHandle.label }),
        { key: "ArrowUp", altKey: true, shiftKey: true },
      );
    });
    expect(order(editor)).toEqual(["b", "a", "c"]);
    expect(screen.getByRole("status").textContent).toBe(
      "Đã chuyển khối đến vị trí 1 trên 3",
    );
    vi.unstubAllGlobals();
    surface.remove();
  });
});
