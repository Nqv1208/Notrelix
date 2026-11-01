import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { CircleAlert, Download, FileSpreadsheet, X } from "lucide-react";
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
  cn,
} from "@notrelix/ui-web";

type Summary = OperationResponse<"WorkManagement.Exports.GetSummary", 200>;
type Job = OperationResponse<"WorkManagement.Exports.Get", 200>;
type Jobs = OperationResponse<"WorkManagement.Exports.List", 200>;
type Format = "csv" | "xlsx";
type Scope = "view" | "all";

const BUTTON =
  "inline-flex h-8 items-center justify-center gap-1.5 rounded-[7px] border px-3 text-compact font-medium outline-none focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-not-allowed disabled:opacity-60 max-sm:h-11";
const GHOST = `${BUTTON} border-line-strong bg-popover text-foreground hover:bg-state-hover-strong`;
const PRIMARY = `${BUTTON} border-primary bg-primary font-semibold text-primary-foreground hover:opacity-90`;

/**
 * Export a board or its current view to CSV or XLSX (WM-Export, UC-WM-B31). The file is made on the server in the
 * background and lands in the person's inbox when ready; it is theirs alone and expires after 24 hours. The
 * dialog can be closed while it is made: reopening shows the latest export again.
 */
export function BoardExportDialog({
  boardId,
  boardName,
  viewName,
  filter,
  sort,
  open,
  onClose,
}: {
  boardId: string;
  boardName: string;
  viewName: string;
  filter: string | undefined;
  sort: string | undefined;
  open: boolean;
  onClose: () => void;
}) {
  const copy = useBoardLifecycleCopy().export;
  const c = copy as Record<string, string>;
  const { api } = useAppRuntime();
  const { locale } = useLocale();
  const [format, setFormat] = useState<Format>("csv");
  const [scope, setScope] = useState<Scope>("view");
  const [jobId, setJobId] = useState<string | null>(null);
  const [sending, setSending] = useState(false);
  const [failure, setFailure] = useState<"denied" | "error" | null>(null);
  const [attempt, setAttempt] = useState(() => crypto.randomUUID());

  const summary = useQuery({
    queryKey: [
      "work-management",
      "board-export-summary",
      boardId,
      filter ?? "",
      sort ?? "",
    ],
    enabled: open,
    gcTime: 0,
    staleTime: 0,
    retry: false,
    queryFn: () => {
      const q = new URLSearchParams();
      if (filter) q.set("filter", filter);
      if (sort) q.set("sort", sort);
      const qs = q.toString();
      return api.api.get<Summary>(
        `${endpoints.boards.exportSummary(boardId)}${qs ? `?${qs}` : ""}`,
        { skipGlobalErrorToast: true },
      );
    },
  });
  const recent = useQuery({
    queryKey: ["work-management", "board-export-recent", boardId],
    enabled: open,
    gcTime: 0,
    staleTime: 0,
    retry: false,
    queryFn: () =>
      api.api.get<Jobs>(endpoints.boards.exports(boardId), {
        skipGlobalErrorToast: true,
      }),
  });
  useEffect(() => {
    if (!open) return;
    setFailure(null);
    setSending(false);
    setAttempt(crypto.randomUUID());
    setJobId(null);
  }, [open]);
  useEffect(() => {
    if (!open || jobId !== null || !recent.data) return;
    const latest = recent.data[0];
    if (
      latest?.id &&
      (latest.status === "queued" ||
        latest.status === "running" ||
        latest.status === "ready")
    )
      setJobId(latest.id);
  }, [open, jobId, recent.data]);

  const job = useQuery({
    queryKey: ["work-management", "board-export-job", boardId, jobId],
    enabled: open && jobId !== null,
    gcTime: 0,
    retry: false,
    refetchInterval: (query) => {
      const status = (query.state.data as Job | undefined)?.status;
      return status === "ready" || status === "failed" || status === "expired"
        ? false
        : 600;
    },
    queryFn: () =>
      api.api.get<Job>(endpoints.boards.exportJob(boardId, jobId!), {
        skipGlobalErrorToast: true,
      }),
  });
  const status = job.data?.status ?? (jobId ? "queued" : null);

  const viewCount = summary.data?.viewCount ?? 0;
  const allCount = summary.data?.allCount ?? 0;
  const count = scope === "view" ? viewCount : allCount;
  const emptyView = summary.isSuccess && scope === "view" && viewCount === 0;
  const guestDenied =
    summary.isError &&
    (summary.error as { status?: number } | null)?.status === 403;
  const number = (n: number) => new Intl.NumberFormat(locale).format(n);

  async function submit() {
    if (sending || !summary.isSuccess || emptyView) return;
    setSending(true);
    setFailure(null);
    try {
      const created = await api.api.post<Job>(
        endpoints.boards.exports(boardId),
        {
          format,
          scope,
          filter: scope === "view" ? (filter ?? null) : null,
          sort: scope === "view" ? (sort ?? null) : null,
          locale: locale === "en" ? "en" : "vi",
        },
        { idempotencyKey: attempt, skipGlobalErrorToast: true },
      );
      setJobId(created.id ?? null);
    } catch (error) {
      const code = (error as { status?: number } | null)?.status;
      setFailure(code === 403 || code === 404 ? "denied" : "error");
      setAttempt(crypto.randomUUID());
    } finally {
      setSending(false);
    }
  }

  async function download() {
    if (!jobId) return;
    const blob = await api.api.getBlob(
      endpoints.boards.exportFile(boardId, jobId),
      { skipGlobalErrorToast: true },
    );
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = job.data?.fileName ?? "export";
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  const again = () => {
    setJobId(null);
    setAttempt(crypto.randomUUID());
  };
  const sizeLabel = (bytes: number) =>
    bytes >= 1024 * 1024
      ? `${new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }).format(bytes / (1024 * 1024))} MB`
      : `${Math.max(1, Math.round(bytes / 1024))} KB`;
  const expiresLabel = (iso: string) =>
    new Date(iso).toLocaleString(locale, {
      day: "2-digit",
      month: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    });

  const running = status === "queued" || status === "running";
  const sheetClass =
    "gap-3 sm:max-w-[600px] max-sm:top-auto max-sm:bottom-0 max-sm:max-w-full max-sm:translate-y-0 max-sm:rounded-b-none";

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => (next || sending ? undefined : onClose())}
    >
      <DialogContent showCloseButton={false} className={sheetClass}>
        <DialogHeader>
          <div className="flex items-start gap-3">
            <div className="flex-1">
              <DialogTitle>
                {status === "ready"
                  ? copy.ready
                  : status === "failed"
                    ? copy.failedTitle
                    : status === "expired"
                      ? copy.expired
                      : running
                        ? copy.running
                        : copy.title}
              </DialogTitle>
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

        {jobId === null ? (
          <div className="flex flex-col gap-3">
            <fieldset className="m-0 flex flex-col gap-1.5 border-0 p-0">
              <legend className="mb-1.5 text-compact font-semibold">
                {copy.formatLabel}
              </legend>
              <div
                role="radiogroup"
                aria-label={copy.formatLabel}
                className="grid grid-cols-2 gap-2 max-sm:grid-cols-1"
              >
                {(["csv", "xlsx"] as const).map((f) => (
                  <Choice
                    key={f}
                    checked={format === f}
                    name="format"
                    onSelect={() => setFormat(f)}
                    title={c[f] ?? f}
                    body={c[`${f}Body`] ?? ""}
                    icon={
                      <FileSpreadsheet aria-hidden="true" className="size-4" />
                    }
                  />
                ))}
              </div>
            </fieldset>
            <fieldset className="m-0 flex flex-col gap-1.5 border-0 p-0">
              <legend className="mb-1.5 text-compact font-semibold">
                {copy.scopeLabel}
              </legend>
              <div
                role="radiogroup"
                aria-label={copy.scopeLabel}
                className="flex flex-col gap-2"
              >
                <Choice
                  checked={scope === "view"}
                  name="scope"
                  onSelect={() => setScope("view")}
                  title={fill(copy.scopeView, {
                    view: viewName,
                    count: summary.isSuccess ? number(viewCount) : "…",
                  })}
                  body={copy.scopeViewBody}
                />
                <Choice
                  checked={scope === "all"}
                  name="scope"
                  onSelect={() => setScope("all")}
                  title={fill(copy.scopeAll, {
                    count: summary.isSuccess ? number(allCount) : "…",
                  })}
                  body={copy.scopeAllBody}
                />
              </div>
            </fieldset>
            <div className="flex flex-col gap-0.5 rounded-[10px] border border-line bg-background px-3 py-2.5">
              <b className="text-compact font-semibold">{copy.visibleTitle}</b>
              <span className="text-metadata text-meta-foreground">
                {summary.data?.hiddenFields?.length
                  ? fill(copy.hidden, {
                      count: summary.data.hiddenFields.length,
                      names: summary.data.hiddenFields.join(", "),
                    })
                  : copy.visibleBody}
              </span>
            </div>
            {emptyView ? (
              <div
                role="status"
                className="flex flex-col gap-0.5 rounded-[10px] bg-warning-subtle px-3 py-2 text-warning-text"
              >
                <b className="text-compact font-semibold">{copy.emptyTitle}</b>
                <span className="text-metadata">{copy.emptyBody}</span>
              </div>
            ) : null}
            {guestDenied || failure === "denied" ? (
              <p role="alert" className="m-0 text-compact text-danger-text">
                {copy.denied}
              </p>
            ) : summary.isError ? (
              <p role="alert" className="m-0 text-compact text-danger-text">
                {copy.summaryFailed}
              </p>
            ) : failure === "error" ? (
              <p role="alert" className="m-0 text-compact text-danger-text">
                {copy.failedBody}
              </p>
            ) : null}
          </div>
        ) : (
          <div className="flex flex-col gap-2 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:duration-150">
            {running ? (
              <div
                role="status"
                className="flex flex-col gap-1.5 rounded-[10px] border border-line px-3 py-3"
              >
                <b className="text-compact font-semibold">
                  {status === "queued" ? copy.queued : copy.running}
                </b>
                <div className="h-1.5 overflow-hidden rounded-full bg-state-hover-strong">
                  <div
                    className="h-full rounded-full bg-primary motion-safe:transition-[width] motion-safe:duration-300"
                    style={{
                      width: `${job.data?.rowCount ? Math.round(((job.data.processed ?? 0) / job.data.rowCount) * 100) : 0}%`,
                    }}
                  />
                </div>
                {job.data?.rowCount ? (
                  <span className="text-metadata text-meta-foreground">
                    {fill(copy.progress, {
                      done: number(job.data.processed ?? 0),
                      total: number(job.data.rowCount),
                    })}
                  </span>
                ) : null}
                <span className="text-metadata text-meta-foreground">
                  {copy.runningBody}
                </span>
              </div>
            ) : status === "ready" ? (
              <div
                role="status"
                className="flex flex-col gap-1 rounded-[10px] bg-success-subtle px-3 py-2.5 text-success-text"
              >
                <b className="text-compact font-semibold">
                  {job.data?.fileName}
                </b>
                <span className="text-metadata">
                  {fill(copy.fileMeta, {
                    format: (job.data?.format ?? "").toUpperCase(),
                    size: sizeLabel(job.data?.size ?? 0),
                    expires: expiresLabel(
                      job.data?.expiresAt ?? new Date().toISOString(),
                    ),
                  })}
                </span>
                <span className="text-metadata">{copy.readyBody}</span>
              </div>
            ) : status === "expired" ? (
              <div
                role="status"
                className="flex flex-col gap-0.5 rounded-[10px] border border-line px-3 py-2.5"
              >
                <b className="text-compact font-semibold">{copy.expired}</b>
                <span className="text-metadata text-meta-foreground">
                  {copy.expiredBody}
                </span>
              </div>
            ) : (
              <div
                role="alert"
                className="flex flex-col gap-0.5 rounded-[10px] bg-danger-subtle px-3 py-2.5 text-danger-subtle-foreground"
              >
                <b className="flex items-center gap-1.5 text-compact font-semibold">
                  <CircleAlert aria-hidden="true" className="size-4" />
                  {copy.failedTitle}
                </b>
                <span className="text-metadata">
                  {job.data?.failureCode === "no-access"
                    ? copy.failedNoAccess
                    : copy.failedBody}
                </span>
              </div>
            )}
          </div>
        )}

        <DialogFooter className="max-sm:flex-col-reverse">
          {jobId === null ? (
            <>
              <button type="button" className={GHOST} onClick={onClose}>
                {copy.cancel}
              </button>
              <button
                type="button"
                className={PRIMARY}
                disabled={
                  sending || !summary.isSuccess || emptyView || guestDenied
                }
                aria-disabled={
                  sending || !summary.isSuccess || emptyView || guestDenied
                }
                onClick={() => void submit()}
              >
                {sending
                  ? copy.working
                  : summary.isSuccess && count > 0
                    ? pluralLifecycle(
                        c as Record<string, string>,
                        "submit",
                        count,
                      )
                    : copy.submitNone}
              </button>
            </>
          ) : status === "ready" ? (
            <>
              <button type="button" className={GHOST} onClick={onClose}>
                {copy.close}
              </button>
              <button type="button" className={GHOST} onClick={again}>
                {copy.again}
              </button>
              <button
                type="button"
                className={PRIMARY}
                autoFocus
                onClick={() => void download()}
              >
                <Download aria-hidden="true" className="size-4" />
                {copy.download}
              </button>
            </>
          ) : status === "failed" || status === "expired" ? (
            <>
              <button type="button" className={GHOST} onClick={onClose}>
                {copy.close}
              </button>
              <button type="button" className={PRIMARY} onClick={again}>
                {status === "failed" ? copy.retry : copy.again}
              </button>
            </>
          ) : (
            <button type="button" className={GHOST} onClick={onClose}>
              {copy.close}
            </button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function Choice({
  checked,
  name,
  onSelect,
  title,
  body,
  icon,
}: {
  checked: boolean;
  name: string;
  onSelect: () => void;
  title: string;
  body: string;
  icon?: React.ReactNode;
}) {
  return (
    <label
      className={cn(
        "flex cursor-pointer items-start gap-2.5 rounded-[10px] border px-3 py-2.5 has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-focus-ring",
        checked ? "border-primary bg-state-hover-strong" : "border-line",
      )}
    >
      <input
        type="radio"
        name={name}
        checked={checked}
        onChange={onSelect}
        className="mt-0.5 size-4 accent-primary"
      />
      <span className="flex-1">
        <b className="flex items-center gap-1.5 text-compact font-semibold">
          {icon}
          {title}
        </b>
        <span className="block text-metadata text-meta-foreground">{body}</span>
      </span>
    </label>
  );
}
