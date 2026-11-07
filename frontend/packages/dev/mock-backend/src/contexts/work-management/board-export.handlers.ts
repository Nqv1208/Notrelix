/**
 * Work Management — the board export dialog's reads and writes (WM-Export, UC-WM-B31):
 *
 *   GET  /boards/:boardId/exports/summary  — how many items the view and the whole board export
 *   GET  /boards/:boardId/exports          — the person's recent exports, newest first
 *   POST /boards/:boardId/exports          — queue one
 *   GET  /boards/:boardId/exports/:jobId   — one job: queued on the first read, running on the second, then ready
 *
 * The file itself is not served: the dialog's download is not part of the measured frames.
 */
import { defineMockOperation } from "../../operations/types";
import { created, notFound, ok } from "../../transport/create-response";
import { canReadBoard } from "./boards.handlers";

interface MockExportJob {
  id: string;
  status: "queued" | "running" | "ready" | "expired" | "failed";
  format: string;
  scope: string;
  fileName: string;
  rowCount: number;
  processed: number;
  size: number;
  expiresAt: string | null;
  failureCode: string | null;
  createdAt: string;
}

/** The mock world's board is smaller than the artboard's; these are the artboard's counts. */
const VIEW_COUNT = 37;
const ALL_COUNT = 212;
/**
 * A job is queued, then running, then ready, by how often it was read (the dialog polls twice a second; the mock
 * clock stands still), so a spec can hold each state for a moment.
 */
const RUNNING_AFTER_READS = 3;
const READY_AFTER_READS = 9;
const jobs = new Map<string, MockExportJob[]>();
const reads = new Map<string, number>();

function present(job: MockExportJob) {
  return job;
}

/**
 * A spec can hold the job in one state (`globalThis.__mockExportStage`), with the artboard's file: 5.000 rows,
 * 1,2 MB, made at 10:42 on 31/10/2026. Absent, the job moves by age.
 */
function hold(job: MockExportJob, stage: string): MockExportJob {
  const made = "2026-10-31T10:42:00+07:00";
  job.rowCount = 5_000;
  job.size = 1_258_291;
  job.createdAt = made;
  job.status = stage as MockExportJob["status"];
  job.processed = stage === "running" ? 2_400 : stage === "queued" ? 0 : 5_000;
  if (stage === "ready" || stage === "expired") {
    job.fileName = `Triển khai ERP Q4 - ${stage === "ready" ? "31" : "30"}-10-2026.xlsx`;
    job.expiresAt = stage === "ready" ? "2026-11-01T10:42:00+07:00" : made;
  }
  if (stage === "failed") job.failureCode = "server";
  return job;
}

function advance(
  job: MockExportJob,
  store: { getClock(): { offsetDays(days: number): string } },
): MockExportJob {
  const stage = (globalThis as { __mockExportStage?: string })
    .__mockExportStage;
  if (stage) return hold(job, stage);
  const read = (reads.get(job.id) ?? 0) + 1;
  reads.set(job.id, read);
  if (read >= RUNNING_AFTER_READS) {
    job.status = "running";
    job.processed = Math.round(job.rowCount * 0.48);
  }
  if (read >= READY_AFTER_READS) {
    job.status = "ready";
    job.processed = job.rowCount;
    job.size = job.rowCount * 118;
    job.expiresAt = store.getClock().offsetDays(1);
  }
  return job;
}

export const boardExportOperations = [
  defineMockOperation<{ boardId: string }, never, unknown>({
    id: "wm.exports.summary",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Exports.GetSummary",
    },
    method: "GET",
    route: "/boards/:boardId/exports/summary",
    async handle({ params, store }) {
      if (!canReadBoard(params.boardId, store)) return notFound();
      const held = (globalThis as { __mockExportViewCount?: number })
        .__mockExportViewCount;
      return ok({
        viewCount: held ?? VIEW_COUNT,
        allCount: ALL_COUNT,
        hiddenFields: ["Ngân sách"],
      });
    },
  }),

  defineMockOperation<{ boardId: string }, never, unknown>({
    id: "wm.exports.list",
    contract: { kind: "openapi", operationId: "WorkManagement.Exports.List" },
    method: "GET",
    route: "/boards/:boardId/exports",
    async handle({ params, store }) {
      if (!canReadBoard(params.boardId, store)) return notFound();
      return ok(
        (jobs.get(params.boardId) ?? []).map((j) => present(advance(j, store))),
      );
    },
  }),

  defineMockOperation<
    { boardId: string },
    { format?: string; scope?: string },
    unknown
  >({
    id: "wm.exports.create",
    contract: { kind: "openapi", operationId: "WorkManagement.Exports.Create" },
    method: "POST",
    route: "/boards/:boardId/exports",
    async handle({ params, store, body }) {
      if (!canReadBoard(params.boardId, store)) return notFound();
      const format = body.format === "xlsx" ? "xlsx" : "csv";
      const scope = body.scope === "all" ? "all" : "view";
      const job: MockExportJob = {
        id: store.nextId("export"),
        status: "queued",
        format,
        scope,
        fileName: `trien-khai-erp-q4.${format}`,
        rowCount: scope === "all" ? ALL_COUNT : VIEW_COUNT,
        processed: 0,
        size: 0,
        expiresAt: null,
        failureCode: null,
        createdAt: store.getClock().isoNow(),
      };
      jobs.set(params.boardId, [job, ...(jobs.get(params.boardId) ?? [])]);
      return created(present(job));
    },
  }),

  defineMockOperation<{ boardId: string; jobId: string }, never, unknown>({
    id: "wm.exports.get",
    contract: { kind: "openapi", operationId: "WorkManagement.Exports.Get" },
    method: "GET",
    route: "/boards/:boardId/exports/:jobId",
    async handle({ params, store }) {
      if (!canReadBoard(params.boardId, store)) return notFound();
      const job = jobs.get(params.boardId)?.find((j) => j.id === params.jobId);
      return job ? ok(present(advance(job, store))) : notFound();
    },
  }),
];
