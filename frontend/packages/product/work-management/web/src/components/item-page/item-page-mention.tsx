import { AlertTriangle } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { personInitials } from "@notrelix/work-management-core";
import { identityMarkClass } from "../shared/item-display";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import type { ItemPageCopy } from "./item-page.copy";
import { foldForSearch } from "./item-pickers";
import type { ItemPagePeople } from "./item-page-types";

/**
 * The @mention list of the comment box: people and teams that match what was
 * typed after "@". It says up front who the item reaches: a person who cannot
 * see the item is listed with a warning and is not notified.
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
  readonly kind: "person" | "team";
  readonly name: string;
  /** Who is notified when this is picked. */
  readonly userIds: readonly string[];
  readonly note: string | null;
  /** Shown under the name when the mention would not reach anyone. */
  readonly warning: string | null;
}

const MAX_CANDIDATES = 6;

export function mentionCandidates(
  token: MentionToken | null,
  people: ItemPagePeople | undefined,
  copy: ItemPageCopy,
  viewerUserId?: string,
): MentionCandidate[] {
  if (!token || !people) return [];
  const t = copy.composer;
  const needle = foldForSearch(token.query);
  const matches = (name: string) =>
    !needle || foldForSearch(name).includes(needle);
  const persons = people.people
    .filter((person) => person.userId !== viewerUserId && matches(person.name))
    .map((person): MentionCandidate => {
      const level = person.level ? t.mentionLevels[person.level] : null;
      return {
        key: person.userId,
        kind: "person",
        name: person.name,
        userIds: person.onBoard ? [person.userId] : [],
        note: person.onBoard
          ? [person.department, level].filter(Boolean).join(" · ") || null
          : person.department,
        warning: person.onBoard
          ? null
          : fillSheetCopy(t.mentionNoAccess, {
              name: person.name.split(" ").at(-1) ?? person.name,
            }),
      };
    });
  const teams = (people.teams ?? [])
    .filter((team) => matches(team.name))
    .map((team): MentionCandidate => ({
      key: team.id,
      kind: "team",
      name: team.name,
      userIds: team.viewableUserIds,
      note:
        team.viewableCount === team.memberCount
          ? fillSheetCopy(t.mentionTeamAll, { count: team.memberCount })
          : fillSheetCopy(t.mentionTeamSome, {
              count: team.memberCount,
              viewable: team.viewableCount,
            }),
      warning: null,
    }));
  // Who the item reaches first, teams between, who it cannot reach last.
  const reached = persons.filter((candidate) => !candidate.warning);
  const unreached = persons.filter((candidate) => candidate.warning);
  return [...reached, ...teams, ...unreached].slice(0, MAX_CANDIDATES);
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
  query,
  activeIndex,
  copy,
  onPick,
  onHover,
}: {
  candidates: readonly MentionCandidate[];
  query: string;
  activeIndex: number;
  copy: ItemPageCopy;
  onPick: (candidate: MentionCandidate) => void;
  onHover: (index: number) => void;
}) {
  if (candidates.length === 0) return null;
  return (
    <div
      role="listbox"
      id="item-page-mention-list"
      aria-label={copy.composer.mentionList}
      className="absolute bottom-full left-0 z-modal mb-2 w-75 overflow-hidden rounded-[10px] border border-line-strong bg-popover p-1.5 shadow-lg"
    >
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
              candidate.kind === "team"
                ? "bg-work-status-doing-lane text-work-status-doing-text"
                : identityMarkClass(candidate.key),
              candidate.warning && "opacity-60",
            )}
          >
            {personInitials(candidate.name)}
          </span>
          <span className="min-w-0 flex-1">
            <span className="block leading-tight">
              <Matched text={candidate.name} query={query} />
            </span>
            {candidate.note ? (
              <span className="block text-item-note leading-snug text-meta-foreground">
                {candidate.note}
              </span>
            ) : null}
            {candidate.warning ? (
              <span className="mt-0.5 flex items-start gap-1 text-item-note leading-snug text-warning-text">
                <AlertTriangle
                  aria-hidden="true"
                  className="mt-0.5 size-3 shrink-0"
                />
                {candidate.warning}
              </span>
            ) : null}
          </span>
        </div>
      ))}
    </div>
  );
}
