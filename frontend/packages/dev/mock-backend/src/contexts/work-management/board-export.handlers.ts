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
  status: "queued" | "running" | "ready";
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
/** A job is queued, then running, then ready, by age, so a spec can hold each state for a moment. */
const RUNNING_AFTER_MS = 1_500;
const READY_AFTER_MS = 4_500;
const jobs = new Map<string, MockExportJob[]>();

function present(job: MockExportJob) {
  return job;
}

function advance(job: MockExportJob): MockExportJob {
  const age = Date.now() - Date.parse(job.createdAt);
  if (age >= RUNNING_AFTER_MS) {
    job.status = "running";
    job.processed = Math.round(job.rowCount * 0.48);
  }
  if (age >= READY_AFTER_MS) {
    job.status = "ready";
    job.processed = job.rowCount;
    job.size = job.rowCount * 118;
    job.expiresAt = new Date(Date.now() + 24 * 3_600_000).toISOString();
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
      return ok({
        viewCount: VIEW_COUNT,
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
        (jobs.get(params.boardId) ?? []).map((j) => present(advance(j))),
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
        id: crypto.randomUUID(),
        status: "queued",
        format,
        scope,
        fileName: `trien-khai-erp-q4.${format}`,
        rowCount: scope === "all" ? ALL_COUNT : VIEW_COUNT,
        processed: 0,
        size: 0,
        expiresAt: null,
        failureCode: null,
        createdAt: new Date().toISOString(),
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
      return job ? ok(present(advance(job))) : notFound();
    },
  }),
];
