import { useState } from "react";
import { Info, TriangleAlert } from "lucide-react";
import {
  Button,
  Checkbox,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  cn,
} from "@notrelix/ui-web";
import { fillLifecycleCopy } from "../item-lifecycle/item-lifecycle.copy";
import type { ItemActionsCopy } from "./item-actions.copy";

export interface MoveTargetBoard {
  readonly id: string;
  readonly title: string;
}

/** What moving to the chosen board will do, already worded for the person. */
export interface MovePreviewView {
  readonly boardTitle: string;
  readonly group: { readonly title: string | null; readonly matched: boolean };
  readonly status: { readonly name: string | null; readonly matched: boolean };
  /** Each thing that will be lost, e.g. "property Budget". */
  readonly losses: readonly string[];
}

export type MoveDialogStatus =
  "idle" | "busy" | "error" | "deps" | "archived" | "conflict";

const rowClass =
  "flex gap-3 border-t border-line px-3.5 py-2.5 text-compact first:border-t-0";

function PreviewRow({
  title,
  body,
  warn,
}: {
  title: string;
  body: string;
  warn?: boolean;
}) {
  const Icon = warn ? TriangleAlert : Info;
  return (
    <li className={rowClass}>
      <span
        aria-hidden="true"
        className={cn(
          "grid size-7 shrink-0 place-items-center rounded-[7px] border border-line bg-background text-muted-foreground",
          warn &&
            "border-transparent bg-warning-subtle text-warning-subtle-foreground",
        )}
      >
        <Icon className="size-4" />
      </span>
      <span className="min-w-0">
        <b className="block font-semibold">{title}</b>
        <small className="block text-metadata text-muted-foreground">
          {body}
        </small>
      </span>
    </li>
  );
}

/**
 * Picks the board an item moves to and shows what carries over before the
 * person confirms. Anything that will be lost is named and needs a tick; a
 * refusal (dependencies, archived) shows with its way out; once the request is
 * sent the dialog cannot be dismissed. The preview is provisional, the server
 * decides.
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
  const busy = status === "busy";
  const needle = query.trim().toLowerCase();
  const shown = boards.filter((board) =>
    board.title.toLowerCase().includes(needle),
  );
  const losses = preview?.losses ?? [];
  const mustAck = losses.length > 0;
  const reset = () => {
    setQuery("");
    setChosen(null);
    setAcked(false);
  };
  const close = () => {
    if (busy) return;
    reset();
    onCancel();
  };
  const refusal =
    status === "deps"
      ? copy.moveRefusedDeps
      : status === "archived"
        ? copy.moveRefusedArchived
        : status === "error"
          ? copy.moveFailed
          : status === "conflict"
            ? copy.moveConflict
            : null;
  const canConfirm =
    chosen !== null &&
    !busy &&
    status !== "deps" &&
    status !== "archived" &&
    (!mustAck || acked);
  return (
    <Dialog open={open} onOpenChange={(next) => (next ? null : close())}>
      <DialogContent
        className="sm:max-w-[560px]"
        showCloseButton={!busy}
        onEscapeKeyDown={(event) => {
          if (busy) event.preventDefault();
        }}
        onInteractOutside={(event) => {
          if (busy) event.preventDefault();
        }}
      >
        <DialogHeader>
          <DialogTitle>
            {fillLifecycleCopy(copy.moveTitle, { title })}
          </DialogTitle>
          <DialogDescription>{copy.moveBody}</DialogDescription>
        </DialogHeader>
        {boards.length === 0 ? (
          <p className="text-compact text-muted-foreground">
            {copy.moveNoBoards}
          </p>
        ) : (
          <>
            <Input
              value={query}
              autoFocus
              onChange={(event) => setQuery(event.target.value)}
              placeholder={copy.moveSearch}
              aria-label={copy.moveSearch}
            />
            <ul
              role="listbox"
              aria-label={copy.moveSearch}
              className="max-h-44 overflow-y-auto rounded-[10px] border border-line py-1"
            >
              {shown.length === 0 ? (
                <li
                  role="presentation"
                  className="px-3 py-2 text-compact text-muted-foreground"
                >
                  {copy.moveNoMatch}
                </li>
              ) : null}
              {shown.map((board) => (
                <li key={board.id} role="presentation">
                  <button
                    type="button"
                    role="option"
                    aria-selected={chosen === board.id}
                    disabled={busy}
                    onClick={() => {
                      setChosen(board.id);
                      setAcked(false);
                      onPick(board.id);
                    }}
                    className={cn(
                      "mx-1 flex w-[calc(100%-0.5rem)] cursor-pointer items-center rounded-md px-2.5 py-2 text-left text-compact hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring",
                      chosen === board.id &&
                        "bg-brand-subtle font-semibold text-brand-subtle-foreground",
                    )}
                  >
                    {board.title}
                  </button>
                </li>
              ))}
            </ul>
            {!chosen ? (
              <p className="text-metadata text-muted-foreground">
                {copy.movePick}
              </p>
            ) : null}
          </>
        )}
        {chosen && preview && status !== "deps" && status !== "archived" ? (
          <ul
            aria-label={copy.moveTitle.replace("{{title}}", title)}
            className="rounded-[10px] border border-line"
          >
            <PreviewRow
              title={fillLifecycleCopy(copy.movePreviewGroup, {
                title: preview.group.title ?? "",
              })}
              body={
                preview.group.matched
                  ? copy.movePreviewGroupMatched
                  : copy.movePreviewGroupFallback
              }
            />
            <PreviewRow
              title={fillLifecycleCopy(copy.movePreviewStatus, {
                name: preview.status.name ?? "",
              })}
              body={
                preview.status.matched
                  ? copy.movePreviewStatusMatched
                  : copy.movePreviewStatusFallback
              }
            />
            <PreviewRow
              title={copy.movePreviewKey}
              body={fillLifecycleCopy(copy.movePreviewKeyBody, {
                key: itemKey ?? title,
              })}
            />
            {mustAck ? (
              <PreviewRow
                warn
                title={fillLifecycleCopy(copy.moveLoses, {
                  count: losses.length,
                })}
                body={losses.join(", ")}
              />
            ) : null}
          </ul>
        ) : null}
        {chosen &&
        preview &&
        mustAck &&
        status !== "deps" &&
        status !== "archived" ? (
          <label className="flex items-center gap-2.5 text-compact">
            <Checkbox
              checked={acked}
              disabled={busy}
              onCheckedChange={(next) => setAcked(next === true)}
            />
            {fillLifecycleCopy(copy.moveAck, { what: losses.join(", ") })}
          </label>
        ) : null}
        {refusal ? (
          <p
            role="alert"
            className="flex items-start gap-2 rounded-lg bg-destructive/10 px-2.5 py-2 text-compact text-destructive"
          >
            <TriangleAlert
              aria-hidden="true"
              className="mt-0.5 size-3.5 shrink-0"
            />
            <span>{refusal}</span>
          </p>
        ) : null}
        <DialogFooter>
          {!busy ? (
            <Button type="button" variant="ghost" onClick={close}>
              {copy.cancel}
            </Button>
          ) : null}
          {status === "deps" && blockingKey && onOpenBlocking ? (
            <Button type="button" variant="outline" onClick={onOpenBlocking}>
              {fillLifecycleCopy(copy.moveOpenBlocking, { key: blockingKey })}
            </Button>
          ) : null}
          {status === "archived" && onUnarchive ? (
            <Button type="button" onClick={onUnarchive}>
              {copy.unarchive}
            </Button>
          ) : null}
          {status !== "archived" ? (
            <Button
              type="button"
              disabled={!canConfirm}
              aria-disabled={!canConfirm}
              onClick={() => {
                if (!chosen) return;
                onConfirm(chosen);
              }}
            >
              {busy
                ? copy.moveBusy
                : status === "error" || status === "conflict"
                  ? copy.retry
                  : mustAck
                    ? fillLifecycleCopy(copy.moveConfirmLosing, {
                        count: losses.length,
                      })
                    : copy.moveConfirm}
            </Button>
          ) : null}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
