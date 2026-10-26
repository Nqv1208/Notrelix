import { useCallback, useEffect, useRef, useState } from "react";
import type { Editor } from "@tiptap/react";
import { Popover, PopoverAnchor, PopoverContent } from "@notrelix/ui-web";
import type { DocPageCopy } from "../components/doc-page.copy";
import type { DocMarkTargets } from "../components/doc-page-models";
import { DocAvatar } from "../components/doc-page-ui";

type OpenChip =
  | { readonly kind: "mention"; readonly userId: string; readonly el: Element }
  | { readonly kind: "page"; readonly el: Element };

const CHIP_SELECTOR = "[data-user-id],[data-page-id]";

function chipAtCaret(editor: Editor): Element | null {
  const { from } = editor.state.selection;
  const marks = editor.state.selection.$from.marks();
  if (
    !marks.some(
      (m) => m.type.name === "mentionUser" || m.type.name === "pageRef",
    )
  ) {
    return null;
  }
  const at = editor.view.domAtPos(from);
  const node = at.node.nodeType === 3 ? at.node.parentElement : at.node;
  return node instanceof Element ? node.closest(CHIP_SELECTOR) : null;
}

/**
 * Clicking a mention chip opens the member's card; clicking a page reference
 * opens that page, or says it is unavailable when the viewer cannot read it
 * (one generic fallback). While editing, a page reference needs Cmd/Ctrl+click
 * so a plain click still places the caret; Alt+Enter on a chip under the caret
 * is the keyboard route for both.
 */
export function MarkChips({
  editor,
  copy,
  targets,
}: {
  editor: Editor;
  copy: DocPageCopy;
  targets?: DocMarkTargets;
}) {
  const [open, setOpen] = useState<OpenChip | null>(null);
  const targetsRef = useRef(targets);
  targetsRef.current = targets;

  const activate = useCallback(
    (el: Element, modified: boolean) => {
      const current = targetsRef.current;
      if (!current) return;
      const userId = (el as HTMLElement).dataset.userId;
      if (userId) {
        setOpen({ kind: "mention", userId, el });
        return;
      }
      const pageId = (el as HTMLElement).dataset.pageId;
      if (!pageId) return;
      if (editor.isEditable && !modified) return;
      if (current.page(pageId)) current.openPage?.(pageId);
      else setOpen({ kind: "page", el });
    },
    [editor],
  );

  useEffect(() => {
    let detach: (() => void) | null = null;
    const attach = () => {
      if (detach || editor.isDestroyed) return;
      const dom = editor.view.dom;
      const onClick = (event: MouseEvent) => {
        const el = (event.target as Element | null)?.closest?.(CHIP_SELECTOR);
        if (!el || !dom.contains(el)) return;
        activate(el, event.metaKey || event.ctrlKey);
      };
      const onKeyDown = (event: KeyboardEvent) => {
        if (event.key !== "Enter" || !event.altKey) return;
        const el = chipAtCaret(editor);
        if (!el) return;
        event.preventDefault();
        activate(el, true);
      };
      dom.addEventListener("click", onClick);
      dom.addEventListener("keydown", onKeyDown);
      detach = () => {
        dom.removeEventListener("click", onClick);
        dom.removeEventListener("keydown", onKeyDown);
      };
    };
    // The view exists once the editor content is mounted.
    if (editor.isInitialized) attach();
    else editor.on("create", attach);
    return () => {
      editor.off("create", attach);
      detach?.();
    };
  }, [editor, activate]);

  if (!open || !targets) return null;
  const person = open.kind === "mention" ? targets.person(open.userId) : null;
  return (
    <Popover open onOpenChange={(next) => !next && setOpen(null)}>
      <PopoverAnchor virtualRef={{ current: open.el }} />
      <PopoverContent
        align="start"
        className="w-64 gap-2 p-3"
        onCloseAutoFocus={(event) => {
          event.preventDefault();
          editor.commands.focus();
        }}
      >
        {person ? (
          <div className="flex items-center gap-2.5">
            <DocAvatar person={person} className="size-8" />
            <div className="min-w-0">
              <div className="truncate text-nav font-semibold leading-[1.45] text-foreground">
                {person.name === "—" ? copy.chips.unknownMember : person.name}
              </div>
              <div className="text-crumb leading-[1.45] text-meta-foreground">
                {copy.chips.member}
              </div>
            </div>
          </div>
        ) : (
          <p className="m-0 text-compact leading-[1.45] text-meta-foreground">
            {copy.blocks.pageReference.unavailable}
          </p>
        )}
      </PopoverContent>
    </Popover>
  );
}
