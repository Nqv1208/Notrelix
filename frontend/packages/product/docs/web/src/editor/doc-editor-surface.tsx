import { useRef } from "react";
import { EditorContent, type Editor } from "@tiptap/react";
import type { DocPageCopy } from "../components/doc-page.copy";
import type {
  DocMarkTargets,
  DocPageCallbacks,
  DocPerson,
  DocTaskRef,
} from "../components/doc-page-models";
import { BlockHandle } from "./block-handle";
import { EditorBubbleMenu } from "./bubble-menu";
import { FindPanel } from "./find-panel";
import { MarkChips } from "./mark-chips";
import { MentionTrigger } from "./mention-trigger";
import { SlashMenu } from "./slash-menu";
import { TableMenu } from "./table-menu";

/** Heading, placeholder and list styling of the editing surface (app tokens only). */
const CONTENT =
  "[&_.ProseMirror]:outline-none [&_h1]:mt-7 [&_h1]:mb-2.5 [&_h1]:text-page-title [&_h2]:mt-8 [&_h2]:mb-3 [&_h2]:text-doc-heading max-md:[&_h2]:mt-7 max-md:[&_h2]:mb-2.5 max-md:[&_h2]:text-[18px] max-md:[&_h2]:leading-[25px] [&_h3]:mt-7 [&_h3]:mb-2.5 [&_h3]:text-subheading [&_h3]:font-semibold [&_.is-empty]:before:pointer-events-none [&_.is-empty]:before:float-left [&_.is-empty]:before:h-0 [&_.is-empty]:before:text-meta-foreground [&_.is-empty]:before:content-[attr(data-placeholder)] [&_.comment-anchor]:cursor-pointer [&_.comment-anchor]:rounded-[2px] [&_.comment-anchor]:bg-doc-highlight [&_.comment-anchor-active]:ring-2 [&_.comment-anchor-active]:ring-focus-ring [&_.find-match]:rounded-[2px] [&_.find-match]:bg-warning-subtle [&_.find-match-current]:bg-brand-subtle [&_.find-match-current]:ring-2 [&_.find-match-current]:ring-focus-ring [&_.ProseMirror-selectednode]:rounded-[9px] [&_.ProseMirror-selectednode]:ring-2 [&_.ProseMirror-selectednode]:ring-focus-ring";

export interface DocEditorSurfaceProps {
  readonly editor: Editor;
  readonly copy: DocPageCopy;
  readonly mentionTasks: readonly DocTaskRef[];
  readonly mentionPeople?: readonly DocPerson[];
  readonly markTargets?: DocMarkTargets;
  readonly callbacks?: DocPageCallbacks;
}

/** The page's editing surface: the editor with its bubble, slash and mention menus. */
export function DocEditorSurface({
  editor,
  copy,
  mentionTasks,
  mentionPeople,
  markTargets,
  callbacks,
}: DocEditorSurfaceProps) {
  const surface = useRef<HTMLDivElement | null>(null);
  return (
    <div ref={surface} className="relative">
      <FindPanel editor={editor} copy={copy} />
      <EditorContent
        editor={editor}
        role="presentation"
        className={CONTENT}
        aria-label={copy.blockPlaceholder}
      />
      <MarkChips editor={editor} copy={copy} targets={markTargets} />
      {editor.isEditable ? (
        <>
          <BlockHandle editor={editor} copy={copy} surface={surface} />
          <EditorBubbleMenu
            editor={editor}
            copy={copy}
            onComment={callbacks?.onCommentStart}
          />
          <TableMenu editor={editor} copy={copy} />
          <SlashMenu
            editor={editor}
            copy={copy}
            onPickPage={callbacks?.onOpenPagePicker}
            onPickBoard={callbacks?.onOpenBoardPicker}
          />
          <MentionTrigger
            editor={editor}
            copy={copy}
            tasks={mentionTasks}
            people={mentionPeople}
            callbacks={callbacks}
          />
        </>
      ) : null}
    </div>
  );
}
