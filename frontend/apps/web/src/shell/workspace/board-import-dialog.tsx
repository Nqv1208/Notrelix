import { useEffect, useMemo, useRef, useState } from "react";
import type { ReactNode } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, CircleAlert, FileSpreadsheet, Lock, X } from "lucide-react";
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
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  cn,
} from "@notrelix/ui-web";

type Upload = OperationResponse<"WorkManagement.Imports.Upload", 200>;
type Preview = OperationResponse<"WorkManagement.Imports.Preview", 200>;
type Job = OperationResponse<"WorkManagement.Imports.Get", 200>;
type Errors = OperationResponse<"WorkManagement.Imports.GetErrors", 200>;
type RowError = NonNullable<Errors["errors"]>[number];
type ColumnMap = { column: number; target: string };

const SKIP = "$skip";
const BUTTON =
  "inline-flex h-8 items-center justify-center rounded-[7px] border px-3 text-compact font-medium outline-none focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-not-allowed disabled:opacity-60 max-sm:h-11";
const GHOST = `${BUTTON} border-line-strong bg-popover text-foreground hover:bg-state-hover-strong`;
const PRIMARY = `${BUTTON} border-primary bg-primary font-semibold text-primary-foreground hover:opacity-90`;
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
  const busy = sending || status === "queued" || status === "running";
  const progress = job.data;
  const total = progress?.total ?? rowCount;

  const summary: ReactNode =
    upload && step > 1 ? (
      <div className="flex items-center gap-2.5 rounded-[10px] border border-line bg-background px-3 py-2.5">
        <FileSpreadsheet
          aria-hidden="true"
          className="size-4 shrink-0 text-meta-foreground"
        />
        <span className="min-w-0 flex-1">
          <b className="block truncate text-compact font-semibold">
            {upload.fileName}
          </b>
          <span className="block text-metadata text-meta-foreground">
            {fill(copy.fileMeta, {
              rows: numberLabel(rowCount),
              columns: upload.columnCount ?? 0,
              size: sizeLabel(upload.size ?? 0),
            })}
          </span>
        </span>
        {step < 4 ? (
          <button type="button" className={GHOST} onClick={reset}>
            {copy.changeFile}
          </button>
        ) : null}
      </div>
    ) : null;

  const sheetClass =
    "gap-3 sm:max-w-[880px] max-sm:top-auto max-sm:bottom-0 max-sm:max-h-[calc(100dvh-24px)] max-sm:max-w-full max-sm:translate-y-0 max-sm:rounded-b-none max-sm:overflow-y-auto";

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => (next || sending ? undefined : onClose())}
    >
      <DialogContent showCloseButton={false} className={sheetClass}>
        <DialogHeader>
          <div className="flex items-start gap-3">
            <div className="flex-1">
              <DialogTitle>{copy.title}</DialogTitle>
              <DialogDescription>
                {fill(copy.intro, { name: boardName })}
              </DialogDescription>
            </div>
            <button
              type="button"
              aria-label={copy.close}
              title={copy.close}
              onClick={onClose}
              disabled={sending}
              className="inline-flex size-8 items-center justify-center rounded-[7px] text-meta-foreground hover:bg-state-hover-strong max-sm:size-11"
            >
              <X aria-hidden="true" className="size-4" />
            </button>
          </div>
        </DialogHeader>
        <ol
          aria-label={copy.stepsLabel}
          className="m-0 flex list-none gap-3 p-0 text-compact"
        >
          {steps.map((label, i) => (
            <li
              key={label}
              aria-current={step === i + 1 ? "step" : undefined}
              className={cn(
                "flex items-center gap-1.5",
                step === i + 1
                  ? "font-semibold text-foreground"
                  : "text-meta-foreground",
              )}
            >
              <span
                className={cn(
                  "inline-flex size-5 items-center justify-center rounded-full text-metadata",
                  step > i + 1
                    ? "bg-success-subtle text-success-text"
                    : step === i + 1
                      ? "bg-primary text-primary-foreground"
                      : "bg-state-hover-strong",
                )}
              >
                {step > i + 1 ? (
                  <Check aria-hidden="true" className="size-3" />
                ) : (
                  i + 1
                )}
              </span>
              <span className="max-sm:hidden">{label}</span>
              <span className="sm:hidden">{step === i + 1 ? label : ""}</span>
            </li>
          ))}
        </ol>

        {summary}

        {denied ? (
          <p role="alert" className="m-0 text-compact text-danger-text">
            {copy.denied}
          </p>
        ) : null}

        {step === 1 ? (
          <>
            {rejection ? (
              <div
                role="alert"
                className="flex flex-col gap-1 rounded-[10px] bg-danger-subtle px-3 py-2.5 text-danger-subtle-foreground"
              >
                <b className="text-compact font-semibold">
                  {fill(
                    c[
                      `${REJECTIONS[rejection.code] ?? "reject_malformed"}_title`
                    ] ?? "",
                    { file: rejection.file, rows: numberLabel(rejection.rows) },
                  )}
                </b>
                <span className="text-metadata">
                  {c[REJECTIONS[rejection.code] ?? "reject_malformed"]}
                </span>
              </div>
            ) : null}
            {reading ? (
              <div
                role="status"
                className="flex flex-col gap-1.5 rounded-[10px] border border-line px-3 py-3"
              >
                <b className="text-compact font-semibold">{reading.file}</b>
                <div className="h-1.5 overflow-hidden rounded-full bg-state-hover-strong">
                  <div
                    className="h-full rounded-full bg-primary"
                    style={{ width: `${reading.done}%` }}
                  />
                </div>
                <span className="text-metadata text-meta-foreground">
                  {copy.reading} · {copy.readingBody}
                </span>
              </div>
            ) : (
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
                className={cn(
                  "flex flex-col items-center gap-2 rounded-[10px] border border-dashed px-4 py-8 text-center",
                  dragging
                    ? "border-primary bg-state-hover-strong"
                    : "border-line-strong",
                )}
              >
                <FileSpreadsheet
                  aria-hidden="true"
                  className="size-6 text-meta-foreground"
                />
                <b className="text-compact font-semibold">{copy.dropTitle}</b>
                <span className="text-metadata text-meta-foreground">
                  {copy.dropOr}
                </span>
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
                  className={GHOST}
                  onClick={() => fileInput.current?.click()}
                >
                  {rejection ? copy.chooseOther : copy.choose}
                </button>
                <span className="text-metadata text-meta-foreground">
                  {copy.dropHint}
                </span>
              </div>
            )}
          </>
        ) : null}

        {step === 2 && upload ? (
          <div className="flex flex-col gap-2 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-top-1 motion-safe:duration-150">
            {!hasTitle || blocking.length > 0 ? (
              <div
                role="alert"
                className="flex flex-col gap-0.5 rounded-[10px] bg-danger-subtle px-3 py-2 text-danger-subtle-foreground"
              >
                <b className="text-compact font-semibold">
                  {!hasTitle
                    ? copy.missingTitle
                    : pluralLifecycle(
                        c as Record<string, string>,
                        "mapProblem",
                        blocking.length,
                      )}
                </b>
                <span className="text-metadata">
                  {!hasTitle ? copy.missingTitleBody : copy.mapProblemBody}
                </span>
              </div>
            ) : null}
            <div
              role="table"
              aria-label={copy.stepMap}
              className="flex flex-col divide-y divide-line rounded-[10px] border border-line"
            >
              <div
                role="row"
                className="grid grid-cols-[1fr_1fr_auto] gap-3 px-3 py-2 text-metadata font-semibold text-meta-foreground max-sm:hidden"
              >
                <span role="columnheader">{copy.mapColumn}</span>
                <span role="columnheader">{copy.mapField}</span>
                <span role="columnheader" className="min-w-[120px]">
                  {copy.mapCheck}
                </span>
              </div>
              {(upload.columns ?? []).map((column) => {
                const index = column.index ?? 0;
                const target = mapped(index);
                const result = checkOf(index);
                const info = targets.find((t) => t.id === target);
                return (
                  <div
                    key={index}
                    role="row"
                    className="grid grid-cols-[1fr_1fr_auto] items-center gap-3 px-3 py-2 max-sm:grid-cols-1 max-sm:gap-1.5"
                  >
                    <span role="cell" className="min-w-0">
                      <b className="block truncate text-compact font-semibold">
                        {column.header}
                      </b>
                      <span className="block truncate text-metadata text-meta-foreground">
                        {(column.examples ?? [])
                          .map((e) => `“${e}”`)
                          .join(", ")}
                      </span>
                    </span>
                    <span role="cell">
                      <Select
                        value={target}
                        onValueChange={(value) => setTarget(index, value)}
                      >
                        <SelectTrigger
                          aria-label={`${copy.mapField}: ${column.header}`}
                          className="w-full"
                        >
                          <SelectValue placeholder={copy.mapChoose} />
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
                      {target === "$title" ? (
                        <span className="mt-0.5 block text-metadata text-meta-foreground">
                          {copy.mapRequired}
                        </span>
                      ) : null}
                      {result?.status === "incompatible" ? (
                        <span
                          role="alert"
                          className="mt-1 block text-metadata text-danger-text"
                        >
                          {fill(copy.incompatibleBody, {
                            example: result.examples?.[0] ?? "",
                            field: targetName(target),
                            type: typeLabel(info?.type ?? ""),
                          })}
                        </span>
                      ) : null}
                    </span>
                    <span role="cell" className="min-w-[120px]">
                      {target === SKIP ? null : result ? (
                        <span
                          className={cn(
                            "inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-metadata font-medium",
                            result.status === "ok"
                              ? "bg-success-subtle text-success-text"
                              : result.status === "unknown-values"
                                ? "bg-warning-subtle text-warning-text"
                                : "bg-danger-subtle text-danger-subtle-foreground",
                          )}
                        >
                          {result.status === "ok" ? (
                            <Check aria-hidden="true" className="size-3" />
                          ) : (
                            <CircleAlert
                              aria-hidden="true"
                              className="size-3"
                            />
                          )}
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
                  </div>
                );
              })}
            </div>
            <div className="text-metadata text-meta-foreground">
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
          </div>
        ) : null}

        {step === 3 && upload ? (
          <div className="flex flex-col gap-2 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-top-1 motion-safe:duration-150">
            {preview === null ? (
              <p
                role="status"
                className="m-0 text-compact text-meta-foreground"
              >
                {copy.checkingTitle}
              </p>
            ) : clean ? (
              <div
                role="status"
                className="flex flex-col gap-0.5 rounded-[10px] bg-success-subtle px-3 py-2 text-success-text"
              >
                <b className="text-compact font-semibold">
                  {fill(copy.allValid, { rows: numberLabel(rowCount) })}
                </b>
                <span className="text-metadata">
                  {fill(copy.allValidBody, {
                    rows: numberLabel(rowCount),
                    name: boardName,
                  })}
                </span>
              </div>
            ) : (
              <div
                role="alert"
                className="flex flex-col gap-0.5 rounded-[10px] bg-danger-subtle px-3 py-2 text-danger-subtle-foreground"
              >
                <b className="text-compact font-semibold">
                  {fill(copy.hasErrors, {
                    errors: numberLabel(errorCount),
                    rows: numberLabel(preview.errorRows ?? 0),
                  })}
                </b>
                <span className="text-metadata">{copy.hasErrorsBody}</span>
              </div>
            )}
            {preview ? (
              <div className="overflow-x-auto rounded-[10px] border border-line">
                <table className="w-full border-collapse text-compact">
                  <thead>
                    <tr className="text-left text-metadata text-meta-foreground">
                      <th className="px-3 py-1.5 font-semibold">
                        {copy.colRow}
                      </th>
                      {mapping.map((m) => (
                        <th
                          key={m.target}
                          className="px-3 py-1.5 font-semibold"
                        >
                          {targetName(m.target)}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {(preview.sample ?? []).map((row) => (
                      <tr key={row.row} className="border-t border-line">
                        <td className="px-3 py-1.5 text-meta-foreground">
                          {numberLabel(row.row ?? 0)}
                        </td>
                        {mapping.map((m) => {
                          const bad = row.errorTargets?.includes(m.target);
                          const value = row.cells?.[m.target];
                          return (
                            <td
                              key={m.target}
                              className={cn(
                                "px-3 py-1.5",
                                bad
                                  ? "text-danger-text outline outline-1 -outline-offset-1 outline-danger"
                                  : "",
                              )}
                            >
                              {bad ? (
                                <CircleAlert
                                  aria-hidden="true"
                                  className="mr-1 inline size-3"
                                />
                              ) : null}
                              {value ?? copy.empty}
                            </td>
                          );
                        })}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : null}
            {preview ? (
              <p className="m-0 text-metadata text-meta-foreground">
                {fill(copy.previewNote, {
                  shown: preview.sample?.length ?? 0,
                  rows: numberLabel(rowCount),
                })}
              </p>
            ) : null}
            {preview && !clean ? (
              <>
                <ul className="m-0 flex list-none flex-col gap-0.5 p-0 text-compact">
                  {(preview.errors ?? []).slice(0, 3).map((e, i) => (
                    <li key={i}>
                      <b className="font-semibold">
                        {fill(copy.rowLabel, { row: numberLabel(e.row ?? 0) })}
                      </b>{" "}
                      {e.column}: {e.value ? `“${e.value}”` : copy.empty}{" "}
                      {reasonText(e)}
                    </li>
                  ))}
                </ul>
                {errorCount > 3 ? (
                  <p className="m-0 text-metadata text-meta-foreground">
                    {fill(copy.moreErrors, {
                      count: numberLabel(errorCount - 3),
                    })}
                  </p>
                ) : null}
                <p className="m-0 text-metadata text-meta-foreground">
                  {copy.reportNote}
                </p>
              </>
            ) : null}
          </div>
        ) : null}

        {step === 4 ? (
          <div className="flex flex-col gap-2 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-top-1 motion-safe:duration-150">
            {succeeded ? (
              <>
                <div
                  role="status"
                  className="flex flex-col gap-0.5 rounded-[10px] bg-success-subtle px-3 py-2 text-success-text"
                >
                  <b className="text-compact font-semibold">
                    {fill(copy.done, {
                      count: numberLabel(progress?.created ?? 0),
                      name: boardName,
                    })}
                  </b>
                  <span className="text-metadata">
                    {fill(copy.doneBody, {
                      first: progress?.firstKey ?? "",
                      last: progress?.lastKey ?? "",
                    })}
                  </span>
                </div>
                <div className="flex flex-col gap-0.5 rounded-[10px] border border-line px-3 py-2">
                  <b className="text-compact font-semibold">{copy.doneAsk}</b>
                  <span className="text-metadata text-meta-foreground">
                    {copy.doneAskBody}
                  </span>
                </div>
              </>
            ) : failed ? (
              <>
                <div
                  role="alert"
                  className="flex flex-col gap-0.5 rounded-[10px] bg-danger-subtle px-3 py-2 text-danger-subtle-foreground"
                >
                  <b className="text-compact font-semibold">
                    {progress?.failureCode === "board-archived"
                      ? fill(copy.archivedTitle, {
                          name: progress.failureBy ?? "",
                        })
                      : progress?.failureCode === "board-gone"
                        ? copy.goneTitle
                        : copy.failedTitle}
                  </b>
                  <span className="text-metadata">
                    {progress?.failureCode === "board-archived"
                      ? copy.archivedBody
                      : progress?.failureCode === "board-gone"
                        ? copy.goneBody
                        : progress?.failureCode === "has-errors" &&
                            failedErrors.data?.errors?.[0]
                          ? fill(copy.failedHasErrors, {
                              row: numberLabel(
                                failedErrors.data.errors[0].row ?? 0,
                              ),
                              column: failedErrors.data.errors[0].column ?? "",
                              value:
                                failedErrors.data.errors[0].value ?? copy.empty,
                              reason: reasonText(failedErrors.data.errors[0]),
                            })
                          : copy.errorBody}
                  </span>
                </div>
                {progress?.failureCode === "has-errors" ? (
                  <p className="m-0 text-metadata text-meta-foreground">
                    {copy.failedRetryNote}
                  </p>
                ) : null}
              </>
            ) : (
              <div
                role="status"
                className="flex flex-col gap-1.5 rounded-[10px] border border-line px-3 py-3"
              >
                <b className="text-compact font-semibold">
                  {status === "running" && progress?.phase === "creating"
                    ? fill(copy.creating, {
                        done: numberLabel(progress.processed ?? 0),
                        total: numberLabel(total),
                      })
                    : copy.queued}
                </b>
                <div className="h-1.5 overflow-hidden rounded-full bg-state-hover-strong">
                  <div
                    className="h-full rounded-full bg-primary motion-safe:transition-[width] motion-safe:duration-300"
                    style={{
                      width: `${total ? Math.round(((progress?.processed ?? 0) / total) * 100) : 0}%`,
                    }}
                  />
                </div>
                <span className="text-metadata text-meta-foreground">
                  {copy.creatingBody}
                </span>
                <span className="text-metadata text-meta-foreground">
                  {copy.creatingNote}
                </span>
              </div>
            )}
          </div>
        ) : null}

        {lost ? (
          <div
            role="alert"
            className="flex flex-col gap-0.5 rounded-[10px] bg-danger-subtle px-3 py-2 text-danger-subtle-foreground"
          >
            <b className="text-compact font-semibold">{copy.lostTitle}</b>
            <span className="text-metadata">{copy.lostBody}</span>
          </div>
        ) : null}

        <DialogFooter className="max-sm:flex-col-reverse">
          {step === 1 ? (
            <>
              <button type="button" className={GHOST} onClick={onClose}>
                {copy.cancel}
              </button>
              <button type="button" className={PRIMARY} disabled>
                {copy.next}
              </button>
            </>
          ) : step === 2 ? (
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
          ) : step === 3 ? (
            <>
              <button
                type="button"
                className={GHOST}
                onClick={() => setStep(2)}
              >
                {copy.back}
              </button>
              {preview && !clean ? (
                <>
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
                    {copy.downloadReport}
                  </button>
                  <button type="button" className={GHOST} onClick={reset}>
                    {copy.chooseOther}
                  </button>
                </>
              ) : null}
              <button
                type="button"
                className={PRIMARY}
                disabled={!clean || sending}
                aria-disabled={!clean || sending}
                onClick={() => void send()}
              >
                {sending
                  ? copy.working
                  : pluralLifecycle(
                      c as Record<string, string>,
                      "submit",
                      rowCount,
                    )}
              </button>
            </>
          ) : succeeded ? (
            <>
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
                {copy.doneOpen}
              </button>
              <button type="button" className={PRIMARY} onClick={onClose}>
                {copy.finish}
              </button>
            </>
          ) : failed ? (
            <>
              {progress?.failureCode === "has-errors" && failedErrors.data ? (
                <button
                  type="button"
                  className={GHOST}
                  onClick={() => download(failedErrors.data?.errors ?? [])}
                >
                  {copy.downloadReport}
                </button>
              ) : null}
              <button type="button" className={GHOST} onClick={onClose}>
                {copy.close}
              </button>
              <button type="button" className={PRIMARY} onClick={reset}>
                {progress?.failureCode === "has-errors"
                  ? copy.chooseFixed
                  : copy.chooseOther}
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
                  {copy.retry}
                </button>
              ) : (
                <button
                  type="button"
                  className={PRIMARY}
                  disabled
                  aria-disabled
                >
                  {copy.working}
                </button>
              )}
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
