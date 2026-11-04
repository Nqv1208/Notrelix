/**
 * Work Management — the board import dialog's reads and writes (WM-Import, UC-WM-B29/B30). The mock has no file
 * reader: the uploaded file's NAME picks what the server would have found in it.
 *
 *   POST /boards/:boardId/imports                — upload: a PDF is refused, `khach-hang-day-du` has 5.012 rows,
 *                                                  `tep-rong` has none, anything else reads as 5.000 rows
 *   POST /boards/:boardId/imports/:jobId/preview — the mapping's check
 *   POST /boards/:boardId/imports/:jobId/confirm — the job
 *   GET  /boards/:boardId/imports/:jobId         — one job
 *   GET  /boards/:boardId/imports/:jobId/errors  — the errors a refused job reports
 *
 * What the name holds picks the story: `cot-loi` has a column that cannot map (and a value not in the options),
 * `thieu-ten` has no column for the title, `co-loi` previews with 12 errors in 9 rows; at confirm `dang-tao` stays
 * at 3.200 of 5.000, `gan-xong` at 4.900, `bi-tu-choi` is refused at row 3.480, `luu-tru` finds the board archived,
 * `mat-mang` loses the connection; any other name is checked clean and done at once.
 *
 * `globalThis.__mockImportHold` keeps an upload open (the dialog reading the file) for a spec to measure.
 */
import { defineMockOperation } from "../../operations/types";
import {
  created,
  notFound,
  ok,
  serverError,
} from "../../transport/create-response";
import { canReadBoard } from "./boards.handlers";

const MB = 1024 * 1024;
const files = new Map<string, { fileName: string; rows: number }>();

const COLUMNS = [
  { index: 0, header: "Tiêu đề", examples: ["Chuyển dữ liệu khách hàng…"] },
  {
    index: 1,
    header: "Người phụ trách",
    examples: ["Mai Quốc Minh", "Hà Nguyễn"],
  },
  { index: 2, header: "Hạn", examples: ["16/10/2026", "20/10/2026"] },
  { index: 3, header: "Ưu tiên", examples: ["Cao", "Khẩn", "Thấp"] },
  { index: 4, header: "Ước lượng", examples: ["8", "12", "2,5"] },
  { index: 5, header: "Ghi chú cũ", examples: ["đã bàn giao"] },
];
const SUGGESTED = [
  { column: 0, target: "$title" },
  { column: 1, target: "assignee" },
  { column: 2, target: "due" },
  { column: 3, target: "priority" },
  { column: 4, target: "estimate" },
];
const SAMPLE = [
  {
    row: 1,
    cells: [
      "Chuyển dữ liệu khách hàng từ hệ thống cũ",
      "Mai Quốc Minh",
      "16/10/2026",
      "Cao",
      "8",
    ],
  },
  {
    row: 2,
    cells: [
      "Kiểm thử phân hệ công nợ tháng 9",
      "Hà Nguyễn",
      "20/10/2026",
      "Trung bình",
      "12",
    ],
  },
  {
    row: 14,
    cells: [
      "Đối chiếu số liệu tồn kho",
      "Phan Ngọc Anh",
      "22/10/2026",
      "Cao",
      "3",
    ],
  },
  {
    row: 212,
    cells: ["Lập báo cáo tồn kho", "Vũ Thanh", "24/10/2026", "Thấp", "2,5"],
  },
  {
    row: 3_480,
    cells: ["Chốt sổ cuối quý", "Đỗ Khánh Linh", "31/12/2026", "Cao", "16"],
  },
];
const ORDER = ["$title", "assignee", "due", "priority", "estimate"];
const ERRORS = [
  {
    row: 14,
    column: "Ước lượng (giờ)",
    target: "estimate",
    value: "ba ngày",
    code: "not-number",
  },
  {
    row: 212,
    column: "Tên việc",
    target: "$title",
    value: null,
    code: "required",
  },
  {
    row: 3_480,
    column: "Hạn chót",
    target: "due",
    value: "31/02/2026",
    code: "not-date",
  },
  ...[400, 401, 402, 403, 404, 405, 405, 405, 405].map((row) => ({
    row,
    column: "Hạn chót",
    target: "due",
    value: "31/02/2026",
    code: "not-date",
  })),
];
const sampleRows = (bad: boolean) =>
  SAMPLE.map((r) => ({
    row: r.row,
    cells: Object.fromEntries(ORDER.map((t, i) => [t, r.cells[i]])),
    errorTargets: !bad
      ? []
      : r.row === 14
        ? ["estimate"]
        : r.row === 212
          ? ["$title"]
          : r.row === 3_480
            ? ["due"]
            : [],
  })).map((r) =>
    bad && r.row === 14
      ? { ...r, cells: { ...r.cells, estimate: "ba ngày" } }
      : bad && r.row === 212
        ? { ...r, cells: { ...r.cells, $title: null } }
        : bad && r.row === 3_480
          ? { ...r, cells: { ...r.cells, due: "31/02/2026" } }
          : r,
  );

function targets() {
  return [
    { id: "$title", name: "Tên việc", type: "Text", required: true },
    { id: "$group", name: "Nhóm", type: "Group", required: false },
    { id: "assignee", name: "Người phụ trách", type: "Person" },
    { id: "due", name: "Hạn chót", type: "Date" },
    { id: "priority", name: "Mức ưu tiên", type: "Select" },
    { id: "estimate", name: "Ước lượng (giờ)", type: "Number" },
  ];
}

function job(id: string, file: { fileName: string; rows: number }) {
  const base = { id, total: file.rows, fileName: file.fileName };
  const name = file.fileName;
  if (/dang-tao/.test(name))
    return { ...base, status: "running", phase: "creating", processed: 3_200 };
  if (/gan-xong/.test(name))
    return { ...base, status: "running", phase: "creating", processed: 4_900 };
  if (/bi-tu-choi/.test(name))
    return {
      ...base,
      status: "failed",
      failureCode: "has-errors",
      processed: 3_480,
    };
  if (/luu-tru/.test(name))
    return {
      ...base,
      status: "failed",
      failureCode: "board-archived",
      failureBy: "Hà Nguyễn",
      processed: 0,
    };
  return {
    ...base,
    status: "succeeded",
    processed: file.rows,
    created: file.rows,
    firstKey: "ERP-213",
    lastKey: "ERP-5212",
  };
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
      const jobId = store.nextId("import");
      files.set(jobId, { fileName, rows: 5_000 });
      return ok({
        ...base,
        jobId,
        rowCount: 5_000,
        size: Math.round(1.8 * MB),
        columns: COLUMNS.map((col) =>
          col.index === 4 && /cot-loi/.test(fileName)
            ? { ...col, examples: ["8", "ba ngày", "2,5"] }
            : col,
        ),
        targets: targets(),
        suggested: /thieu-ten/.test(fileName) ? SUGGESTED.slice(1) : SUGGESTED,
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
    async handle({ params, store, body }) {
      const file = files.get(params.jobId);
      if (!file || !canReadBoard(params.boardId, store)) return notFound();
      const mapped = new Set(
        (
          (body as { mapping?: { target: string }[] } | undefined)?.mapping ??
          []
        ).map((m) => m.target),
      );
      const named = file.fileName;
      const bad = /co-loi/.test(named);
      const checks = SUGGESTED.filter((m) => mapped.has(m.target)).map((m) => ({
        column: m.column,
        target: m.target,
        status:
          m.target === "estimate" && /cot-loi/.test(named)
            ? "incompatible"
            : m.target === "priority" && /cot-loi|thieu-ten/.test(named)
              ? "unknown-values"
              : "ok",
        count: m.target === "priority" ? 2 : 0,
        examples:
          m.target === "estimate" && /cot-loi/.test(named)
            ? ["ba ngày"]
            : m.target === "priority"
              ? ["Khẩn"]
              : [],
      }));
      return ok({
        rowCount: file.rows,
        missingTitle: !mapped.has("$title"),
        checks,
        errorCount: bad ? ERRORS.length : 0,
        errorRows: bad ? 9 : 0,
        errors: bad ? ERRORS.slice(0, 3) : [],
        sample: sampleRows(bad),
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
      if (/mat-mang/.test(file.fileName)) return serverError();
      return created(job(params.jobId, file));
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
      return ok(job(params.jobId, file));
    },
  }),

  defineMockOperation<{ boardId: string; jobId: string }, never, unknown>({
    id: "wm.imports.errors",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Imports.GetErrors",
    },
    method: "GET",
    route: "/boards/:boardId/imports/:jobId/errors",
    async handle({ params, store }) {
      const file = files.get(params.jobId);
      if (!file || !canReadBoard(params.boardId, store)) return notFound();
      return ok({ errors: ERRORS });
    },
  }),
];
