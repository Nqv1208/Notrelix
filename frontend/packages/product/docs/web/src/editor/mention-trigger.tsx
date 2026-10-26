import { useEffect, useRef, useState } from "react";
import { useEditorState, type Editor } from "@tiptap/react";
import type { DocPageCopy } from "../components/doc-page.copy";
import type {
  DocPageCallbacks,
  DocPerson,
  DocTaskRef,
} from "../components/doc-page-models";
import { DocMentionMenuSurface } from "../components/doc-popups-surface";

interface MentionMatch {
  readonly from: number;
  readonly to: number;
  readonly query: string;
  readonly blockId: string;
}

function selectMention(editor: Editor | null): MentionMatch | null {
  if (!editor || editor.isDestroyed || !editor.isEditable) return null;
  const { selection } = editor.state;
  if (!selection.empty) return null;
  const { $from } = selection;
  if (!$from.parent.isTextblock || $from.parent.type.name === "codeBlock") {
    return null;
  }
  const before = $from.parent.textBetween(
    0,
    $from.parentOffset,
    undefined,
    "￼",
  );
  const match = /(?:^|\s)@([^\s@]*)$/.exec(before);
  if (!match) return null;
  return {
    from: selection.from - match[1]!.length - 1,
    to: selection.from,
    query: match[1]!,
    blockId: String($from.parent.attrs.blockId ?? ""),
  };
}

/** "@" before the caret lists tasks to mention; picking one inserts its chip. */
export function MentionTrigger({
  editor,
  copy,
  tasks,
  people,
  callbacks,
}: {
  editor: Editor;
  copy: DocPageCopy;
  tasks: readonly DocTaskRef[];
  people?: readonly DocPerson[];
  callbacks?: DocPageCallbacks;
}) {
  const match = useEditorState({
    editor,
    selector: ({ editor: current }) => selectMention(current),
  });
  const last = useRef<string | null>(null);
  const [dismissed, setDismissed] = useState<number | null>(null);

  // The page host searches tasks for the typed query.
  useEffect(() => {
    if (!match) {
      if (last.current !== null) callbacks?.onBlockTriggerClose?.();
      last.current = null;
      return;
    }
    if (last.current === match.query) return;
    last.current = match.query;
    callbacks?.onBlockTrigger?.(match.blockId, {
      kind: "mention",
      query: match.query,
      text: "",
      before: "",
    });
  }, [match, callbacks]);

  if (!match || dismissed === match.from) return null;
  const coords = editor.view.coordsAtPos(match.from);
  const host = editor.view.dom.parentElement?.getBoundingClientRect();
  return (
    <DocMentionMenuSurface
      copy={copy}
      query={match.query}
      tasks={tasks}
      people={people}
      className=""
      style={{
        top: coords.bottom - (host?.top ?? 0) + 6,
        left: Math.max(0, coords.left - (host?.left ?? 0)),
      }}
      onSelect={(taskId) => {
        editor
          .chain()
          .focus()
          .insertContentAt({ from: match.from, to: match.to }, [
            { type: "taskRef", attrs: { taskId } },
            { type: "text", text: " " },
          ])
          .run();
        callbacks?.onBlockTriggerClose?.();
      }}
      onSelectPerson={(person) => {
        // The run's text is the label at write time; the mark carries the user.
        editor
          .chain()
          .focus()
          .insertContentAt({ from: match.from, to: match.to }, [
            {
              type: "text",
              text: `@${person.name}`,
              marks: [{ type: "mentionUser", attrs: { userId: person.id } }],
            },
            { type: "text", text: " " },
          ])
          .run();
        callbacks?.onBlockTriggerClose?.();
      }}
      onClose={() => {
        setDismissed(match.from);
        callbacks?.onBlockTriggerClose?.();
      }}
    />
  );
}
