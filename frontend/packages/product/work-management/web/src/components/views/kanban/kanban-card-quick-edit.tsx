import { useState } from "react";
import {
  CalendarDays,
  Check,
  ExternalLink,
  Pencil,
  Tag,
  UserRound,
} from "lucide-react";
import {
  Button,
  Input,
  Popover,
  PopoverContent,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
import type { Card, CardLabel } from "@notrelix/work-management-core";
import { fillCopy, type KanbanCardCopy } from "./kanban-card.copy";

/** What the board lends a card to edit it in place, without the detail popup. */
export interface KanbanQuickEdit {
  readonly labels: readonly Pick<CardLabel, "id" | "name">[];
  readonly members: readonly { userId: string; name: string }[];
  readonly onRename: (card: Card, title: string) => void;
  readonly onToggleLabel: (card: Card, labelId: string, on: boolean) => void;
  readonly onToggleMember: (card: Card, userId: string, on: boolean) => void;
  /** `YYYY-MM-DD` or null for each end. */
  readonly onSetDates: (
    card: Card,
    dates: { start: string | null; due: string | null },
  ) => void;
  readonly onOpen: (cardId: string) => void;
}

type Panel = "labels" | "members" | "dates" | null;

const ROW =
  "flex h-9 w-full cursor-pointer items-center gap-2 rounded-[8px] border-0 bg-transparent px-2 text-left text-[13.5px] text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-primary";

const cardDueDay = (card: Card) => dayOf(card.dueAt ?? card.dueDate);
const dayOf = (value: string | undefined | null) =>
  typeof value === "string" ? value.slice(0, 10) : "";

/**
 * The pencil of a card: a panel over the card to rename it, put labels and people on it, set its dates or open
 * it in full, so a small change never needs the detail popup. Each pick saves at once; the name saves with
 * Save (or Enter).
 */
export function KanbanCardQuickEdit({
  card,
  copy,
  edit,
  open,
  onOpenChange,
  className,
}: {
  card: Card;
  copy: KanbanCardCopy;
  edit: KanbanQuickEdit;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  className: string;
}) {
  const [title, setTitle] = useState(card.title);
  const [panel, setPanel] = useState<Panel>(null);
  const [start, setStart] = useState(dayOf(card.startDate));
  const [due, setDue] = useState(cardDueDay(card));
  const onLabels = new Set((card.labels ?? []).map((label) => label.id));
  const onMembers = new Set((card.members ?? []).map((m) => m.userId));
  const badDates = start !== "" && due !== "" && due < start;
  const save = () => {
    const next = title.trim();
    if (next && next !== card.title) edit.onRename(card, next);
    onOpenChange(false);
  };
  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        if (next) {
          setTitle(card.title);
          setPanel(null);
          setStart(dayOf(card.startDate));
          setDue(cardDueDay(card));
        }
        onOpenChange(next);
      }}
    >
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className={className}
          aria-label={copy.quickEdit}
          title={copy.quickEdit}
        >
          <Pencil aria-hidden="true" className="size-3.5" />
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="end"
        className="flex w-72 flex-col gap-1.5 p-2"
        onClick={(event) => event.stopPropagation()}
        onKeyDown={(event) => event.stopPropagation()}
      >
        <textarea
          autoFocus
          rows={3}
          value={title}
          maxLength={200}
          aria-label={fillCopy(copy.quickEditTitle, { title: card.title })}
          onChange={(event) => setTitle(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter" && !event.shiftKey) {
              event.preventDefault();
              save();
            }
          }}
          className="box-border w-full resize-none rounded-[8px] border border-primary bg-popover p-2 text-[14px] text-foreground ring-[3px] ring-brand-subtle outline-none"
        />
        <Button
          type="button"
          size="sm"
          className="self-start"
          disabled={title.trim() === ""}
          onClick={save}
        >
          {copy.quickSave}
        </Button>
        <button
          type="button"
          className={ROW}
          onClick={() => {
            onOpenChange(false);
            edit.onOpen(card.id);
          }}
        >
          <ExternalLink aria-hidden="true" className="size-4" />
          {copy.openCard}
        </button>
        <button
          type="button"
          className={ROW}
          aria-expanded={panel === "labels"}
          onClick={() => setPanel(panel === "labels" ? null : "labels")}
        >
          <Tag aria-hidden="true" className="size-4" />
          {copy.editLabels}
        </button>
        {panel === "labels" ? (
          <div className="flex max-h-48 flex-col overflow-y-auto pl-4">
            {edit.labels.length === 0 ? (
              <p className="px-2 py-1 text-metadata text-muted-foreground">
                {copy.noLabels}
              </p>
            ) : (
              edit.labels.map((label) => (
                <PickRow
                  key={label.id}
                  checked={onLabels.has(label.id)}
                  onClick={() =>
                    edit.onToggleLabel(card, label.id, !onLabels.has(label.id))
                  }
                >
                  {label.name}
                </PickRow>
              ))
            )}
          </div>
        ) : null}
        <button
          type="button"
          className={ROW}
          aria-expanded={panel === "members"}
          onClick={() => setPanel(panel === "members" ? null : "members")}
        >
          <UserRound aria-hidden="true" className="size-4" />
          {copy.changeMembers}
        </button>
        {panel === "members" ? (
          <div className="flex max-h-48 flex-col overflow-y-auto pl-4">
            {edit.members.length === 0 ? (
              <p className="px-2 py-1 text-metadata text-muted-foreground">
                {copy.noMembers}
              </p>
            ) : (
              edit.members.map((member) => (
                <PickRow
                  key={member.userId}
                  checked={onMembers.has(member.userId)}
                  onClick={() =>
                    edit.onToggleMember(
                      card,
                      member.userId,
                      !onMembers.has(member.userId),
                    )
                  }
                >
                  {member.name}
                </PickRow>
              ))
            )}
          </div>
        ) : null}
        <button
          type="button"
          className={ROW}
          aria-expanded={panel === "dates"}
          onClick={() => setPanel(panel === "dates" ? null : "dates")}
        >
          <CalendarDays aria-hidden="true" className="size-4" />
          {copy.editDates}
        </button>
        {panel === "dates" ? (
          <div className="flex flex-col gap-2 pl-4">
            <label className="flex flex-col gap-1 text-metadata font-semibold text-muted-foreground">
              {copy.startDay}
              <Input
                type="date"
                value={start}
                onChange={(event) => setStart(event.target.value)}
              />
            </label>
            <label className="flex flex-col gap-1 text-metadata font-semibold text-muted-foreground">
              {copy.dueDay}
              <Input
                type="date"
                value={due}
                aria-invalid={badDates || undefined}
                onChange={(event) => setDue(event.target.value)}
              />
            </label>
            {badDates ? (
              <p role="alert" className="text-metadata text-destructive">
                {copy.datesBad}
              </p>
            ) : null}
            <div className="flex gap-2">
              <Button
                type="button"
                size="sm"
                disabled={badDates}
                onClick={() => {
                  edit.onSetDates(card, {
                    start: start || null,
                    due: due || null,
                  });
                  onOpenChange(false);
                }}
              >
                {copy.saveDates}
              </Button>
              {start || due ? (
                <Button
                  type="button"
                  size="sm"
                  variant="ghost"
                  onClick={() => {
                    edit.onSetDates(card, { start: null, due: null });
                    onOpenChange(false);
                  }}
                >
                  {copy.clearDates}
                </Button>
              ) : null}
            </div>
          </div>
        ) : null}
      </PopoverContent>
    </Popover>
  );
}

function PickRow({
  checked,
  onClick,
  children,
}: {
  checked: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      role="menuitemcheckbox"
      aria-checked={checked}
      className={cn(ROW, "h-8 justify-between")}
      onClick={onClick}
    >
      <span className="min-w-0 truncate">{children}</span>
      {checked ? <Check aria-hidden="true" className="size-4" /> : null}
    </button>
  );
}
