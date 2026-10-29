import { useId, useState, type FormEvent } from "react";
import {
  Bookmark,
  BookmarkPlus,
  Check,
  ChevronDown,
  Lock,
  Pencil,
  Trash2,
  Users,
  X,
} from "lucide-react";
import {
  Button,
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@notrelix/ui-web";
import type { SavedFilter } from "@notrelix/work-management-core";
import { kanbanPillClass } from "../views/kanban/kanban-chrome";
import {
  fillSavedFiltersCopy,
  type SavedFiltersCopy,
} from "./saved-filters.copy";

/** How a write the host sent ended; the host's result is assignable to it. */
export type SavedFilterWriteResult =
  | { readonly ok: true }
  | { readonly ok: false; readonly reason: "forbidden" | "missing" | "failed" };

export type SavedFiltersStatus = "loading" | "error" | "ready";

export interface SavedFiltersMenuProps {
  copy: SavedFiltersCopy;
  status: SavedFiltersStatus;
  filters: readonly SavedFilter[];
  /** The saved filter the bar currently shows, if any. */
  appliedId: string | null;
  /** The viewer may save and change filters (view-edit right). Shapes the controls only; the server decides. */
  canEdit: boolean;
  /** The bar has a filter or grouping worth saving. */
  canSaveCurrent: boolean;
  onApply: (filter: SavedFilter) => void;
  onClearApplied: () => void;
  onRetry: () => void;
  onSave: (input: {
    name: string;
    visibility: "board" | "me";
  }) => Promise<SavedFilterWriteResult>;
  onRename: (
    filter: SavedFilter,
    name: string,
  ) => Promise<SavedFilterWriteResult>;
  onSetVisibility: (filter: SavedFilter, visibility: "board" | "me") => void;
  onRemove: (filter: SavedFilter) => void;
  /** The "Lưu bộ lọc" form is controlled by the host so the removed-field warning can open it. */
  /** Only the icon until a filter is applied (a crowded bar, like the Calendar's); the name stays for assistive technology. */
  compact?: boolean;
  saveOpen?: boolean;
  onSaveOpenChange?: (open: boolean) => void;
}

const iconButtonClass =
  "grid size-7 shrink-0 cursor-pointer place-items-center rounded-md border-0 bg-transparent text-muted-foreground hover:bg-state-hover-strong hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring";

/**
 * The filter bar's saved-filter controls (pure surface): "Lưu bộ lọc" (a form
 * that grows under the button) and the "Bộ lọc đã lưu" list with apply,
 * rename, share and delete. Personal filters come first, then the shared ones.
 */
export function SavedFiltersMenu(props: SavedFiltersMenuProps) {
  const { copy, canEdit, canSaveCurrent } = props;
  return (
    <>
      {canEdit && canSaveCurrent ? (
        <SaveFilterPopover
          copy={copy}
          onSave={props.onSave}
          open={props.saveOpen}
          onOpenChange={props.onSaveOpenChange}
        />
      ) : null}
      <SavedFiltersList {...props} copy={copy} />
    </>
  );
}

function SaveFilterPopover({
  copy,
  onSave,
  open: controlledOpen,
  onOpenChange,
}: Pick<SavedFiltersMenuProps, "copy" | "onSave"> & {
  open?: boolean | undefined;
  onOpenChange?: ((open: boolean) => void) | undefined;
}) {
  const t = copy.save;
  const [innerOpen, setInnerOpen] = useState(false);
  const open = controlledOpen ?? innerOpen;
  const setOpen = onOpenChange ?? setInnerOpen;
  const [name, setName] = useState("");
  const [visibility, setVisibility] = useState<"board" | "me">("me");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const errorId = useId();
  const groupName = useId();

  const reset = () => {
    setName("");
    setVisibility("me");
    setError(null);
    setBusy(false);
  };

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (busy) return;
    const trimmed = name.trim();
    if (trimmed === "") {
      setError(t.nameRequired);
      return;
    }
    setBusy(true);
    setError(null);
    const result = await onSave({ name: trimmed, visibility });
    if (result.ok) {
      setOpen(false);
      reset();
      return;
    }
    setBusy(false);
    setError(result.reason === "forbidden" ? t.forbidden : t.failed);
  }

  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (!next) reset();
      }}
    >
      <PopoverTrigger asChild>
        <Button variant="ghost" size="sm" className={kanbanPillClass}>
          <BookmarkPlus className="size-4" aria-hidden="true" />
          {t.trigger}
        </Button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-80 p-3">
        <form
          onSubmit={(event) => void submit(event)}
          aria-label={t.title}
          className="flex flex-col gap-3"
        >
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor={`${groupName}-name`}
              className="text-xs font-semibold text-muted-foreground"
            >
              {t.name}
            </label>
            <input
              id={`${groupName}-name`}
              type="text"
              autoFocus
              value={name}
              placeholder={t.namePlaceholder}
              maxLength={200}
              aria-invalid={error ? true : undefined}
              aria-describedby={error ? errorId : undefined}
              onChange={(event) => {
                setName(event.target.value);
                setError(null);
              }}
              className="box-border h-8 w-full rounded-md border border-line-strong bg-popover px-2 text-sm outline-none placeholder:text-meta-foreground focus-visible:ring-2 focus-visible:ring-focus-ring aria-invalid:border-danger"
            />
          </div>
          <fieldset className="m-0 flex flex-col gap-2 border-0 p-0">
            <legend className="mb-1 p-0 text-xs font-semibold text-muted-foreground">
              {t.visibilityLabel}
            </legend>
            {(
              [
                ["me", t.personal, t.personalHint],
                ["board", t.shared, t.sharedHint],
              ] as const
            ).map(([value, label, hint]) => (
              <label key={value} className="flex items-start gap-2 text-sm">
                <input
                  type="radio"
                  name={`${groupName}-visibility`}
                  value={value}
                  checked={visibility === value}
                  onChange={() => setVisibility(value)}
                  className="mt-1 accent-[var(--brand)]"
                />
                <span className="flex flex-col">
                  <span className="font-semibold">{label}</span>
                  <span className="text-xs text-muted-foreground">{hint}</span>
                </span>
              </label>
            ))}
          </fieldset>
          {error ? (
            <p
              id={errorId}
              role="alert"
              className="m-0 text-xs text-danger-text"
            >
              {error}
            </p>
          ) : null}
          <div className="flex justify-end gap-2">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => setOpen(false)}
            >
              {t.cancel}
            </Button>
            <Button type="submit" size="sm" disabled={busy}>
              {busy
                ? t.saving
                : error && error !== t.nameRequired
                  ? t.retry
                  : t.submit}
            </Button>
          </div>
        </form>
      </PopoverContent>
    </Popover>
  );
}

function SavedFiltersList({
  copy,
  status,
  filters,
  appliedId,
  canEdit,
  onApply,
  onClearApplied,
  onRetry,
  onRename,
  onSetVisibility,
  onRemove,
  compact,
}: SavedFiltersMenuProps) {
  const t = copy.menu;
  const [open, setOpen] = useState(false);
  const applied = filters.find((filter) => filter.id === appliedId);
  const mine = filters.filter((filter) => filter.visibility === "me");
  const shared = filters.filter((filter) => filter.visibility === "board");
  const section = (heading: string, rows: readonly SavedFilter[]) =>
    rows.length === 0 ? null : (
      <section aria-label={heading} className="flex flex-col gap-0.5">
        <h3 className="m-0 px-2 pt-2 pb-1 text-xs font-semibold text-muted-foreground">
          {heading}
        </h3>
        <ul className="m-0 flex list-none flex-col gap-0.5 p-0">
          {rows.map((filter) => (
            <SavedFilterRow
              key={filter.id}
              copy={copy}
              filter={filter}
              applied={filter.id === appliedId}
              canEdit={canEdit}
              onApply={(filter) => {
                setOpen(false);
                onApply(filter);
              }}
              onRename={onRename}
              onSetVisibility={onSetVisibility}
              onRemove={onRemove}
            />
          ))}
        </ul>
      </section>
    );

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className={kanbanPillClass}
          aria-haspopup="dialog"
        >
          <Bookmark className="size-4" aria-hidden="true" />
          {applied ? (
            <b className="max-w-40 truncate font-semibold text-foreground">
              {applied.name}
            </b>
          ) : (
            <span className={compact ? "sr-only" : undefined}>{t.trigger}</span>
          )}
          <ChevronDown className="size-3.5" aria-hidden="true" />
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        aria-label={t.label}
        className="w-96 max-w-[calc(100vw-2rem)] p-1.5"
      >
        {status === "loading" ? (
          <div
            role="status"
            aria-label={t.loading}
            className="flex flex-col gap-1.5 p-1"
          >
            <div className="h-10 rounded-md bg-state-hover-strong" />
            <div className="h-10 rounded-md bg-state-hover-strong" />
          </div>
        ) : status === "error" ? (
          <div role="alert" className="flex flex-col items-start gap-2 p-2.5">
            <p className="m-0 text-sm text-danger-text">{t.loadFailed}</p>
            <Button type="button" variant="outline" size="sm" onClick={onRetry}>
              {t.retry}
            </Button>
          </div>
        ) : (
          <>
            {filters.length === 0 ? (
              <div className="px-3 py-4 text-center">
                <b className="block text-sm font-semibold">{t.empty}</b>
                <span className="text-xs text-muted-foreground">
                  {t.emptyHint}
                </span>
              </div>
            ) : (
              <>
                {section(t.mine, mine)}
                {section(t.shared, shared)}
              </>
            )}
            {!canEdit ? (
              <p className="m-0 px-2.5 pt-1.5 pb-2 text-xs text-muted-foreground">
                {t.readOnly}
              </p>
            ) : null}
            {applied ? (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="mt-1 w-full justify-start"
                onClick={() => {
                  setOpen(false);
                  onClearApplied();
                }}
              >
                <X className="size-4" aria-hidden="true" />
                {t.clear}
              </Button>
            ) : null}
          </>
        )}
      </PopoverContent>
    </Popover>
  );
}

function SavedFilterRow({
  copy,
  filter,
  applied,
  canEdit,
  onApply,
  onRename,
  onSetVisibility,
  onRemove,
}: Pick<
  SavedFiltersMenuProps,
  "copy" | "canEdit" | "onApply" | "onRename" | "onSetVisibility" | "onRemove"
> & { filter: SavedFilter; applied: boolean }) {
  const t = copy.row;
  const [renaming, setRenaming] = useState(false);
  const [draft, setDraft] = useState(filter.name);
  const [error, setError] = useState(false);
  const errorId = useId();
  const shared = filter.visibility === "board";
  const name = { name: filter.name };

  async function commitRename() {
    const next = draft.trim();
    if (next === "" || next === filter.name) {
      setRenaming(false);
      return;
    }
    const result = await onRename(filter, next);
    if (result.ok) setRenaming(false);
    else setError(true);
  }

  if (renaming) {
    return (
      <li className="rounded-md bg-state-selected p-1.5">
        <div className="flex items-center gap-1.5">
          <input
            type="text"
            autoFocus
            value={draft}
            maxLength={200}
            aria-label={t.renameLabel}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? errorId : undefined}
            onChange={(event) => {
              setDraft(event.target.value);
              setError(false);
            }}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                event.preventDefault();
                void commitRename();
              } else if (event.key === "Escape") {
                event.stopPropagation();
                setRenaming(false);
              }
            }}
            className="box-border h-8 min-w-0 flex-1 rounded-md border border-line-strong bg-popover px-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-invalid:border-danger"
          />
          <Button type="button" size="sm" onClick={() => void commitRename()}>
            {t.renameSave}
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => setRenaming(false)}
          >
            {t.renameCancel}
          </Button>
        </div>
        {error ? (
          <p
            id={errorId}
            role="alert"
            className="m-0 mt-1 text-xs text-danger-text"
          >
            {fillSavedFiltersCopy(copy.toast.failed, name)}
          </p>
        ) : null}
      </li>
    );
  }

  return (
    <li
      className={
        applied
          ? "flex items-center gap-1 rounded-md bg-state-selected pr-1"
          : "flex items-center gap-1 rounded-md pr-1 hover:bg-state-hover"
      }
    >
      <button
        type="button"
        aria-label={fillSavedFiltersCopy(t.apply, name)}
        aria-current={applied ? "true" : undefined}
        onClick={() => onApply(filter)}
        className="flex min-h-10 min-w-0 flex-1 cursor-pointer items-center gap-2 rounded-md border-0 bg-transparent px-2 text-left text-sm focus-visible:outline-2 focus-visible:outline-ring"
      >
        <span className="grid size-4 shrink-0 place-items-center text-brand-subtle-foreground">
          {applied ? (
            <Check className="size-4" aria-hidden="true" />
          ) : shared ? (
            <Users
              className="size-4 text-muted-foreground"
              aria-hidden="true"
            />
          ) : (
            <Lock className="size-4 text-muted-foreground" aria-hidden="true" />
          )}
        </span>
        <span className="min-w-0 flex-1 truncate font-semibold">
          {filter.name}
        </span>
        <span className="shrink-0 rounded-md bg-state-hover-strong px-1.5 py-0.5 text-xs font-semibold text-muted-foreground">
          {shared ? copy.menu.badgeShared : copy.menu.badgeMine}
        </span>
      </button>
      {canEdit ? (
        <span className="flex shrink-0 items-center">
          <button
            type="button"
            className={iconButtonClass}
            aria-label={fillSavedFiltersCopy(t.rename, name)}
            onClick={() => {
              setDraft(filter.name);
              setError(false);
              setRenaming(true);
            }}
          >
            <Pencil className="size-3.5" aria-hidden="true" />
          </button>
          {filter.isMine ? (
            <button
              type="button"
              className={iconButtonClass}
              aria-label={fillSavedFiltersCopy(
                shared ? t.unshare : t.share,
                name,
              )}
              onClick={() => onSetVisibility(filter, shared ? "me" : "board")}
            >
              {shared ? (
                <Lock className="size-3.5" aria-hidden="true" />
              ) : (
                <Users className="size-3.5" aria-hidden="true" />
              )}
            </button>
          ) : null}
          <button
            type="button"
            className={iconButtonClass}
            aria-label={fillSavedFiltersCopy(t.remove, name)}
            onClick={() => onRemove(filter)}
          >
            <Trash2 className="size-3.5" aria-hidden="true" />
          </button>
        </span>
      ) : null}
    </li>
  );
}
