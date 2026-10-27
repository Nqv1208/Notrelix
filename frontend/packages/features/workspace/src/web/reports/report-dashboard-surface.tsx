import * as React from "react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  cn,
} from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
import {
  BTN,
  BTN_PRIMARY,
  BTN_SM,
  BTN_SM_PRIMARY,
  FOCUS,
} from "../spaces/space-page-parts";
import { ReportGlyph } from "./report-glyph";
import {
  ReportAddWidgetDialog,
  ReportConfirmDialog,
  ReportDefinitionDialog,
  ReportExportDialog,
  ReportRenameDialog,
  ReportShareDialog,
  type ReportSharingState,
  type ReportWidgetOptionsState,
} from "./report-dialogs";
import { ReportDrillSheet, type ReportDrillState } from "./report-drill-sheet";
import { ReportStart } from "./report-start";
import {
  ReportAnchor,
  ReportWidgetBody,
  widgetDefinition,
  widgetTitle,
  type ReportLinks,
  type WidgetEnv,
} from "./report-widgets";
import {
  dayLabel,
  percentOf,
  rangeLabel,
  timeLabel,
  type ReportFormatters,
} from "./reports-format";
import {
  mainBlocks,
  moveWidget,
  neighbourIndex,
  splitColumns,
  type ReportDto,
  type ReportExportJobDto,
  type ReportLayoutId,
  type ReportWidgetDto,
  type ReportWidgetSize,
  type ReportWidgetSpec,
} from "./reports-models";
import { ICON_BTN, LINK_BTN, ScopeChip } from "./reports-parts";
import type { ReportsCopy } from "./reports.copy";

/** What a person may be waiting on while the dashboard is open. */
export type ReportPending =
  "save" | "rename" | "share" | "delete" | "remove" | "layout" | "export";

export interface ReportDashboardSurfaceProps {
  readonly copy: ReportsCopy;
  readonly formatters: ReportFormatters;
  readonly links: ReportLinks;
  readonly report: ReportDto;
  /** The assignee the numbers are narrowed to; null = everyone. */
  readonly assigneeId: string | null;
  readonly onAssignee: (assigneeId: string | null) => void;
  readonly reloading: boolean;
  readonly onReload: () => void;
  // The page state the address holds.
  readonly editing: boolean;
  readonly onStartEdit: () => void;
  readonly onCancelEdit: () => void;
  readonly addOpen: boolean;
  readonly onOpenAdd: () => void;
  readonly onCloseAdd: () => void;
  readonly drillKey: string | null;
  readonly onDrill: (key: string | null) => void;
  readonly exportOpen: boolean;
  readonly onOpenExport: () => void;
  readonly onCloseExport: () => void;
  // What the host reads and writes.
  readonly drill: ReportDrillState;
  readonly onRetryDrill: () => void;
  readonly widgetOptions: ReportWidgetOptionsState;
  readonly onRetryOptions: () => void;
  readonly onPreviewWidget: (
    spec: ReportWidgetSpec,
  ) => Promise<ReportWidgetDto>;
  readonly sharing: ReportSharingState;
  readonly exportJob: ReportExportJobDto | null;
  readonly onCheckExport: () => void;
  /** The writes answer whether they were saved; a refusal keeps the dialog. */
  readonly onStartExport: (format: "pdf" | "csv") => Promise<boolean>;
  readonly onSaveLayout: (
    widgets: readonly ReportWidgetSpec[],
  ) => Promise<boolean>;
  readonly onRemoveWidget: (widgetId: string) => Promise<boolean>;
  readonly onRename: (name: string) => Promise<boolean>;
  readonly onShare: (scopeIds: readonly string[]) => Promise<boolean>;
  readonly onDelete: () => Promise<boolean>;
  readonly onApplyLayout: (layout: ReportLayoutId) => Promise<boolean>;
  /** Stories and tests open the move mode of one widget. */
  readonly initialMovingId?: string | null;
}

const WG =
  "relative min-w-0 rounded-[12px] border bg-popover px-4 pt-3 pb-3.5 dark:bg-card";

function FilterChip({
  prefix,
  value,
  children,
}: {
  prefix?: string;
  value: string;
  children: React.ReactNode;
}) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          data-f="fchip"
          className={cn(
            "inline-flex h-7 cursor-pointer items-center gap-1.5 rounded-[7px] border border-line-strong bg-popover px-2.5 text-crumb leading-[1.45] text-foreground hover:bg-state-hover-strong dark:bg-card",
            FOCUS,
          )}
        >
          {prefix ? (
            <span className="text-meta-foreground">{prefix}</span>
          ) : null}
          {value}
          <ReportGlyph
            name="chevron"
            className="size-[13px] text-meta-foreground"
          />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="min-w-56">
        {children}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function MenuButton({
  label,
  children,
  small = true,
}: {
  label: string;
  children: React.ReactNode;
  small?: boolean;
}) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          aria-label={label}
          aria-haspopup="menu"
          className={cn(ICON_BTN, small && "size-7")}
        >
          <ReportGlyph name="more" />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">{children}</DropdownMenuContent>
    </DropdownMenu>
  );
}

export function ReportDashboardSurface(props: ReportDashboardSurfaceProps) {
  const { copy, formatters, links, report, assigneeId, editing, drillKey } =
    props;
  const [pending, setPending] = React.useState<ReadonlySet<ReportPending>>(
    () => new Set(),
  );
  const [failed, setFailed] = React.useState<ReadonlySet<ReportPending>>(
    () => new Set(),
  );
  const run = React.useCallback(
    async (op: ReportPending, action: () => Promise<boolean>) => {
      setPending((current) => new Set(current).add(op));
      setFailed((current) => {
        const next = new Set(current);
        next.delete(op);
        return next;
      });
      let ok = false;
      try {
        ok = await action();
      } catch {
        ok = false;
      }
      setPending((current) => {
        const next = new Set(current);
        next.delete(op);
        return next;
      });
      if (!ok) setFailed((current) => new Set(current).add(op));
      return ok;
    },
    [],
  );
  const c = copy.page;
  const e = copy.edit;
  const canEdit = report.access === "edit";
  const [filtersShown, setFiltersShown] = React.useState(true);
  const [draft, setDraft] = React.useState<readonly ReportWidgetDto[] | null>(
    null,
  );
  const [movingId, setMovingId] = React.useState<string | null>(
    props.initialMovingId ?? null,
  );
  const [movingFrom, setMovingFrom] = React.useState<
    readonly ReportWidgetDto[] | null
  >(null);
  const [announce, setAnnounce] = React.useState("");
  const [shareOpen, setShareOpen] = React.useState(false);
  const [renameOpen, setRenameOpen] = React.useState(false);
  const [deleteOpen, setDeleteOpen] = React.useState(false);
  const [removeId, setRemoveId] = React.useState<string | null>(null);
  const [definitionId, setDefinitionId] = React.useState<string | null>(null);
  const [dragId, setDragId] = React.useState<string | null>(null);
  const [applying, setApplying] = React.useState<ReportLayoutId | null>(null);

  React.useEffect(() => {
    if (!editing) {
      setDraft(null);
      setMovingId(null);
      setMovingFrom(null);
    }
  }, [editing]);

  const base = report.widgets;
  const shown: readonly ReportWidgetDto[] = editing ? (draft ?? base) : base;
  const widgetsRef = React.useRef(shown);
  widgetsRef.current = shown;

  const timeZone = report.timeZone;
  const asOf = timeLabel(formatters, report.dataAsOf, timeZone);
  const env: WidgetEnv = {
    copy,
    formatters,
    timeZone,
    dataAsOf: report.dataAsOf,
    links,
    drillKey,
    onDrill: editing
      ? undefined
      : (key) => props.onDrill(drillKey === key ? null : key),
    onReload: props.onReload,
    reloading: props.reloading,
  };

  const name = report.name ?? c.unnamed;
  const assigneeName =
    report.filters.assignee?.options.find((o) => o.id === assigneeId)?.name ??
    null;

  // ── Move mode: keyboard swaps with the neighbour of the same column ──
  const startMove = (id: string) => {
    setMovingId(id);
    setMovingFrom(shown);
    const index = shown.findIndex((widget) => widget.id === id);
    setAnnounce(
      formatCopy(e.moved, {
        pos: index + 1,
        total: shown.length,
      }),
    );
  };
  const placeMove = () => {
    const index = shown.findIndex((widget) => widget.id === movingId);
    setAnnounce(formatCopy(e.placed, { pos: index + 1, total: shown.length }));
    setMovingId(null);
    setMovingFrom(null);
  };
  const cancelMove = () => {
    if (movingFrom) setDraft(movingFrom === base ? null : movingFrom);
    setMovingId(null);
    setMovingFrom(null);
  };
  React.useEffect(() => {
    if (!movingId) return;
    const onKey = (event: KeyboardEvent) => {
      const list = widgetsRef.current;
      const index = list.findIndex((widget) => widget.id === movingId);
      if (index < 0) return;
      const direction =
        event.key === "ArrowUp" || event.key === "ArrowLeft"
          ? -1
          : event.key === "ArrowDown" || event.key === "ArrowRight"
            ? 1
            : 0;
      if (direction !== 0) {
        event.preventDefault();
        const to = neighbourIndex(list, index, direction);
        if (to !== index) {
          setDraft(moveWidget(list, index, to));
          setAnnounce(formatCopy(e.moved, { pos: to + 1, total: list.length }));
        }
      } else if (event.key === "Enter") {
        event.preventDefault();
        placeMove();
      } else if (event.key === "Escape") {
        event.preventDefault();
        event.stopPropagation();
        cancelMove();
      }
    };
    document.addEventListener("keydown", onKey, true);
    return () => document.removeEventListener("keydown", onKey, true);
  });

  const resize = (id: string, size: ReportWidgetSize) =>
    setDraft(
      shown.map((widget) =>
        widget.id === id
          ? ({
              ...widget,
              size,
              spec: { ...widget.spec, size },
            } as ReportWidgetDto)
          : widget,
      ),
    );

  const dropOn = (targetId: string) => {
    if (!dragId || dragId === targetId) return;
    const from = shown.findIndex((widget) => widget.id === dragId);
    const to = shown.findIndex((widget) => widget.id === targetId);
    if (from >= 0 && to >= 0) setDraft(moveWidget(shown, from, to));
    setDragId(null);
  };

  const specsOf = (list: readonly ReportWidgetDto[]) =>
    list.map((widget) => ({ ...widget.spec, size: widget.size }));

  // ── Parts ────────────────────────────────────────────────────────────
  const renderWidget = (widget: ReportWidgetDto, index: number) => {
    const title = widgetTitle(widget, env);
    const moving = movingId === widget.id;
    const wide = widget.size === "wide";
    const headingId = `report-w-${widget.id}`;
    return (
      <section
        key={widget.id}
        data-f="wg"
        aria-labelledby={headingId}
        aria-busy={widget.staleAsOf ? true : undefined}
        onDragOver={editing ? (event) => event.preventDefault() : undefined}
        onDrop={editing ? () => dropOn(widget.id) : undefined}
        className={cn(
          WG,
          editing
            ? "border-[1.5px] border-dashed border-primary"
            : "border-line",
          moving &&
            "border-solid shadow-[0_0_0_3px_var(--color-brand-subtle),var(--shadow-popover)]",
        )}
      >
        <div className="flex min-h-7 flex-wrap items-center gap-2">
          <h2
            id={headingId}
            data-f="wgh"
            className="m-0 text-item-prose leading-[1.45] font-[650]"
          >
            {title}
          </h2>
          {editing ? (
            <span className="ml-auto flex items-center gap-1">
              <button
                type="button"
                draggable
                aria-label={formatCopy(e.grip, { title })}
                onDragStart={() => setDragId(widget.id)}
                onDragEnd={() => setDragId(null)}
                className={cn(ICON_BTN, "size-7 cursor-grab")}
              >
                <ReportGlyph name="grip" />
              </button>
              {wide ? (
                <button
                  type="button"
                  aria-pressed={moving}
                  onClick={() => (moving ? placeMove() : startMove(widget.id))}
                  className={BTN_SM}
                >
                  <ReportGlyph name="move" />
                  {e.move}
                </button>
              ) : (
                <button
                  type="button"
                  aria-pressed={moving}
                  aria-label={formatCopy(e.moveAria, { title })}
                  title={e.move}
                  onClick={() => (moving ? placeMove() : startMove(widget.id))}
                  className={cn(ICON_BTN)}
                >
                  <ReportGlyph name="move" />
                </button>
              )}
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <button
                    type="button"
                    aria-label={
                      wide ? undefined : formatCopy(e.resizeAria, { title })
                    }
                    title={wide ? undefined : e.resize}
                    className={wide ? BTN_SM : ICON_BTN}
                  >
                    <ReportGlyph name="resize" />
                    {wide ? e.resize : null}
                  </button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuRadioGroup
                    value={widget.size}
                    onValueChange={(value) =>
                      resize(widget.id, value as ReportWidgetSize)
                    }
                  >
                    {(["wide", "half", "side"] as const).map((size) => (
                      <DropdownMenuRadioItem key={size} value={size}>
                        {e.sizes[size]}
                      </DropdownMenuRadioItem>
                    ))}
                  </DropdownMenuRadioGroup>
                </DropdownMenuContent>
              </DropdownMenu>
            </span>
          ) : (
            <span className="ml-auto">
              <MenuButton label={formatCopy(copy.widget.options, { title })}>
                <DropdownMenuItem onSelect={() => setDefinitionId(widget.id)}>
                  <ReportGlyph name="info" />
                  {copy.widget.definition}
                </DropdownMenuItem>
                {canEdit ? (
                  <DropdownMenuItem
                    variant="destructive"
                    onSelect={() => setRemoveId(widget.id)}
                  >
                    <ReportGlyph name="trash" />
                    {copy.widget.remove}
                  </DropdownMenuItem>
                ) : null}
              </MenuButton>
            </span>
          )}
        </div>
        {moving ? (
          <div
            role="status"
            className="mt-0.5 mb-2.5 flex items-center gap-2 rounded-[7px] bg-brand-subtle px-2.5 py-1.5 text-caption tracking-normal leading-[1.45] font-medium text-brand-ink"
          >
            {formatCopy(e.movingA, {
              pos: index + 1,
              total: shown.length,
            })}
            {" ← ↑ → ↓ "}
            {e.movingB}
            {" Enter "}
            {e.movingC}
            {" Esc "}
            {e.movingD}
          </div>
        ) : null}
        <ReportWidgetBody widget={widget} env={env} />
      </section>
    );
  };

  const { main, side } = splitColumns(shown);
  const indexOf = (widget: ReportWidgetDto) => shown.indexOf(widget);
  const blocks = mainBlocks(main);
  const empty = shown.length === 0 && !editing;
  const summary = report.summary;

  // ── Header ───────────────────────────────────────────────────────────
  const time = report.filters.time;
  const sharedText =
    report.sharedWith.length === 0
      ? c.notShared
      : formatCopy(report.includesViewer ? c.sharedWithYou : c.sharedWith, {
          names: formatters.list(report.sharedWith, { type: "unit" }),
        });
  const accessText = canEdit ? c.canEdit : c.viewOnly;

  const header = (
    <div
      data-f="dhd"
      className={cn(
        "flex-none px-7 pt-4",
        empty && "border-b border-line pb-3.5",
      )}
    >
      <div className="flex flex-wrap items-center gap-3.5">
        <div className="flex flex-col">
          <small
            data-f="crumb"
            className="text-crumb leading-[1.45] text-meta-foreground"
          >
            <ReportAnchor
              link={links.list}
              className="text-inherit hover:underline"
            >
              {c.reports}
            </ReportAnchor>
            {" › "}
            {report.scope.label}
          </small>
          <h1
            data-f="h1"
            className={cn(
              "m-0 text-page-title leading-[1.25] font-[650] tracking-[-0.015em]",
              report.name === null && "text-muted-foreground",
            )}
          >
            {name}
          </h1>
        </div>
        {empty && canEdit ? (
          <button
            type="button"
            aria-label={c.renameReport}
            onClick={() => setRenameOpen(true)}
            className={cn(ICON_BTN, "mt-4")}
          >
            <ReportGlyph name="edit" />
          </button>
        ) : null}
        <ScopeChip
          label={report.scope.label}
          tone={report.scope.tone}
          className="mt-4"
          data-f="chip"
        />
        {!editing ? (
          <div
            data-f="acts"
            className="relative ml-auto flex items-center gap-2"
          >
            {empty ? (
              <>
                <button
                  type="button"
                  onClick={() => setShareOpen(true)}
                  className={BTN}
                >
                  <ReportGlyph name="share" />
                  {c.share}
                </button>
                {canEdit ? (
                  <button
                    type="button"
                    onClick={props.onOpenAdd}
                    className={BTN_PRIMARY}
                  >
                    <ReportGlyph name="plus" />
                    {c.addWidget}
                  </button>
                ) : null}
              </>
            ) : (
              <>
                <button
                  type="button"
                  aria-pressed={filtersShown}
                  onClick={() => setFiltersShown((v) => !v)}
                  data-f="hbtn"
                  className={BTN}
                >
                  <ReportGlyph name="filter" />
                  {c.filter}
                </button>
                <button
                  type="button"
                  aria-haspopup="dialog"
                  aria-expanded={props.exportOpen}
                  onClick={props.onOpenExport}
                  data-f="hbtn"
                  className={BTN}
                >
                  <ReportGlyph name="download" />
                  {c.export}
                </button>
                {canEdit ? (
                  <button
                    type="button"
                    onClick={props.onStartEdit}
                    data-f="hbtn"
                    className={BTN}
                  >
                    <ReportGlyph name="edit" />
                    {c.edit}
                  </button>
                ) : null}
                <MenuButton label={c.more} small={false}>
                  <DropdownMenuItem onSelect={() => setShareOpen(true)}>
                    <ReportGlyph name="share" />
                    {c.share}
                  </DropdownMenuItem>
                  {canEdit ? (
                    <>
                      <DropdownMenuItem onSelect={() => setRenameOpen(true)}>
                        <ReportGlyph name="edit" />
                        {c.rename}
                      </DropdownMenuItem>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem
                        variant="destructive"
                        onSelect={() => setDeleteOpen(true)}
                      >
                        <ReportGlyph name="trash" />
                        {c.delete}
                      </DropdownMenuItem>
                    </>
                  ) : null}
                </MenuButton>
              </>
            )}
          </div>
        ) : null}
      </div>
      <div
        data-f="dmeta"
        className="mt-1.5 flex flex-wrap items-center gap-x-2 gap-y-1 text-crumb leading-[1.45] text-meta-foreground"
      >
        {empty ? (
          <>
            <span>
              {formatCopy(c.createdAt, {
                time: timeLabel(formatters, report.createdAt, timeZone),
              })}
            </span>
            <Dot />
            <span>{sharedText}</span>
            <Dot />
            <span>{c.noWidgets}</span>
          </>
        ) : (
          <>
            {report.owner ? (
              <>
                <span>
                  {formatCopy(c.createdBy, { name: report.owner.name })}
                </span>
                <Dot />
              </>
            ) : null}
            <span>{sharedText}</span>
            <Dot />
            <span>
              {c.yourAccess}
              <b className="font-semibold text-muted-foreground">
                {accessText}
              </b>
            </span>
            <Dot />
            <span>
              {formatCopy(c.updated, {
                time: timeLabel(formatters, report.updatedAt, timeZone),
                asOf,
              })}
            </span>
            <button
              type="button"
              disabled={props.reloading}
              onClick={props.onReload}
              className={LINK_BTN}
            >
              <ReportGlyph name="refresh" className="size-[13px]" />
              {c.reload}
            </button>
          </>
        )}
      </div>
    </div>
  );

  const filterBar =
    !editing && !empty && filtersShown ? (
      <div
        role="group"
        aria-label={c.filtersLabel}
        data-f="fbar"
        className="flex flex-none flex-wrap items-center gap-2 border-b border-line px-7 py-2.5"
      >
        {report.filters.board ? (
          <FilterChip
            prefix={copy.filters.board}
            value={
              report.filters.board.options.find(
                (o) => o.id === report.filters.board?.selectedId,
              )?.name ?? ""
            }
          >
            <DropdownMenuRadioGroup value={report.filters.board.selectedId}>
              {report.filters.board.options.map((option) => (
                <DropdownMenuRadioItem key={option.id} value={option.id}>
                  {option.name}
                </DropdownMenuRadioItem>
              ))}
            </DropdownMenuRadioGroup>
            <DropdownMenuSeparator />
            <DropdownMenuLabel className="max-w-64 text-caption tracking-normal font-normal text-meta-foreground">
              {copy.filters.onlyOption}
            </DropdownMenuLabel>
          </FilterChip>
        ) : null}
        {report.filters.sources ? (
          <FilterChip
            prefix={copy.filters.source}
            value={formatCopy(copy.filters.sourceBoards, {
              count: report.filters.sources.departments.length,
            })}
          >
            {report.filters.sources.departments.map((department) => (
              <DropdownMenuItem key={department} disabled>
                {department}
              </DropdownMenuItem>
            ))}
            <DropdownMenuSeparator />
            <DropdownMenuLabel className="max-w-64 text-caption tracking-normal font-normal text-meta-foreground">
              {copy.filters.sourcesNote}
            </DropdownMenuLabel>
          </FilterChip>
        ) : null}
        {time ? (
          <FilterChip
            prefix={copy.filters.time}
            value={
              time.kind === "until"
                ? formatCopy(copy.filters.until, {
                    day: dayLabel(formatters, time.day),
                  })
                : formatCopy(copy.filters.week, {
                    range: rangeLabel(formatters, time.from, time.to),
                  })
            }
          >
            <DropdownMenuRadioGroup value="current">
              <DropdownMenuRadioItem value="current">
                {time.kind === "until"
                  ? formatCopy(copy.filters.until, {
                      day: dayLabel(formatters, time.day),
                    })
                  : formatCopy(copy.filters.week, {
                      range: rangeLabel(formatters, time.from, time.to),
                    })}
              </DropdownMenuRadioItem>
            </DropdownMenuRadioGroup>
            <DropdownMenuSeparator />
            <DropdownMenuLabel className="max-w-64 text-caption tracking-normal font-normal text-meta-foreground">
              {copy.filters.onlyOption}
            </DropdownMenuLabel>
          </FilterChip>
        ) : null}
        {report.filters.assignee ? (
          <FilterChip
            prefix={assigneeName ? copy.filters.assignee : undefined}
            value={assigneeName ?? copy.filters.allAssignees}
          >
            <DropdownMenuRadioGroup
              value={assigneeId ?? "all"}
              onValueChange={(value) =>
                props.onAssignee(value === "all" ? null : value)
              }
            >
              <DropdownMenuRadioItem value="all">
                {copy.filters.allAssignees}
              </DropdownMenuRadioItem>
              {report.filters.assignee.options.map((option) => (
                <DropdownMenuRadioItem key={option.id} value={option.id}>
                  {option.name}
                </DropdownMenuRadioItem>
              ))}
            </DropdownMenuRadioGroup>
          </FilterChip>
        ) : null}
        <span
          data-f="basis"
          className="ml-auto inline-flex items-center gap-1.5 text-caption tracking-normal leading-[1.45] text-meta-foreground"
        >
          <ReportGlyph name="clock" className="size-[13px]" />
          {copy.filters.basis}
        </span>
      </div>
    ) : null;

  const editBar = editing ? (
    <div
      role="region"
      aria-label={e.label}
      className="flex flex-none items-center gap-2.5 border-b border-line bg-brand-subtle px-7 py-2.5 text-compact leading-[1.45] text-foreground"
    >
      <ReportGlyph name="layout" className="text-brand-ink" />
      <span>
        <b className="font-[650] text-brand-ink">{e.title}</b>{" "}
        <span className="text-crumb text-muted-foreground">{e.caption}</span>
      </span>
      <span className="ml-auto flex items-center gap-1.5">
        {failed.has("save") ? (
          <span role="alert" className="mr-2 text-crumb text-danger-text">
            {e.saveFailed}
          </span>
        ) : null}
        <button
          type="button"
          aria-haspopup="dialog"
          aria-expanded={props.addOpen}
          onClick={props.onOpenAdd}
          className={BTN_SM}
        >
          <ReportGlyph name="plus" />
          {e.addTitle}
        </button>
        <button type="button" onClick={props.onCancelEdit} className={BTN_SM}>
          {e.cancel}
        </button>
        <button
          type="button"
          aria-busy={pending.has("save") ? true : undefined}
          aria-disabled={pending.has("save") ? true : undefined}
          onClick={() => {
            if (!pending.has("save"))
              void run("save", () => props.onSaveLayout(specsOf(shown)));
          }}
          className={BTN_SM_PRIMARY}
        >
          {pending.has("save") ? e.saving : e.save}
        </button>
      </span>
    </div>
  ) : null;

  return (
    <div
      data-f="reports"
      className="relative flex h-full min-h-0 flex-col bg-surface-canvas text-body leading-[1.45] font-normal text-foreground"
    >
      {editBar}
      {header}
      {editing ? <div className="h-3 flex-none border-b border-line" /> : null}
      {filterBar}
      {empty ? (
        <ReportStart
          copy={copy}
          canEdit={canEdit}
          applying={applying}
          failed={failed.has("layout")}
          onApplyLayout={(layout) => {
            setApplying(layout);
            void run("layout", () => props.onApplyLayout(layout)).then(() =>
              setApplying(null),
            );
          }}
          onAddWidget={props.onOpenAdd}
        />
      ) : (
        <div
          data-f="dcol"
          className={cn(
            "flex min-h-0 flex-1 flex-col gap-3.5 overflow-auto px-7 pt-3.5 pb-7",
            editing &&
              "[background-image:radial-gradient(var(--color-line-strong)_1px,transparent_1.2px)] [background-size:18px_18px]",
          )}
        >
          {summary ? (
            <div
              role="list"
              aria-label={formatCopy(copy.summary.label, {
                board: report.sourceBoard?.name ?? "",
                asOf,
              })}
              data-f="sum"
              className="flex flex-wrap items-baseline gap-y-1.5 text-compact leading-[1.45] text-muted-foreground"
            >
              <span role="listitem" className="pr-4 leading-5">
                <b className="text-item-heading leading-5 font-[650] tabular-nums text-foreground">
                  {summary.done}/{summary.total}
                </b>
                {copy.summary.done}
                <b className="text-item-heading leading-5 font-[650] tabular-nums text-foreground">
                  {percentOf(summary.done, summary.total)}%
                </b>
              </span>
              <span
                role="listitem"
                className={cn(
                  "border-l border-line-strong px-4 leading-5",
                  summary.overdue > 0 && "[&>b]:text-danger-text",
                )}
              >
                <b className="text-item-heading leading-5 font-[650] tabular-nums text-foreground">
                  {summary.overdue}
                </b>
                {summary.overdue === 1
                  ? copy.summary.overdue_one
                  : copy.summary.overdue_other}
              </span>
              <span
                role="listitem"
                className="border-l border-line-strong px-4 leading-5"
              >
                <b className="text-item-heading leading-5 font-[650] tabular-nums text-foreground">
                  {summary.dueSoon}
                </b>
                {copy.summary.dueSoon}
              </span>
              {summary.daysToLaunch !== null && summary.launchDate ? (
                <span
                  role="listitem"
                  className="border-l border-line-strong px-4 leading-5"
                >
                  <b className="text-item-heading leading-5 font-[650] tabular-nums text-foreground">
                    {summary.daysToLaunch}
                  </b>
                  {formatCopy(copy.summary.launch, {
                    day: dayLabel(formatters, summary.launchDate),
                  })}
                </span>
              ) : null}
            </div>
          ) : null}
          {report.excludedBoardCount > 0 ? (
            <div
              role="note"
              className="flex items-start gap-[9px] rounded-[10px] border border-line bg-popover px-3 py-2.5 text-compact leading-[1.5] text-foreground dark:bg-card"
            >
              <ReportGlyph
                name="info"
                className="mt-0.5 size-[15px] text-meta-foreground"
              />
              <span>
                {formatCopy(
                  report.excludedBoardCount === 1
                    ? copy.partial.calm_one
                    : copy.partial.calm_other,
                  { count: report.excludedBoardCount },
                )}
              </span>
            </div>
          ) : null}
          <div
            className={cn(
              "grid items-start gap-3.5",
              side.length > 0 && main.length > 0
                ? "grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)]"
                : "grid-cols-1",
            )}
          >
            {main.length > 0 ? (
              <div className="flex min-w-0 flex-col gap-3.5">
                {blocks.map((block) =>
                  block.kind === "single" ? (
                    renderWidget(block.widget, indexOf(block.widget))
                  ) : (
                    <div
                      key={block.widgets[0].id}
                      className="grid grid-cols-2 gap-3.5"
                    >
                      {block.widgets.map((widget) =>
                        renderWidget(widget, indexOf(widget)),
                      )}
                    </div>
                  ),
                )}
              </div>
            ) : null}
            {side.length > 0 || editing ? (
              <div className="flex min-w-0 flex-col gap-3.5">
                {side.map((widget) => renderWidget(widget, indexOf(widget)))}
                {editing ? (
                  <button
                    type="button"
                    onClick={props.onOpenAdd}
                    className={cn(
                      "flex min-h-16 w-full cursor-pointer items-center justify-center gap-2 rounded-[12px] border-[1.5px] border-dashed border-line-strong bg-popover text-nav leading-[1.45] font-medium text-muted-foreground hover:border-primary hover:text-brand-ink dark:bg-card",
                      FOCUS,
                    )}
                  >
                    <ReportGlyph name="plus" />
                    {c.addWidget}
                  </button>
                ) : null}
              </div>
            ) : null}
          </div>
        </div>
      )}
      <span role="status" aria-live="polite" className="sr-only">
        {announce}
      </span>
      {drillKey && !editing ? (
        <ReportDrillSheet
          copy={copy}
          formatters={formatters}
          links={links}
          state={props.drill}
          onClose={() => props.onDrill(null)}
          onRetry={props.onRetryDrill}
        />
      ) : null}
      <ReportExportDialog
        copy={copy}
        formatters={formatters}
        open={props.exportOpen}
        reportName={name}
        boardName={report.sourceBoard?.name ?? null}
        time={report.filters.time}
        assigneeName={assigneeName}
        dataAsOf={report.dataAsOf}
        timeZone={timeZone}
        job={props.exportJob}
        starting={pending.has("export")}
        failed={failed.has("export")}
        onStart={(format) =>
          void run("export", () => props.onStartExport(format))
        }
        onCheck={props.onCheckExport}
        onClose={props.onCloseExport}
      />
      <ReportAddWidgetDialog
        key={props.addOpen ? "open" : "closed"}
        copy={copy}
        open={props.addOpen}
        reportName={name}
        options={props.widgetOptions}
        defaultBoardId={report.sourceBoard?.id ?? null}
        env={env}
        onPreview={props.onPreviewWidget}
        onAdd={(widget) => {
          setDraft([...shown, widget]);
          props.onCloseAdd();
        }}
        onClose={props.onCloseAdd}
        onRetryOptions={props.onRetryOptions}
      />
      <ReportShareDialog
        copy={copy}
        open={shareOpen}
        state={props.sharing}
        saving={pending.has("share")}
        failed={failed.has("share")}
        onSave={(ids) =>
          void run("share", () => props.onShare(ids)).then((ok) => {
            if (ok) setShareOpen(false);
          })
        }
        onClose={() => setShareOpen(false)}
      />
      <ReportRenameDialog
        key={`${renameOpen}`}
        copy={copy}
        open={renameOpen}
        initial={report.name ?? ""}
        saving={pending.has("rename")}
        failed={failed.has("rename")}
        onSave={(next) =>
          void run("rename", () => props.onRename(next)).then((ok) => {
            if (ok) setRenameOpen(false);
          })
        }
        onClose={() => setRenameOpen(false)}
      />
      <ReportConfirmDialog
        open={deleteOpen}
        title={copy.remove.title}
        body={copy.remove.body}
        confirm={copy.remove.confirm}
        cancel={copy.remove.cancel}
        busy={pending.has("delete")}
        failedText={failed.has("delete") ? copy.remove.failed : null}
        onConfirm={() => void run("delete", props.onDelete)}
        onClose={() => setDeleteOpen(false)}
      />
      <ReportConfirmDialog
        open={removeId !== null}
        title={e.removeTitle}
        body={e.removeBody}
        confirm={e.removeConfirm}
        cancel={copy.remove.cancel}
        busy={pending.has("remove")}
        failedText={failed.has("remove") ? e.removeFailed : null}
        onConfirm={() => {
          if (removeId)
            void run("remove", () => props.onRemoveWidget(removeId)).then(
              (ok) => {
                if (ok) setRemoveId(null);
              },
            );
        }}
        onClose={() => setRemoveId(null)}
      />
      <ReportDefinitionDialog
        open={definitionId !== null}
        title={
          definitionId
            ? widgetTitle(
                shown.find((w) => w.id === definitionId) ?? shown[0]!,
                env,
              )
            : ""
        }
        body={
          definitionId
            ? widgetDefinition(
                shown.find((w) => w.id === definitionId) ?? shown[0]!,
                env,
              )
            : ""
        }
        close={copy.export.close}
        onClose={() => setDefinitionId(null)}
      />
    </div>
  );
}

function Dot() {
  return (
    <span
      aria-hidden="true"
      className="inline-block size-[3px] rounded-full bg-line-strong"
    />
  );
}
