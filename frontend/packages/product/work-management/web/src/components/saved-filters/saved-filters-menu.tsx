import { useId, useState, type FormEvent } from "react";
import {
  Bookmark,
  BookmarkPlus,
  Check,
  ChevronDown,
  CircleAlert,
  Lock,
  Pencil,
  RotateCw,
  Trash2,
  Users,
  X,
} from "lucide-react";
import {
  Button,
  cn,
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
  | {
      readonly ok: false;
      readonly reason: "forbidden" | "missing" | "conflict" | "failed";
      readonly by?: string | null;
    };

/** One phrase of a filter's summary; the grouping reads apart from the conditions. */
export interface SummaryPhrase {
  readonly text: string;
  readonly group?: boolean;
  /** The condition is on a field that was removed: it is ignored, and the row says so. */
  readonly stale?: boolean;
}

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
  /** A write found the filter gone (or no longer visible): the menu says so above the refreshed list. */
  gone?: boolean;
  /** The menu closed: the notice has been seen. */
  onGoneSeen?: () => void;
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
  /** A phone: the list trigger is a 36 px icon and the save trigger only anchors the form. */
  phone?: boolean;
  saveOpen?: boolean;
  onSaveOpenChange?: (open: boolean) => void;
  /** What the bar shows now, one phrase per condition and the grouping: the save form's summary chips. */
  summary?: readonly SummaryPhrase[];
  /** The phrases of a saved filter's criteria: the subtitle of its row. */
  describe?: (criteria: SavedFilter["criteria"]) => readonly SummaryPhrase[];
}

const iconButtonClass =
  "grid size-7.5 shrink-0 cursor-pointer place-items-center rounded-md border-0 bg-transparent text-muted-foreground hover:bg-state-hover-strong hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring";

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
          summary={props.summary ?? []}
          onSave={props.onSave}
          open={props.saveOpen}
          onOpenChange={props.onSaveOpenChange}
          compact={props.phone}
        />
      ) : null}
      <SavedFiltersList {...props} copy={copy} />
    </>
  );
}

function SaveFilterPopover({
  copy,
  summary,
  onSave,
  open: controlledOpen,
  onOpenChange,
  compact,
}: Pick<SavedFiltersMenuProps, "copy" | "onSave"> & {
  /** A phone: "Lưu bộ lọc" is in the filter sheet; the trigger here only anchors the form. */
  compact?: boolean | undefined;
  summary: readonly SummaryPhrase[];
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
        <Button
          variant="ghost"
          size="sm"
          className={cn(kanbanPillClass, compact && "sr-only")}
          {...(compact ? { tabIndex: -1, "aria-hidden": true } : {})}
        >
          <BookmarkPlus className="size-4" aria-hidden="true" />
          {t.trigger}
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        className="w-117.5 max-w-[calc(100vw-2rem)] rounded-xl border border-brand p-3.5 leading-[1.45] shadow-[0_0_0_3px_var(--state-selected)]"
      >
        <form
          onSubmit={(event) => void submit(event)}
          aria-label={t.title}
          className="flex flex-col gap-3"
        >
          <p className="m-0 text-xs leading-[1.45] font-semibold text-meta-foreground">
            {t.title}
          </p>
          {summary.length > 0 ? (
            <ul
              aria-label={t.summaryLabel}
              className="m-0 flex list-none flex-wrap gap-1.5 p-0"
            >
              {summary.map((phrase, index) => (
                <li
                  key={`${phrase.text}-${index}`}
                  className={
                    phrase.group
                      ? "inline-flex h-5 items-center rounded-md bg-state-hover-strong px-1.75 text-[11.5px] font-semibold text-muted-foreground"
                      : "inline-flex h-5 items-center rounded-md bg-state-selected px-1.75 text-[11.5px] font-semibold text-state-selected-foreground"
                  }
                >
                  {phrase.text}
                </li>
              ))}
            </ul>
          ) : null}
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor={`${groupName}-name`}
              className="text-[12.5px] font-semibold text-muted-foreground"
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
              className="box-border h-8.5 w-full rounded-[7px] border border-line-strong bg-popover px-2.5 text-[13.5px] outline-none placeholder:text-meta-foreground focus-visible:border-brand focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-brand aria-invalid:border-danger"
            />
          </div>
          <fieldset className="m-0 flex flex-col gap-2 border-0 p-0">
            <legend className="sr-only">{t.visibilityLabel}</legend>
            {(
              [
                ["me", t.personal, t.personalHint],
                ["board", t.shared, t.sharedHint],
              ] as const
            ).map(([value, label, hint]) => (
              <label key={value} className="flex items-start gap-2.5 text-sm">
                <input
                  type="radio"
                  name={`${groupName}-visibility`}
                  value={value}
                  checked={visibility === value}
                  onChange={() => setVisibility(value)}
                  className="mt-0.5 box-border size-4 shrink-0 cursor-pointer appearance-none rounded-full border-[1.5px] border-line-strong checked:border-brand checked:bg-[radial-gradient(circle,var(--brand)_0_4px,transparent_4.5px)] focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-brand"
                />
                <span className="flex flex-col">
                  <span className="text-[13.5px] font-semibold">{label}</span>
                  <span className="text-[12.5px] text-meta-foreground">
                    {hint}
                  </span>
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
  phone,
  describe,
  gone,
  onGoneSeen,
}: SavedFiltersMenuProps) {
  const t = copy.menu;
  const [open, setOpenInner] = useState(false);
  const setOpen = (next: boolean) => {
    setOpenInner(next);
    if (!next) onGoneSeen?.();
  };
  const applied = filters.find((filter) => filter.id === appliedId);
  const mine = filters.filter((filter) => filter.visibility === "me");
  const shared = filters.filter((filter) => filter.visibility === "board");
  const section = (heading: string, rows: readonly SavedFilter[]) =>
    rows.length === 0 ? null : (
      <section aria-label={heading} className="flex flex-col gap-0.5">
        <h3 className="m-0 px-2.5 pt-2 pb-1 text-xs leading-[1.45] font-semibold text-meta-foreground">
          {heading}
        </h3>
        <ul className="m-0 flex list-none flex-col gap-0.5 p-0">
          {rows.map((filter) => (
            <SavedFilterRow
              key={filter.id}
              copy={copy}
              filter={filter}
              stale={
                describe
                  ? describe(filter.criteria).filter((phrase) => phrase.stale)
                      .length
                  : 0
              }
              subtitle={[
                // A filter shared by a colleague says who made it ("Hà Nguyễn · …").
                ...(filter.isMine || !filter.createdByName
                  ? []
                  : [filter.createdByName]),
                ...(describe
                  ? describe(filter.criteria)
                      .filter((phrase) => !phrase.stale)
                      .map((phrase) => phrase.text)
                  : []),
              ].join(" · ")}
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
          className={cn(
            kanbanPillClass,
            phone && "h-9 min-w-9 justify-center px-2",
          )}
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
          {compact && !applied ? null : (
            <ChevronDown className="size-3.5" aria-hidden="true" />
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        aria-label={t.label}
        className="w-95 max-w-[calc(100vw-2rem)] gap-0.5 rounded-xl border border-line p-1.5 leading-[1.45]"
      >
        {status === "loading" ? (
          <div
            role="status"
            aria-label={t.loading}
            className="flex flex-col gap-0.5"
          >
            <div className="h-10 rounded-md bg-state-hover-strong" />
            <div className="h-10 rounded-md bg-state-hover-strong" />
          </div>
        ) : status === "error" ? (
          <div role="alert" className="flex flex-col items-start gap-0.5 pb-2">
            <div className="flex items-start gap-2 p-2.5 text-[12.5px] text-danger-text">
              <CircleAlert
                aria-hidden="true"
                className="mt-px size-4 shrink-0"
              />
              <p className="m-0 flex-1">{t.loadFailed}</p>
            </div>
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="mx-2.5 h-7 gap-2 text-[12.5px]"
              onClick={onRetry}
            >
              <RotateCw aria-hidden="true" className="size-3.5" />
              {t.retry}
            </Button>
          </div>
        ) : (
          <>
            {gone ? (
              <div
                role="alert"
                className="flex items-start gap-2 p-2.5 text-[12.5px] text-danger-text"
              >
                <CircleAlert
                  aria-hidden="true"
                  className="mt-px size-4 shrink-0"
                />
                <p className="m-0 flex-1">{t.gone}</p>
              </div>
            ) : null}
            {filters.length === 0 && gone ? null : filters.length === 0 ? (
              <div className="px-3.5 py-[18px] text-center">
                <b className="block text-[13.5px] font-semibold">{t.empty}</b>
                <span className="text-[12.5px] text-muted-foreground">
                  {t.emptyHint}
                </span>
              </div>
            ) : (
              <>
                {section(t.mine, mine)}
                {mine.length > 0 && shared.length > 0 ? (
                  <div role="separator" className="my-1 h-px bg-line" />
                ) : null}
                {section(t.shared, shared)}
              </>
            )}
            {!canEdit ? (
              <p className="m-0 px-2.5 pt-2 pb-2.5 text-[12.5px] leading-[1.45] text-meta-foreground">
                {t.readOnly}
              </p>
            ) : null}
            {applied ? (
              <div role="separator" className="my-1 h-px bg-line" />
            ) : null}
            {applied ? (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="h-8.5 w-full justify-start px-3 text-[13px] font-medium text-muted-foreground"
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
  subtitle,
  stale,
  applied,
  canEdit,
  onApply,
  onRename,
  onSetVisibility,
  onRemove,
}: Pick<
  SavedFiltersMenuProps,
  "copy" | "canEdit" | "onApply" | "onRename" | "onSetVisibility" | "onRemove"
> & {
  filter: SavedFilter;
  subtitle: string;
  /** Conditions on a removed field: they are ignored when the filter is applied. */
  stale: number;
  applied: boolean;
}) {
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
      <li className="rounded-lg bg-state-selected py-2.5 pr-2 pl-2.5">
        <div className="flex items-center gap-2.5">
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
            className="box-border h-8 min-w-0 flex-1 rounded-md border border-line-strong bg-popover px-2.5 text-[13.5px] outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-invalid:border-danger"
          />
          <span className="flex items-center gap-0.5">
            <Button
              type="button"
              size="sm"
              className="h-7 px-2.5 text-[12.5px]"
              onClick={() => void commitRename()}
            >
              {t.renameSave}
            </Button>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="h-7 px-2.5 text-[12.5px]"
              onClick={() => setRenaming(false)}
            >
              {t.renameCancel}
            </Button>
          </span>
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
          ? "flex items-center rounded-lg bg-state-selected pr-2"
          : "flex items-center rounded-lg pr-2 hover:bg-state-hover"
      }
    >
      <button
        type="button"
        aria-label={fillSavedFiltersCopy(t.apply, name)}
        aria-current={applied ? "true" : undefined}
        onClick={() => onApply(filter)}
        className={`flex ${subtitle || stale > 0 ? "min-h-13" : "min-h-11"} min-w-0 flex-1 cursor-pointer ${stale > 0 ? "items-start py-2" : "items-center py-1"} gap-2.5 rounded-lg border-0 bg-transparent pr-2.5 pl-2.5 text-left text-sm focus-visible:outline-2 focus-visible:outline-ring`}
      >
        <span
          className={cn(
            "grid h-4 w-4.5 shrink-0 place-items-center text-brand-subtle-foreground",
            stale > 0 && "mt-0.5",
          )}
        >
          {stale > 0 ? (
            <CircleAlert
              className="size-4 text-warning-text"
              aria-hidden="true"
            />
          ) : applied ? (
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
        <span className="min-w-0 flex-1">
          <b className="block truncate text-[13.5px] font-semibold">
            {filter.name}
          </b>
          {stale > 0 ? (
            <small className="block text-xs leading-[1.45] text-warning-text">
              {t.staleHint}
            </small>
          ) : null}
          {subtitle ? (
            <small className="block truncate text-xs leading-[1.45] text-meta-foreground">
              {subtitle}
            </small>
          ) : null}
        </span>
        {stale > 0 ? (
          <small className="shrink-0 text-xs leading-[1.45] whitespace-nowrap text-meta-foreground">
            {fillSavedFiltersCopy(t.staleCount, { count: stale })}
          </small>
        ) : null}
        {canEdit ? null : (
          <span
            className={cn(
              "inline-flex h-5 shrink-0 items-center rounded-md px-1.75 text-[11.5px] font-semibold whitespace-nowrap",
              shared
                ? "bg-state-selected text-state-selected-foreground"
                : "bg-state-hover-strong text-muted-foreground",
            )}
          >
            {shared ? copy.menu.badgeShared : copy.menu.badgeMine}
          </span>
        )}
      </button>
      {canEdit ? (
        <span className="flex shrink-0 items-center gap-0.5">
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
