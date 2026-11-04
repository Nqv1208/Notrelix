/**
 * Work Management — the board import dialog's reads and writes (WM-Import, UC-WM-B29/B30). The mock has no file
 * reader: the uploaded file's NAME picks what the server would have found in it.
 *
 *   POST /boards/:boardId/imports                — upload: a PDF is refused, `khach-hang-day-du` has 5.012 rows,
 *                                                  `tep-rong` has none, anything else reads as 5.000 rows
 *   POST /boards/:boardId/imports/:jobId/preview — the mapping's check, all clean
 *   POST /boards/:boardId/imports/:jobId/confirm — the job, done at once
 *   GET  /boards/:boardId/imports/:jobId         — one job
 *
 * `globalThis.__mockImportHold` keeps an upload open (the dialog reading the file) for a spec to measure.
 */
import { defineMockOperation } from "../../operations/types";
import { created, notFound, ok } from "../../transport/create-response";
import { canReadBoard } from "./boards.handlers";

const MB = 1024 * 1024;
const files = new Map<string, { fileName: string; rows: number }>();

function targets() {
  return [
    { id: "$title", name: "Tên việc", type: "text", required: true },
    { id: "$group", name: "Nhóm", type: "group", required: false },
  ];
}

export const boardImportOperations = [
  defineMockOperation<{ boardId: string }, never, unknown>({
    id: "wm.imports.upload",
    contract: { kind: "openapi", operationId: "WorkManagement.Imports.Upload" },
    method: "POST",
    route: "/boards/:boardId/imports",
    async handle({ params, store, request }) {
      if (!canReadBoard(params.boardId, store)) return notFound();
      if ((globalThis as { __mockImportHold?: boolean }).__mockImportHold)
        await new Promise<never>(() => undefined);
      const part = request.formData?.get("file");
      const fileName = part && typeof part !== "string" ? part.name : "tep";
      const base = { fileName, format: "xlsx", columnCount: 12 };
      if (/\.pdf$/i.test(fileName))
        return ok({ ...base, rejection: "unsupported-type", size: 380 * 1024 });
      if (/day-du/.test(fileName))
        return ok({
          ...base,
          rejection: "too-many-rows",
          rowCount: 5_012,
          size: Math.round(2.1 * MB),
        });
      if (/tep-rong/.test(fileName))
        return ok({ ...base, rejection: "no-rows", rowCount: 0, size: 4096 });
      const jobId = crypto.randomUUID();
      files.set(jobId, { fileName, rows: 5_000 });
      return ok({
        ...base,
        jobId,
        rowCount: 5_000,
        size: Math.round(1.2 * MB),
        columns: [
          { index: 0, header: "Tên khách hàng", examples: ["Công ty An"] },
        ],
        targets: targets(),
        suggested: [{ column: 0, target: "$title" }],
      });
    },
  }),

  defineMockOperation<{ boardId: string; jobId: string }, never, unknown>({
    id: "wm.imports.preview",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Imports.Preview",
    },
    method: "POST",
    route: "/boards/:boardId/imports/:jobId/preview",
    async handle({ params, store }) {
      const file = files.get(params.jobId);
      if (!file || !canReadBoard(params.boardId, store)) return notFound();
      return ok({
        rowCount: file.rows,
        missingTitle: false,
        checks: [{ column: 0, target: "$title", status: "ok", count: 0 }],
        errorCount: 0,
        errorRows: 0,
        errors: [],
        sample: [],
        unmatchedPersonRows: [],
      });
    },
  }),

  defineMockOperation<{ boardId: string; jobId: string }, never, unknown>({
    id: "wm.imports.confirm",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Imports.Confirm",
    },
    method: "POST",
    route: "/boards/:boardId/imports/:jobId/confirm",
    async handle({ params, store }) {
      const file = files.get(params.jobId);
      if (!file || !canReadBoard(params.boardId, store)) return notFound();
      return created({
        id: params.jobId,
        status: "succeeded",
        total: file.rows,
        processed: file.rows,
        created: file.rows,
        fileName: file.fileName,
      });
    },
  }),

  defineMockOperation<{ boardId: string; jobId: string }, never, unknown>({
    id: "wm.imports.get",
    contract: { kind: "openapi", operationId: "WorkManagement.Imports.Get" },
    method: "GET",
    route: "/boards/:boardId/imports/:jobId",
    async handle({ params, store }) {
      const file = files.get(params.jobId);
      if (!file || !canReadBoard(params.boardId, store)) return notFound();
      return ok({
        id: params.jobId,
        status: "succeeded",
        total: file.rows,
        processed: file.rows,
        created: file.rows,
        fileName: file.fileName,
      });
    },
  }),
];
