import { Fragment, useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowRight,
  Check,
  CircleAlert,
  Download,
  FileSpreadsheet,
  LoaderCircle,
  Lock,
  RotateCw,
  Table2,
  TriangleAlert,
  Upload,
} from "lucide-react";
import { endpoints } from "@notrelix/contracts";
import type { OperationResponse } from "@notrelix/contracts";
import { useLocale } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  fillLifecycleCopy as fill,
  pluralLifecycle,
  useBoardLifecycleCopy,
} from "@notrelix/work-management-web";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  cn,
} from "@notrelix/ui-web";
import {
  FrameBanner,
  FrameBody,
  FrameBox,
  FrameDialog,
  FrameFooter,
  FrameHeader,
} from "./lifecycle-dialog-frame";

type Upload = OperationResponse<"WorkManagement.Imports.Upload", 200>;
type Preview = OperationResponse<"WorkManagement.Imports.Preview", 200>;
type Job = OperationResponse<"WorkManagement.Imports.Get", 200>;
type Errors = OperationResponse<"WorkManagement.Imports.GetErrors", 200>;
type RowError = NonNullable<Errors["errors"]>[number];
type ColumnMap = { column: number; target: string };

const SKIP = "$skip";
const BUTTON =
  "inline-flex h-8 flex-none items-center justify-center gap-1.5 whitespace-nowrap rounded-[7px] border px-3 text-frame-small font-medium outline-none focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-not-allowed max-sm:h-11 max-sm:min-w-[120px] max-sm:flex-1";
// A button that is not a footer button keeps its own width on the phone.
const SOLO = "max-sm:min-w-0 max-sm:flex-none";
const GHOST = `${BUTTON} border-line-strong bg-popover text-foreground hover:bg-state-hover-strong`;
const PRIMARY = `${BUTTON} border-primary bg-primary font-semibold text-primary-foreground hover:opacity-90 disabled:border-line-strong disabled:bg-state-hover-strong disabled:font-medium disabled:text-meta-foreground disabled:hover:opacity-100`;
const LINK =
  "inline-flex h-8 flex-none items-center justify-center whitespace-nowrap rounded-[7px] border border-transparent px-2 text-frame-small font-semibold text-info-subtle-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring max-sm:h-11";
const CHIP =
  "inline-flex h-[22px] items-center gap-1 whitespace-nowrap rounded-[6px] px-2 text-frame-hint font-semibold";
const CHIPS = {
  ok: "bg-success-subtle text-success-text",
  warn: "bg-warning-subtle text-warning-text",
  err: "bg-danger-subtle text-danger-subtle-foreground",
  skip: "bg-state-hover-strong text-meta-foreground",
} as const;
const CELL_WIDTH: Record<string, string> = {
  Date: "92px",
  Number: "92px",
  Select: "100px",
  Status: "100px",
  Person: "minmax(0,1fr)",
};
const MAP_GRID =
  "grid grid-cols-[minmax(0,1.15fr)_22px_minmax(0,1fr)_128px] gap-3 px-3.5";
const PV_CELL = "truncate px-2.5";
const REJECTIONS: Record<string, string> = {
  "unsupported-type": "reject_unsupportedType",
  "too-large": "reject_tooLarge",
  "no-rows": "reject_noRows",
  "too-many-rows": "reject_tooManyRows",
  "too-many-columns": "reject_tooManyColumns",
  "unsafe-content": "reject_unsafe",
  malformed: "reject_malformed",
};
const REASONS: Record<string, string> = {
  required: "reason_required",
  "too-long": "reason_tooLong",
  "not-number": "reason_notNumber",
  "not-boolean": "reason_notBoolean",
  "not-date": "reason_notDate",
  "unknown-option": "reason_unknownOption",
  "invalid-link": "reason_invalidLink",
  "invalid-value": "reason_invalidValue",
  "no-group": "reason_noGroup",
};

/**
 * Import items from a CSV or XLSX file (WM-Import, UC-WM-B29/B30): file, column mapping, check, send. The server
 * reads the file and decides every row; the dialog only shows what it answers. All or nothing: while any error is
 * left the Import button stays locked, and the file that previews clean is the file that imports.
 */
export function BoardImportDialog({
  workspaceId,
  boardId,
  boardName,
  open,
  onClose,
}: {
  workspaceId: string;
  boardId: string;
  boardName: string;
  open: boolean;
  onClose: () => void;
}) {
  const copy = useBoardLifecycleCopy().import;
  const c = copy as Record<string, string>;
  const { api } = useAppRuntime();
  const { locale } = useLocale();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const fileInput = useRef<HTMLInputElement>(null);
  const [step, setStep] = useState<1 | 2 | 3 | 4>(1);
  const [upload, setUpload] = useState<Upload | null>(null);
  const [rejection, setRejection] = useState<{
    code: string;
    file: string;
    rows: number;
    columns: number;
    size: number;
  } | null>(null);
  const [reading, setReading] = useState<{ file: string; done: number } | null>(
    null,
  );
  const [mapping, setMapping] = useState<ColumnMap[]>([]);
  const [preview, setPreview] = useState<Preview | null>(null);
  const [checking, setChecking] = useState(false);
  const [jobId, setJobId] = useState<string | null>(null);
  const [lost, setLost] = useState(false);
  const [sending, setSending] = useState(false);
  const [denied, setDenied] = useState(false);
  const [attempt, setAttempt] = useState(() => crypto.randomUUID());
  const [dragging, setDragging] = useState(false);

  const reset = () => {
    setStep(1);
    setUpload(null);
    setRejection(null);
    setReading(null);
    setMapping([]);
    setPreview(null);
    setJobId(null);
    setLost(false);
    setSending(false);
    setDenied(false);
    setAttempt(crypto.randomUUID());
  };
  useEffect(() => {
    if (open) reset();
  }, [open]);

  const targets = upload?.targets ?? [];
  const targetName = (id: string) =>
    id === "$title"
      ? copy.targetTitle
      : id === "$group"
        ? copy.targetGroup
        : (targets.find((t) => t.id === id)?.name ?? id);
  const typeLabel = (type: string) => c[`type_${type}`] ?? type;
  const sizeLabel = (bytes: number) =>
    bytes >= 1024 * 1024
      ? `${new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }).format(bytes / (1024 * 1024))} MB`
      : `${Math.max(1, Math.round(bytes / 1024))} KB`;
  const numberLabel = (n: number) => new Intl.NumberFormat(locale).format(n);
  const unmatchedNote = (rows: readonly number[] | null | undefined) =>
    rows && rows.length > 0 ? (
      <p
        role="status"
        className="m-0 rounded-[10px] border border-line px-3 py-2 text-metadata text-meta-foreground"
      >
        {fill(copy.peopleUnmatched, {
          count: numberLabel(rows.length),
          rows:
            rows.slice(0, 8).map(numberLabel).join(", ") +
            (rows.length > 8 ? "…" : ""),
        })}
      </p>
    ) : null;

  // The job: polled while it waits or runs, so closing and reopening the dialog never loses it.
  const job = useQuery({
    queryKey: ["work-management", "board-import-job", boardId, jobId],
    enabled: open && jobId !== null,
    retry: false,
    gcTime: 0,
    refetchInterval: (query) => {
      const status = (query.state.data as Job | undefined)?.status;
      return status === "succeeded" || status === "failed" ? false : 500;
    },
    queryFn: () =>
      api.api.get<Job>(endpoints.boards.importJob(boardId, jobId!), {
        skipGlobalErrorToast: true,
      }),
  });
  const status = job.data?.status ?? (jobId ? "queued" : null);
  const failed = status === "failed";
  const succeeded = status === "succeeded";
  const failedErrors = useQuery({
    queryKey: ["work-management", "board-import-errors", boardId, jobId],
    enabled: open && failed && job.data?.failureCode === "has-errors",
    retry: false,
    gcTime: 0,
    queryFn: () =>
      api.api.get<Errors>(endpoints.boards.importErrors(boardId, jobId!), {
        skipGlobalErrorToast: true,
      }),
  });
  useEffect(() => {
    if (succeeded)
      void queryClient.invalidateQueries({ queryKey: ["work-management"] });
  }, [succeeded, queryClient]);

  async function choose(file: File | undefined) {
    if (!file) return;
    setRejection(null);
    setReading({ file: file.name, done: 0 });
    try {
      const form = new FormData();
      form.append("file", file, file.name);
      const result = await api.api.upload<Upload>(
        endpoints.boards.imports(boardId),
        form,
        {
          idempotencyKey: crypto.randomUUID(),
          onProgress: (p: { loaded: number; total?: number }) =>
            setReading({
              file: file.name,
              done: p.total ? Math.round((p.loaded / p.total) * 100) : 0,
            }),
        },
      );
      if (result.rejection || !result.jobId) {
        setRejection({
          code: result.rejection ?? "malformed",
          file: file.name,
          rows: result.rowCount ?? 0,
          columns: result.columnCount ?? 0,
          size: result.size ?? file.size,
        });
        setUpload(null);
        return;
      }
      setUpload(result);
      setMapping(
        (result.suggested ?? []).map((m) => ({
          column: m.column ?? 0,
          target: m.target ?? "",
        })),
      );
      setStep(2);
    } catch (error) {
      const code = (error as { status?: number } | null)?.status;
      setRejection({
        code:
          code === 413
            ? "too-large"
            : code === 415
              ? "unsupported-type"
              : "malformed",
        file: file.name,
        rows: 0,
        columns: 0,
        size: file.size,
      });
      if (code === 403 || code === 404) setDenied(true);
    } finally {
      setReading(null);
      if (fileInput.current) fileInput.current.value = "";
    }
  }

  // The server checks the mapping on entering step 2 and after every change (the latest answer wins).
  const check = useRef(0);
  useEffect(() => {
    if (!open || !upload?.jobId || step === 1 || step === 4) return;
    const mine = ++check.current;
    setChecking(true);
    api.api
      .post<Preview>(
        endpoints.boards.importPreview(boardId, upload.jobId),
        { mapping },
        { skipGlobalErrorToast: true },
      )
      .then((result) => {
        if (mine === check.current) setPreview(result);
      })
      .catch(() => {
        if (mine === check.current) setPreview(null);
      })
      .finally(() => {
        if (mine === check.current) setChecking(false);
      });
  }, [mapping, upload?.jobId, step, open, api.api, boardId]);

  const checkOf = (column: number) =>
    preview?.checks?.find((x) => x.column === column);
  const mapped = (column: number) =>
    mapping.find((m) => m.column === column)?.target ?? SKIP;
  const setTarget = (column: number, target: string) =>
    setMapping((prev) =>
      [
        ...prev.filter(
          (m) =>
            m.column !== column && (target === SKIP || m.target !== target),
        ),
        ...(target === SKIP ? [] : [{ column, target }]),
      ].sort((a, b) => a.column - b.column),
    );
  const hasTitle = mapping.some((m) => m.target === "$title");
  const blocking = (preview?.checks ?? []).filter(
    (x) => x.status === "incompatible",
  );
  const mapOk =
    hasTitle && blocking.length === 0 && preview !== null && !checking;
  const errorCount = preview?.errorCount ?? 0;
  const rowCount = upload?.rowCount ?? 0;
  const clean = preview !== null && errorCount === 0 && !preview.missingTitle;

  async function send() {
    if (!upload?.jobId || sending) return;
    setSending(true);
    setLost(false);
    try {
      const result = await api.api.post<Job>(
        endpoints.boards.importConfirm(boardId, upload.jobId),
        { mapping },
        { idempotencyKey: attempt, skipGlobalErrorToast: true },
      );
      setJobId(result.id ?? upload.jobId);
      setStep(4);
    } catch (error) {
      const code = (error as { status?: number } | null)?.status;
      if (code === 403 || code === 404) setDenied(true);
      else if (code === 400) setPreview(null);
      else setLost(true);
    } finally {
      setSending(false);
    }
  }

  function download(errors: readonly RowError[]) {
    // A cell that starts like a formula is prefixed so a spreadsheet never runs it.
    const safe = (v: string) => (/^[=+\-@\t\r]/.test(v) ? `'${v}` : v);
    const cell = (v: string) => {
      const t = safe(v);
      return /[",\r\n;]/.test(t) ? `"${t.replace(/"/g, '""')}"` : t;
    };
    const lines = [
      [copy.colRow, copy.colColumn, copy.colValue, copy.colReason]
        .map(cell)
        .join(","),
    ];
    for (const e of errors)
      lines.push(
        [
          String(e.row ?? ""),
          e.column ?? "",
          e.value ?? copy.empty,
          `${targetName(e.target ?? "")} ${c[REASONS[e.code ?? ""] ?? "reason_invalidValue"]}`,
        ]
          .map(cell)
          .join(","),
      );
    const blob = new Blob(["﻿" + lines.join("\r\n")], {
      type: "text/csv;charset=utf-8",
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fill(copy.reportName, {
      file: (upload?.fileName ?? "tep").replace(/\.[^.]+$/, ""),
    });
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  const reasonText = (e: RowError) =>
    c[REASONS[e.code ?? ""] ?? "reason_invalidValue"] ?? "";
  const steps = [copy.stepFile, copy.stepMap, copy.stepCheck, copy.stepRun];
  const shown = lost ? 4 : step;
  const progress = job.data;
  const total = progress?.total ?? rowCount;
  const unknownCheck = (preview?.checks ?? []).find((x) => (x.count ?? 0) > 0);
  const firstSkipped = (upload?.columns ?? [])
    .map((col) => col.index ?? 0)
    .find((index) => mapped(index) === SKIP);
  const percent = total
    ? Math.round(((progress?.processed ?? 0) / total) * 100)
    : 0;
  const previewGrid = {
    gridTemplateColumns: [
      "64px",
      ...mapping.map((m) =>
        m.target === "$title"
          ? "minmax(0,1.5fr)"
          : (CELL_WIDTH[targets.find((t) => t.id === m.target)?.type ?? ""] ??
            "minmax(0,1fr)"),
      ),
    ].join(" "),
  };
  const submitLabel = fill(
    c[rowCount === 1 ? "submit_one" : "submit_other"] ?? "",
    { count: numberLabel(rowCount) },
  );
  const archived = failed && progress?.failureCode === "board-archived";
  // A board archived before the run starts is shown where the person was: the check.
  const at = archived ? 3 : shown;
  const hasErrorsFailure = failed && progress?.failureCode === "has-errors";
  const fileBad =
    (shown === 3 && preview !== null && !clean) || hasErrorsFailure;
  // Errors left of the first three, and the rows they sit in (the rows of the three shown are not counted again).
  const errorList: readonly RowError[] = hasErrorsFailure
    ? (failedErrors.data?.errors ?? [])
    : (preview?.errors ?? []);
  // The server stops at one row: the error of the row it processed last, else the first.
  const refusedAt = hasErrorsFailure
    ? (errorList.find((e) => e.row === progress?.processed) ?? errorList[0])
    : undefined;
  const firstErrors = errorList.slice(0, 3);
  const restErrors = hasErrorsFailure
    ? errorList.length - firstErrors.length
    : errorCount - firstErrors.length;
  const restRows = hasErrorsFailure
    ? new Set(errorList.slice(3).map((e) => e.row)).size
    : Math.max(
        0,
        (preview?.errorRows ?? 0) - new Set(firstErrors.map((e) => e.row)).size,
      );
  const errorLines =
    firstErrors.length > 0 ? (
      <div
        aria-label={copy.firstErrors}
        className="flex flex-col rounded-[10px] border border-line bg-popover text-frame-small leading-[1.5]"
      >
        {firstErrors.map((e, i) => (
          <div
            key={i}
            className="flex items-start gap-2.5 border-t border-line px-3.5 py-[9px] first:border-t-0"
          >
            <span className="w-[76px] flex-none font-mono text-frame-hint text-meta-foreground">
              {fill(copy.rowLabel, { row: numberLabel(e.row ?? 0) })}
            </span>
            <span>
              {`${e.column}: ${e.value ? `“${e.value}” ` : ""}${reasonText(e)}`}
            </span>
          </div>
        ))}
        {restErrors > 0 ? (
          <div className="flex items-start gap-2.5 border-t border-line px-3.5 py-[9px]">
            <span className="text-frame-note text-meta-foreground">
              {fill(copy.moreErrors, {
                count: numberLabel(restErrors),
                rows: numberLabel(restRows),
              })}
            </span>
          </div>
        ) : null}
      </div>
    ) : null;

  const summary: ReactNode =
    upload && shown > 1 ? (
      <FileCard
        bad={fileBad}
        name={upload.fileName ?? ""}
        meta={fill(copy.fileMeta, {
          rows: numberLabel(rowCount),
          columns: upload.columnCount ?? 0,
          size: sizeLabel(upload.size ?? 0),
        })}
        action={
          shown < 4 ? (
            <button type="button" className={LINK} onClick={reset}>
              {copy.changeFile}
            </button>
          ) : null
        }
      />
    ) : null;

  const readsRows =
    rejection?.code === "too-many-rows" || rejection?.code === "no-rows";
  const rejectionKey = REJECTIONS[rejection?.code ?? ""] ?? "reject_malformed";

  return (
    <FrameDialog
      open={open}
      wide
      onOpenChange={(next) => (next || sending ? undefined : onClose())}
    >
      <FrameHeader
        title={copy.title}
        description={
          shown === 1 ? fill(copy.intro, { name: boardName }) : undefined
        }
        onClose={onClose}
        closeLabel={copy.close}
        closeDisabled={sending}
      />
      <FrameBody className="max-sm:max-h-[calc(100dvh-180px)] max-sm:overflow-y-auto">
        <ol
          aria-label={copy.stepsLabel}
          className="m-0 flex list-none flex-wrap items-center gap-2 p-0 pb-1 pt-0.5"
        >
          {steps.map((label, i) => (
            <Fragment key={label}>
              {i > 0 ? (
                <li aria-hidden="true" className="h-px w-6 bg-line-strong" />
              ) : null}
              <li
                aria-current={at === i + 1 ? "step" : undefined}
                className={cn(
                  "inline-flex items-center gap-2 text-frame-small",
                  at === i + 1
                    ? "font-semibold text-foreground"
                    : at > i + 1
                      ? "font-medium text-meta-foreground"
                      : "font-medium text-meta-foreground",
                )}
              >
                <span
                  className={cn(
                    "grid size-[22px] place-items-center rounded-full text-frame-hint font-bold",
                    at > i + 1
                      ? "bg-success-subtle text-success-text"
                      : at === i + 1
                        ? "bg-primary text-primary-foreground"
                        : "bg-state-hover-strong text-meta-foreground",
                  )}
                >
                  {at > i + 1 ? (
                    <Check aria-hidden="true" className="size-3" />
                  ) : (
                    i + 1
                  )}
                </span>
                {label}
              </li>
            </Fragment>
          ))}
        </ol>

        {step === 1 && readsRows && rejection ? (
          <FileCard
            bad
            name={rejection.file}
            meta={fill(copy.fileMeta, {
              rows: numberLabel(rejection.rows),
              columns: rejection.columns,
              size: sizeLabel(rejection.size),
            })}
            action={
              <button type="button" className={cn(GHOST, SOLO)} onClick={reset}>
                {copy.changeFile}
              </button>
            }
          />
        ) : null}
        {summary}

        {denied ? (
          <p role="alert" className="m-0 text-compact text-danger-text">
            {copy.denied}
          </p>
        ) : null}

        {step === 1 ? (
          <>
            {rejection && readsRows ? (
              <Note
                tone="err"
                title={fill(c[`${rejectionKey}_title`] ?? "", {
                  file: rejection.file,
                  rows: numberLabel(rejection.rows),
                })}
                body={c[rejectionKey] ?? ""}
              />
            ) : null}
            {reading ? (
              <>
                <FileCard
                  name={reading.file}
                  meta={fill(copy.readingMeta, { percent: reading.done })}
                />
                <div
                  role="progressbar"
                  aria-label={copy.reading}
                  aria-valuemin={0}
                  aria-valuemax={100}
                  aria-valuenow={reading.done}
                  className="h-2 w-full overflow-hidden rounded-[4px] bg-state-hover-strong"
                >
                  <div
                    className="h-full rounded-[4px] bg-primary"
                    style={{ width: `${reading.done}%` }}
                  />
                </div>
                <span
                  role="status"
                  className="text-frame-hint text-meta-foreground"
                >
                  {copy.readingBody}
                </span>
              </>
            ) : readsRows ? null : (
              <div
                onDragOver={(event) => {
                  event.preventDefault();
                  setDragging(true);
                }}
                onDragLeave={() => setDragging(false)}
                onDrop={(event) => {
                  event.preventDefault();
                  setDragging(false);
                  void choose(event.dataTransfer.files[0]);
                }}
                role={rejection ? "alert" : undefined}
                className={cn(
                  "flex flex-col items-center gap-2 rounded-xl border-[1.5px] border-dashed px-5 py-[34px] text-center",
                  rejection
                    ? "border-danger bg-danger-subtle"
                    : dragging
                      ? "border-primary bg-state-hover-strong"
                      : "border-line-strong bg-popover",
                )}
              >
                {rejection ? (
                  <CircleAlert
                    aria-hidden="true"
                    className="size-5 text-danger-text"
                  />
                ) : (
                  <span
                    aria-hidden="true"
                    className="grid size-11 place-items-center rounded-xl bg-info-subtle text-info-subtle-foreground"
                  >
                    <Upload className="size-5" />
                  </span>
                )}
                <b
                  className={cn(
                    "text-frame-lead font-bold",
                    rejection && "text-danger-text",
                  )}
                >
                  {rejection
                    ? fill(c[`${rejectionKey}_title`] ?? "", {
                        file: rejection.file,
                        rows: numberLabel(rejection.rows),
                      })
                    : copy.dropTitle}
                </b>
                {rejection ? (
                  <span className="text-frame-body text-foreground">
                    {c[rejectionKey]}
                  </span>
                ) : (
                  <span className="text-frame-note text-meta-foreground">
                    {copy.dropOr}
                  </span>
                )}
                <input
                  ref={fileInput}
                  type="file"
                  accept=".csv,.xlsx,text/csv,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                  className="sr-only"
                  tabIndex={-1}
                  aria-hidden="true"
                  onChange={(event) => void choose(event.target.files?.[0])}
                />
                <button
                  type="button"
                  className={cn(
                    rejection ? GHOST : PRIMARY,
                    SOLO,
                    "max-sm:px-3.5",
                  )}
                  onClick={() => fileInput.current?.click()}
                >
                  <Upload aria-hidden="true" className="size-4" />
                  {rejection ? copy.chooseOther : copy.choose}
                </button>
                {rejection ? null : (
                  <span className="text-frame-hint text-meta-foreground">
                    {copy.dropHint}
                  </span>
                )}
              </div>
            )}
          </>
        ) : null}

        {shown === 2 && upload ? (
          <div className="flex flex-col gap-3.5 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-top-1 motion-safe:duration-150">
            {!hasTitle || blocking.length > 0 ? (
              <FrameBanner
                tone="err"
                role="alert"
                icon={<TriangleAlert aria-hidden="true" />}
                title={
                  !hasTitle
                    ? copy.missingTitle
                    : pluralLifecycle(
                        c as Record<string, string>,
                        "mapProblem",
                        blocking.length,
                      )
                }
                body={!hasTitle ? copy.missingTitleBody : copy.mapProblemBody}
              />
            ) : preview ? (
              <FrameBanner
                tone="ok"
                icon={<Check aria-hidden="true" />}
                title={copy.mapAllOk}
                body={
                  unknownCheck
                    ? fill(copy.mapUnknownNote, {
                        column:
                          upload.columns?.find(
                            (col) => col.index === unknownCheck.column,
                          )?.header ?? "",
                        count: unknownCheck.count ?? 0,
                        values: (unknownCheck.examples ?? []).join(", "),
                      })
                    : copy.mapAllOkBody
                }
              />
            ) : null}
            <div
              role="table"
              aria-label={copy.stepMap}
              className="overflow-hidden rounded-[10px] border border-line bg-popover max-sm:-mb-[2.2px] max-sm:mt-[1.5px] max-sm:flex max-sm:flex-col max-sm:gap-[13.3px] max-sm:overflow-visible max-sm:border-0 max-sm:bg-transparent"
            >
              <div
                role="row"
                className={cn(
                  MAP_GRID,
                  "border-b border-line bg-state-hover py-2 text-frame-cap font-semibold text-meta-foreground max-sm:hidden",
                )}
              >
                <span role="columnheader">{copy.mapColumn}</span>
                <span role="columnheader" />
                <span role="columnheader">{copy.mapField}</span>
                <span role="columnheader">{copy.mapCheck}</span>
              </div>
              {(upload.columns ?? []).map((column) => {
                const index = column.index ?? 0;
                const target = mapped(index);
                const result = checkOf(index);
                const info = targets.find((t) => t.id === target);
                const needsTitle = !hasTitle && index === firstSkipped;
                const incompatible = result?.status === "incompatible";
                const why = needsTitle
                  ? copy.mapRequiredWhy
                  : incompatible
                    ? fill(copy.incompatibleBody, {
                        example: result?.examples?.[0] ?? "",
                        field: targetName(target),
                        type: typeLabel(info?.type ?? ""),
                      })
                    : null;
                // The phone sheet words the same problem shorter (the field is already in the select above).
                const whyShort = incompatible
                  ? fill(copy.incompatibleShort, {
                      example: result?.examples?.[0] ?? "",
                      type: typeLabel(info?.type ?? ""),
                    })
                  : null;
                return (
                  <div
                    key={index}
                    role="row"
                    className={cn(
                      MAP_GRID,
                      "min-h-[62px] items-center border-t border-line py-2.5 first:border-t-0 max-sm:grid max-sm:grid-cols-[minmax(0,1fr)_auto] max-sm:gap-x-2 max-sm:gap-y-2 max-sm:rounded-xl max-sm:border max-sm:border-line max-sm:bg-popover max-sm:px-3.5 max-sm:pb-3.5 max-sm:pt-2.5 max-sm:first:border-t",
                      why &&
                        "bg-danger-subtle max-sm:border-danger max-sm:bg-danger-subtle",
                    )}
                  >
                    <span role="cell" className="min-w-0 max-sm:contents">
                      <b className="block font-semibold max-sm:col-start-1 max-sm:row-start-1 max-sm:text-sm max-sm:font-bold">
                        {column.header}
                      </b>
                      <small className="block truncate font-mono text-frame-hint font-normal text-meta-foreground max-sm:col-span-2 max-sm:row-start-2">
                        {(column.examples ?? [])
                          .map((e) => `“${e}”`)
                          .join(", ")}
                      </small>
                    </span>
                    <span
                      role="presentation"
                      className="text-meta-foreground max-sm:hidden"
                    >
                      <ArrowRight aria-hidden="true" className="size-4" />
                    </span>
                    <span
                      role="cell"
                      className="max-sm:col-span-2 max-sm:row-start-3"
                    >
                      <Select
                        value={needsTitle ? "" : target}
                        onValueChange={(value) => setTarget(index, value)}
                      >
                        <SelectTrigger
                          aria-label={`${copy.mapField}: ${column.header}`}
                          aria-invalid={needsTitle || incompatible || undefined}
                          className="h-9 w-full rounded-[7px] pr-2.5 text-frame-body shadow-none max-sm:data-[size=default]:h-11 max-sm:text-frame-lead aria-invalid:ring-1 aria-invalid:ring-inset aria-invalid:ring-danger"
                        >
                          <SelectValue
                            placeholder={
                              <span className="inline-flex text-meta-foreground">
                                {copy.mapChoose}
                              </span>
                            }
                          >
                            {needsTitle ? (
                              <span className="inline-flex text-meta-foreground">
                                {copy.mapChoose}
                              </span>
                            ) : target === SKIP ? (
                              <span className="inline-flex">
                                {copy.mapSkip}
                              </span>
                            ) : (
                              <span className="inline-flex items-center gap-2">
                                {targetName(target)}
                                <span className="text-frame-note text-meta-foreground max-sm:hidden">
                                  {typeLabel(info?.type ?? "")}
                                </span>
                              </span>
                            )}
                          </SelectValue>
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value={SKIP}>{copy.mapSkip}</SelectItem>
                          {targets.map((t) => (
                            <SelectItem
                              key={t.id}
                              value={t.id ?? ""}
                              disabled={Boolean(t.lockedReason)}
                            >
                              {targetName(t.id ?? "")} ·{" "}
                              {typeLabel(t.type ?? "")}
                              {t.lockedReason
                                ? ` · ${c[`locked_${t.lockedReason}`] ?? ""}`
                                : ""}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </span>
                    <span
                      role="cell"
                      className="max-sm:col-start-2 max-sm:row-start-1"
                    >
                      {needsTitle ? (
                        <span className={cn(CHIP, CHIPS.err)}>
                          {copy.mapRequired}
                        </span>
                      ) : target === SKIP ? (
                        <span className={cn(CHIP, CHIPS.skip)}>
                          {copy.checkSkipped}
                        </span>
                      ) : result ? (
                        <span
                          className={cn(
                            CHIP,
                            result.status === "ok"
                              ? CHIPS.ok
                              : result.status === "unknown-values"
                                ? CHIPS.warn
                                : CHIPS.err,
                          )}
                        >
                          {result.status === "ok"
                            ? copy.checkOk
                            : result.status === "unknown-values"
                              ? pluralLifecycle(
                                  c as Record<string, string>,
                                  "checkUnknown",
                                  result.count ?? 0,
                                )
                              : copy.checkIncompatible}
                        </span>
                      ) : null}
                    </span>
                    {why ? (
                      <p
                        role="alert"
                        className="col-span-full m-0 flex items-start gap-1.5 max-sm:gap-[5px] text-frame-note font-medium leading-[1.5] text-danger-text max-sm:leading-[1.45]"
                      >
                        <TriangleAlert
                          aria-hidden="true"
                          className="mt-0.5 size-[13px] flex-none"
                        />
                        <span
                          className={whyShort ? "max-sm:hidden" : undefined}
                        >
                          {why}
                        </span>
                        {whyShort ? (
                          <span className="hidden max-sm:inline">
                            {whyShort}
                          </span>
                        ) : null}
                      </p>
                    ) : null}
                  </div>
                );
              })}
            </div>
            {targets.some((t) => t.lockedReason) ? (
              <div className="text-frame-hint text-meta-foreground">
                {targets
                  .filter((t) => t.lockedReason)
                  .map((t) => (
                    <span
                      key={t.id}
                      className="mr-3 inline-flex items-center gap-1"
                    >
                      <Lock aria-hidden="true" className="size-3" />
                      {t.name}: {c[`locked_${t.lockedReason}`]}
                    </span>
                  ))}
              </div>
            ) : null}
          </div>
        ) : null}

        {shown === 3 && upload ? (
          <div className="flex flex-col gap-3.5 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-top-1 motion-safe:duration-150">
            {preview === null ? (
              <p
                role="status"
                className="m-0 text-frame-small text-meta-foreground"
              >
                {copy.checkingTitle}
              </p>
            ) : clean ? (
              <FrameBanner
                tone="ok"
                icon={<Check aria-hidden="true" />}
                title={fill(copy.allValid, { rows: numberLabel(rowCount) })}
                body={fill(copy.allValidBody, {
                  rows: numberLabel(rowCount),
                  name: boardName,
                })}
              />
            ) : (
              <FrameBanner
                tone="err"
                role="alert"
                icon={<TriangleAlert aria-hidden="true" />}
                title={fill(copy.hasErrors, {
                  errors: numberLabel(errorCount),
                  rows: numberLabel(preview.errorRows ?? 0),
                })}
                body={copy.hasErrorsBody}
              />
            )}
            {unmatchedNote(preview?.unmatchedPersonRows)}
            {preview ? (
              <div
                role="table"
                aria-label={copy.previewLabel}
                className="overflow-hidden rounded-[10px] border border-line bg-popover text-frame-small max-sm:hidden"
              >
                <div
                  role="row"
                  style={previewGrid}
                  className="grid min-h-[34px] items-center bg-state-hover text-frame-hint font-semibold text-meta-foreground"
                >
                  <span role="columnheader" className={PV_CELL}>
                    {copy.colRow}
                  </span>
                  {mapping.map((m) => (
                    <span
                      key={m.target}
                      role="columnheader"
                      className={PV_CELL}
                    >
                      {targetName(m.target)}
                    </span>
                  ))}
                </div>
                {(preview.sample ?? []).map((row) => (
                  <div
                    key={row.row}
                    role="row"
                    style={previewGrid}
                    className="grid min-h-9 items-center border-t border-line"
                  >
                    <span
                      role="cell"
                      className={cn(
                        PV_CELL,
                        "font-mono text-frame-hint text-meta-foreground",
                      )}
                    >
                      {numberLabel(row.row ?? 0)}
                    </span>
                    {mapping.map((m) => {
                      const bad = row.errorTargets?.includes(m.target);
                      const value = row.cells?.[m.target];
                      return (
                        <span
                          key={m.target}
                          role="cell"
                          className={cn(
                            PV_CELL,
                            bad &&
                              "flex items-center gap-1 self-stretch bg-danger-subtle font-semibold text-danger-text shadow-[inset_0_0_0_1.5px_var(--danger)]",
                          )}
                        >
                          {bad ? (
                            <TriangleAlert
                              aria-hidden="true"
                              className="size-3.5 flex-none"
                            />
                          ) : null}
                          {value ?? copy.empty}
                        </span>
                      );
                    })}
                  </div>
                ))}
              </div>
            ) : null}
            {preview && clean ? (
              <span className="text-frame-hint text-meta-foreground max-sm:hidden">
                {fill(copy.previewNote, {
                  shown: preview.sample?.length ?? 0,
                  rows: numberLabel(rowCount),
                })}
              </span>
            ) : null}
            {preview && !clean ? errorLines : null}
          </div>
        ) : null}

        {shown === 4 ? (
          <div className="flex flex-col gap-3.5 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-top-1 motion-safe:duration-150">
            {lost ? (
              <FrameBanner
                tone="warn"
                role="alert"
                icon={<TriangleAlert aria-hidden="true" />}
                title={copy.lostTitle}
                body={copy.lostBody}
              />
            ) : succeeded ? (
              <>
                <FrameBanner
                  tone="ok"
                  icon={<Check aria-hidden="true" />}
                  title={fill(copy.done, {
                    count: numberLabel(progress?.created ?? 0),
                    name: boardName,
                  })}
                  body={fill(copy.doneBody, {
                    first: progress?.firstKey ?? "",
                  })}
                />
                {unmatchedNote(progress?.unmatchedPersonRows)}
                <FrameBox>
                  <div className="flex min-h-[52px] items-center gap-3 px-3.5 py-2">
                    <span className="flex min-w-0 flex-1 flex-col gap-0.5">
                      <b className="text-frame-body font-semibold">
                        {copy.doneAsk}
                      </b>
                      <small className="text-frame-note leading-[1.4] text-meta-foreground">
                        {copy.doneAskBody}
                      </small>
                    </span>
                    <button
                      type="button"
                      className={GHOST}
                      onClick={() => {
                        onClose();
                        void navigate({
                          to: "/workspaces/$workspaceId/boards/$boardId",
                          params: { workspaceId, boardId },
                          search: {
                            view: "table",
                            filter: undefined,
                            sort: "$created.desc",
                          },
                        } as never);
                      }}
                    >
                      <Table2 aria-hidden="true" className="size-4" />
                      {copy.doneOpen}
                    </button>
                  </div>
                </FrameBox>
              </>
            ) : failed ? (
              <>
                <FrameBanner
                  tone={archived ? "warn" : "err"}
                  role="alert"
                  icon={<TriangleAlert aria-hidden="true" />}
                  title={
                    archived
                      ? fill(copy.archivedTitle, {
                          name: progress?.failureBy ?? "",
                        })
                      : progress?.failureCode === "board-gone"
                        ? copy.goneTitle
                        : copy.failedTitle
                  }
                  body={
                    archived
                      ? copy.archivedBody
                      : progress?.failureCode === "board-gone"
                        ? copy.goneBody
                        : refusedAt
                          ? fill(copy.failedHasErrors, {
                              row: numberLabel(refusedAt.row ?? 0),
                              column: refusedAt.column ?? "",
                              value: refusedAt.value
                                ? `“${refusedAt.value}”`
                                : "",
                              reason: reasonText(refusedAt),
                            })
                          : copy.errorBody
                  }
                />
                {hasErrorsFailure ? (
                  <>
                    {errorLines}
                    <span className="text-frame-hint text-meta-foreground">
                      {copy.failedRetryNote}
                    </span>
                  </>
                ) : null}
              </>
            ) : (
              <>
                <FrameBox>
                  <div
                    role="status"
                    className="flex min-h-[52px] items-start gap-3 px-3.5 py-2"
                  >
                    <LoaderCircle
                      aria-hidden="true"
                      className="mt-0.5 size-4 flex-none text-info-subtle-foreground motion-safe:animate-spin"
                    />
                    <span className="flex min-w-0 flex-1 flex-col gap-0.5">
                      <b className="text-frame-body font-semibold">
                        {status === "running" && progress?.phase === "creating"
                          ? fill(copy.creating, {
                              done: numberLabel(progress.processed ?? 0),
                              total: numberLabel(total),
                            })
                          : copy.queued}
                      </b>
                      <span
                        role="progressbar"
                        aria-label={copy.creatingLabel}
                        aria-valuemin={0}
                        aria-valuemax={100}
                        aria-valuenow={percent}
                        className="h-2 w-full overflow-hidden rounded-[4px] bg-state-hover-strong"
                      >
                        <span
                          className="block h-full rounded-[4px] bg-primary motion-safe:transition-[width] motion-safe:duration-300"
                          style={{ width: `${percent}%` }}
                        />
                      </span>
                      <small className="text-frame-note leading-[1.4] text-meta-foreground">
                        {copy.creatingBody}
                      </small>
                    </span>
                  </div>
                </FrameBox>
                <span className="text-frame-hint text-meta-foreground">
                  {copy.creatingNote}
                </span>
              </>
            )}
          </div>
        ) : null}
      </FrameBody>
      <FrameFooter>
        {step === 1 && !lost ? (
          <>
            <button type="button" className={GHOST} onClick={onClose}>
              {copy.cancel}
            </button>
            <button type="button" className={PRIMARY} disabled>
              {copy.next}
            </button>
          </>
        ) : shown === 2 ? (
          <>
            <button type="button" className={GHOST} onClick={reset}>
              {copy.back}
            </button>
            <button
              type="button"
              className={PRIMARY}
              disabled={!mapOk}
              aria-disabled={!mapOk}
              onClick={() => setStep(3)}
            >
              {copy.next}
            </button>
          </>
        ) : shown === 3 ? (
          <>
            {preview && !clean ? (
              <span className="min-w-0 flex-1 text-frame-note leading-[1.4] text-meta-foreground max-sm:hidden">
                {copy.reportNote}
              </span>
            ) : null}
            {preview && !clean ? (
              <button
                type="button"
                className={GHOST}
                onClick={() =>
                  void api.api
                    .post<Preview>(
                      endpoints.boards.importPreview(
                        boardId,
                        upload?.jobId ?? "",
                      ),
                      { mapping, allErrors: true },
                      { skipGlobalErrorToast: true },
                    )
                    .then((all) => download(all.errors ?? []))
                    .catch(() => download(preview.errors ?? []))
                }
              >
                <Download aria-hidden="true" className="size-4" />
                {copy.downloadReport}
              </button>
            ) : null}
            {preview && !clean ? (
              <button type="button" className={GHOST} onClick={reset}>
                {copy.chooseOther}
              </button>
            ) : (
              <button
                type="button"
                className={GHOST}
                onClick={() => setStep(2)}
              >
                {copy.back}
              </button>
            )}
            <button
              type="button"
              className={PRIMARY}
              disabled={!clean || sending}
              aria-disabled={!clean || sending}
              onClick={() => void send()}
            >
              {sending ? copy.working : submitLabel}
            </button>
          </>
        ) : succeeded ? (
          <button type="button" className={PRIMARY} onClick={onClose}>
            {copy.finish}
          </button>
        ) : archived ? (
          <>
            <button type="button" className={GHOST} onClick={onClose}>
              {copy.close}
            </button>
            <button type="button" className={PRIMARY} disabled aria-disabled>
              {submitLabel}
            </button>
          </>
        ) : failed ? (
          <>
            {hasErrorsFailure && failedErrors.data ? (
              <button
                type="button"
                className={GHOST}
                onClick={() => download(failedErrors.data?.errors ?? [])}
              >
                <Download aria-hidden="true" className="size-4" />
                {copy.downloadReport}
              </button>
            ) : null}
            {hasErrorsFailure ? null : (
              <button type="button" className={GHOST} onClick={onClose}>
                {copy.close}
              </button>
            )}
            <button type="button" className={PRIMARY} onClick={reset}>
              <Upload aria-hidden="true" className="size-4" />
              {hasErrorsFailure ? copy.chooseFixed : copy.chooseOther}
            </button>
          </>
        ) : (
          <>
            <button type="button" className={GHOST} onClick={onClose}>
              {copy.close}
            </button>
            {lost ? (
              <button
                type="button"
                className={PRIMARY}
                onClick={() => void send()}
              >
                <RotateCw aria-hidden="true" className="size-4" />
                {copy.retry}
              </button>
            ) : (
              <button type="button" className={PRIMARY} disabled aria-disabled>
                {copy.working}
              </button>
            )}
          </>
        )}
      </FrameFooter>
    </FrameDialog>
  );
}

function FileCard({
  name,
  meta,
  action,
  bad,
}: {
  name: string;
  meta: string;
  action?: ReactNode;
  bad?: boolean;
}) {
  return (
    <div className="flex items-center gap-3 rounded-[10px] border border-line bg-popover px-3.5 py-2.5">
      <span
        className={cn(
          "grid h-10 w-9 flex-none place-items-center rounded-[7px]",
          bad
            ? "bg-danger-subtle text-danger-text"
            : "bg-success-subtle text-success-text",
        )}
      >
        <FileSpreadsheet aria-hidden="true" className="size-4" />
      </span>
      <span className="min-w-0 flex-1">
        <b className="block truncate text-frame-body font-semibold">{name}</b>
        <small className="block text-frame-note text-meta-foreground">
          {meta}
        </small>
      </span>
      {action}
    </div>
  );
}

function Note({
  tone,
  title,
  body,
}: {
  tone: "err";
  title: string;
  body: string;
}) {
  return (
    <div
      role="alert"
      data-tone={tone}
      className="flex items-start gap-2.5 rounded-[10px] bg-danger-subtle px-3.5 py-2.5 text-frame-small leading-[1.4] text-danger-subtle-foreground"
    >
      <CircleAlert aria-hidden="true" className="mt-0.5 size-4 flex-none" />
      <span className="flex min-w-0 flex-1 flex-col gap-px">
        <b className="font-semibold">{title}</b>
        <span>{body}</span>
      </span>
    </div>
  );
}
