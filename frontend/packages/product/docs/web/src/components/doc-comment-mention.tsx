import { cn } from "@notrelix/ui-web";
import type { DocPageCopy } from "./doc-page.copy";
import type { DocMentionPeople, DocPerson } from "./doc-page-models";
import { DocAvatar } from "./doc-page-ui";

/** The "@name" the caret is in, when the "@" starts the text or follows a space. */
export function commentMentionToken(
  text: string,
  caret: number,
): { from: number; to: number; query: string } | null {
  const match = /(^|\s)@([^\s@]*)$/.exec(text.slice(0, caret));
  if (!match) return null;
  const query = match[2] ?? "";
  return { from: caret - query.length - 1, to: caret, query };
}

/** The ids to send: people whose "@name" is still in the text, once each. */
export function mentionIdsIn(
  text: string,
  mentions: readonly { readonly id: string; readonly name: string }[],
): string[] {
  return [
    ...new Set(
      mentions
        .filter((entry) => text.includes(`@${entry.name}`))
        .map((entry) => entry.id),
    ),
  ];
}

/**
 * The people the server offers for the text after "@" in a comment field (only
 * people who can view the page): loading, nobody, a failed read with a retry,
 * or the rows. The field keeps focus; keys are handled by the field.
 */
export function CommentMentionList({
  copy,
  people: state,
  activeIndex,
  onPick,
  onHover,
}: {
  copy: DocPageCopy;
  people: DocMentionPeople;
  activeIndex: number;
  onPick: (person: DocPerson) => void;
  onHover: (index: number) => void;
}) {
  const m = copy.mention;
  return (
    <div
      role="listbox"
      aria-label={m.people}
      aria-busy={state.status === "loading"}
      className="absolute bottom-full left-0 z-dropdown mb-1.5 w-full overflow-hidden rounded-[10px] border border-line-strong bg-popover p-1 shadow-popover"
    >
      {state.people.length === 0 ? (
        <span
          role={state.status === "error" ? "alert" : "status"}
          className="flex items-center justify-between gap-2 px-2 py-1.5 text-crumb leading-[1.45] text-meta-foreground"
        >
          {state.status === "loading" ? (
            m.peopleLoading
          ) : state.status === "error" ? (
            <>
              {m.peopleError}
              <button
                type="button"
                onMouseDown={(event) => event.preventDefault()}
                onClick={state.onRetry}
                className="rounded-[6px] px-2 py-0.5 font-semibold text-brand-subtle-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring"
              >
                {m.peopleRetry}
              </button>
            </>
          ) : (
            m.peopleEmpty
          )}
        </span>
      ) : null}
      {state.people.map((person, index) => (
        <button
          key={person.id}
          type="button"
          role="option"
          aria-selected={index === activeIndex}
          // The field keeps focus: a click must not blur it first.
          onMouseDown={(event) => event.preventDefault()}
          onMouseEnter={() => onHover(index)}
          onClick={() => onPick(person)}
          className={cn(
            "flex w-full items-center gap-2 rounded-[7px] border-0 bg-transparent px-2 py-1.5 text-left text-compact leading-[1.45] text-foreground outline-none",
            index === activeIndex && "bg-state-hover-strong",
          )}
        >
          <DocAvatar person={person} />
          <span className="min-w-0 truncate">{person.name}</span>
        </button>
      ))}
    </div>
  );
}
