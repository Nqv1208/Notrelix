import { ThumbsUp, Eye } from "lucide-react";
import { useState } from "react";
import { PopoverTrigger, cn } from "@notrelix/ui-web";
import {
  personInitials,
  type CardDetail,
  type CardVoteValue,
  type FieldDefinition,
} from "@notrelix/work-management-core";
import {
  useItemViewers,
  useItemVotes,
  useRecordItemView,
  useVoteWrites,
} from "@notrelix/work-management-state";
import {
  ChipBtn,
  FieldGroup,
  PanelBtn,
  PanelPopover,
  PersonMark,
} from "../popup-ui";
import { Caption, Hint, PickerHead } from "../pickers/picker-bits";
import { useCardPopup } from "../card-popup-context";
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

function markOf(person: { userId: string; name: string }) {
  return { ...person, initials: personInitials(person.name) };
}

/** The value of a Vote field on a card, `{ count: 0, voted: false }` when it carries none. */
export function readVoteValue(
  card: CardDetail,
  fieldId: string,
): CardVoteValue {
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
  const scope = {
    itemId: card.id,
    boardId: card.boardId,
    workspaceId: card.workspaceId,
  };
  const write = useVoteWrites(scope, field.id);
  const value = readVoteValue(card, field.id);
  const voters = useItemVotes(card.id, field.id, card.workspaceId, true);
  const shown = write.data ?? value;
  const [voteOpen, setVoteOpen] = useState(false);
  return (
    <FieldGroup label={field.name || copy.vote.toggle}>
      <div className="flex items-center gap-2">
        <ChipBtn
          disabled={!canVote || write.isPending}
          aria-pressed={shown.voted}
          tone={shown.voted ? "on" : "default"}
          onClick={() => write.mutate(!shown.voted)}
        >
          <ThumbsUp className="size-[13px]" aria-hidden="true" />
          {shown.voted ? copy.vote.voted : copy.vote.toggle}
        </ChipBtn>
        <PanelPopover
          open={voteOpen}
          onOpenChange={setVoteOpen}
          width={300}
          trigger={
            <PopoverTrigger asChild>
              <ChipBtn
                aria-label={fillCapabilityCopy(copy.vote.count, {
                  count: shown.count,
                })}
                className="min-w-9 justify-center px-2.5 font-semibold tabular-nums"
              >
                {shown.count}
              </ChipBtn>
            </PopoverTrigger>
          }
        >
          <PickerHead
            title={copy.vote.panel}
            onClose={() => setVoteOpen(false)}
          />
          <PanelBtn
            className="w-full"
            disabled={!canVote || write.isPending}
            aria-pressed={shown.voted}
            onClick={() => write.mutate(!shown.voted)}
          >
            <ThumbsUp aria-hidden="true" className="size-4" />
            {copy.vote.title}
          </PanelBtn>
          <Caption>
            {shown.count === 0
              ? copy.vote.none
              : fillCapabilityCopy(copy.vote.people, { count: shown.count })}
          </Caption>
          {(voters.data?.voters ?? []).map((voter) => (
            <div
              key={voter.userId}
              className="flex min-h-10 items-center gap-2.5 px-1"
            >
              <PersonMark member={markOf(voter)} size={24} />
              <span className="min-w-0 flex-1 truncate text-[13.5px]">
                {voter.name}
              </span>
            </div>
          ))}
          <Hint className="mt-2 px-0.5">{copy.vote.note}</Hint>
        </PanelPopover>
      </div>
    </FieldGroup>
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
  const [open, setOpen] = useState(false);
  const viewerId = useCardPopup().data.viewerUserId ?? null;
  return (
    <FieldGroup label={copy.viewers.label}>
      <PanelPopover
        open={open}
        onOpenChange={setOpen}
        width={320}
        trigger={
          <PopoverTrigger asChild>
            <ChipBtn
              aria-label={fillCapabilityCopy(copy.viewers.count, { count })}
              className="px-2.5 font-semibold tabular-nums"
            >
              <Eye className="size-[13px]" aria-hidden="true" />
              {count}
            </ChipBtn>
          </PopoverTrigger>
        }
      >
        <PickerHead title={copy.viewers.label} onClose={() => setOpen(false)} />
        <Caption className="mt-0">
          {count === 0
            ? copy.viewers.none
            : fillCapabilityCopy(copy.viewers.people, { count })}
        </Caption>
        {(viewers.data?.viewers ?? []).map((viewer) => (
          <div
            key={viewer.userId}
            className="flex min-h-11 items-center gap-2.5 px-1"
          >
            <PersonMark member={markOf(viewer)} size={24} />
            <span className="min-w-0 flex-1 truncate text-[13.5px]">
              {viewer.name}
            </span>
            <span className="shrink-0 text-[12.5px] text-meta-foreground">
              {viewer.userId === viewerId
                ? copy.viewers.you
                : stamp(viewer.lastViewedAt, copy.locale)}
            </span>
          </div>
        ))}
        <Hint className="mt-2 px-0.5">{copy.viewers.note}</Hint>
      </PanelPopover>
    </FieldGroup>
  );
}
