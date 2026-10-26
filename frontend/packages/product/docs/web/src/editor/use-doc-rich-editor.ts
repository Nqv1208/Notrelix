import { useCallback, useEffect, useMemo, useRef } from "react";
import { useEditor, type Editor } from "@tiptap/react";
import { draftEquals, type BlockDraft } from "@notrelix/docs-core";
import { TextSelection } from "@tiptap/pm/state";
import { docToDrafts, draftsToDoc } from "./block-mapping";
import {
  createDocExtensions,
  type ContextHolder,
  type DocEditorContext,
} from "./extensions";

export interface UseDocRichEditorInput {
  /** The page the editor belongs to; another page builds another editor. */
  readonly pageId: string;
  /** False until the page's blocks have loaded. */
  readonly ready: boolean;
  /** The page's blocks, read once when the editor is built. */
  readonly initial: readonly BlockDraft[];
  readonly editable: boolean;
  readonly placeholder: string;
  readonly emptyPlaceholder: string;
  /** Changes when the facts the node views show (tasks, pages) change. */
  readonly contextKey?: unknown;
  /** The accessible name of the editing surface. */
  readonly label: string;
  readonly context: DocEditorContext;
  /** The editor's blocks after every change. */
  readonly onDrafts: (drafts: BlockDraft[]) => void;
}

export interface DocRichEditor {
  readonly editor: Editor | null;
  /**
   * Shows `drafts` in the editor, touching only the blocks that differ (the
   * selection and the member's undo history stay). Used when stored changes
   * merge in and when a stale block is replaced.
   */
  readonly applyDrafts: (drafts: readonly BlockDraft[]) => void;
  /** The editor's blocks now. */
  readonly currentDrafts: () => BlockDraft[];
  /** Sets properties of one top-level block node (page link, checklist...). */
  readonly updateBlockAttrs: (
    blockId: string,
    attrs: Record<string, unknown>,
  ) => void;
}

/**
 * One editor per page. The page's blocks are read when the editor is built;
 * later refetches never replace what the member is typing.
 */
export function useDocRichEditor(input: UseDocRichEditorInput): DocRichEditor {
  const holder = useMemo<ContextHolder>(
    () => ({ current: input.context, version: 0, listeners: new Set() }),
    // The holder is created once; its `current` follows the input each render.
    [],
  );
  holder.current = input.context;
  // The task and page facts changed: node views show the new ones.
  useEffect(() => {
    holder.version += 1;
    for (const listener of holder.listeners) listener();
  }, [holder, input.contextKey]);
  const initialRef = useRef(input.initial);
  initialRef.current = input.initial;
  const onDraftsRef = useRef(input.onDrafts);
  onDraftsRef.current = input.onDrafts;

  const extensions = useMemo(
    () =>
      createDocExtensions({
        context: holder,
        placeholder: input.placeholder,
        emptyPlaceholder: input.emptyPlaceholder,
      }),
    [holder, input.placeholder, input.emptyPlaceholder],
  );

  const editor = useEditor(
    {
      extensions,
      content: input.ready ? draftsToDoc(initialRef.current) : undefined,
      editable: input.editable,
      immediatelyRender: false,
      shouldRerenderOnTransaction: false,
      editorProps: {
        attributes: {
          class: "outline-none",
          role: "textbox",
          "aria-multiline": "true",
          "aria-label": input.label,
          spellcheck: "true",
        },
      },
      onUpdate: ({ editor: current, transaction }) => {
        if (!transaction.docChanged) return;
        onDraftsRef.current(docToDrafts(current.getJSON()));
      },
    },
    [input.pageId, input.ready, extensions],
  );

  useEffect(() => {
    editor?.setEditable(input.editable);
  }, [editor, input.editable]);

  const currentDrafts = useCallback(
    () => (editor && !editor.isDestroyed ? docToDrafts(editor.getJSON()) : []),
    [editor],
  );

  const applyDrafts = useCallback(
    (drafts: readonly BlockDraft[]) => {
      if (!editor || editor.isDestroyed) return;
      const { schema, doc } = editor.state;
      const current = docToDrafts(editor.getJSON());
      const nodes = draftsToDoc(drafts).content ?? [];
      const sameOrder =
        current.length === drafts.length &&
        current.every((draft, index) => draft.id === drafts[index]?.id);
      const tr = editor.state.tr.setMeta("addToHistory", false);
      if (sameOrder) {
        const offsets: number[] = [];
        doc.forEach((_node, offset) => offsets.push(offset));
        // From the end, so earlier offsets stay valid.
        for (let index = current.length - 1; index >= 0; index -= 1) {
          if (
            draftEquals(
              current[index] as BlockDraft,
              drafts[index] as BlockDraft,
            )
          ) {
            continue;
          }
          const at = offsets[index] as number;
          const old = doc.child(index);
          tr.replaceWith(
            at,
            at + old.nodeSize,
            schema.nodeFromJSON(nodes[index]),
          );
        }
      } else {
        const { from } = editor.state.selection;
        tr.replaceWith(
          0,
          doc.content.size,
          schema.nodeFromJSON(draftsToDoc(drafts)).content,
        );
        const at = Math.min(from, tr.doc.content.size);
        tr.setSelection(TextSelection.near(tr.doc.resolve(at)));
      }
      if (tr.docChanged) editor.view.dispatch(tr);
    },
    [editor],
  );

  const updateBlockAttrs = useCallback(
    (blockId: string, attrs: Record<string, unknown>) => {
      if (!editor || editor.isDestroyed) return;
      editor.state.doc.forEach((node, offset) => {
        if (node.attrs.blockId !== blockId) return;
        editor.view.dispatch(
          editor.state.tr.setNodeMarkup(offset, undefined, {
            ...node.attrs,
            ...attrs,
          }),
        );
      });
    },
    [editor],
  );

  return {
    editor: input.ready ? editor : null,
    applyDrafts,
    currentDrafts,
    updateBlockAttrs,
  };
}
