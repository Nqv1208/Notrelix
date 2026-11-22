import { useRef, useState, type KeyboardEvent } from "react";
import { cn } from "@notrelix/ui-web";
import { personInitials } from "@notrelix/work-management-core";
import { identityMarkClass } from "../shared/item-display";
import {
  useWmUiPorts,
  type MentionSuggestionsView,
} from "../shared/wm-ui-ports";
import type { ItemPageCopy } from "./item-page.copy";
import { foldForSearch } from "./item-pickers";

/**
 * The @mention list of the comment box: the people the server offers for what
 * was typed after "@". The server lists only people who can view the item, so a
 * mention always reaches the person it names and the list never leaks anyone.
 */

export interface MentionToken {
  /** Index of the "@" and of the end of the typed name. */
  readonly from: number;
  readonly to: number;
  readonly query: string;
}

/** The "@name" the caret is in, when the "@" starts the text or follows a space. */
export function mentionToken(text: string, caret: number): MentionToken | null {
  const match = /(^|\s)@([^\s@]*)$/.exec(text.slice(0, caret));
  if (!match) return null;
  const query = match[2] ?? "";
  return { from: caret - query.length - 1, to: caret, query };
}

export interface MentionCandidate {
  readonly key: string;
  readonly name: string;
  /** Who is notified when this is picked. */
  readonly userIds: readonly string[];
}

/** What the server offered, as list rows. */
export function mentionCandidates(
  people: MentionSuggestionsView["people"],
): MentionCandidate[] {
  return people.map((person) => ({
    key: person.userId,
    name: person.name,
    userIds: [person.userId],
  }));
}

/** The draft the field writes to (`useCommentDraft`). */
interface MentionDraft {
  readonly body: string;
  changeBody(next: string): void;
  insertMention(
    range: { readonly from: number; readonly to: number },
    label: string,
    userIds: readonly string[],
  ): void;
}

/**
 * The "@" behaviour of a comment box: the token the caret is in, the people it
 * matches, arrows and Enter/Tab to choose, Escape to dismiss, and the "@"
 * button. The field and the list stay with the caller.
 */
export function useMentionField({
  draft,
  workspaceId,
  itemId,
}: {
  draft: MentionDraft;
  workspaceId: string;
  itemId: string;
}) {
  const fieldRef = useRef<HTMLTextAreaElement>(null);
  const [token, setToken] = useState<MentionToken | null>(null);
  const [active, setActive] = useState(0);
  const suggestions = useWmUiPorts().useMentionSuggestions(
    workspaceId,
    itemId,
    token ? token.query : null,
  );
  const candidates = mentionCandidates(suggestions.people);
  // Open while "@" is being typed: it also says loading, nobody to mention and a failed read.
  const listOpen = token !== null;
  const activeIndex = Math.min(active, candidates.length - 1);

  const pick = (candidate: MentionCandidate) => {
    if (!token) return;
    draft.insertMention(token, candidate.name, candidate.userIds);
    setToken(null);
    const caret = token.from + candidate.name.length + 2;
    requestAnimationFrame(() => {
      fieldRef.current?.focus();
      fieldRef.current?.setSelectionRange(caret, caret);
    });
  };
  const readToken = (text: string, caret: number) => {
    setToken(mentionToken(text, caret));
    setActive(0);
  };
  /** True when the key was the list's (the caller then skips its own handling). */
  const handleKey = (event: KeyboardEvent<HTMLTextAreaElement>): boolean => {
    if (!listOpen) return false;
    if (
      candidates.length > 0 &&
      (event.key === "ArrowDown" || event.key === "ArrowUp")
    ) {
      event.preventDefault();
      const step = event.key === "ArrowDown" ? 1 : -1;
      setActive(
        (current) => (current + step + candidates.length) % candidates.length,
      );
      return true;
    }
    if (
      candidates.length > 0 &&
      ((event.key === "Enter" && !event.metaKey && !event.ctrlKey) ||
        event.key === "Tab")
    ) {
      event.preventDefault();
      const chosen = candidates[activeIndex];
      if (chosen) pick(chosen);
      return true;
    }
    if (event.key === "Escape") {
      // The sheet closes on Escape: this one only dismisses the list.
      event.preventDefault();
      event.stopPropagation();
      setToken(null);
      return true;
    }
    return false;
  };
  const openAtEnd = () => {
    const text = draft.body;
    const spaced = text === "" || /\s$/.test(text) ? text : `${text} `;
    draft.changeBody(`${spaced}@`);
    readToken(`${spaced}@`, spaced.length + 1);
    fieldRef.current?.focus();
  };

  return {
    fieldRef,
    token,
    candidates,
    status: suggestions.status,
    retry: suggestions.retry,
    listOpen,
    activeIndex,
    setActive,
    pick,
    readToken,
    dismiss: () => setToken(null),
    handleKey,
    openAtEnd,
  };
}

function Matched({ text, query }: { text: string; query: string }) {
  const needle = foldForSearch(query);
  if (!needle) return <>{text}</>;
  let folded = "";
  const starts: number[] = [];
  for (let index = 0; index < text.length; index += 1) {
    const piece = foldForSearch(text[index] ?? "");
    for (let k = 0; k < Math.max(piece.length, 1); k += 1) starts.push(index);
    folded += piece || " ";
  }
  const at = folded.indexOf(needle);
  if (at < 0) return <>{text}</>;
  const from = starts[at] ?? 0;
  const to = (starts[at + needle.length - 1] ?? text.length - 1) + 1;
  return (
    <>
      {text.slice(0, from)}
      <b className="font-semibold text-brand-subtle-foreground">
        {text.slice(from, to)}
      </b>
      {text.slice(to)}
    </>
  );
}

export function MentionList({
  candidates,
  status,
  query,
  activeIndex,
  copy,
  onPick,
  onHover,
  onRetry,
}: {
  candidates: readonly MentionCandidate[];
  status: MentionSuggestionsView["status"];
  query: string;
  activeIndex: number;
  copy: ItemPageCopy;
  onPick: (candidate: MentionCandidate) => void;
  onHover: (index: number) => void;
  onRetry: () => void;
}) {
  const t = copy.composer;
  return (
    <div
      role="listbox"
      id="item-page-mention-list"
      aria-label={t.mentionList}
      aria-busy={status === "loading"}
      className="absolute bottom-full left-0 z-modal mb-2 w-75 overflow-hidden rounded-[10px] border border-line-strong bg-popover p-1.5 shadow-lg"
    >
      {candidates.length === 0 ? (
        <div
          role={status === "error" ? "alert" : "status"}
          className="flex items-center justify-between gap-2 px-2.5 py-1.5 text-item-ui text-meta-foreground"
        >
          {status === "loading" ? (
            t.mentionLoading
          ) : status === "error" ? (
            <>
              <span>{t.mentionError}</span>
              <button
                type="button"
                // The textarea keeps focus: a click must not blur it first.
                onMouseDown={(event) => event.preventDefault()}
                onClick={onRetry}
                className="shrink-0 rounded-[6px] px-2 py-0.5 font-semibold text-brand-subtle-foreground hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring"
              >
                {t.mentionRetry}
              </button>
            </>
          ) : (
            t.mentionEmpty
          )}
        </div>
      ) : null}
      {candidates.map((candidate, index) => (
        <div
          key={candidate.key}
          id={`item-page-mention-${candidate.key}`}
          role="option"
          aria-selected={index === activeIndex}
          // The textarea keeps focus: a click must not blur it first.
          onMouseDown={(event) => event.preventDefault()}
          onClick={() => onPick(candidate)}
          onMouseEnter={() => onHover(index)}
          className={cn(
            "flex cursor-pointer items-start gap-2.5 rounded-[7px] px-2.5 py-1.5 text-item-ui",
            index === activeIndex && "bg-state-hover-strong",
          )}
        >
          <span
            aria-hidden="true"
            className={cn(
              "mt-0.5 grid size-6 shrink-0 place-items-center rounded-full text-avatar-xs",
              identityMarkClass(candidate.key),
            )}
          >
            {personInitials(candidate.name)}
          </span>
          <span className="min-w-0 flex-1 leading-tight">
            <Matched text={candidate.name} query={query} />
          </span>
        </div>
      ))}
    </div>
  );
}
