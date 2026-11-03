import * as React from "react";
import { ChevronDown, CircleX, Search } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import {
  highlightSegments,
  PALETTE_FILTERS,
  type PaletteFilter,
} from "../../core/model/command-palette";
import { initialsFor } from "../../core/model/navigation";
import { DepartmentMark } from "../home/work-row-parts";
import { formatCopy, IDENTITY_MARK_CLASS } from "../shell/shell-models";
import type { PaletteRow, PaletteView } from "./command-build";
import type { PaletteWorkspace } from "./command-models";
import type { WorkspaceCommandCopy } from "./workspace-command.copy";

const STATUS_DOT = {
  todo: "bg-work-status-todo",
  doing: "bg-work-status-doing",
  review: "bg-work-status-review",
  done: "bg-work-status-done",
} as const;

/** The title with the typed term marked (Final-M-Search `mark.hit`). */
function Marked({ text, term }: { text: string; term: string }) {
  return (
    <>
      {highlightSegments(text, term).map((segment, index) =>
        segment.hit ? (
          <mark
            key={index}
            data-f="hl"
            className="rounded-[3px] bg-brand-subtle px-px font-bold text-inherit"
          >
            {segment.text}
          </mark>
        ) : (
          <React.Fragment key={index}>{segment.text}</React.Fragment>
        ),
      )}
    </>
  );
}

function Tile({
  row,
  Icon,
}: {
  row: PaletteRow;
  Icon: React.ComponentType<{ className?: string; strokeWidth?: number }>;
}) {
  return (
    <span
      data-f="ic"
      aria-hidden="true"
      className="box-border grid size-9 shrink-0 place-items-center rounded-[10px] border border-line bg-surface-canvas text-muted-foreground"
    >
      {row.person ? (
        <span
          className={cn(
            "grid size-6 place-items-center rounded-full text-mark-xs font-bold",
            IDENTITY_MARK_CLASS[row.person.identity],
          )}
        >
          {initialsFor(row.person.name)}
        </span>
      ) : (
        <Icon className="size-5" strokeWidth={1.8} />
      )}
    </span>
  );
}

export interface PhoneSearchSheetProps {
  readonly copy: WorkspaceCommandCopy;
  readonly query: string;
  readonly term: string;
  readonly prefix: string;
  readonly onQueryChange: (typed: string) => void;
  readonly onInputKeyDown: (
    event: React.KeyboardEvent<HTMLInputElement>,
  ) => void;
  readonly inputRef: React.RefObject<HTMLInputElement | null>;
  readonly onCancel: () => void;
  readonly view: PaletteView;
  readonly filter: PaletteFilter;
  readonly onFilter: (filter: PaletteFilter) => void;
  readonly workspace: PaletteWorkspace;
  readonly listId: string;
  readonly listLabel: string;
  readonly listRef: React.RefObject<HTMLDivElement | null>;
  readonly activeId: string | null;
  readonly optionDomId: (id: string) => string;
  readonly onChoose: (row: PaletteRow) => void;
  readonly icons: Readonly<
    Record<
      string,
      React.ComponentType<{ className?: string; strokeWidth?: number }>
    >
  >;
  readonly emptyTitle: string;
}

/**
 * The palette on a phone (Final-M-Search): the query field with "Huỷ", the type chips, the result count with the
 * scope, then each kind of result as a card of rows. The shell's top bar, the tab bar and the keyboard are the
 * device's, so they are not drawn here. Same combobox and listbox contract as the dialog's.
 */
export function PhoneSearchSheet(props: PhoneSearchSheetProps) {
  const { copy, view, filter, term } = props;
  const t = copy.phone;
  const showChips = view.state === "results";
  const total = view.groups.reduce((sum, group) => sum + group.rows.length, 0);
  return (
    <div className="flex min-h-0 flex-1 flex-col bg-surface-canvas">
      <header className="relative z-10 flex flex-none flex-col gap-2.5 bg-surface-canvas px-4 pt-1 pb-2.5 shadow-[0_1px_0_var(--color-line)]">
        <div data-f="qrow" className="flex items-center gap-1 text-[15px]">
          <div
            data-f="q"
            className="box-border flex h-12 min-w-0 flex-1 items-center rounded-xl border border-meta-foreground bg-popover pl-3 text-meta-foreground focus-within:border-primary focus-within:shadow-[0_0_0_1px_var(--color-primary)] dark:bg-card"
          >
            <Search
              aria-hidden="true"
              className="size-5 shrink-0"
              strokeWidth={1.8}
            />
            <input
              ref={props.inputRef}
              type="text"
              role="combobox"
              enterKeyHint="search"
              aria-label={copy.placeholder}
              aria-expanded="true"
              aria-controls={props.listId}
              aria-autocomplete="list"
              aria-activedescendant={
                props.activeId ? props.optionDomId(props.activeId) : undefined
              }
              autoComplete="off"
              spellCheck={false}
              placeholder={t.placeholder}
              value={
                props.prefix ? props.query.trimStart().slice(1) : props.query
              }
              onChange={(event) => {
                const typed = event.target.value;
                props.onQueryChange(
                  props.prefix && !/^[>@#]/.test(typed)
                    ? `${props.prefix}${typed}`
                    : typed,
                );
              }}
              onKeyDown={props.onInputKeyDown}
              className="m-0 box-border h-full min-w-0 flex-1 appearance-none border-0 bg-transparent py-0 pr-1 pl-2 text-[16px] text-foreground caret-primary outline-none placeholder:text-meta-foreground"
            />
            {props.query ? (
              <button
                type="button"
                aria-label={t.clear}
                onClick={() => {
                  props.onQueryChange("");
                  props.inputRef.current?.focus();
                }}
                className="grid size-11 flex-none cursor-pointer place-items-center rounded-[10px] border-0 bg-transparent p-0 text-meta-foreground"
              >
                <CircleX
                  className="size-5"
                  strokeWidth={1.8}
                  aria-hidden="true"
                />
              </button>
            ) : null}
          </div>
          <button
            type="button"
            data-f="cancel"
            onClick={props.onCancel}
            className="h-11 flex-none cursor-pointer rounded-[10px] border-0 bg-transparent pr-1 pl-2.5 text-[16px] font-medium text-state-selected-foreground"
          >
            {t.cancel}
          </button>
        </div>
        {showChips ? (
          <div
            data-f="filters"
            role="tablist"
            aria-label={copy.filtersLabel}
            className="-mx-4 flex gap-2 overflow-x-auto px-4 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
          >
            {PALETTE_FILTERS.map((name) => {
              const count = view.counts[name];
              const selected = name === filter;
              return (
                <button
                  key={name}
                  type="button"
                  role="tab"
                  data-f="fl"
                  aria-selected={selected}
                  aria-controls={props.listId}
                  onClick={() => props.onFilter(name)}
                  className={cn(
                    "box-border inline-flex h-11 flex-none cursor-pointer items-center gap-1.5 rounded-xl border px-3.5 text-[14px] whitespace-nowrap",
                    selected
                      ? "border-foreground bg-foreground font-semibold text-popover dark:text-card"
                      : "border-line-strong bg-popover font-medium text-foreground dark:bg-card",
                  )}
                >
                  {t.filters[name]}
                  {count ? (
                    <span className="font-semibold tabular-nums">{count}</span>
                  ) : null}
                </button>
              );
            })}
          </div>
        ) : null}
      </header>

      <div
        ref={props.listRef}
        id={props.listId}
        role="listbox"
        aria-label={props.listLabel}
        className="flex min-h-0 flex-1 flex-col gap-4 overflow-auto px-4 pt-2.5 pb-6"
      >
        {showChips ? (
          <div
            data-f="rcount"
            className="-mt-1.5 -mb-2.5 flex min-h-11 items-center gap-2 text-[13px] text-muted-foreground"
          >
            <span>
              {formatCopy(total === 1 ? t.results_one : t.results_other, {
                count: total,
              })}
            </span>
            <button
              type="button"
              data-f="scope"
              aria-disabled="true"
              aria-label={`${copy.scope.label}: ${props.workspace.name}`}
              title={copy.scope.unavailable}
              className="-mr-1.5 ml-auto inline-flex h-11 cursor-not-allowed items-center gap-1.5 rounded-[10px] border-0 bg-transparent pr-1.5 pl-2 text-[13px] font-semibold text-foreground"
            >
              <DepartmentMark
                name={props.workspace.name}
                markTone={props.workspace.markTone}
                small
              />
              {props.workspace.name}
              <ChevronDown
                aria-hidden="true"
                className="size-3.5 text-meta-foreground"
                strokeWidth={1.8}
              />
            </button>
          </div>
        ) : null}
        {view.state === "none" ? (
          <p
            data-f="empty"
            className="m-0 py-6 text-center text-[13px] text-muted-foreground"
          >
            <b className="mb-1 block text-[15px] font-bold text-foreground">
              {props.emptyTitle}
            </b>
            {copy.empty.text}
          </p>
        ) : null}
        {view.groups.map((group) => (
          <section key={group.id} className="flex flex-col">
            {group.label ? (
              <h2 className="m-0 mb-1 flex min-h-10 items-center gap-2 text-[17px] font-[650] tracking-[-0.01em]">
                {group.label}
                {group.count !== null ? (
                  <span className="text-[15px] font-medium text-meta-foreground">
                    {group.count}
                  </span>
                ) : null}
              </h2>
            ) : null}
            <div className="overflow-hidden rounded-[14px] border border-line bg-popover dark:bg-card">
              {group.rows.map((row, index) => {
                const Icon = props.icons[row.icon] ?? Search;
                const selected = row.id === props.activeId;
                return (
                  <div
                    key={row.id}
                    id={props.optionDomId(row.id)}
                    data-f="r"
                    data-row-id={row.id}
                    role="option"
                    aria-selected={selected}
                    aria-disabled={row.unavailable ? true : undefined}
                    title={row.unavailable ?? undefined}
                    onClick={() => props.onChoose(row)}
                    className={cn(
                      "box-border flex min-h-[60px] w-full cursor-pointer items-start gap-3 px-3 py-2.5 text-left active:bg-state-hover",
                      index > 0 && "border-t border-line",
                      row.unavailable && "cursor-not-allowed",
                    )}
                  >
                    <Tile row={row} Icon={Icon} />
                    <span className="min-w-0 flex-1">
                      <span
                        data-f="t"
                        className="line-clamp-2 overflow-hidden text-[15px] leading-[1.36] font-medium"
                      >
                        <Marked text={row.title} term={term} />
                      </span>
                      {row.ctx.length > 0 ? (
                        <span
                          data-f="ctx"
                          className="mt-1 flex min-w-0 items-center gap-1.5 text-[13px] whitespace-nowrap text-meta-foreground"
                        >
                          {row.ctx.map((piece, i) =>
                            piece.type === "mark" ? (
                              <DepartmentMark
                                key={i}
                                name={piece.department.name}
                                markTone={piece.department.markTone}
                                small
                              />
                            ) : piece.type === "mono" ? (
                              <span
                                key={i}
                                className="flex-none font-mono text-[12px]"
                              >
                                {piece.text}
                              </span>
                            ) : (
                              <span key={i} className="min-w-0 truncate">
                                {piece.text}
                              </span>
                            ),
                          )}
                        </span>
                      ) : null}
                    </span>
                    {row.status ? (
                      <span
                        data-f="end"
                        className="flex flex-none flex-col items-end gap-1 pt-0.5 text-meta-foreground"
                      >
                        <span className="inline-flex items-center gap-[5px] text-[12px] font-semibold whitespace-nowrap text-muted-foreground">
                          <span
                            aria-hidden="true"
                            className={cn(
                              "inline-block size-2 rounded-full",
                              STATUS_DOT[row.status.bucket],
                            )}
                          />
                          {row.status.label}
                        </span>
                      </span>
                    ) : null}
                  </div>
                );
              })}
            </div>
          </section>
        ))}
      </div>
    </div>
  );
}
