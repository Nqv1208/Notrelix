import { EditorContent, type Editor } from "@tiptap/react";
import type { DocPageCopy } from "../components/doc-page.copy";
import type {
  DocPageCallbacks,
  DocTaskRef,
} from "../components/doc-page-models";
import { EditorBubbleMenu } from "./bubble-menu";
import { MentionTrigger } from "./mention-trigger";
import { SlashMenu } from "./slash-menu";
import { TableMenu } from "./table-menu";

/** Heading, placeholder and list styling of the editing surface (app tokens only). */
const CONTENT =
  "[&_.ProseMirror]:outline-none [&_h1]:mt-7 [&_h1]:mb-2.5 [&_h1]:text-page-title [&_h2]:mt-7 [&_h2]:mb-2.5 [&_h2]:text-doc-heading [&_h3]:mt-7 [&_h3]:mb-2.5 [&_h3]:text-subheading [&_h3]:font-semibold [&_.is-empty]:before:pointer-events-none [&_.is-empty]:before:float-left [&_.is-empty]:before:h-0 [&_.is-empty]:before:text-meta-foreground [&_.is-empty]:before:content-[attr(data-placeholder)] [&_.ProseMirror-selectednode]:rounded-[9px] [&_.ProseMirror-selectednode]:ring-2 [&_.ProseMirror-selectednode]:ring-focus-ring";

export interface DocEditorSurfaceProps {
  readonly editor: Editor;
  readonly copy: DocPageCopy;
  readonly mentionTasks: readonly DocTaskRef[];
  readonly callbacks?: DocPageCallbacks;
}

/** The page's editing surface: the editor with its bubble, slash and mention menus. */
export function DocEditorSurface({
  editor,
  copy,
  mentionTasks,
  callbacks,
}: DocEditorSurfaceProps) {
  return (
    <div className="relative">
      <EditorContent
        editor={editor}
        role="presentation"
        className={CONTENT}
        aria-label={copy.blockPlaceholder}
      />
      {editor.isEditable ? (
        <>
          <EditorBubbleMenu editor={editor} copy={copy} />
          <TableMenu editor={editor} copy={copy} />
          <SlashMenu
            editor={editor}
            copy={copy}
            onPickPage={callbacks?.onOpenPagePicker}
          />
          <MentionTrigger
            editor={editor}
            copy={copy}
            tasks={mentionTasks}
            callbacks={callbacks}
          />
        </>
      ) : null}
    </div>
  );
}
