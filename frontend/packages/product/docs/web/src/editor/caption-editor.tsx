import { useEffect, useRef, useState } from "react";
import { Node } from "@tiptap/core";
import { Placeholder } from "@tiptap/extensions";
import { EditorContent, useEditor, useEditorState } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import { Bold, Code, Italic, Link2 } from "lucide-react";
import { isSafeHref, sanitizeCaptionRuns, type Run } from "@notrelix/docs-core";
import type { DocPageCopy } from "../components/doc-page.copy";
import { inlineToRuns, runsToInline } from "./block-mapping";
import { LinkForm, ToolButton } from "./editor-toolbar";

/** One line of inline content: the whole caption. */
const CaptionDocument = Node.create({
  name: "doc",
  topNode: true,
  content: "paragraph",
});

const ICON = "size-3.5 stroke-[1.8]";

function captionDoc(runs: readonly Run[]) {
  const content = runsToInline(runs);
  return {
    type: "doc",
    content: [{ type: "paragraph", ...(content.length ? { content } : {}) }],
  };
}

function runsOfEditor(editor: NonNullable<ReturnType<typeof useEditor>>) {
  const paragraph = editor.getJSON().content?.[0];
  return sanitizeCaptionRuns(inlineToRuns(paragraph?.content));
}

const same = (a: readonly Run[], b: readonly Run[]) =>
  JSON.stringify(a) === JSON.stringify(b);

/**
 * An image caption edited as runs: bold, italic, code and link only (the
 * v1 marks the producer stores). Enter ends the edit; the change is handed to
 * `onCommit` when the field loses focus.
 */
export function CaptionEditor({
  runs,
  copy,
  onCommit,
}: {
  runs: readonly Run[];
  copy: DocPageCopy;
  onCommit: (runs: Run[]) => void;
}) {
  const [focused, setFocused] = useState(false);
  const [linking, setLinking] = useState(false);
  const stored = useRef(runs);
  stored.current = runs;
  const commit = useRef(onCommit);
  commit.current = onCommit;

  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        document: false,
        blockquote: false,
        bulletList: false,
        orderedList: false,
        listItem: false,
        listKeymap: false,
        codeBlock: false,
        heading: false,
        horizontalRule: false,
        hardBreak: false,
        underline: false,
        strike: false,
        trailingNode: false,
        dropcursor: false,
        gapcursor: false,
        paragraph: { HTMLAttributes: { class: "m-0" } },
        link: {
          openOnClick: false,
          autolink: true,
          linkOnPaste: true,
          defaultProtocol: "https",
          isAllowedUri: (url) => isSafeHref(url),
          HTMLAttributes: {
            class: "text-brand-subtle-foreground underline underline-offset-2",
            rel: "noopener noreferrer nofollow",
            target: "_blank",
          },
        },
        code: {
          HTMLAttributes: {
            class:
              "rounded-[4px] bg-state-hover px-1 py-0.5 font-mono text-[0.9em]",
          },
        },
      }),
      CaptionDocument,
      Placeholder.configure({
        placeholder: copy.media.image.captionPlaceholder,
      }),
    ],
    content: captionDoc(runs),
    editorProps: {
      attributes: {
        role: "textbox",
        "aria-label": copy.media.image.caption,
        class:
          "min-h-8 w-full rounded-[7px] border border-transparent px-2 py-1.5 text-compact text-meta-foreground outline-none hover:border-line-strong focus-visible:ring-2 focus-visible:ring-focus-ring [&_.is-empty]:before:pointer-events-none [&_.is-empty]:before:float-left [&_.is-empty]:before:h-0 [&_.is-empty]:before:content-[attr(data-placeholder)]",
      },
      handleKeyDown: (view, event) => {
        if (event.key !== "Enter") return false;
        event.preventDefault();
        (view.dom as HTMLElement).blur();
        return true;
      },
    },
    onFocus: () => setFocused(true),
    onBlur: ({ editor: current }) => {
      setFocused(false);
      const next = runsOfEditor(current);
      if (!same(next, sanitizeCaptionRuns(stored.current)))
        commit.current(next);
    },
  });

  // A stored caption changed from elsewhere (undo, another member): show it.
  const signature = JSON.stringify(runs);
  useEffect(() => {
    if (!editor || editor.isDestroyed || editor.isFocused) return;
    if (!same(runsOfEditor(editor), sanitizeCaptionRuns(runs))) {
      editor.commands.setContent(captionDoc(runs), { emitUpdate: false });
    }
    // `signature` stands for `runs`.
  }, [editor, signature]);

  const state = useEditorState({
    editor,
    selector: ({ editor: current }) => ({
      bold: current?.isActive("bold") ?? false,
      italic: current?.isActive("italic") ?? false,
      code: current?.isActive("code") ?? false,
      link: (current?.getAttributes("link").href as string | undefined) ?? null,
    }),
  });

  if (!editor) return null;
  const t = copy.toolbar;
  const showTools = focused || linking;
  return (
    <div className="min-w-0 flex-1">
      <EditorContent editor={editor} />
      {showTools ? (
        <div
          role="toolbar"
          aria-label={copy.media.image.captionTools}
          className="mt-1 flex items-center gap-0.5"
        >
          {linking ? (
            <div className="w-64 p-1">
              <LinkForm
                editor={editor}
                copy={copy}
                initial={state.link}
                onDone={() => setLinking(false)}
              />
            </div>
          ) : (
            <>
              <ToolButton
                label={t.bold}
                pressed={state.bold}
                onClick={() => editor.chain().focus().toggleBold().run()}
              >
                <Bold aria-hidden="true" className={ICON} />
              </ToolButton>
              <ToolButton
                label={t.italic}
                pressed={state.italic}
                onClick={() => editor.chain().focus().toggleItalic().run()}
              >
                <Italic aria-hidden="true" className={ICON} />
              </ToolButton>
              <ToolButton
                label={t.inlineCode}
                pressed={state.code}
                onClick={() => editor.chain().focus().toggleCode().run()}
              >
                <Code aria-hidden="true" className={ICON} />
              </ToolButton>
              <ToolButton
                label={t.link}
                pressed={state.link !== null}
                onClick={() => setLinking(true)}
              >
                <Link2 aria-hidden="true" className={ICON} />
              </ToolButton>
            </>
          )}
        </div>
      ) : null}
    </div>
  );
}
