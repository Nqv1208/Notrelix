import { useState } from "react";
import { BubbleMenu } from "@tiptap/react/menus";
import { useEditorState, type Editor } from "@tiptap/react";
import {
  Bold,
  Code,
  Italic,
  Link2,
  MessageSquarePlus,
  Strikethrough,
  Underline,
} from "lucide-react";
import type { DocPageCopy } from "../components/doc-page.copy";
import { LinkForm, ToolButton } from "./editor-toolbar";
import { selectToolbarState } from "./editor-state";

const ICON = "size-4 stroke-[1.8]";

/** The floating bar over a text selection: marks and the link. */
export function EditorBubbleMenu({
  editor,
  copy,
  onComment,
}: {
  editor: Editor;
  copy: DocPageCopy;
  /** Starts a comment on the selection; absent offers no comment button. */
  onComment?: () => void;
}) {
  const t = copy.toolbar;
  const state = useEditorState({
    editor,
    selector: ({ editor: current }) => selectToolbarState(current),
  });
  const [linking, setLinking] = useState(false);

  return (
    <BubbleMenu
      editor={editor}
      options={{ placement: "top", offset: 8 }}
      shouldShow={({ editor: current, from, to }) =>
        current.isEditable &&
        from !== to &&
        current.state.selection.$from.parent.type.name !== "codeBlock" &&
        !current.isActive("taskRef")
      }
      className="z-dropdown"
    >
      <div
        role="toolbar"
        aria-label={t.bubble}
        className="flex items-center gap-0.5 rounded-[10px] border border-line-strong bg-doc-panel p-1 shadow-popover"
      >
        {linking ? (
          <div className="w-64 p-2">
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
              label={t.underline}
              pressed={state.underline}
              onClick={() => editor.chain().focus().toggleUnderline().run()}
            >
              <Underline aria-hidden="true" className={ICON} />
            </ToolButton>
            <ToolButton
              label={t.strike}
              pressed={state.strike}
              onClick={() => editor.chain().focus().toggleStrike().run()}
            >
              <Strikethrough aria-hidden="true" className={ICON} />
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
            {onComment && state.commentable ? (
              <ToolButton
                label={copy.comments.commentOnSelection}
                onClick={onComment}
              >
                <MessageSquarePlus aria-hidden="true" className={ICON} />
              </ToolButton>
            ) : null}
          </>
        )}
      </div>
    </BubbleMenu>
  );
}
