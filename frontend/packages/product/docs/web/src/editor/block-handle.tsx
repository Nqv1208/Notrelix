import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type DragEvent,
  type KeyboardEvent,
  type RefObject,
} from "react";
import type { Editor } from "@tiptap/react";
import { NodeSelection } from "@tiptap/pm/state";
import { GripVertical } from "lucide-react";
import { fillDocCopy, type DocPageCopy } from "../components/doc-page.copy";
import { BLOCK_MOVE_META, moveBlock, type BlockMoveInfo } from "./block-move";

interface Hovered {
  readonly index: number;
  /** Top of the block and its first line, relative to the editor surface. */
  readonly top: number;
}

function blockStart(editor: Editor, index: number): number {
  let start = 0;
  for (let at = 0; at < index; at += 1) {
    start += editor.state.doc.child(at).nodeSize;
  }
  return start;
}

function blockElement(editor: Editor, index: number): HTMLElement | null {
  const dom = editor.view.nodeDOM(blockStart(editor, index));
  return dom instanceof HTMLElement ? dom : null;
}

/**
 * A grip at the left of the block under the pointer: drag it to reorder the
 * page's blocks, or press Alt+Shift+Arrow Up/Down in the text (or on the grip)
 * to move the block with the keyboard. Each move is announced.
 */
export function BlockHandle({
  editor,
  copy,
  surface,
}: {
  editor: Editor;
  copy: DocPageCopy;
  /** The positioned element the grip is placed in. */
  surface: RefObject<HTMLElement | null>;
}) {
  const t = copy.blockHandle;
  const [hovered, setHovered] = useState<Hovered | null>(null);
  const [announcement, setAnnouncement] = useState("");
  const frame = useRef<number | null>(null);

  const locate = useCallback(
    (clientX: number, clientY: number) => {
      const root = surface.current;
      if (!root || editor.isDestroyed || !editor.isEditable) return;
      const box = editor.view.dom.getBoundingClientRect();
      const found = editor.view.posAtCoords({
        left: Math.min(Math.max(clientX, box.left + 1), box.right - 1),
        top: clientY,
      });
      if (!found) return setHovered(null);
      const { doc } = editor.state;
      const index = Math.min(
        doc.resolve(found.pos).index(0),
        doc.childCount - 1,
      );
      const element = blockElement(editor, index);
      if (!element) return setHovered(null);
      const top =
        element.getBoundingClientRect().top - root.getBoundingClientRect().top;
      setHovered((current) =>
        current && current.index === index && current.top === top
          ? current
          : { index, top },
      );
    },
    [editor, surface],
  );

  useEffect(() => {
    const root = surface.current;
    if (!root) return;
    const onMove = (event: MouseEvent) => {
      const { clientX, clientY } = event;
      if (frame.current !== null) cancelAnimationFrame(frame.current);
      frame.current = requestAnimationFrame(() => locate(clientX, clientY));
    };
    const onLeave = () => setHovered(null);
    root.addEventListener("mousemove", onMove);
    root.addEventListener("mouseleave", onLeave);
    return () => {
      root.removeEventListener("mousemove", onMove);
      root.removeEventListener("mouseleave", onLeave);
      if (frame.current !== null) cancelAnimationFrame(frame.current);
    };
  }, [locate, surface]);

  // A move says where the block went; a change of the page drops the grip.
  useEffect(() => {
    const onTransaction = ({
      transaction,
    }: {
      transaction: { getMeta: (key: string) => unknown; docChanged: boolean };
    }) => {
      const moved = transaction.getMeta(BLOCK_MOVE_META) as
        BlockMoveInfo | undefined;
      if (moved) {
        setAnnouncement(
          fillDocCopy(t.moved, {
            position: moved.index + 1,
            total: moved.total,
          }),
        );
      }
      if (transaction.docChanged) setHovered(null);
    };
    editor.on("transaction", onTransaction);
    return () => {
      editor.off("transaction", onTransaction);
    };
  }, [editor, t.moved]);

  const onDragStart = (event: DragEvent<HTMLButtonElement>) => {
    if (!hovered) return;
    const { view, state } = editor;
    const selection = NodeSelection.create(
      state.doc,
      blockStart(editor, hovered.index),
    );
    view.dispatch(state.tr.setSelection(selection));
    // The same hand-off ProseMirror makes for a node it drags itself.
    (view as unknown as { dragging: unknown }).dragging = {
      slice: selection.content(),
      move: true,
      node: selection,
    };
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", " ");
    const element = blockElement(editor, hovered.index);
    if (element) event.dataTransfer.setDragImage(element, 0, 0);
  };

  const onKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
    if (!hovered) return;
    if (event.altKey && event.shiftKey && event.key === "ArrowUp") {
      event.preventDefault();
      moveBlock(editor, hovered.index, hovered.index - 1);
    } else if (event.altKey && event.shiftKey && event.key === "ArrowDown") {
      event.preventDefault();
      moveBlock(editor, hovered.index, hovered.index + 1);
    }
  };

  return (
    <>
      {hovered && editor.isEditable ? (
        <button
          type="button"
          draggable
          tabIndex={-1}
          aria-label={t.label}
          title={t.label}
          data-block-handle=""
          onDragStart={onDragStart}
          onKeyDown={onKeyDown}
          onClick={() => {
            editor
              .chain()
              .focus()
              .setNodeSelection(blockStart(editor, hovered.index))
              .run();
          }}
          style={{ top: hovered.top }}
          className="absolute -left-7 z-dropdown grid size-6 cursor-grab place-items-center rounded-[6px] border-0 bg-transparent p-0 text-meta-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring active:cursor-grabbing"
        >
          <GripVertical aria-hidden="true" className="size-4" />
        </button>
      ) : null}
      <span role="status" aria-live="polite" className="sr-only">
        {announcement}
      </span>
    </>
  );
}
