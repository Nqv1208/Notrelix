import * as React from "react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Checkbox,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  cn,
} from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
import { BTN, BTN_PRIMARY, FOCUS } from "../spaces/space-page-parts";
import { ReportGlyph, type ReportGlyphName } from "./report-glyph";
import { ReportWidgetBody, type WidgetEnv } from "./report-widgets";
import {
  dayLabel,
  rangeLabel,
  timeLabel,
  type ReportFormatters,
} from "./reports-format";
import {
  GROUPED_TYPES,
  WIDGET_TYPES,
  type ReportExportJobDto,
  type ReportFilterId,
  type ReportGroupBy,
  type ReportMetric,
  type ReportScopeDto,
  type ReportSharingDto,
  type ReportTimeFilter,
  type ReportWidgetDto,
  type ReportWidgetOptionsDto,
  type ReportWidgetSize,
  type ReportWidgetSpec,
  type ReportWidgetType,
} from "./reports-models";
import { ICON_BTN, LINK_BTN, ScopeMark } from "./reports-parts";
import type { ReportsCopy } from "./reports.copy";

const OVERLAY = "bg-modal-scrim backdrop-blur-none";
const DIALOG =
  "box-border flex max-w-[calc(100%-2rem)] translate-y-0 flex-col gap-0 overflow-hidden rounded-[14px] border-0 bg-popover p-0 text-body leading-[1.45] font-normal shadow-dialog ring-0 sm:max-w-[calc(100%-2rem)] dark:bg-card";
const INPUT =
  "box-border h-[34px] w-full rounded-[8px] border border-line-strong bg-popover px-2.5 text-nav leading-[1.45] font-normal text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-state-selected dark:bg-card";
const LABEL = "text-crumb leading-[1.45] font-semibold text-muted-foreground";
const HINT = "text-caption tracking-normal leading-[1.5] text-meta-foreground";

function DialogHead({
  id,
  title,
  closeLabel,
  onClose,
}: {
  id?: string;
  title: string;
  closeLabel: string;
  onClose: () => void;
}) {
  return (
    <div className="flex items-center gap-2.5 px-5 pt-4 pb-1">
      <DialogTitle
        id={id}
        className="m-0 text-dialog-title leading-[1.45] font-[650] text-foreground"
      >
        {title}
      </DialogTitle>
      <button
        type="button"
        aria-label={closeLabel}
        onClick={onClose}
        className={cn(ICON_BTN, "ml-auto")}
      >
        <ReportGlyph name="x" />
      </button>
    </div>
  );
}

// ── Export ──────────────────────────────────────────────────────────────

export interface ReportExportDialogProps {
  readonly copy: ReportsCopy;
  readonly formatters: ReportFormatters;
  readonly open: boolean;
  readonly reportName: string;
  readonly boardName: string | null;
  readonly time: ReportTimeFilter | null;
  readonly assigneeName: string | null;
  readonly dataAsOf: string;
  readonly timeZone: string;
  /** The file being made or made; null before the person asks for one. */
  readonly job: ReportExportJobDto | null;
  readonly starting: boolean;
  readonly failed: boolean;
  readonly onStart: (format: "pdf" | "csv") => void;
  readonly onCheck: () => void;
  readonly onClose: () => void;
}

export function ReportExportDialog({
  copy,
  formatters,
  open,
  reportName,
  boardName,
  time,
  assigneeName,
  dataAsOf,
  timeZone,
  job,
  starting,
  failed,
  onStart,
  onCheck,
  onClose,
}: ReportExportDialogProps) {
  const c = copy.export;
  const [format, setFormat] = React.useState<"pdf" | "csv">("pdf");
  const chosen = job?.format ?? format;
  const locked = job !== null || starting;
  const formats = [
    { id: "pdf" as const, title: c.pdf, body: c.pdfBody },
    { id: "csv" as const, title: c.csv, body: c.csvBody },
  ];
  const scope =
    (boardName ? formatCopy(c.scopeBoard, { board: boardName }) : "") +
    (time?.kind === "until"
      ? formatCopy(c.scopeUntil, { day: dayLabel(formatters, time.day) })
      : time?.kind === "week"
        ? formatCopy(c.scopeWeek, {
            range: rangeLabel(formatters, time.from, time.to),
          })
        : "") +
    (assigneeName
      ? formatCopy(c.scopePerson, { name: assigneeName })
      : c.scopeAll);
  const onRadioKey = (event: React.KeyboardEvent) => {
    if (locked) return;
    if (
      !["ArrowDown", "ArrowRight", "ArrowUp", "ArrowLeft"].includes(event.key)
    )
      return;
    event.preventDefault();
    setFormat((current) => (current === "pdf" ? "csv" : "pdf"));
  };
  return (
    <Dialog open={open} onOpenChange={(next) => (next ? undefined : onClose())}>
      <DialogContent
        showCloseButton={false}
        overlayClassName={OVERLAY}
        aria-labelledby="report-export-title"
        className={cn(DIALOG, "top-[72px] w-[540px]")}
      >
        <DialogHead
          id="report-export-title"
          title={c.title}
          closeLabel={c.close}
          onClose={onClose}
        />
        <DialogDescription className="m-0 px-5 text-crumb leading-[1.45] text-meta-foreground">
          {reportName}
        </DialogDescription>
        <div className="flex flex-col gap-3.5 px-5 pt-3.5 pb-4">
          <div
            role="radiogroup"
            aria-labelledby="report-export-format"
            onKeyDown={onRadioKey}
            className="flex flex-col gap-1.5"
          >
            <span id="report-export-format" className={LABEL}>
              {c.format}
            </span>
            {formats.map((item) => {
              const on = chosen === item.id;
              return (
                <div
                  key={item.id}
                  role="radio"
                  aria-checked={on}
                  aria-disabled={locked && !on ? true : undefined}
                  tabIndex={on ? 0 : -1}
                  onClick={() => {
                    if (!locked) setFormat(item.id);
                  }}
                  onKeyDown={(event) => {
                    if (event.key === " ") {
                      event.preventDefault();
                      if (!locked) setFormat(item.id);
                    }
                  }}
                  className={cn(
                    "flex cursor-pointer items-start gap-2.5 rounded-[10px] border px-3 py-2.5",
                    on ? "border-primary bg-brand-subtle" : "border-line",
                    FOCUS,
                  )}
                >
                  <span
                    aria-hidden="true"
                    className={cn(
                      "mt-0.5 box-border grid size-4 shrink-0 place-items-center rounded-full border-[1.5px]",
                      on ? "border-primary" : "border-line-strong",
                    )}
                  >
                    {on ? (
                      <span className="size-2 rounded-full bg-primary" />
                    ) : null}
                  </span>
                  <span>
                    <b className="block text-nav leading-[1.45] font-semibold">
                      {item.title}
                    </b>
                    <small className="block text-crumb leading-[1.5] text-meta-foreground">
                      {item.body}
                    </small>
                  </span>
                </div>
              );
            })}
          </div>
          <div className="flex flex-col gap-1.5">
            <span className={LABEL}>{c.scope}</span>
            <div className="flex flex-col gap-0.5 rounded-[10px] border border-line bg-surface-canvas px-3 py-2.5 text-compact leading-[1.45]">
              <span>{scope}</span>
              <small className="text-caption tracking-normal leading-[1.45] text-meta-foreground">
                {formatCopy(c.scopeAsOf, {
                  asOf: timeLabel(formatters, dataAsOf, timeZone),
                })}
              </small>
            </div>
            <span className={HINT}>{c.hint}</span>
          </div>
          {job?.status === "done" && job.downloadUrl ? (
            <div
              role="status"
              className="flex items-start gap-2.5 rounded-[10px] border border-line px-3 py-3 text-compact leading-[1.45]"
            >
              <ReportGlyph
                name="check"
                className="mt-px size-[18px] text-success-text"
              />
              <span>
                <b className="block font-semibold">{c.done}</b>
                <small className="mt-0.5 block text-caption tracking-normal leading-[1.5] text-meta-foreground">
                  {c.doneBody}
                </small>
                <small className="block text-caption tracking-normal leading-[1.5] text-meta-foreground">
                  {c.expires}
                </small>
              </span>
            </div>
          ) : job || starting ? (
            <div
              role="status"
              className="flex items-start gap-2.5 rounded-[10px] border border-line px-3 py-3 text-compact leading-[1.45]"
            >
              <span
                aria-hidden="true"
                className="mt-px box-border size-[18px] shrink-0 rounded-full border-2 border-line-strong border-t-primary motion-safe:animate-spin"
              />
              <span>
                <b className="block font-semibold">{c.generating}</b>
                {job ? (
                  <small className="mt-0.5 block text-caption tracking-normal leading-[1.5] text-meta-foreground">
                    {formatCopy(c.generatingBody, {
                      time: timeLabel(formatters, job.startedAt, timeZone),
                    })}
                  </small>
                ) : null}
                <small className="mt-0.5 block text-caption tracking-normal leading-[1.5] text-meta-foreground">
                  {c.expires}
                </small>
              </span>
            </div>
          ) : null}
          {failed ? (
            <p role="alert" className="m-0 text-crumb text-danger-text">
              {c.failed}
            </p>
          ) : null}
        </div>
        <div className="flex items-center justify-end gap-2 border-t border-line px-5 pt-3 pb-4">
          {job && job.status === "running" ? (
            <span className="mr-auto text-caption tracking-normal leading-[1.45] text-meta-foreground">
              {c.slow}{" "}
              <button
                type="button"
                onClick={onCheck}
                className={cn(LINK_BTN, "text-caption tracking-normal")}
              >
                {c.check}
              </button>
            </span>
          ) : null}
          <button type="button" onClick={onClose} className={BTN}>
            {c.close}
          </button>
          {job?.status === "done" && job.downloadUrl ? (
            <a
              href={job.downloadUrl}
              download={job.filename}
              className={cn(BTN_PRIMARY, FOCUS)}
            >
              <ReportGlyph name="download" />
              {formatCopy(c.download, { filename: job.filename })}
            </a>
          ) : job || starting ? (
            <button
              type="button"
              aria-disabled="true"
              className={cn(
                BTN,
                "cursor-not-allowed bg-state-hover-strong text-muted-foreground hover:bg-state-hover-strong dark:bg-state-hover-strong",
              )}
            >
              {starting && !job ? c.starting : c.working}
            </button>
          ) : (
            <button
              type="button"
              onClick={() => onStart(format)}
              className={BTN_PRIMARY}
            >
              {c.start}
            </button>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}

// ── Add widget ──────────────────────────────────────────────────────────

const TYPE_GLYPH: Readonly<Record<ReportWidgetType, ReportGlyphName>> = {
  number: "number",
  columns: "bars",
  line: "line",
  donut: "donut",
  progress: "progress",
  workload: "workload",
  table: "table",
  note: "note",
};

const TYPE_SIZE: Readonly<Record<ReportWidgetType, ReportWidgetSize>> = {
  number: "half",
  columns: "wide",
  line: "wide",
  donut: "half",
  progress: "half",
  workload: "side",
  table: "wide",
  note: "side",
};

export type ReportWidgetOptionsState =
  | { readonly status: "loading" }
  | { readonly status: "error" }
  | { readonly status: "ready"; readonly data: ReportWidgetOptionsDto };

export interface ReportAddWidgetDialogProps {
  readonly copy: ReportsCopy;
  readonly open: boolean;
  readonly reportName: string;
  readonly options: ReportWidgetOptionsState;
  readonly defaultBoardId: string | null;
  readonly env: WidgetEnv;
  /** Counts what the widget would show, from data the viewer may see. */
  readonly onPreview: (spec: ReportWidgetSpec) => Promise<ReportWidgetDto>;
  readonly onAdd: (widget: ReportWidgetDto) => void;
  readonly onClose: () => void;
  readonly onRetryOptions: () => void;
}

type PreviewState =
  | { readonly status: "idle" }
  | { readonly status: "loading" }
  | { readonly status: "error" }
  | { readonly status: "ready"; readonly widget: ReportWidgetDto };

function newWidgetId(): string {
  return `widget-${globalThis.crypto?.randomUUID?.() ?? String(Math.random()).slice(2)}`;
}

function defaultNameFor(
  copy: ReportsCopy,
  type: ReportWidgetType,
  metric: ReportMetric,
  groupBy: ReportGroupBy,
  filters: readonly ReportFilterId[],
): string {
  const c = copy.add;
  if (GROUPED_TYPES.has(type)) {
    const group = c.groupsLower[groupBy];
    const key =
      metric === "overdue"
        ? "overdue"
        : filters.includes("not-done")
          ? "open"
          : "all";
    return formatCopy(c.defaultName[key], { group });
  }
  return c.defaultName[
    type as "number" | "progress" | "workload" | "table" | "note"
  ];
}

function SelectBox({
  id,
  value,
  onChange,
  children,
  icon,
  describedBy,
}: {
  id: string;
  value: string;
  onChange: (value: string) => void;
  children: React.ReactNode;
  icon?: ReportGlyphName;
  describedBy?: string;
}) {
  return (
    <span className="relative block">
      {icon ? (
        <ReportGlyph
          name={icon}
          className="pointer-events-none absolute top-1/2 left-2.5 size-3.5 -translate-y-1/2 text-meta-foreground"
        />
      ) : null}
      <Select value={value} onValueChange={onChange}>
        <SelectTrigger
          id={id}
          aria-describedby={describedBy}
          className={cn("w-full", icon && "pl-[30px]")}
        >
          <SelectValue />
        </SelectTrigger>
        <SelectContent>{children}</SelectContent>
      </Select>
    </span>
  );
}

export function ReportAddWidgetDialog({
  copy,
  open,
  reportName,
  options,
  defaultBoardId,
  env,
  onPreview,
  onAdd,
  onClose,
  onRetryOptions,
}: ReportAddWidgetDialogProps) {
  const c = copy.add;
  const boards = options.status === "ready" ? options.data.boards : [];
  const [type, setType] = React.useState<ReportWidgetType>("columns");
  const [boardId, setBoardId] = React.useState<string | null>(defaultBoardId);
  const [metric, setMetric] = React.useState<ReportMetric>("count");
  const [groupBy, setGroupBy] = React.useState<ReportGroupBy>("assignee");
  const [filters, setFilters] = React.useState<readonly ReportFilterId[]>([
    "not-done",
  ]);
  const [name, setName] = React.useState<string | null>(null);
  const [note, setNote] = React.useState("");
  const [touched, setTouched] = React.useState(false);
  const [preview, setPreview] = React.useState<PreviewState>({
    status: "idle",
  });
  const sequence = React.useRef(0);

  const board = boards.find((b) => b.id === boardId) ?? boards[0] ?? null;
  const effectiveBoard = board?.id ?? null;
  const dimensions = board?.dimensions ?? [];
  const grouped = GROUPED_TYPES.has(type);
  const lineAllowed = groupBy === "week";
  const defaultName = defaultNameFor(copy, type, metric, groupBy, filters);
  const shownName = name ?? defaultName;

  // A grouping the board does not offer falls back to the first it does.
  React.useEffect(() => {
    if (dimensions.length > 0 && !dimensions.includes(groupBy))
      setGroupBy(dimensions[0]!);
  }, [dimensions, groupBy]);

  const previewSpec = React.useMemo<ReportWidgetSpec>(
    () => ({
      id: "preview",
      kind: type,
      size: TYPE_SIZE[type],
      boardId: effectiveBoard,
      metric,
      groupBy,
      filters,
    }),
    [type, effectiveBoard, metric, groupBy, filters],
  );
  const spec = React.useMemo<ReportWidgetSpec>(
    () => ({
      ...previewSpec,
      title: shownName.trim() || null,
      text: type === "note" ? note : null,
    }),
    [previewSpec, shownName, type, note],
  );

  React.useEffect(() => {
    if (!open || type === "note" || !effectiveBoard) return;
    const mine = (sequence.current += 1);
    setPreview({ status: "loading" });
    onPreview(previewSpec).then(
      (widget) => {
        if (sequence.current === mine) setPreview({ status: "ready", widget });
      },
      () => {
        if (sequence.current === mine) setPreview({ status: "error" });
      },
    );
  }, [open, type, effectiveBoard, previewSpec, onPreview]);

  const remaining = (
    options.status === "ready" ? options.data.filters : []
  ).filter((filter) => !filters.includes(filter));
  const nameInvalid = touched && shownName.trim() === "";
  const noteInvalid = type === "note" && touched && note.trim() === "";
  const canAdd =
    type === "note"
      ? note.trim() !== ""
      : effectiveBoard !== null && preview.status === "ready";

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (shownName.trim() === "" || !canAdd) return;
    if (type === "note") {
      onAdd({
        id: newWidgetId(),
        kind: "note",
        size: TYPE_SIZE.note,
        title: shownName.trim(),
        titleKey: null,
        staleAsOf: null,
        spec: { ...spec, id: newWidgetId(), size: TYPE_SIZE.note },
        author: null,
        at: null,
        paragraphs: note
          .split("\n")
          .map((line) => line.trim())
          .filter(Boolean),
      });
      return;
    }
    if (preview.status !== "ready") return;
    const id = newWidgetId();
    onAdd({
      ...preview.widget,
      id,
      title: shownName.trim(),
      spec: { ...spec, id, size: TYPE_SIZE[type] },
    } as ReportWidgetDto);
  };

  return (
    <Dialog open={open} onOpenChange={(next) => (next ? undefined : onClose())}>
      <DialogContent
        showCloseButton={false}
        overlayClassName={OVERLAY}
        aria-labelledby="report-add-title"
        className={cn(DIALOG, "top-10 w-[840px]")}
      >
        <form onSubmit={submit} noValidate className="contents">
          <DialogHead
            id="report-add-title"
            title={c.title}
            closeLabel={copy.export.close}
            onClose={onClose}
          />
          <DialogDescription className="m-0 px-5 text-crumb leading-[1.45] text-meta-foreground">
            {formatCopy(c.into, { name: reportName })}
          </DialogDescription>
          <div className="px-5 pt-3.5 pb-4">
            <div className="grid grid-cols-[minmax(0,1fr)_290px] gap-[18px]">
              <div className="flex min-w-0 flex-col gap-3.5">
                <div className="flex flex-col gap-1.5">
                  <span id="report-add-type" className={LABEL}>
                    {c.type}
                  </span>
                  <div
                    role="radiogroup"
                    aria-labelledby="report-add-type"
                    className="grid grid-cols-4 gap-1.5"
                  >
                    {WIDGET_TYPES.map((item) => {
                      const off = item === "line" && !lineAllowed;
                      const on = type === item;
                      return (
                        <button
                          key={item}
                          type="button"
                          role="radio"
                          aria-checked={on}
                          aria-disabled={off ? true : undefined}
                          aria-describedby={
                            item === "line" ? "report-add-line" : undefined
                          }
                          onClick={() => {
                            if (!off) setType(item);
                          }}
                          className={cn(
                            "flex h-10 cursor-pointer items-center gap-1.5 rounded-[8px] border bg-popover px-2 text-left text-compact leading-[1.45] whitespace-nowrap text-foreground hover:bg-state-hover-strong dark:bg-card",
                            on &&
                              "border-primary bg-brand-subtle font-semibold text-brand-ink",
                            !on && "border-line",
                            off &&
                              "cursor-not-allowed border-dashed bg-transparent text-meta-foreground hover:bg-transparent dark:bg-transparent",
                            FOCUS,
                          )}
                        >
                          <ReportGlyph
                            name={TYPE_GLYPH[item]}
                            className={cn(
                              "text-meta-foreground",
                              on && "text-brand-ink",
                            )}
                          />
                          {c.types[item]}
                        </button>
                      );
                    })}
                  </div>
                  <span id="report-add-line" className={HINT}>
                    {c.lineHint}
                  </span>
                </div>
                {type === "note" ? (
                  <div className="flex flex-col gap-1.5">
                    <label htmlFor="report-add-note" className={LABEL}>
                      {c.noteLabel}
                    </label>
                    <textarea
                      id="report-add-note"
                      value={note}
                      onChange={(event) => setNote(event.target.value)}
                      placeholder={c.notePlaceholder}
                      aria-invalid={noteInvalid ? true : undefined}
                      className={cn(INPUT, "h-28 resize-none py-2")}
                    />
                    {noteInvalid ? (
                      <p
                        role="alert"
                        className="m-0 text-crumb text-danger-text"
                      >
                        {c.noteRequired}
                      </p>
                    ) : null}
                  </div>
                ) : (
                  <>
                    <div className="flex flex-col gap-1.5">
                      <label htmlFor="report-add-source" className={LABEL}>
                        {c.source}
                      </label>
                      {options.status === "error" ? (
                        <p
                          role="alert"
                          className="m-0 text-crumb text-danger-text"
                        >
                          {copy.page.errorTitle}{" "}
                          <button
                            type="button"
                            onClick={onRetryOptions}
                            className={LINK_BTN}
                          >
                            {copy.page.retry}
                          </button>
                        </p>
                      ) : boards.length === 0 && options.status === "ready" ? (
                        <p className="m-0 text-crumb text-meta-foreground">
                          {c.noBoards}
                        </p>
                      ) : (
                        <SelectBox
                          id="report-add-source"
                          icon="board"
                          value={effectiveBoard ?? ""}
                          onChange={setBoardId}
                        >
                          {boards.map((item) => (
                            <SelectItem key={item.id} value={item.id}>
                              {formatCopy(c.sourceBoard, { name: item.name })}
                            </SelectItem>
                          ))}
                        </SelectBox>
                      )}
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                      <div className="flex flex-col gap-1.5">
                        <label htmlFor="report-add-metric" className={LABEL}>
                          {c.metric}
                        </label>
                        <SelectBox
                          id="report-add-metric"
                          value={metric}
                          describedBy="report-add-def"
                          onChange={(value) => setMetric(value as ReportMetric)}
                        >
                          {(options.status === "ready"
                            ? options.data.metrics
                            : (["count"] as const)
                          ).map((item) => (
                            <SelectItem key={item} value={item}>
                              {c.metrics[item]}
                            </SelectItem>
                          ))}
                        </SelectBox>
                      </div>
                      {grouped ? (
                        <div className="flex flex-col gap-1.5">
                          <label htmlFor="report-add-group" className={LABEL}>
                            {c.groupBy}
                          </label>
                          <SelectBox
                            id="report-add-group"
                            value={groupBy}
                            onChange={(value) => {
                              setGroupBy(value as ReportGroupBy);
                              if (type === "line" && value !== "week")
                                setType("columns");
                            }}
                          >
                            {dimensions.map((item) => (
                              <SelectItem key={item} value={item}>
                                {c.groups[item]}
                              </SelectItem>
                            ))}
                          </SelectBox>
                        </div>
                      ) : (
                        <span />
                      )}
                      <span
                        id="report-add-def"
                        className="col-span-2 -mt-1.5 text-caption tracking-normal leading-[1.45] text-meta-foreground"
                      >
                        {c.definitions[metric]}
                      </span>
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <span className={LABEL}>{c.filter}</span>
                      <div className="flex flex-wrap items-center gap-1.5">
                        {filters.map((filter) => (
                          <span
                            key={filter}
                            className="inline-flex h-7 items-center gap-1.5 rounded-[7px] bg-state-hover-strong py-0 pr-1 pl-[9px] text-crumb leading-[1.45] font-medium"
                          >
                            {c.filters[filter]}
                            <button
                              type="button"
                              aria-label={formatCopy(c.removeFilter, {
                                field: c.filterFields[filter],
                              })}
                              onClick={() =>
                                setFilters((current) =>
                                  current.filter((f) => f !== filter),
                                )
                              }
                              className={cn(
                                "grid size-5 cursor-pointer place-items-center rounded border-0 bg-transparent p-0 text-meta-foreground hover:text-foreground",
                                FOCUS,
                              )}
                            >
                              <ReportGlyph name="x" className="size-3" />
                            </button>
                          </span>
                        ))}
                        {remaining.length > 0 ? (
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <button type="button" className={cn(LINK_BTN)}>
                                <ReportGlyph
                                  name="plus"
                                  className="size-[13px]"
                                />
                                {c.addFilter}
                              </button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="start">
                              {remaining.map((filter) => (
                                <DropdownMenuItem
                                  key={filter}
                                  onSelect={() =>
                                    setFilters((current) => [
                                      ...current,
                                      filter,
                                    ])
                                  }
                                >
                                  {c.filters[filter]}
                                </DropdownMenuItem>
                              ))}
                            </DropdownMenuContent>
                          </DropdownMenu>
                        ) : null}
                      </div>
                    </div>
                  </>
                )}
                <div className="flex flex-col gap-1.5">
                  <label htmlFor="report-add-name" className={LABEL}>
                    {c.name}
                  </label>
                  <input
                    id="report-add-name"
                    type="text"
                    value={shownName}
                    onChange={(event) => setName(event.target.value)}
                    aria-invalid={nameInvalid ? true : undefined}
                    className={INPUT}
                  />
                  {nameInvalid ? (
                    <p role="alert" className="m-0 text-crumb text-danger-text">
                      {c.nameRequired}
                    </p>
                  ) : null}
                </div>
              </div>
              <div
                aria-labelledby="report-add-preview"
                className="self-start rounded-[10px] border border-line bg-surface-canvas px-3.5 py-3"
              >
                <h3
                  id="report-add-preview"
                  className="m-0 mb-0.5 text-compact leading-[1.45] font-[650]"
                >
                  {c.preview}
                </h3>
                {type === "note" ? (
                  <>
                    <p className="m-0 mb-2.5 text-caption tracking-normal leading-[1.5] text-meta-foreground">
                      {c.previewNoteText}
                    </p>
                    {note
                      .split("\n")
                      .filter((line) => line.trim() !== "")
                      .map((line, index) => (
                        <p
                          key={index}
                          className="m-0 mb-2 text-item-text leading-[1.6]"
                        >
                          {line}
                        </p>
                      ))}
                  </>
                ) : preview.status === "ready" ? (
                  <>
                    <ReportWidgetBody widget={preview.widget} env={env} />
                    <p className="mt-2 mb-0 text-caption tracking-normal leading-[1.55] text-meta-foreground">
                      {c.previewNote}
                    </p>
                  </>
                ) : preview.status === "error" ? (
                  <p role="alert" className="m-0 text-crumb text-danger-text">
                    {c.previewError}
                  </p>
                ) : (
                  <p
                    role="status"
                    className="m-0 text-crumb text-meta-foreground"
                  >
                    {c.previewLoading}
                  </p>
                )}
              </div>
            </div>
          </div>
          <div className="flex items-center justify-end gap-2 border-t border-line px-5 pt-3 pb-4">
            <span className="mr-auto text-caption tracking-normal leading-[1.45] text-meta-foreground">
              {c.foot}
            </span>
            <button type="button" onClick={onClose} className={BTN}>
              {c.cancel}
            </button>
            <button type="submit" className={BTN_PRIMARY}>
              {c.add}
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ── Share ───────────────────────────────────────────────────────────────

export type ReportSharingState =
  | { readonly status: "loading" }
  | { readonly status: "error" }
  | { readonly status: "ready"; readonly data: ReportSharingDto };

export interface ReportShareDialogProps {
  readonly copy: ReportsCopy;
  readonly open: boolean;
  readonly state: ReportSharingState;
  readonly saving: boolean;
  readonly failed: boolean;
  readonly onSave: (scopeIds: readonly string[]) => void;
  readonly onClose: () => void;
}

export function ReportShareDialog({
  copy,
  open,
  state,
  saving,
  failed,
  onSave,
  onClose,
}: ReportShareDialogProps) {
  const c = copy.share;
  const [chosen, setChosen] = React.useState<readonly string[] | null>(null);
  const data = state.status === "ready" ? state.data : null;
  const selected = chosen ?? data?.selected ?? [];
  const toggle = (target: ReportScopeDto) =>
    setChosen(
      selected.includes(target.id)
        ? selected.filter((id) => id !== target.id)
        : [...selected, target.id],
    );
  return (
    <Dialog open={open} onOpenChange={(next) => (next ? undefined : onClose())}>
      <DialogContent
        showCloseButton={false}
        overlayClassName={OVERLAY}
        aria-labelledby="report-share-title"
        className={cn(DIALOG, "top-[110px] w-[470px]")}
      >
        <DialogHead
          id="report-share-title"
          title={c.title}
          closeLabel={copy.export.close}
          onClose={onClose}
        />
        <DialogDescription className="m-0 px-5 text-crumb leading-[1.5] text-meta-foreground">
          {c.sub}
        </DialogDescription>
        <div className="flex flex-col gap-2 px-5 pt-3.5 pb-4">
          {state.status === "loading" ? (
            <p role="status" className="m-0 text-crumb text-meta-foreground">
              {c.loading}
            </p>
          ) : state.status === "error" ? (
            <p role="alert" className="m-0 text-crumb text-danger-text">
              {c.failed}
            </p>
          ) : (
            <>
              {state.data.targets.map((target) => (
                <label
                  key={target.id}
                  className="flex cursor-pointer items-center gap-2.5 rounded-[8px] border border-line px-3 py-2 text-compact leading-[1.45] hover:bg-state-hover"
                >
                  <Checkbox
                    checked={selected.includes(target.id)}
                    disabled={!state.data.canEdit}
                    onCheckedChange={() => toggle(target)}
                  />
                  <ScopeMark label={target.label} tone={target.tone} />
                  {target.label}
                </label>
              ))}
              {!state.data.canEdit ? (
                <p className="m-0 text-crumb text-meta-foreground">
                  {c.readOnly}
                </p>
              ) : null}
            </>
          )}
          {failed ? (
            <p role="alert" className="m-0 text-crumb text-danger-text">
              {c.failed}
            </p>
          ) : null}
        </div>
        <div className="flex justify-end gap-2 border-t border-line px-5 pt-3 pb-4">
          <button type="button" onClick={onClose} className={BTN}>
            {c.cancel}
          </button>
          <button
            type="button"
            aria-disabled={!data?.canEdit || saving ? true : undefined}
            onClick={() => {
              if (data?.canEdit && !saving) onSave(selected);
            }}
            className={BTN_PRIMARY}
          >
            {c.save}
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

// ── Rename and confirm ──────────────────────────────────────────────────

export function ReportRenameDialog({
  copy,
  open,
  initial,
  saving,
  failed,
  onSave,
  onClose,
}: {
  readonly copy: ReportsCopy;
  readonly open: boolean;
  readonly initial: string;
  readonly saving: boolean;
  readonly failed: boolean;
  readonly onSave: (name: string) => void;
  readonly onClose: () => void;
}) {
  const c = copy.page;
  const [value, setValue] = React.useState(initial);
  const [touched, setTouched] = React.useState(false);
  const trimmed = value.trim();
  const invalid = trimmed === "" || trimmed.length > 80;
  return (
    <Dialog open={open} onOpenChange={(next) => (next ? undefined : onClose())}>
      <DialogContent
        showCloseButton={false}
        overlayClassName={OVERLAY}
        aria-labelledby="report-rename-title"
        className={cn(DIALOG, "top-[110px] w-[470px]")}
      >
        <form
          noValidate
          className="contents"
          onSubmit={(event) => {
            event.preventDefault();
            setTouched(true);
            if (!invalid && !saving) onSave(trimmed);
          }}
        >
          <DialogHead
            id="report-rename-title"
            title={c.rename}
            closeLabel={copy.export.close}
            onClose={onClose}
          />
          <DialogDescription className="sr-only">{c.rename}</DialogDescription>
          <div className="flex flex-col gap-1.5 px-5 pt-3.5 pb-4">
            <label htmlFor="report-rename" className={LABEL}>
              {c.renameLabel}
            </label>
            <input
              id="report-rename"
              value={value}
              maxLength={100}
              autoFocus
              onChange={(event) => setValue(event.target.value)}
              aria-invalid={touched && invalid ? true : undefined}
              className={INPUT}
            />
            {(touched && invalid) || failed ? (
              <p role="alert" className="m-0 text-crumb text-danger-text">
                {c.renameInvalid}
              </p>
            ) : null}
          </div>
          <div className="flex justify-end gap-2 border-t border-line px-5 pt-3 pb-4">
            <button type="button" onClick={onClose} className={BTN}>
              {c.renameCancel}
            </button>
            <button
              type="submit"
              aria-disabled={saving ? true : undefined}
              className={BTN_PRIMARY}
            >
              {c.renameSave}
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

export function ReportConfirmDialog({
  open,
  title,
  body,
  confirm,
  cancel,
  busy,
  failedText,
  onConfirm,
  onClose,
}: {
  readonly open: boolean;
  readonly title: string;
  readonly body: string;
  readonly confirm: string;
  readonly cancel: string;
  readonly busy: boolean;
  readonly failedText: string | null;
  readonly onConfirm: () => void;
  readonly onClose: () => void;
}) {
  return (
    <AlertDialog
      open={open}
      onOpenChange={(next) => (next ? undefined : onClose())}
    >
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{body}</AlertDialogDescription>
        </AlertDialogHeader>
        {failedText ? (
          <p role="alert" className="m-0 text-crumb text-danger-text">
            {failedText}
          </p>
        ) : null}
        <AlertDialogFooter>
          <AlertDialogCancel>{cancel}</AlertDialogCancel>
          <AlertDialogAction
            variant="destructive"
            disabled={busy}
            onClick={(event) => {
              event.preventDefault();
              onConfirm();
            }}
          >
            {confirm}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

/** How a widget's numbers are counted. */
export function ReportDefinitionDialog({
  open,
  title,
  body,
  close,
  onClose,
}: {
  readonly open: boolean;
  readonly title: string;
  readonly body: string;
  readonly close: string;
  readonly onClose: () => void;
}) {
  return (
    <Dialog open={open} onOpenChange={(next) => (next ? undefined : onClose())}>
      <DialogContent
        showCloseButton={false}
        overlayClassName={OVERLAY}
        className={cn(DIALOG, "top-[110px] w-[470px]")}
      >
        <DialogHead title={title} closeLabel={close} onClose={onClose} />
        <DialogDescription className="m-0 px-5 pt-1 pb-5 text-compact leading-[1.6] text-muted-foreground">
          {body}
        </DialogDescription>
      </DialogContent>
    </Dialog>
  );
}
