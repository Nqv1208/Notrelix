import { useCallback, useEffect, useMemo, useRef } from "react";
import { useEditor, type Editor } from "@tiptap/react";
import type { BlockDraft } from "@notrelix/docs-core";
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
  /** Gives a block the id the producer stored it under. */
  readonly assignBlockId: (clientId: string, storedId: string) => void;
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

  const assignBlockId = useCallback(
    (clientId: string, storedId: string) => {
      if (!editor || editor.isDestroyed) return;
      const { doc } = editor.state;
      doc.forEach((node, offset) => {
        if (node.attrs.blockId !== clientId) return;
        editor.view.dispatch(
          editor.state.tr
            .setNodeMarkup(offset, undefined, {
              ...node.attrs,
              blockId: storedId,
            })
            .setMeta("addToHistory", false),
        );
      });
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
    assignBlockId,
    updateBlockAttrs,
  };
}
