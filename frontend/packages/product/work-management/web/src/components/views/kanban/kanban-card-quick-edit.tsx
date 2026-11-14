import { useLayoutEffect, useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import {
  CalendarDays,
  Check,
  ArrowRight,
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
import type { KanbanCardMove } from "./kanban-card-move-menu";

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

/** A one-click row of the side stack (copy card, copy link, archive). */
export interface KanbanQuickRow {
  readonly key: string;
  readonly label: string;
  readonly icon: ReactNode;
  readonly run: () => void;
}

type Panel = "labels" | "members" | "dates" | "move" | null;

const ROW =
  "flex h-9 w-full cursor-pointer items-center gap-2 rounded-[8px] border-0 bg-transparent px-2 text-left text-[13.5px] text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-primary";

const cardDueDay = (card: Card) => dayOf(card.dueAt ?? card.dueDate);
const dayOf = (value: string | undefined | null) =>
  typeof value === "string" ? value.slice(0, 10) : "";

const SIDE =
  "flex h-9 w-full cursor-pointer items-center gap-2 rounded-[8px] border border-line-strong bg-popover px-3 text-left text-[14px] font-medium whitespace-nowrap text-foreground shadow-card-hover hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-primary";

type Rect = { left: number; top: number; width: number; height: number };

/**
 * The pencil of a card. Editing happens in the card itself, as on Trello: the card's own place on the board
 * turns into a name box with Save under it, the rest of the board dims, and a stack of buttons beside it
 * opens the card, picks labels and people, sets dates, moves, copies or archives it. Each pick saves at once;
 * the name saves with Save or Enter; Escape or a click outside closes it.
 */
export function KanbanCardQuickEdit({
  card,
  copy,
  edit,
  rows,
  move,
  open,
  onOpenChange,
  className,
}: {
  card: Card;
  copy: KanbanCardCopy;
  edit: KanbanQuickEdit;
  rows: readonly KanbanQuickRow[];
  move?: KanbanCardMove;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  className: string;
}) {
  const trigger = useRef<HTMLButtonElement>(null);
  const [rect, setRect] = useState<Rect | null>(null);
  const [title, setTitle] = useState(card.title);
  const [panel, setPanel] = useState<Panel>(null);
  const [start, setStart] = useState(dayOf(card.startDate));
  const [due, setDue] = useState(cardDueDay(card));
  const area = useRef<HTMLTextAreaElement>(null);

  useLayoutEffect(() => {
    if (!open) return;
    const el = trigger.current?.closest("article");
    if (!el) return;
    const box = el.getBoundingClientRect();
    setRect({
      left: box.left,
      top: box.top,
      width: box.width,
      height: box.height,
    });
    setTitle(card.title);
    setPanel(null);
    setStart(dayOf(card.startDate));
    setDue(cardDueDay(card));
    // Only when it opens: a pick that changes the card must not reset the name being typed.
  }, [open]);
  useLayoutEffect(() => {
    if (open && rect) area.current?.select();
  }, [open, rect === null]);

  const onLabels = new Set((card.labels ?? []).map((label) => label.id));
  const onMembers = new Set((card.members ?? []).map((m) => m.userId));
  const badDates = start !== "" && due !== "" && due < start;
  const close = () => onOpenChange(false);
  const save = () => {
    const next = title.trim();
    if (next && next !== card.title) edit.onRename(card, next);
    close();
  };
  const sideRight = rect
    ? rect.left + rect.width + 8 + 240 <= window.innerWidth
    : true;
  const toggle = (next: Exclude<Panel, null>) =>
    setPanel(panel === next ? null : next);

  const side = (
    key: Exclude<Panel, null>,
    icon: ReactNode,
    label: string,
    body: ReactNode,
  ) => (
    <Popover
      open={panel === key}
      onOpenChange={(next) => setPanel(next ? key : null)}
    >
      <PopoverTrigger asChild>
        <button type="button" className={SIDE} onClick={() => toggle(key)}>
          {icon}
          {label}
        </button>
      </PopoverTrigger>
      <PopoverContent
        side={sideRight ? "right" : "left"}
        align="start"
        className="flex w-64 flex-col gap-1 p-2"
        onClick={(event) => event.stopPropagation()}
      >
        {body}
      </PopoverContent>
    </Popover>
  );

  return (
    <>
      <Button
        ref={trigger}
        type="button"
        variant="ghost"
        size="icon-sm"
        className={className}
        aria-label={copy.quickEdit}
        title={copy.quickEdit}
        onClick={() => onOpenChange(true)}
      >
        <Pencil aria-hidden="true" className="size-3.5" />
      </Button>
      {open && rect
        ? createPortal(
            <div
              role="dialog"
              aria-label={fillCopy(copy.quickEditTitle, { title: card.title })}
              className="fixed inset-0 z-40 bg-black/55"
              onClick={(event) => {
                event.stopPropagation();
                close();
              }}
              onPointerDown={(event) => event.stopPropagation()}
              onKeyDown={(event) => {
                event.stopPropagation();
                if (event.key === "Escape") close();
              }}
            >
              <div
                className="absolute"
                style={{ left: rect.left, top: rect.top, width: rect.width }}
                onClick={(event) => event.stopPropagation()}
              >
                <textarea
                  ref={area}
                  autoFocus
                  value={title}
                  maxLength={200}
                  aria-label={fillCopy(copy.quickEditTitle, {
                    title: card.title,
                  })}
                  onChange={(event) => setTitle(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === "Enter" && !event.shiftKey) {
                      event.preventDefault();
                      save();
                    }
                  }}
                  style={{ minHeight: rect.height }}
                  className="box-border block w-full resize-none rounded-[10px] border border-primary bg-card p-3 text-[14px] leading-snug font-medium text-foreground shadow-card-hover outline-none"
                />
                <Button
                  type="button"
                  className="mt-2"
                  disabled={title.trim() === ""}
                  onClick={save}
                >
                  {copy.quickSave}
                </Button>
              </div>
              <div
                className="absolute flex w-60 flex-col gap-1"
                style={{
                  top: rect.top,
                  left: sideRight ? rect.left + rect.width + 8 : undefined,
                  right: sideRight
                    ? undefined
                    : window.innerWidth - rect.left + 8,
                }}
                onClick={(event) => event.stopPropagation()}
              >
                <button
                  type="button"
                  className={SIDE}
                  onClick={() => {
                    close();
                    edit.onOpen(card.id);
                  }}
                >
                  <ExternalLink aria-hidden="true" className="size-4" />
                  {copy.openCard}
                </button>
                {side(
                  "labels",
                  <Tag aria-hidden="true" className="size-4" />,
                  copy.editLabels,
                  edit.labels.length === 0 ? (
                    <p className="px-2 py-1 text-metadata text-muted-foreground">
                      {copy.noLabels}
                    </p>
                  ) : (
                    <div className="flex max-h-64 flex-col overflow-y-auto">
                      {edit.labels.map((label) => (
                        <PickRow
                          key={label.id}
                          checked={onLabels.has(label.id)}
                          onClick={() =>
                            edit.onToggleLabel(
                              card,
                              label.id,
                              !onLabels.has(label.id),
                            )
                          }
                        >
                          {label.name}
                        </PickRow>
                      ))}
                    </div>
                  ),
                )}
                {side(
                  "members",
                  <UserRound aria-hidden="true" className="size-4" />,
                  copy.changeMembers,
                  edit.members.length === 0 ? (
                    <p className="px-2 py-1 text-metadata text-muted-foreground">
                      {copy.noMembers}
                    </p>
                  ) : (
                    <div className="flex max-h-64 flex-col overflow-y-auto">
                      {edit.members.map((member) => (
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
                      ))}
                    </div>
                  ),
                )}
                {side(
                  "dates",
                  <CalendarDays aria-hidden="true" className="size-4" />,
                  copy.editDates,
                  <div className="flex flex-col gap-2 p-1">
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
                      <p
                        role="alert"
                        className="text-metadata text-destructive"
                      >
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
                          close();
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
                            close();
                          }}
                        >
                          {copy.clearDates}
                        </Button>
                      ) : null}
                    </div>
                  </div>,
                )}
                {move && !move.disabledReason
                  ? side(
                      "move",
                      <ArrowRight aria-hidden="true" className="size-4" />,
                      copy.moveTo,
                      <div className="flex max-h-64 flex-col overflow-y-auto">
                        {move.lanes.map((lane) => (
                          <PickRow
                            key={lane.id}
                            checked={move.currentLaneId(card) === lane.id}
                            onClick={() => {
                              if (move.currentLaneId(card) !== lane.id)
                                move.onMove(card, lane.id);
                              close();
                            }}
                          >
                            {lane.title}
                          </PickRow>
                        ))}
                      </div>,
                    )
                  : null}
                {rows.map((row) => (
                  <button
                    key={row.key}
                    type="button"
                    className={SIDE}
                    onClick={() => {
                      close();
                      row.run();
                    }}
                  >
                    {row.icon}
                    {row.label}
                  </button>
                ))}
              </div>
            </div>,
            document.body,
          )
        : null}
    </>
  );
}

function PickRow({
  checked,
  onClick,
  children,
}: {
  checked: boolean;
  onClick: () => void;
  children: ReactNode;
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
