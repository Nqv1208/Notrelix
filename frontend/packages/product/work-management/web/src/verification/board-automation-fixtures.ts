import type {
  AutomationCatalogDto,
  AutomationCheckDto,
  AutomationRuleDetailDto,
  AutomationRuleDocumentDto,
  AutomationRunDto,
  AutomationRunsDto,
  AutomationPersonDto,
} from "@notrelix/work-management-core";
import type {
  AutomationActions,
  AutomationPanelProps,
} from "../components/board-settings/automation-types";

/**
 * Mockup world of the automation editor and run log (Final-Automation,
 * "Triển khai ERP Q4"), for stories and tests only. Deterministic: every
 * date is a literal; "now" is Monday 12/10/2026 10:40 in Hanoi.
 */
export const AUTOMATION_NOW = "2026-10-12T03:40:00.000Z";

const person = (
  id: string,
  name: string,
  initials: string,
  color: string,
): AutomationPersonDto => ({ id, name, initials, color });

export const ha = person("p-ha", "Nguyễn Thu Hà", "HA", "#4E8574");
export const phuc = person("p-phuc", "Lê Hoàng Phúc", "HP", "#B4500B");
const quan = person("p-quan", "Trần Minh Quân", "TQ", "#1E6FD0");
const minh = person("p-minh", "Hồ Quang Minh", "HM", "#57534E");

export const automationCatalog: AutomationCatalogDto = {
  statuses: [
    { id: "s-todo", name: "Cần làm" },
    { id: "s-doing", name: "Đang làm" },
    { id: "s-review", name: "Chờ duyệt" },
    { id: "s-done", name: "Hoàn thành" },
  ],
  priorities: [
    { id: "p-urgent", name: "Khẩn" },
    { id: "p-high", name: "Cao" },
    { id: "p-medium", name: "Trung bình" },
    { id: "p-low", name: "Thấp" },
  ],
  fields: [
    {
      id: "sf-status",
      name: "Trạng thái",
      type: "status",
      archived: false,
      options: [
        { id: "s-todo", name: "Cần làm" },
        { id: "s-done", name: "Hoàn thành" },
      ],
    },
    {
      id: "sf-module",
      name: "Phân hệ ERP",
      type: "multiselect",
      archived: false,
      options: [
        { id: "m-accounting", name: "Kế toán" },
        { id: "m-warehouse", name: "Kho" },
      ],
    },
    {
      id: "sf-due",
      name: "Hạn chót",
      type: "date",
      archived: false,
      options: [],
    },
    {
      id: "sf-handover",
      name: "Ngày bàn giao",
      type: "date",
      archived: true,
      options: [],
    },
  ],
  groups: [{ id: "g-data", name: "Dữ liệu" }],
  people: [ha, phuc, quan, minh],
  teams: [
    { id: "t-accounting", name: "Nhóm Kế toán tổng hợp", memberCount: 6 },
  ],
  channels: [{ id: "ch-erp", name: "ERP – Điều hành" }],
  webhooks: [{ id: "wh-erp", name: "Đồng bộ ERP nội bộ" }],
  teamsNeedsReauthorization: true,
  boardHasEmailAddress: false,
};

const ok = (
  id: string,
  code: AutomationCheckDto["code"],
  params: AutomationCheckDto["params"],
): AutomationCheckDto => ({ id, level: "ok", code, params, fixes: [] });

const base = {
  boardId: "board-erp",
  revision: 1,
  scope: {
    boardTitle: "Triển khai ERP Q4",
    detail: "Phòng Công nghệ · chỉ việc trên board này kích hoạt quy tắc",
  },
  checkedAt: AUTOMATION_NOW,
  catalog: automationCatalog,
  blocked: false,
  nextRun: null,
} satisfies Partial<AutomationRuleDetailDto>;

const draftDocument: AutomationRuleDocumentDto = {
  name: "Báo kế toán khi việc Kế toán hoàn thành",
  trigger: { kind: "status-changed", optionId: "s-done", schedule: null },
  conditions: [
    {
      id: "c-1",
      fieldId: "sf-module",
      operator: "is",
      optionId: "m-accounting",
    },
  ],
  steps: [
    {
      id: "st-1",
      kind: "notify",
      targetId: "t-accounting",
      note: "tên việc, người đánh dấu hoàn thành và liên kết tới việc",
    },
  ],
};

/** A draft that passes every check (soan). */
export const draftRule: AutomationRuleDetailDto = {
  ...base,
  id: "sa-draft",
  status: "draft",
  definition: draftDocument,
  subtitle:
    "Nháp · tạo bởi Nguyễn Thu Hà · chưa chạy lần nào. Nháp được phép thiếu, chỉ cần đủ khi bật.",
  version: 0,
  runAs: { ...ha, role: "người tạo" },
  versions: [],
  recentRuns: [],
  checks: [
    ok("trigger", "trigger-valid", {
      event: "Trạng thái thay đổi sang Hoàn thành",
    }),
    ok("cond", "condition-valid", { field: "Phân hệ ERP", value: "Kế toán" }),
    ok("team", "team-can-see-board", {
      team: "Nhóm Kế toán tổng hợp",
      count: 6,
    }),
    ok("runner", "runner-can-notify", { runner: "Nguyễn Thu Hà" }),
  ],
};

/** A running rule that waits for an item to reach Chờ duyệt (sua-khi-dang-bat). */
export const eventRule: AutomationRuleDetailDto = {
  ...base,
  id: "sa-review",
  status: "on",
  definition: {
    name: "Gửi duyệt khi chuyển sang Chờ duyệt",
    trigger: { kind: "status-changed", optionId: "s-review", schedule: null },
    conditions: [],
    steps: [
      { id: "st-r1", kind: "assign", targetId: "p-ha", note: null },
      { id: "st-r2", kind: "notify", targetId: "role:reviewer", note: null },
    ],
  },
  subtitle:
    "Phiên bản 3 · sửa lần cuối 02/10 bởi Lê Hoàng Phúc · chạy 3 lần trong 7 ngày",
  version: 3,
  runAs: { ...phuc, role: "Quản trị Phòng Công nghệ" },
  versions: [
    {
      number: 3,
      at: "2026-10-02T03:00:00.000Z",
      byName: "Lê Hoàng Phúc",
      summary: ["Khi: Trạng thái chuyển sang Chờ duyệt"],
    },
    {
      number: 2,
      at: "2026-09-18T03:00:00.000Z",
      byName: null,
      summary: [
        "Khi: Trạng thái chuyển sang Chờ duyệt",
        "Thì 1: Giao Nguyễn Thu Hà duyệt",
      ],
    },
    {
      number: 1,
      at: "2026-08-14T03:00:00.000Z",
      byName: null,
      summary: ["Khi: Có việc mới"],
    },
  ],
  recentRuns: [],
  checks: [ok("valid", "rule-valid", {})],
};

/** A running rule on a schedule (lich). */
export const scheduleRule: AutomationRuleDetailDto = {
  ...base,
  id: "sa-overdue",
  status: "on",
  definition: {
    name: "Nhắc việc quá hạn",
    trigger: {
      kind: "schedule",
      optionId: null,
      schedule: {
        time: "09:00",
        timeZone: "Asia/Ho_Chi_Minh",
        repeat: "daily",
        days: [1, 2, 3, 4, 5, 6, 7],
        date: null,
        missed: "catch-up",
      },
    },
    conditions: [
      { id: "c-o1", fieldId: "sf-due", operator: "passed", optionId: null },
      {
        id: "c-o2",
        fieldId: "sf-status",
        operator: "is-not",
        optionId: "s-done",
      },
    ],
    steps: [
      { id: "st-o1", kind: "remind", targetId: "role:assignee", note: null },
    ],
  },
  subtitle:
    "Phiên bản 2 · chạy 7 lần trong 7 ngày · lần cuối 09:00 hôm nay, thành công",
  version: 2,
  runAs: { ...phuc, role: "Quản trị Phòng Công nghệ" },
  versions: [],
  recentRuns: [
    {
      id: "run-2",
      at: "2026-10-12T02:00:00.000Z",
      brief: "1 việc khớp: ERP-142",
      outcome: "success",
    },
    {
      id: "run-3",
      at: "2026-10-11T02:00:00.000Z",
      brief: "1 việc khớp: ERP-142",
      outcome: "success",
    },
    {
      id: "run-5",
      at: "2026-10-10T02:00:00.000Z",
      brief: "Không khớp điều kiện",
      outcome: "skipped",
    },
  ],
  checks: [ok("valid", "rule-valid", {})],
  nextRun: "09:00 Thứ Ba 13/10",
};

/** A copy whose step writes into an archived field (loi-cau-hinh). */
export const blockedRule: AutomationRuleDetailDto = {
  ...base,
  id: "sa-urgent-copy",
  status: "draft",
  blocked: true,
  definition: {
    name: "Khẩn → báo Trưởng phòng (bản sao)",
    trigger: { kind: "priority-changed", optionId: "p-urgent", schedule: null },
    conditions: [],
    steps: [
      { id: "st-c1", kind: "set-today", targetId: "sf-handover", note: null },
      { id: "st-c2", kind: "teams", targetId: "ch-erp", note: null },
    ],
  },
  subtitle:
    "Bản sao của Khẩn → báo Trưởng phòng · tạo bởi Nguyễn Thu Hà · chưa bật",
  version: 0,
  runAs: { ...ha, role: "người tạo bản sao" },
  versions: [],
  recentRuns: [],
  checks: [
    {
      id: "step-st-c1",
      level: "error",
      code: "step-field-archived",
      params: {
        step: 1,
        field: "Ngày bàn giao",
        by: "Trần Minh Quân",
        at: "2026-10-02T04:00:00.000Z",
        writes: "date",
      },
      fixes: [
        { kind: "restore-field", fieldId: "sf-handover", stepId: "st-c1" },
        { kind: "choose-field", stepId: "st-c1" },
      ],
    },
    {
      id: "step-st-c2",
      level: "warning",
      code: "step-teams-reauthorization",
      params: { step: 2 },
      fixes: [{ kind: "reauthorize", stepId: "st-c2" }],
    },
    ok("trigger", "trigger-valid", {
      event: "Mức ưu tiên thay đổi thành Khẩn",
    }),
    ok("runner", "runner-can-notify", { runner: "Nguyễn Thu Hà" }),
  ],
};

const run = (
  index: number,
  base: Omit<AutomationRunDto, "id" | "condition" | "fixes" | "steps"> &
    Partial<Pick<AutomationRunDto, "condition" | "fixes" | "steps">>,
): AutomationRunDto => ({
  id: `run-${index}`,
  condition: "Không có",
  fixes: [],
  steps: [],
  ...base,
});

const none = {
  itemKey: null,
  detail: null,
  detailItemKey: null,
} as const;

/** Nine runs of the board's rules, newest first. */
export const automationRuns: AutomationRunsDto = {
  boardId: "board-erp",
  generatedAt: AUTOMATION_NOW,
  rules: [
    { id: "sa-review", name: "Gửi duyệt khi chuyển sang Chờ duyệt" },
    { id: "sa-overdue", name: "Nhắc việc quá hạn" },
    { id: "sa-urgent", name: "Khẩn → báo Trưởng phòng" },
    { id: "sa-warehouse", name: "Gắn nhóm Dữ liệu cho việc kho" },
  ],
  usage: { month: 10, runs: 3880 },
  runs: [
    run(1, {
      ruleId: "sa-review",
      ruleName: "Gửi duyệt khi chuyển sang Chờ duyệt",
      at: "2026-10-12T03:28:00.000Z",
      trigger: { ...none, itemKey: "ERP-138", text: "Trạng thái → Chờ duyệt" },
      outcome: "success",
      summary: "2/2 bước",
      warning: false,
      stepsDone: 2,
      stepsTotal: 2,
      runAs: phuc,
      version: 3,
      versionIsCurrent: true,
      firedBy: "Trạng thái đổi sang Chờ duyệt · {key}",
    }),
    run(2, {
      ruleId: "sa-overdue",
      ruleName: "Nhắc việc quá hạn",
      at: "2026-10-12T02:00:00.000Z",
      trigger: {
        itemKey: null,
        text: "Lịch 09:00",
        detail: "1 việc khớp: {key}",
        detailItemKey: "ERP-142",
      },
      outcome: "success",
      summary: "Đã nhắc Trần Minh Quân",
      warning: false,
      stepsDone: 1,
      stepsTotal: 1,
      runAs: phuc,
      version: 2,
      versionIsCurrent: true,
      firedBy: "Lịch 09:00 hằng ngày",
    }),
    run(4, {
      ruleId: "sa-urgent",
      ruleName: "Khẩn → báo Trưởng phòng",
      at: "2026-10-10T11:05:00.000Z",
      trigger: { ...none, itemKey: "ERP-142", text: "Mức ưu tiên → Khẩn" },
      outcome: "error",
      summary: "1/2 bước",
      warning: false,
      stepsDone: 1,
      stepsTotal: 2,
      runAs: phuc,
      version: 1,
      versionIsCurrent: true,
      firedBy: "Mức ưu tiên đổi thành Khẩn · {key} · bởi Trần Minh Quân 18:05",
      steps: [
        {
          index: 1,
          title: "Gửi thông báo cho Lê Hoàng Phúc",
          outcome: "success",
          at: "2026-10-10T11:05:00.000Z",
          error: null,
        },
        {
          index: 2,
          title: 'Đăng tin Microsoft Teams vào kênh "ERP – Điều hành"',
          outcome: "error",
          at: "2026-10-10T11:05:00.000Z",
          error:
            "Kết nối Microsoft Teams cần uỷ quyền lại (hết hạn 10/10 02:00)",
        },
      ],
      fixes: [
        {
          kind: "reauthorize",
          title: "Uỷ quyền lại kết nối Microsoft Teams",
          detail:
            "Người kết nối (Lê Hoàng Phúc) hoặc quản trị workspace, như bạn, làm được.",
          blockedReason: null,
        },
        {
          kind: "rerun-step",
          title: "Chạy lại bước lỗi",
          detail: "",
          blockedReason:
            "Chưa bấm được: kết nối Microsoft Teams vẫn cần uỷ quyền lại.",
        },
      ],
    }),
    run(5, {
      ruleId: "sa-overdue",
      ruleName: "Nhắc việc quá hạn",
      at: "2026-10-10T02:00:00.000Z",
      trigger: { ...none, text: "Lịch 09:00", detail: "0 việc quá hạn" },
      outcome: "skipped",
      summary: "Không khớp điều kiện, không phải lỗi",
      warning: false,
      stepsDone: 0,
      stepsTotal: 1,
      runAs: phuc,
      version: 2,
      versionIsCurrent: true,
      firedBy: "Lịch 09:00 hằng ngày",
    }),
    run(8, {
      ruleId: "sa-warehouse",
      ruleName: "Gắn nhóm Dữ liệu cho việc kho",
      at: "2026-10-08T04:20:00.000Z",
      trigger: { ...none, text: "Có việc mới" },
      outcome: "skipped",
      summary: "Không chạy: người chạy không còn quyền",
      warning: true,
      stepsDone: 0,
      stepsTotal: 1,
      runAs: minh,
      version: 1,
      versionIsCurrent: true,
      firedBy: "Có việc mới",
    }),
  ],
};

const done = { ok: true } as const;

/** Actions that answer ok and do nothing; tests override the ones they watch. */
export function noopAutomationActions(
  overrides: Partial<AutomationActions> = {},
): AutomationActions {
  return {
    save: () => Promise.resolve(done),
    setEnabled: () => Promise.resolve(done),
    changeRunner: () => Promise.resolve(done),
    restoreField: () => Promise.resolve(done),
    rerunStep: () => Promise.resolve({ ok: false, reason: "invalid" }),
    ...overrides,
  };
}

/** The panel props of an editor story or test. */
export function automationPanel(
  overrides: Partial<AutomationPanelProps> = {},
): AutomationPanelProps {
  return {
    ruleId: null,
    log: false,
    runId: null,
    rule: { status: "idle", data: null },
    runs: { status: "idle", data: null },
    onOpenRule: () => undefined,
    onCreateRule: () => Promise.resolve(true),
    onCloseRule: () => undefined,
    onOpenLog: () => undefined,
    onOpenRun: () => undefined,
    onReloadRule: () => undefined,
    onReloadRuns: () => undefined,
    actions: noopAutomationActions(),
    billingHref: "/workspaces/w/billing",
    ...overrides,
  };
}
