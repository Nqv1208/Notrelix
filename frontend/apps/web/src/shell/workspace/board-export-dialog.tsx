import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  CircleAlert,
  CircleCheck,
  Clock,
  Download,
  FileSpreadsheet,
  Info,
  LoaderCircle,
  RotateCcw,
  TriangleAlert,
} from "lucide-react";
import { endpoints } from "@notrelix/contracts";
import type { OperationResponse } from "@notrelix/contracts";
import { useLocale } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  fillLifecycleCopy as fill,
  pluralLifecycle,
  useBoardLifecycleCopy,
  useFilterChipLabels,
} from "@notrelix/work-management-web";
import {
  FrameBanner,
  FrameBody,
  FrameBox,
  FrameButton,
  FrameDialog,
  FrameField,
  FrameFooter,
  FrameHeader,
  FrameRadio,
  FrameRow,
} from "./lifecycle-dialog-frame";

type Summary = OperationResponse<"WorkManagement.Exports.GetSummary", 200>;
type Job = OperationResponse<"WorkManagement.Exports.Get", 200>;
type Jobs = OperationResponse<"WorkManagement.Exports.List", 200>;
type Format = "csv" | "xlsx";
type Scope = "view" | "all";

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
  const chips = useFilterChipLabels();
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
  const expiresLabel = (iso: string) => {
    const d = new Date(iso);
    const two = (n: number) => String(n).padStart(2, "0");
    return `${two(d.getDate())}/${two(d.getMonth() + 1)} ${two(d.getHours())}:${two(d.getMinutes())}`;
  };

  const running = status === "queued" || status === "running";
  const summaryLine = fill(copy.summaryLine, {
    format: (job.data?.format ?? format).toUpperCase(),
    scope:
      (job.data?.scope ?? scope) === "all"
        ? copy.summaryAllShort
        : copy.summaryViewShort,
    count: number(job.data?.rowCount ?? count),
  });
  const title =
    status === "ready"
      ? copy.readyTitle
      : status === "expired"
        ? copy.expiredTitle
        : status === "failed" || jobId === null
          ? copy.title
          : status === "queued"
            ? copy.preparingTitle
            : copy.running;
  const fileChip = (tone: "ok" | "bad", meta: string) => (
    <div className="flex items-center gap-3 rounded-[10px] border border-line bg-popover px-3.5 py-2.5">
      <span className="grid h-10 w-9 flex-none place-items-center rounded-[7px] bg-success-subtle text-success-text">
        <FileSpreadsheet
          aria-hidden="true"
          className="size-4"
          data-tone={tone}
        />
      </span>
      <span className="min-w-0 flex-1">
        <b className="block truncate text-frame-body font-semibold">
          {job.data?.fileName}
        </b>
        <small className="text-frame-note text-meta-foreground">{meta}</small>
      </span>
      {tone === "ok" ? (
        <FrameButton
          onClick={again}
          className="max-sm:min-w-0 max-sm:flex-none"
        >
          {copy.again}
        </FrameButton>
      ) : null}
    </div>
  );
  const time = (iso: string) =>
    new Date(iso).toLocaleTimeString(locale, {
      hour: "2-digit",
      minute: "2-digit",
    });

  return (
    <FrameDialog
      open={open}
      onOpenChange={(next) => (next || sending ? undefined : onClose())}
    >
      <FrameHeader
        title={title}
        description={
          jobId === null && status === null
            ? fill(copy.intro, { name: boardName })
            : undefined
        }
        onClose={running ? undefined : onClose}
        closeLabel={copy.close}
        closeDisabled={sending}
      />

      {jobId === null ? (
        <FrameBody>
          <FrameField
            label={copy.formatLabel}
            className="grid grid-cols-2 max-sm:grid-cols-1"
          >
            {(["csv", "xlsx"] as const).map((f) => (
              <FrameRadio
                key={f}
                checked={format === f}
                name="format"
                onSelect={() => setFormat(f)}
                title={c[f] ?? f}
                body={c[`${f}Body`] ?? ""}
              />
            ))}
          </FrameField>
          <FrameField label={copy.scopeLabel}>
            <FrameRadio
              checked={scope === "view"}
              name="scope"
              onSelect={() => setScope("view")}
              title={fill(copy.scopeView, {
                view: viewName,
                count: summary.isSuccess ? number(viewCount) : "…",
              })}
              body={copy.scopeViewBody}
              extra={
                chips.length > 0 ? (
                  <span className="mt-1.5 flex flex-wrap gap-1.5">
                    {chips.map((chip) => (
                      <span
                        key={chip}
                        className="inline-flex h-[22px] items-center rounded-md bg-state-selected px-2 text-xs font-semibold text-state-selected-foreground"
                      >
                        {chip}
                      </span>
                    ))}
                  </span>
                ) : undefined
              }
            />
            <FrameRadio
              checked={scope === "all"}
              name="scope"
              onSelect={() => setScope("all")}
              title={fill(copy.scopeAll, {
                count: summary.isSuccess ? number(allCount) : "…",
              })}
              body={copy.scopeAllBody}
            />
          </FrameField>
          <FrameBanner
            tone="info"
            icon={<Info aria-hidden="true" />}
            title={copy.visibleTitle}
            body={
              summary.data?.hiddenFields?.length
                ? fill(copy.hidden, {
                    count: summary.data.hiddenFields.length,
                    names: summary.data.hiddenFields.join(", "),
                  })
                : copy.visibleBody
            }
          />
          {emptyView ? (
            <FrameBanner
              tone="warn"
              icon={<TriangleAlert aria-hidden="true" />}
              title={copy.emptyTitle}
              body={copy.emptyBody}
            />
          ) : null}
          {guestDenied || failure === "denied" ? (
            <FrameBanner
              tone="err"
              role="alert"
              icon={<CircleAlert aria-hidden="true" />}
              title={copy.denied}
            />
          ) : summary.isError ? (
            <FrameBanner
              tone="err"
              role="alert"
              icon={<CircleAlert aria-hidden="true" />}
              title={copy.summaryFailed}
            />
          ) : failure === "error" ? (
            <FrameBanner
              tone="err"
              role="alert"
              icon={<CircleAlert aria-hidden="true" />}
              title={copy.failedTitle}
              body={copy.failedBody}
            />
          ) : null}
        </FrameBody>
      ) : (
        <FrameBody className="motion-safe:animate-in motion-safe:fade-in-0 motion-safe:duration-150">
          {running ? (
            <FrameBox>
              <FrameRow>
                <span className="flex min-w-0 flex-1 flex-col gap-0.5">
                  <small className="text-frame-note leading-[1.4] text-meta-foreground">
                    {copy.summaryLabel}
                  </small>
                  <b className="text-frame-body font-semibold">{summaryLine}</b>
                </span>
              </FrameRow>
            </FrameBox>
          ) : null}
          {status === "queued" ? (
            <FrameBanner
              tone="info"
              icon={<Clock aria-hidden="true" />}
              title={copy.queuedBanner}
              body={copy.queuedBody}
            />
          ) : status === "running" ? (
            <FrameBox>
              <FrameRow top>
                <span className="mt-0.5 text-info-subtle-foreground">
                  <LoaderCircle
                    aria-hidden="true"
                    className="size-4 motion-safe:animate-spin"
                  />
                </span>
                <span className="flex min-w-0 flex-1 flex-col gap-0.5">
                  <b className="text-frame-body font-semibold">
                    {fill(copy.runningLine, {
                      done: number(job.data?.processed ?? 0),
                      total: number(job.data?.rowCount ?? 0),
                    })}
                  </b>
                  <span
                    role="progressbar"
                    aria-valuemin={0}
                    aria-valuemax={job.data?.rowCount ?? 0}
                    aria-valuenow={job.data?.processed ?? 0}
                    className="block h-2 w-full overflow-hidden rounded-[4px] bg-state-hover-strong"
                  >
                    <span
                      className="block h-full rounded-[4px] bg-primary motion-safe:transition-[width] motion-safe:duration-300"
                      style={{
                        width: `${job.data?.rowCount ? Math.round(((job.data.processed ?? 0) / job.data.rowCount) * 100) : 0}%`,
                      }}
                    />
                  </span>
                  <small className="text-frame-note leading-[1.4] text-meta-foreground">
                    {copy.runningNote}
                  </small>
                </span>
              </FrameRow>
            </FrameBox>
          ) : status === "ready" ? (
            <>
              <FrameBanner
                tone="ok"
                icon={<CircleCheck aria-hidden="true" />}
                title={copy.readyBannerTitle}
                body={fill(copy.readyBannerBody, {
                  rows: number(job.data?.rowCount ?? 0),
                })}
              />
              {fileChip(
                "ok",
                fill(copy.readyMeta, {
                  format: (job.data?.format ?? "").toUpperCase(),
                  size: sizeLabel(job.data?.size ?? 0),
                  time: time(job.data?.createdAt ?? new Date().toISOString()),
                }),
              )}
            </>
          ) : status === "expired" ? (
            <>
              <FrameBanner
                tone="warn"
                icon={<TriangleAlert aria-hidden="true" />}
                title={copy.expired}
                body={copy.expiredBannerBody}
              />
              {fileChip(
                "bad",
                fill(copy.fileMeta, {
                  format: (job.data?.format ?? "").toUpperCase(),
                  size: sizeLabel(job.data?.size ?? 0),
                  expires: expiresLabel(
                    job.data?.expiresAt ?? new Date().toISOString(),
                  ),
                }),
              )}
            </>
          ) : (
            <FrameBanner
              tone="err"
              role="alert"
              icon={<CircleAlert aria-hidden="true" />}
              title={copy.failedTitle}
              body={
                job.data?.failureCode === "no-access"
                  ? copy.failedNoAccess
                  : copy.failedBody
              }
            />
          )}
        </FrameBody>
      )}

      <FrameFooter>
        {jobId === null ? (
          <>
            <FrameButton onClick={onClose}>{copy.cancel}</FrameButton>
            <FrameButton
              primary
              disabled={
                sending || !summary.isSuccess || emptyView || guestDenied
              }
              onClick={() => void submit()}
            >
              {sending ? (
                copy.working
              ) : summary.isSuccess && count > 0 ? (
                <>
                  <Download aria-hidden="true" className="size-4" />
                  {pluralLifecycle(c, "submit", count)}
                </>
              ) : (
                <>
                  <Download aria-hidden="true" className="size-4" />
                  {copy.submitNone}
                </>
              )}
            </FrameButton>
          </>
        ) : status === "ready" ? (
          <>
            <FrameButton onClick={onClose}>{copy.close}</FrameButton>
            <FrameButton primary autoFocus onClick={() => void download()}>
              <Download aria-hidden="true" className="size-4" />
              {copy.download}
            </FrameButton>
          </>
        ) : status === "failed" || status === "expired" ? (
          <>
            <FrameButton onClick={onClose}>{copy.close}</FrameButton>
            <FrameButton primary onClick={again}>
              <RotateCcw aria-hidden="true" className="size-4" />
              {status === "failed" ? copy.retry : copy.again}
            </FrameButton>
          </>
        ) : (
          <>
            <FrameButton onClick={onClose}>{copy.close}</FrameButton>
            <FrameButton primary disabled>
              {status === "queued" ? (
                copy.queuedButton
              ) : (
                <>
                  <Download aria-hidden="true" className="size-4" />
                  {copy.download}
                </>
              )}
            </FrameButton>
          </>
        )}
      </FrameFooter>
    </FrameDialog>
  );
}
