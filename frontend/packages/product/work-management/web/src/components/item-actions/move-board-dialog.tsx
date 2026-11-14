import { useRef, useState } from "react";
import {
  Check,
  CornerDownRight,
  Hash,
  KanbanSquare,
  ListChecks,
  Search,
  TriangleAlert,
} from "lucide-react";
import { Checkbox, Skeleton, cn } from "@notrelix/ui-web";
import { fillLifecycleCopy } from "../item-lifecycle/item-lifecycle.copy";
import {
  ActionBanner,
  ActionButton,
  ActionDialog,
  ImpactLine,
} from "./action-dialog";
import type { ItemActionsCopy } from "./item-actions.copy";

export interface MoveTargetBoard {
  readonly id: string;
  readonly title: string;
  /** The key the board's items carry, shown next to its name when the list says it. */
  readonly keyPrefix?: string;
  /** The department the board belongs to, shown at the right when the list says it. */
  readonly space?: string;
}

/** What moving to the chosen board will do, already worded for the person. */
export interface MovePreviewView {
  readonly boardTitle: string;
  readonly group: { readonly title: string | null; readonly matched: boolean };
  readonly status: { readonly name: string | null; readonly matched: boolean };
  /** Each thing that will be lost, e.g. "Budget (the other board has no such property)". */
  readonly losses: readonly string[];
}

export type MoveDialogStatus =
  "idle" | "busy" | "error" | "deps" | "archived" | "conflict";

/**
 * Picks the board an item moves to and shows what carries over before the
 * person confirms (WM-Actions-Move). Anything that will be lost is named and
 * needs a tick; a refusal (dependencies, archived) shows with its way out;
 * once the request is sent the dialog cannot be dismissed. The preview is
 * provisional, the server decides. On a phone it is a bottom sheet with the
 * boards as rows and the preview as one sentence.
 */
export function MoveBoardDialog({
  copy,
  title,
  itemKey,
  boards,
  open,
  preview,
  status,
  blockingKey,
  onPick,
  onCancel,
  onConfirm,
  onOpenBlocking,
  onUnarchive,
}: {
  copy: ItemActionsCopy;
  title: string;
  itemKey: string | null;
  boards: readonly MoveTargetBoard[];
  open: boolean;
  preview: MovePreviewView | null;
  status: MoveDialogStatus;
  blockingKey?: string | null;
  onPick: (boardId: string) => void;
  onCancel: () => void;
  onConfirm: (boardId: string) => void;
  onOpenBlocking?: () => void;
  onUnarchive?: () => void;
}) {
  const [query, setQuery] = useState("");
  const [chosen, setChosen] = useState<string | null>(null);
  const [acked, setAcked] = useState(false);
  const search = useRef<HTMLInputElement>(null);
  const focus = useRef<HTMLElement | null>(null);
  const busy = status === "busy";
  const archived = status === "archived";
  const deps = status === "deps";
  const needle = query.trim().toLowerCase();
  const shown = boards.filter((board) =>
    `${board.title} ${board.keyPrefix ?? ""}`.toLowerCase().includes(needle),
  );
  const target = boards.find((board) => board.id === chosen) ?? null;
  const losses = preview?.losses ?? [];
  const mustAck = losses.length > 0;
  const showPreview = Boolean(target && preview) && !deps && !archived;
  const close = () => {
    if (busy) return;
    setQuery("");
    setChosen(null);
    setAcked(false);
    onCancel();
  };
  const refusal =
    status === "deps"
      ? blockingKey
        ? fillLifecycleCopy(copy.moveRefusedDepsKey, { key: blockingKey })
        : copy.moveRefusedDeps
      : archived
        ? copy.moveRefusedArchived
        : status === "error"
          ? copy.moveFailed
          : status === "conflict"
            ? copy.moveConflict
            : null;
  const canConfirm =
    target !== null && !busy && !deps && !archived && (!mustAck || acked);
  const noBoards = boards.length === 0;
  const picking = !busy && !archived && !noBoards;
  const label = !target
    ? copy.moveConfirm
    : mustAck
      ? fillLifecycleCopy(copy.moveConfirmLosing, { count: losses.length })
      : fillLifecycleCopy(copy.moveConfirmTo, { board: target.title });
  const hint = busy
    ? ""
    : archived || noBoards || status === "error" || status === "conflict"
      ? ""
      : deps
        ? copy.moveHintDeps
        : !target
          ? copy.movePick
          : mustAck && !acked
            ? copy.moveHintAck
            : "";
  return (
    <ActionDialog
      open={open}
      busy={busy}
      title={fillLifecycleCopy(copy.moveTitle, { title })}
      phoneTitle={copy.movePhoneTitle}
      description={busy ? copy.moveBusyBody : copy.moveBody}
      phoneDescription={null}
      hideDescription={noBoards}
      closeLabel={copy.close}
      maxWidth="sm:max-w-140"
      initialFocus={
        picking ? (search as { current: HTMLElement | null }) : focus
      }
      hint={hint}
      onCancel={close}
      footer={
        <>
          {busy ? null : (
            <ActionButton onClick={close}>
              {noBoards ? copy.close : copy.cancel}
            </ActionButton>
          )}
          {deps && blockingKey && onOpenBlocking ? (
            <ActionButton onClick={onOpenBlocking}>
              {fillLifecycleCopy(copy.moveOpenBlocking, { key: blockingKey })}
            </ActionButton>
          ) : null}
          {archived && onUnarchive ? (
            <ActionButton onClick={onUnarchive}>{copy.unarchive}</ActionButton>
          ) : null}
          {!archived && !noBoards ? (
            <ActionButton
              primary
              disabled={!canConfirm}
              onClick={() => target && onConfirm(target.id)}
            >
              {busy
                ? copy.moveBusy
                : status === "error" || status === "conflict"
                  ? copy.retry
                  : label}
            </ActionButton>
          ) : null}
        </>
      }
    >
      <div
        className={cn(
          "flex flex-col gap-2.5 px-[22px] pt-2.5 max-sm:gap-0 max-sm:px-0 max-sm:pt-0",
          archived && "max-sm:px-0",
        )}
      >
        {busy ? (
          <div aria-hidden="true" className="flex flex-col gap-2 max-sm:px-5">
            <Skeleton className="h-3 w-[60%]" />
            <Skeleton className="h-3 w-[80%]" />
          </div>
        ) : null}
        {noBoards && !busy ? (
          <ActionBanner tone="info" className="max-sm:mx-4 max-sm:mb-2">
            {copy.moveNoBoards}
          </ActionBanner>
        ) : null}
        {picking ? (
          <>
            <label className="flex h-[34px] items-center gap-2 rounded-[8px] border border-brand bg-popover px-2.5 text-[13.5px] focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-ring max-sm:hidden dark:bg-card">
              <Search
                aria-hidden="true"
                className="size-4 shrink-0 text-muted-foreground"
              />
              <input
                ref={search}
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder={copy.movePlaceholder}
                aria-label={copy.moveSearch}
                className="min-w-0 flex-1 bg-transparent outline-none placeholder:text-meta-foreground"
              />
            </label>
            {shown.length === 0 ? (
              <p className="m-0 rounded-[10px] border border-line px-3.5 py-3.5 text-[13px] text-meta-foreground max-sm:mx-5 max-sm:border-0 max-sm:px-0">
                {fillLifecycleCopy(copy.moveNoMatch, { query: query.trim() })}
              </p>
            ) : (
              <ul
                role="listbox"
                aria-label={copy.moveListLabel}
                className="m-0 flex max-h-[212px] list-none flex-col overflow-y-auto rounded-[10px] border border-line py-1 max-sm:max-h-[208px] max-sm:rounded-none max-sm:border-0 max-sm:py-0"
              >
                {shown.map((board) => {
                  const on = chosen === board.id;
                  return (
                    <li key={board.id} role="presentation">
                      <button
                        type="button"
                        role="option"
                        aria-selected={on}
                        onClick={() => {
                          setChosen(board.id);
                          setAcked(false);
                          onPick(board.id);
                        }}
                        className={cn(
                          "mx-1 flex min-h-[34px] w-[calc(100%-0.5rem)] cursor-pointer items-center gap-[9px] rounded-[7px] px-2.5 text-left text-[13.5px] leading-[1.45] hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring",
                          "max-sm:mx-0 max-sm:min-h-[52px] max-sm:w-full max-sm:rounded-none max-sm:px-5 max-sm:text-[15px] max-sm:font-semibold max-sm:hover:bg-transparent",
                          on &&
                            "bg-brand-subtle font-semibold text-brand-subtle-foreground max-sm:bg-transparent max-sm:text-foreground",
                        )}
                      >
                        <KanbanSquare
                          aria-hidden="true"
                          className="size-4 shrink-0 max-sm:hidden"
                        />
                        <span className="min-w-0 flex-1 max-sm:flex-none">
                          {board.title}
                          {board.keyPrefix ? (
                            <>
                              <span className="hidden max-sm:inline"> · </span>{" "}
                              <span className="font-mono text-xs text-meta-foreground max-sm:text-[15px] max-sm:text-inherit">
                                {board.keyPrefix}
                              </span>
                            </>
                          ) : null}
                        </span>
                        {board.space ? (
                          <span className="ml-auto text-xs font-[inherit] text-meta-foreground max-sm:hidden">
                            {board.space}
                          </span>
                        ) : null}
                        {on ? (
                          <Check
                            aria-hidden="true"
                            className="ml-auto size-4 shrink-0 text-brand-subtle-foreground max-sm:text-brand"
                          />
                        ) : null}
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </>
        ) : null}
        {showPreview && preview ? (
          <>
            <ul className="m-0 flex list-none flex-col rounded-[10px] border border-line p-0 max-sm:hidden">
              <ImpactLine
                icon={<CornerDownRight aria-hidden="true" className="size-4" />}
                lead={
                  preview.group.matched
                    ? fillLifecycleCopy(copy.movePreviewGroup, {
                        title: preview.group.title ?? "",
                      })
                    : copy.movePreviewGroupFallback
                }
                note={
                  preview.group.matched
                    ? copy.movePreviewGroupMatched
                    : copy.movePreviewGroupFallbackNote
                }
              />
              <ImpactLine
                icon={<ListChecks aria-hidden="true" className="size-4" />}
                lead={fillLifecycleCopy(
                  preview.status.matched
                    ? copy.movePreviewStatus
                    : copy.movePreviewStatusFallback,
                  { name: preview.status.name ?? "" },
                )}
                note={
                  preview.status.matched
                    ? copy.movePreviewStatusMatched
                    : copy.movePreviewStatusFallbackNote
                }
              />
              <ImpactLine
                icon={<Hash aria-hidden="true" className="size-4" />}
                lead={copy.movePreviewKey}
                note={fillLifecycleCopy(copy.movePreviewKeyBody, {
                  key: itemKey ?? title,
                })}
              />
              {mustAck ? (
                <ImpactLine
                  warn
                  icon={<TriangleAlert aria-hidden="true" className="size-4" />}
                  lead={fillLifecycleCopy(copy.moveLoses, {
                    count: losses.length,
                  })}
                  note={`${losses.join(", ")}.`}
                />
              ) : null}
            </ul>
            <div className="hidden flex-col gap-2 border-t border-line px-5 pt-1.5 pb-2 text-[13.5px] max-sm:mt-1.5 max-sm:flex">
              <div>
                <b>
                  {preview.group.matched
                    ? fillLifecycleCopy(copy.movePreviewGroup, {
                        title: preview.group.title ?? "",
                      })
                    : copy.movePreviewGroupFallback}
                </b>
                ,{" "}
                {fillLifecycleCopy(
                  preview.status.matched
                    ? copy.movePhoneStatusKept
                    : copy.movePhoneStatusFallback,
                  { name: preview.status.name ?? "" },
                )}
                , {copy.movePhoneKey}.
              </div>
              {mustAck ? (
                <ActionBanner tone="warn">
                  <b>
                    {fillLifecycleCopy(copy.moveLoses, {
                      count: losses.length,
                    })}
                  </b>
                  : {losses.join(", ")}.
                </ActionBanner>
              ) : null}
            </div>
          </>
        ) : null}
        {showPreview && mustAck ? (
          <label className="flex cursor-pointer items-center gap-2.5 text-[13px] leading-[1.45] max-sm:px-5 max-sm:pb-2 max-sm:text-[13.5px]">
            <Checkbox
              checked={acked}
              disabled={busy}
              onCheckedChange={(next) => setAcked(next === true)}
            />
            {fillLifecycleCopy(copy.moveAck, { count: losses.length })}
          </label>
        ) : null}
        {refusal && !busy ? (
          <ActionBanner tone="err" className="max-sm:mx-4 max-sm:mb-2">
            {refusal}
          </ActionBanner>
        ) : null}
      </div>
    </ActionDialog>
  );
}
