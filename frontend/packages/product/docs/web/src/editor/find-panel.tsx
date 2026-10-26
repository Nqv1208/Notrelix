import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import { useEditorState, type Editor } from "@tiptap/react";
import { ChevronDown, ChevronUp, ChevronRight, X } from "lucide-react";
import { fillDocCopy, type DocPageCopy } from "../components/doc-page.copy";
import { findStateOf } from "./find-replace";

const FIELD =
  "h-8 min-w-0 flex-1 rounded-[7px] border border-line-strong bg-doc-panel px-2 text-compact text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring";
const ICON_BUTTON =
  "grid size-7 shrink-0 place-items-center rounded-[6px] border-0 bg-transparent text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring disabled:opacity-40";
const TEXT_BUTTON =
  "h-7 rounded-[6px] border border-line-strong bg-transparent px-2.5 text-crumb font-medium text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring disabled:opacity-40";

/** Scrolls the current match into view without moving the editor's focus. */
function revealCurrent(editor: Editor): void {
  const state = findStateOf(editor.state);
  const match = state?.matches[state.index];
  if (!match) return;
  const { node } = editor.view.domAtPos(match.from);
  const element = node instanceof HTMLElement ? node : node.parentElement;
  element?.scrollIntoView?.({ block: "center", behavior: "auto" });
}

/**
 * The find and replace panel: Ctrl/Cmd+F opens it, Ctrl/Cmd+H opens it with
 * the replace field. The count is a live region ("2 of 5").
 */
export function FindPanel({
  editor,
  copy,
}: {
  editor: Editor;
  copy: DocPageCopy;
}) {
  const t = copy.find;
  const state = useEditorState({
    editor,
    selector: ({ editor: current }) => {
      const find = findStateOf(current.state);
      return {
        open: find?.open ?? false,
        opens: find?.opens ?? 0,
        replaceMode: find?.replaceMode ?? false,
        term: find?.term ?? "",
        caseSensitive: find?.caseSensitive ?? false,
        total: find?.matches.length ?? 0,
        index: find?.index ?? -1,
      };
    },
  });
  const [replacement, setReplacement] = useState("");
  const [replaced, setReplaced] = useState<number | null>(null);
  const findField = useRef<HTMLInputElement | null>(null);
  const replaceField = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    if (!state.open) return;
    const target =
      state.replaceMode && state.term
        ? replaceField.current
        : findField.current;
    target?.focus();
    target?.select();
  }, [state.open, state.opens, state.replaceMode]);

  useEffect(() => {
    if (state.open && state.index >= 0) revealCurrent(editor);
  }, [editor, state.open, state.index, state.term, state.total]);

  useEffect(() => {
    setReplaced(null);
  }, [state.term, state.caseSensitive, state.open]);

  if (!state.open) return null;

  const canReplace = editor.isEditable;
  const close = () => {
    editor.chain().closeFind().focus().run();
  };
  const step = (direction: 1 | -1) => editor.commands.stepFind(direction);
  const countText =
    state.term === ""
      ? ""
      : state.total === 0
        ? t.none
        : fillDocCopy(t.count, {
            current: state.index + 1,
            total: state.total,
          });
  const live =
    replaced !== null
      ? fillDocCopy(t.replaced, { count: replaced })
      : countText;

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const mod = event.metaKey || event.ctrlKey;
    if (event.key === "Escape") {
      event.preventDefault();
      close();
    } else if (mod && event.key.toLowerCase() === "f") {
      event.preventDefault();
      findField.current?.focus();
      findField.current?.select();
    } else if (mod && event.key.toLowerCase() === "h" && canReplace) {
      event.preventDefault();
      editor.commands.openFind(true);
    }
  };

  return (
    <div className="sticky top-0 z-dropdown h-0">
      <div
        role="search"
        aria-label={t.label}
        onKeyDown={onKeyDown}
        className="absolute top-0 right-0 flex w-[22rem] max-w-full flex-col gap-2 rounded-[10px] border border-line-strong bg-doc-panel p-2 shadow-popover"
      >
        <div className="flex items-center gap-1">
          {canReplace ? (
            <button
              type="button"
              aria-label={t.toggleReplace}
              aria-expanded={state.replaceMode}
              onClick={() => editor.commands.openFind(!state.replaceMode)}
              className={ICON_BUTTON}
            >
              <ChevronRight
                aria-hidden="true"
                className={`size-4 transition-transform ${state.replaceMode ? "rotate-90" : ""}`}
              />
            </button>
          ) : null}
          <input
            ref={findField}
            type="text"
            aria-label={t.findLabel}
            value={state.term}
            onChange={(event) =>
              editor.commands.setFindTerm(event.target.value)
            }
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                event.preventDefault();
                step(event.shiftKey ? -1 : 1);
              }
            }}
            placeholder={t.findLabel}
            className={FIELD}
          />
          <span
            role="status"
            aria-live="polite"
            className="min-w-[4.5rem] shrink-0 text-right text-crumb text-meta-foreground tabular-nums"
          >
            {live}
          </span>
          <button
            type="button"
            aria-label={t.previous}
            title={t.previous}
            disabled={state.total === 0}
            onClick={() => step(-1)}
            className={ICON_BUTTON}
          >
            <ChevronUp aria-hidden="true" className="size-4" />
          </button>
          <button
            type="button"
            aria-label={t.next}
            title={t.next}
            disabled={state.total === 0}
            onClick={() => step(1)}
            className={ICON_BUTTON}
          >
            <ChevronDown aria-hidden="true" className="size-4" />
          </button>
          <button
            type="button"
            aria-label={t.close}
            title={t.close}
            onClick={close}
            className={ICON_BUTTON}
          >
            <X aria-hidden="true" className="size-4" />
          </button>
        </div>
        <label className="flex items-center gap-2 pl-8 text-crumb text-foreground">
          <input
            type="checkbox"
            checked={state.caseSensitive}
            onChange={(event) =>
              editor.commands.setFindCaseSensitive(event.target.checked)
            }
            className="size-3.5 accent-brand"
          />
          {t.matchCase}
        </label>
        {canReplace && state.replaceMode ? (
          <div className="flex items-center gap-1 pl-8">
            <input
              ref={replaceField}
              type="text"
              aria-label={t.replaceLabel}
              value={replacement}
              onChange={(event) => setReplacement(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  editor.commands.replaceCurrentMatch(replacement);
                }
              }}
              placeholder={t.replaceLabel}
              className={FIELD}
            />
            <button
              type="button"
              disabled={state.total === 0}
              onClick={() => editor.commands.replaceCurrentMatch(replacement)}
              className={TEXT_BUTTON}
            >
              {t.replace}
            </button>
            <button
              type="button"
              disabled={state.total === 0}
              onClick={() => {
                const count = state.total;
                if (editor.commands.replaceAllMatches(replacement)) {
                  setReplaced(count);
                }
              }}
              className={TEXT_BUTTON}
            >
              {t.replaceAll}
            </button>
          </div>
        ) : null}
      </div>
    </div>
  );
}
