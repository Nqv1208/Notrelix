import { ThumbsUp, Eye } from "lucide-react";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
import type {
  CardDetail,
  CardVoteValue,
  FieldDefinition,
} from "@notrelix/work-management-core";
import {
  useItemViewers,
  useItemVotes,
  useRecordItemView,
  useVoteWrites,
} from "@notrelix/work-management-state";
import { fillCapabilityCopy } from "./capabilities.copy";
import { useCapabilitiesCopy } from "./use-capabilities-copy";

function stamp(iso: string, locale: string): string {
  return new Intl.DateTimeFormat(locale, {
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(iso));
}

function initials(name: string): string {
  return (
    name
      .split(/\s+/)
      .filter(Boolean)
      .slice(-2)
      .map((part) => Array.from(part)[0] ?? "")
      .join("")
      .toUpperCase() || "?"
  );
}

/** The value of a Vote field on a card, `{ count: 0, voted: false }` when it carries none. */
export function readVoteValue(card: CardDetail, fieldId: string): CardVoteValue {
  const raw = card.fieldValues?.[fieldId] as Partial<CardVoteValue> | undefined;
  return { count: raw?.count ?? 0, voted: raw?.voted === true };
}

/**
 * The "Bình chọn" chip of a Vote field: press it to vote or withdraw (one vote each), press the number
 * to see who voted. Board members see the voters; `canVote` is the right to comment.
 */
export function VoteChip({
  card,
  field,
  canVote,
}: {
  card: CardDetail;
  field: FieldDefinition;
  canVote: boolean;
}) {
  const copy = useCapabilitiesCopy();
  const scope = { itemId: card.id, boardId: card.boardId, workspaceId: card.workspaceId };
  const write = useVoteWrites(scope, field.id);
  const value = readVoteValue(card, field.id);
  const voters = useItemVotes(card.id, field.id, card.workspaceId, true);
  const shown = write.data ?? value;
  const locale = copy.locale;
  return (
    <div className="flex items-center gap-1.5">
      <button
        type="button"
        disabled={!canVote || write.isPending}
        aria-pressed={shown.voted}
        onClick={() => write.mutate(!shown.voted)}
        className={cn(
          "inline-flex h-7 items-center gap-1 rounded-md border border-border px-2 text-nav font-medium focus-visible:outline-2 focus-visible:outline-ring",
          shown.voted && "border-primary bg-accent-subtle text-primary",
        )}
      >
        <ThumbsUp className="size-3.5" aria-hidden="true" />
        {shown.voted ? copy.vote.voted : field.name || copy.vote.toggle}
      </button>
      <Popover>
        <PopoverTrigger asChild>
          <button
            type="button"
            aria-label={fillCapabilityCopy(copy.vote.count, { count: shown.count })}
            className="inline-flex h-7 min-w-7 items-center justify-center rounded-md bg-state-hover-strong px-1.5 text-nav font-semibold tabular-nums focus-visible:outline-2 focus-visible:outline-ring"
          >
            {shown.count}
          </button>
        </PopoverTrigger>
        <PopoverContent align="start" className="flex w-72 flex-col gap-2 p-3">
          <h2 className="text-nav font-semibold">{copy.vote.title}</h2>
          <p className="text-micro text-meta-foreground">
            {shown.count === 0
              ? copy.vote.none
              : fillCapabilityCopy(copy.vote.people, { count: shown.count })}
          </p>
          <ul className="flex flex-col gap-1.5">
            {(voters.data?.voters ?? []).map((voter) => (
              <li key={voter.userId} className="flex items-center gap-2 text-nav">
                <span
                  aria-hidden="true"
                  className="grid size-6 place-items-center rounded-full bg-state-hover-strong text-mark-xs"
                >
                  {initials(voter.name)}
                </span>
                <span className="min-w-0 flex-1 truncate">{voter.name}</span>
                <span className="text-micro tabular-nums text-meta-foreground">
                  {stamp(voter.votedAt, locale)}
                </span>
              </li>
            ))}
          </ul>
          <p className="text-micro text-meta-foreground">{copy.vote.note}</p>
        </PopoverContent>
      </Popover>
    </div>
  );
}

/**
 * The read-only "Đã xem" chip: how many board members opened the card, with the names and times in its
 * list. Mounting it records the viewer's own view (`recordView`) once per open; guests are skipped by
 * the server.
 */
export function ViewersChip({
  card,
  recordView = true,
}: {
  card: CardDetail;
  recordView?: boolean;
}) {
  const copy = useCapabilitiesCopy();
  useRecordItemView(card.id, recordView && !card.isArchived);
  const viewers = useItemViewers(card.id, card.workspaceId);
  const count = viewers.data?.count ?? 0;
  return (
    <Popover>
      <PopoverTrigger asChild>
        <button
          type="button"
          aria-label={fillCapabilityCopy(copy.viewers.count, { count })}
          className="inline-flex h-7 items-center gap-1 rounded-md bg-state-hover-strong px-2 text-nav font-semibold tabular-nums focus-visible:outline-2 focus-visible:outline-ring"
        >
          <Eye className="size-3.5" aria-hidden="true" />
          {count}
        </button>
      </PopoverTrigger>
      <PopoverContent align="start" className="flex w-72 flex-col gap-2 p-3">
        <h2 className="text-nav font-semibold">{copy.viewers.title}</h2>
        {count === 0 ? (
          <p className="text-micro text-meta-foreground">{copy.viewers.none}</p>
        ) : null}
        <ul className="flex flex-col gap-1.5">
          {(viewers.data?.viewers ?? []).map((viewer) => (
            <li key={viewer.userId} className="flex items-center gap-2 text-nav">
              <span
                aria-hidden="true"
                className="grid size-6 place-items-center rounded-full bg-state-hover-strong text-mark-xs"
              >
                {initials(viewer.name)}
              </span>
              <span className="min-w-0 flex-1 truncate">{viewer.name}</span>
              <span className="text-micro tabular-nums text-meta-foreground">
                {stamp(viewer.lastViewedAt, copy.locale)}
              </span>
            </li>
          ))}
        </ul>
        <p className="text-micro text-meta-foreground">{copy.viewers.note}</p>
      </PopoverContent>
    </Popover>
  );
}
