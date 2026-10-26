import { useId, useRef, useState, type ReactNode } from "react";
import { X } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  cn,
} from "@notrelix/ui-web";
import { foldText } from "./doc-popups-surface";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import type {
  DocConvertView,
  DocPageCallbacks,
  DocPickerBoardView,
  DocPickerPageView,
  DocTrashView,
} from "./doc-page-models";
import { useModalFocus } from "./doc-share-dialog-surface";

const OUTLINE =
  "inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-line-strong bg-doc-panel px-3 text-compact leading-[1.45] font-medium text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-default disabled:opacity-60";

const PRIMARY = cn(
  OUTLINE,
  "border-primary bg-brand-solid font-semibold text-brand-solid-foreground hover:bg-brand-solid",
);

const DANGER = cn(OUTLINE, "border-danger-text font-semibold text-danger-text");

const FIELD =
  "box-border h-9.5 w-full rounded-[9px] border border-line-strong bg-doc-panel px-2.5 text-compact leading-[1.45] text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring";

/** A modal frame: scrim, titled dialog and a close button; Escape and Tab stay inside. */
function DialogFrame({
  title,
  closeLabel,
  width = 520,
  onClose,
  children,
}: {
  title: string;
  closeLabel: string;
  width?: number;
  onClose?: () => void;
  children: ReactNode;
}) {
  const dialog = useRef<HTMLDivElement | null>(null);
  const titleId = useId();
  const onKeyDown = useModalFocus(dialog, onClose);
  return (
    <div
      className="fixed inset-0 z-modal flex items-start justify-center bg-modal-scrim pt-14"
      onKeyDown={onKeyDown}
    >
      <div
        ref={dialog}
        role="dialog"
        aria-modal="true"
        tabIndex={-1}
        aria-labelledby={titleId}
        style={{ width }}
        className="relative flex max-h-[calc(100vh-7rem)] max-w-[calc(100%-2rem)] flex-col rounded-[14px] bg-doc-panel shadow-dialog outline-none"
      >
        <div className="flex items-center gap-2.5 px-5 pt-[18px] pb-1">
          <h2 id={titleId} className="text-dialog-title">
            {title}
          </h2>
          <button
            type="button"
            aria-label={closeLabel}
            onClick={onClose}
            className="ml-auto grid size-8 shrink-0 place-items-center rounded-[7px] text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <X aria-hidden="true" className="size-4 stroke-[1.8]" />
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}

export interface DocTrashDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly trash: DocTrashView;
  readonly callbacks?: DocPageCallbacks;
}

/** The trash: pages moved there, each with Restore and Delete permanently. */
export function DocTrashDialogSurface({
  copy,
  trash,
  callbacks,
}: DocTrashDialogSurfaceProps) {
  const t = copy.trash;
  const [confirmId, setConfirmId] = useState<string | null>(null);
  return (
    <DialogFrame
      title={t.title}
      closeLabel={t.close}
      onClose={callbacks?.onCloseOverlay}
    >
      <p className="m-0 px-5 pb-2 text-crumb leading-[1.5] text-meta-foreground">
        {t.note}
      </p>
      {trash.failed ? (
        <p
          role="alert"
          className="m-0 px-5 pb-2 text-crumb font-semibold text-danger-text"
        >
          {t.failed}
        </p>
      ) : null}
      <div className="flex min-h-[120px] flex-col gap-1 overflow-auto px-5 pb-5">
        {trash.loading ? (
          <span role="status" className="text-compact text-meta-foreground">
            {t.loading}
          </span>
        ) : trash.pages.length === 0 ? (
          <span className="text-compact text-meta-foreground">{t.empty}</span>
        ) : (
          <ul className="m-0 flex list-none flex-col gap-1 p-0">
            {trash.pages.map((page) => {
              const name = page.title || t.untitled;
              const busy = trash.busyId === page.id;
              return (
                <li
                  key={page.id}
                  className="flex min-h-11 items-center gap-2.5 rounded-[9px] border border-border px-3 py-1.5"
                >
                  <span aria-hidden="true" className="w-5 text-center">
                    {page.icon ?? ""}
                  </span>
                  <span className="min-w-0 flex-1 truncate text-nav leading-[1.45] font-medium">
                    {name}
                  </span>
                  <button
                    type="button"
                    disabled={busy}
                    aria-label={fillDocCopy(t.restoreNamed, { title: name })}
                    onClick={() => callbacks?.onRestorePage?.(page.id)}
                    className={OUTLINE}
                  >
                    {t.restore}
                  </button>
                  {confirmId === page.id ? (
                    <>
                      <button
                        type="button"
                        disabled={busy}
                        onClick={() => {
                          setConfirmId(null);
                          callbacks?.onDeletePage?.(page.id);
                        }}
                        className={DANGER}
                      >
                        {t.confirmDelete}
                      </button>
                      <button
                        type="button"
                        onClick={() => setConfirmId(null)}
                        className={OUTLINE}
                      >
                        {t.cancel}
                      </button>
                    </>
                  ) : (
                    <button
                      type="button"
                      disabled={busy}
                      aria-label={fillDocCopy(t.deleteNamed, { title: name })}
                      onClick={() => setConfirmId(page.id)}
                      className={DANGER}
                    >
                      {t.deleteForever}
                    </button>
                  )}
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </DialogFrame>
  );
}

export interface DocPagePickerDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly blockId: string;
  readonly pages: readonly DocPickerPageView[];
  readonly callbacks?: DocPageCallbacks;
}

/** Chooses the page a page-reference block points to. */
export function DocPagePickerDialogSurface({
  copy,
  blockId,
  pages,
  callbacks,
}: DocPagePickerDialogSurfaceProps) {
  const p = copy.picker;
  const [query, setQuery] = useState("");
  const needle = foldText(query.trim());
  const matches = pages.filter(
    (page) => needle === "" || foldText(page.title).includes(needle),
  );
  return (
    <DialogFrame
      title={p.title}
      closeLabel={p.close}
      width={480}
      onClose={callbacks?.onCloseOverlay}
    >
      <div className="flex flex-col gap-2 px-5 pt-2 pb-5">
        <input
          type="search"
          value={query}
          aria-label={p.search}
          placeholder={p.search}
          onChange={(event) => setQuery(event.target.value)}
          className={FIELD}
        />
        <div
          role="listbox"
          aria-label={p.title}
          className="flex max-h-[320px] flex-col gap-0.5 overflow-auto"
        >
          {matches.length === 0 ? (
            <span className="px-1 py-2 text-compact text-meta-foreground">
              {p.none}
            </span>
          ) : (
            matches.map((page) => (
              <button
                key={page.id}
                type="button"
                role="option"
                aria-selected="false"
                onClick={() => callbacks?.onPickPage?.(blockId, page.id)}
                className="flex min-h-11 items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2 py-1 text-left text-nav leading-[1.4] text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring"
              >
                <span aria-hidden="true" className="w-5 text-center">
                  {page.icon ?? ""}
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block truncate font-medium">
                    {page.title || p.untitled}
                  </span>
                  {page.detail ? (
                    <small className="block text-metadata leading-[1.4] tracking-normal text-meta-foreground">
                      {page.detail}
                    </small>
                  ) : null}
                </span>
              </button>
            ))
          )}
        </div>
      </div>
    </DialogFrame>
  );
}

export interface DocBoardPickerDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly blockId: string;
  readonly boards: readonly DocPickerBoardView[];
  readonly callbacks?: DocPageCallbacks;
}

/** Chooses the board a board-reference block embeds. */
export function DocBoardPickerDialogSurface({
  copy,
  blockId,
  boards,
  callbacks,
}: DocBoardPickerDialogSurfaceProps) {
  const p = copy.boardPicker;
  const [query, setQuery] = useState("");
  const needle = foldText(query.trim());
  const matches = boards.filter(
    (board) => needle === "" || foldText(board.title).includes(needle),
  );
  return (
    <DialogFrame
      title={p.title}
      closeLabel={p.close}
      width={480}
      onClose={callbacks?.onCloseOverlay}
    >
      <div className="flex flex-col gap-2 px-5 pt-2 pb-5">
        <input
          type="search"
          value={query}
          aria-label={p.search}
          placeholder={p.search}
          onChange={(event) => setQuery(event.target.value)}
          className={FIELD}
        />
        <div
          role="listbox"
          aria-label={p.title}
          className="flex max-h-[320px] flex-col gap-0.5 overflow-auto"
        >
          {matches.length === 0 ? (
            <span className="px-1 py-2 text-compact text-meta-foreground">
              {p.none}
            </span>
          ) : (
            matches.map((board) => (
              <button
                key={board.id}
                type="button"
                role="option"
                aria-selected="false"
                onClick={() => callbacks?.onPickBoard?.(blockId, board.id)}
                className="flex min-h-11 items-center rounded-[8px] border-0 bg-transparent px-2 py-1 text-left text-nav leading-[1.4] text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring"
              >
                <span className="min-w-0 flex-1 truncate font-medium">
                  {board.title}
                </span>
              </button>
            ))
          )}
        </div>
      </div>
    </DialogFrame>
  );
}

export interface DocConvertTaskDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly blockId: string;
  readonly convert: DocConvertView;
  readonly callbacks?: DocPageCallbacks;
}

/** Picks the board and group a checklist line becomes a work item in. */
export function DocConvertTaskDialogSurface({
  copy,
  blockId,
  convert,
  callbacks,
}: DocConvertTaskDialogSurfaceProps) {
  const c = copy.convert;
  const [groupId, setGroupId] = useState("");
  const boardId = convert.boardId ?? "";
  const ready = boardId !== "" && groupId !== "" && !convert.busy;
  return (
    <DialogFrame
      title={c.title}
      closeLabel={c.close}
      width={460}
      onClose={callbacks?.onCloseOverlay}
    >
      <div className="flex flex-col gap-3 px-5 pt-2 pb-5">
        <p className="m-0 truncate text-crumb leading-[1.5] text-meta-foreground">
          {fillDocCopy(c.line, { text: convert.lineText })}
        </p>
        {convert.boards.length === 0 ? (
          <p className="m-0 text-compact text-meta-foreground">{c.noBoards}</p>
        ) : (
          <>
            <label className="flex flex-col gap-1 text-metadata font-semibold text-meta-foreground">
              {c.board}
              <Select
                value={boardId}
                onValueChange={(value) => {
                  setGroupId("");
                  callbacks?.onConvertBoard?.(value);
                }}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder={c.boardPlaceholder} />
                </SelectTrigger>
                <SelectContent>
                  {convert.boards.map((board) => (
                    <SelectItem key={board.id} value={board.id}>
                      {board.title}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </label>
            <label className="flex flex-col gap-1 text-metadata font-semibold text-meta-foreground">
              {c.group}
              <Select
                value={groupId}
                disabled={boardId === "" || convert.groups === null}
                onValueChange={setGroupId}
              >
                <SelectTrigger className="w-full">
                  <SelectValue
                    placeholder={
                      boardId !== "" && convert.groups === null
                        ? c.groupsLoading
                        : c.groupPlaceholder
                    }
                  />
                </SelectTrigger>
                <SelectContent>
                  {(convert.groups ?? []).map((group) => (
                    <SelectItem key={group.id} value={group.id}>
                      {group.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </label>
          </>
        )}
        {convert.failed ? (
          <p
            role="alert"
            className="m-0 text-crumb font-semibold text-danger-text"
          >
            {c.failed}
          </p>
        ) : null}
        <div className="flex items-center justify-end gap-2">
          <button
            type="button"
            onClick={callbacks?.onCloseOverlay}
            className={OUTLINE}
          >
            {c.cancel}
          </button>
          <button
            type="button"
            disabled={!ready}
            onClick={() =>
              callbacks?.onConvertConfirm?.(blockId, { boardId, groupId })
            }
            className={PRIMARY}
          >
            {convert.busy ? c.busy : c.confirm}
          </button>
        </div>
      </div>
    </DialogFrame>
  );
}
