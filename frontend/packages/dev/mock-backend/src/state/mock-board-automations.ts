/**
 * The automation rules of the mock world (docs/design/mockups/2026-10/src/
 * Final-Automation.dc.html): the editor of a rule (Khi / Nếu / Thì, its
 * runner, versions, checks), the run log and one run. The generated contract
 * has none of these reads, so they are one registered gap
 * (`CTR-GAP-WM-BOARD-AUTOMATIONS`) with the shapes the frontend needs.
 *
 * The four running/paused rules of the board settings list (registry
 * `automations`) keep their list record; this state adds their editable
 * definition and the drafts. A draft is not in the list records until it is
 * turned on. Checks read the board's own registry: an archived field a step
 * writes into blocks the rule.
 *
 * Pure state; deterministic (the mock clock, fixed run history).
 */
import type { MockStore } from "./mock-store";
import type { SettingsAutomationRecord } from "./mock-board-settings";

export type RuleTriggerKind =
  | "item-created"
  | "status-changed"
  | "priority-changed"
  | "assignee-changed"
  | "due-passed"
  | "comment-added"
  | "schedule"
  | "manual";

export interface RuleSchedule {
  time: string;
  timeZone: string;
  repeat: "daily" | "weekly" | "date";
  days: number[];
  date: string | null;
  missed: "catch-up" | "skip";
}

export interface RuleTrigger {
  kind: RuleTriggerKind;
  optionId: string | null;
  schedule: RuleSchedule | null;
}

export interface RuleCondition {
  id: string;
  fieldId: string;
  operator: "is" | "is-not" | "passed" | "is-empty";
  optionId: string | null;
}

export type RuleStepKind =
  | "set-status"
  | "assign"
  | "set-field"
  | "set-today"
  | "move-group"
  | "create-subitem"
  | "archive-item"
  | "notify"
  | "remind"
  | "email"
  | "teams"
  | "webhook";

export interface RuleStep {
  id: string;
  kind: RuleStepKind;
  targetId: string | null;
  note: string | null;
}

export interface RuleDocument {
  name: string;
  trigger: RuleTrigger;
  conditions: RuleCondition[];
  steps: RuleStep[];
}

export interface RuleVersionRecord {
  number: number;
  at: string;
  byName: string | null;
  summary: string[];
}

export interface RulePerson {
  id: string;
  name: string;
  initials: string;
  color: string;
}

export interface RuleRecord {
  id: string;
  definition: RuleDocument;
  revision: number;
  /** 0 for a draft. */
  version: number;
  /** The record of `registry.automations` of a rule that has been turned on. */
  live: boolean;
  runAs: RulePerson & { role: string };
  versions: RuleVersionRecord[];
  /** Subtitle parts of a rule that has run. */
  lastEdit: string | null;
  activity: string;
  /** Subtitle of a draft. */
  draftNote: string;
  /** Runs of the rule shown in the aside, newest first. */
  nextRun: string | null;
}

export type RunOutcome = "success" | "error" | "skipped";

export interface RunRecord {
  id: string;
  ruleId: string;
  at: string;
  trigger: {
    itemKey: string | null;
    text: string;
    detail: string | null;
    detailItemKey: string | null;
  };
  outcome: RunOutcome;
  summary: string;
  brief: string;
  warning: boolean;
  stepsDone: number;
  stepsTotal: number;
  runAs: RulePerson;
  version: number;
  firedBy: string;
  condition: string;
  steps: {
    index: number;
    title: string;
    outcome: "success" | "error";
    at: string;
    error: string | null;
  }[];
}

export type AutomationWrite<T = void> =
  | { readonly result: "ok"; readonly value: T }
  | {
      readonly result: "not-found" | "invalid" | "conflict";
      readonly message?: string;
    };

interface CheckRecord {
  id: string;
  level: "ok" | "error" | "warning";
  code:
    | "trigger-valid"
    | "condition-valid"
    | "step-field-archived"
    | "step-teams-reauthorization"
    | "team-can-see-board"
    | "runner-can-notify"
    | "rule-valid"
    | "no-steps";
  params: Record<string, string | number>;
  fixes: {
    kind: "restore-field" | "choose-field" | "reauthorize";
    fieldId?: string;
    stepId?: string;
  }[];
}

interface BoardAutomations {
  rules: RuleRecord[];
  runs: RunRecord[];
  nextRule: number;
  nextStep: number;
}

const PEOPLE: readonly RulePerson[] = [
  { id: "p-ha", name: "Nguyễn Thu Hà", initials: "HA", color: "#4E8574" },
  { id: "p-phuc", name: "Lê Hoàng Phúc", initials: "HP", color: "#B4500B" },
  { id: "p-quan", name: "Trần Minh Quân", initials: "TQ", color: "#1E6FD0" },
  { id: "p-minh", name: "Hồ Quang Minh", initials: "HM", color: "#57534E" },
];

const TEAMS = [
  { id: "t-ketoan", name: "Nhóm Kế toán tổng hợp", memberCount: 6 },
  { id: "t-hatang", name: "Nhóm Hạ tầng", memberCount: 5 },
];

const CHANNELS = [
  { id: "ch-erp", name: "ERP – Điều hành" },
  { id: "ch-cntt", name: "CNTT – Chung" },
];

const WEBHOOKS = [{ id: "wh-erp-sync", name: "Đồng bộ ERP nội bộ" }];

/** Roles a notification or an assignment can name instead of a person. */
export const ROLE_TARGETS = {
  reviewer: "role:reviewer",
  assignee: "role:assignee",
} as const;

const TRIGGER_LABEL: Readonly<Record<RuleTriggerKind, string>> = {
  "item-created": "Có việc mới",
  "status-changed": "Trạng thái thay đổi",
  "priority-changed": "Mức ưu tiên thay đổi",
  "assignee-changed": "Người phụ trách thay đổi",
  "due-passed": "Hạn chót đã qua",
  "comment-added": "Có bình luận mới",
  schedule: "Theo lịch",
  manual: "Chạy khi bấm nút trên việc",
};

export function isoDay(iso: string): string {
  return new Intl.DateTimeFormat("en-GB", {
    timeZone: "Asia/Ho_Chi_Minh",
    day: "2-digit",
    month: "2-digit",
  }).format(Date.parse(iso));
}

export class MockBoardAutomations {
  private readonly boards = new Map<string, BoardAutomations>();

  constructor(private readonly store: MockStore) {}

  reset(): void {
    this.boards.clear();
  }

  // ── Reads ──

  private state(boardId: string): BoardAutomations {
    const existing = this.boards.get(boardId);
    if (existing) return existing;
    const seeded = this.store.settings
      .registry(boardId)
      .automations.some((rule) => rule.id === "sa-review")
      ? this.seed(boardId)
      : { rules: [], runs: [], nextRule: 1, nextStep: 1 };
    this.boards.set(boardId, seeded);
    return seeded;
  }

  rule(boardId: string, ruleId: string): RuleRecord | undefined {
    return this.state(boardId).rules.find((rule) => rule.id === ruleId);
  }

  /** Drafts as list records, after the rules of the registry. */
  listFor(boardId: string): SettingsAutomationRecord[] {
    const registry = this.store.settings.registry(boardId);
    this.state(boardId);
    const drafts = this.state(boardId).rules.filter((rule) => !rule.live);
    return [
      ...registry.automations,
      ...drafts.map((rule) => ({
        id: rule.id,
        name: rule.definition.name,
        enabled: false,
        draft: true,
        when: this.describeTrigger(boardId, rule.definition.trigger),
        condition:
          rule.definition.conditions.length > 0
            ? this.describeConditions(boardId, rule.definition.conditions)
            : undefined,
        then: rule.definition.steps.map((step) =>
          this.describeStep(boardId, step),
        ),
        stats: rule.draftNote.split(" · ").slice(0, 2).join(" · "),
        runAs: { name: rule.runAs.name, initials: rule.runAs.initials },
      })),
    ];
  }

  private enabledOf(boardId: string, rule: RuleRecord): boolean {
    return (
      this.store.settings
        .registry(boardId)
        .automations.find((entry) => entry.id === rule.id)?.enabled ?? false
    );
  }

  private catalog(boardId: string) {
    const registry = this.store.settings.registry(boardId);
    const fieldOf = (id: string) => registry.fields.find((f) => f.id === id);
    const optionsOf = (id: string) => {
      const field = fieldOf(id);
      return field
        ? this.store.settings
            .optionsOf(boardId, field)
            .map((option) => ({ id: option.id, name: option.name }))
        : [];
    };
    return {
      statuses: optionsOf("sf-status"),
      priorities: optionsOf("sf-priority"),
      fields: registry.fields.map((field) => ({
        id: field.id,
        name: field.name,
        type: field.type,
        archived: field.archived !== undefined,
        options: optionsOf(field.id),
      })),
      groups: this.store
        .getLists(boardId)
        .map((list) => ({ id: list.id, name: list.title })),
      people: PEOPLE,
      teams: TEAMS,
      channels: CHANNELS,
      webhooks: WEBHOOKS,
      teamsNeedsReauthorization: true,
      boardHasEmailAddress: false,
    };
  }

  private optionName(
    boardId: string,
    fieldId: string,
    optionId: string | null,
  ) {
    if (!optionId) return "";
    return (
      this.catalog(boardId)
        .fields.find((field) => field.id === fieldId)
        ?.options.find((option) => option.id === optionId)?.name ?? ""
    );
  }

  private personName(targetId: string | null): string {
    if (targetId === ROLE_TARGETS.reviewer) return "người duyệt";
    if (targetId === ROLE_TARGETS.assignee) return "người phụ trách";
    return (
      PEOPLE.find((p) => p.id === targetId)?.name ??
      TEAMS.find((t) => t.id === targetId)?.name ??
      ""
    );
  }

  describeTrigger(boardId: string, trigger: RuleTrigger): string {
    const label = TRIGGER_LABEL[trigger.kind];
    if (trigger.kind === "status-changed")
      return `Trạng thái chuyển sang ${this.optionName(boardId, "sf-status", trigger.optionId)}`.trim();
    if (trigger.kind === "priority-changed")
      return `Mức ưu tiên đổi thành ${this.optionName(boardId, "sf-priority", trigger.optionId)}`.trim();
    if (trigger.kind === "schedule" && trigger.schedule)
      return `Theo lịch ${trigger.schedule.time}`;
    return label;
  }

  describeConditions(boardId: string, conditions: RuleCondition[]): string {
    const registry = this.store.settings.registry(boardId);
    return conditions
      .map((condition) => {
        const field = registry.fields.find((f) => f.id === condition.fieldId);
        const name = field?.name ?? "";
        if (condition.operator === "passed") return `${name} đã qua`;
        if (condition.operator === "is-empty") return `${name} để trống`;
        const word = condition.operator === "is" ? "là" : "không phải";
        return `${name} ${word} ${this.optionName(boardId, condition.fieldId, condition.optionId)}`.trim();
      })
      .join(" và ");
  }

  describeStep(boardId: string, step: RuleStep): string {
    const registry = this.store.settings.registry(boardId);
    switch (step.kind) {
      case "notify":
        return `Gửi thông báo cho ${this.personName(step.targetId)}`.trim();
      case "email":
        return `Gửi email cho ${this.personName(step.targetId)}`.trim();
      case "assign":
        return `Giao ${this.personName(step.targetId)} duyệt`.trim();
      case "remind":
        return `Nhắc ${this.personName(step.targetId)}`.trim();
      case "set-status":
        return `Đổi trạng thái sang ${this.optionName(boardId, "sf-status", step.targetId)}`.trim();
      case "set-field":
      case "set-today":
        return `Đặt giá trị ${registry.fields.find((f) => f.id === step.targetId)?.name ?? ""}`.trim();
      case "move-group":
        return `Chuyển vào nhóm ${this.catalog(boardId).groups.find((g) => g.id === step.targetId)?.name ?? ""}`.trim();
      case "create-subitem":
        return "Tạo việc con";
      case "archive-item":
        return "Lưu trữ việc";
      case "teams":
        return `Đăng tin vào kênh Microsoft Teams "${CHANNELS.find((c) => c.id === step.targetId)?.name ?? ""}"`;
      case "webhook":
        return "Gọi webhook";
    }
  }

  /** The checks of the saved rule: errors first, then warnings, then passes. */
  private checks(
    boardId: string,
    rule: RuleRecord,
  ): { errors: CheckRecord[]; warnings: CheckRecord[]; passes: CheckRecord[] } {
    const registry = this.store.settings.registry(boardId);
    const catalog = this.catalog(boardId);
    const doc = rule.definition;
    const errors: CheckRecord[] = [];
    const warnings: CheckRecord[] = [];
    const passes: CheckRecord[] = [];
    const triggerText = (() => {
      if (doc.trigger.kind === "status-changed")
        return `Trạng thái thay đổi sang ${this.optionName(boardId, "sf-status", doc.trigger.optionId)}`;
      if (doc.trigger.kind === "priority-changed")
        return `Mức ưu tiên thay đổi thành ${this.optionName(boardId, "sf-priority", doc.trigger.optionId)}`;
      return TRIGGER_LABEL[doc.trigger.kind];
    })();
    passes.push({
      id: "trigger",
      level: "ok",
      code: "trigger-valid",
      params: { event: triggerText },
      fixes: [],
    });
    doc.conditions.forEach((condition) => {
      const field = registry.fields.find((f) => f.id === condition.fieldId);
      if (!field) return;
      if (field.archived) {
        errors.push({
          id: `cond-${condition.id}`,
          level: "error",
          code: "step-field-archived",
          params: {
            step: 0,
            field: field.name,
            by: field.archived.byName,
            at: field.archived.at,
            writes: "read",
          },
          fixes: [
            { kind: "restore-field", fieldId: field.id },
            { kind: "choose-field" },
          ],
        });
      } else if (condition.optionId) {
        passes.push({
          id: `cond-${condition.id}`,
          level: "ok",
          code: "condition-valid",
          params: {
            field: field.name,
            value: this.optionName(boardId, field.id, condition.optionId),
          },
          fixes: [],
        });
      }
    });
    doc.steps.forEach((step, index) => {
      const n = index + 1;
      if (step.kind === "set-field" || step.kind === "set-today") {
        const field = registry.fields.find((f) => f.id === step.targetId);
        if (field?.archived)
          errors.push({
            id: `step-${step.id}`,
            level: "error",
            code: "step-field-archived",
            params: {
              step: n,
              field: field.name,
              by: field.archived.byName,
              at: field.archived.at,
              writes: step.kind === "set-today" ? "date" : "value",
            },
            fixes: [
              { kind: "restore-field", fieldId: field.id, stepId: step.id },
              { kind: "choose-field", stepId: step.id },
            ],
          });
      }
      if (step.kind === "teams" && catalog.teamsNeedsReauthorization)
        warnings.push({
          id: `step-${step.id}`,
          level: "warning",
          code: "step-teams-reauthorization",
          params: { step: n },
          fixes: [{ kind: "reauthorize", stepId: step.id }],
        });
      if (step.kind === "notify" || step.kind === "email") {
        const team = TEAMS.find((t) => t.id === step.targetId);
        if (team)
          passes.push({
            id: `step-${step.id}`,
            level: "ok",
            code: "team-can-see-board",
            params: { team: team.name, count: team.memberCount },
            fixes: [],
          });
      }
    });
    if (doc.steps.length === 0)
      errors.push({
        id: "no-steps",
        level: "error",
        code: "no-steps",
        params: {},
        fixes: [],
      });
    if (
      doc.steps.some(
        (s) => s.kind === "notify" || s.kind === "email" || s.kind === "teams",
      )
    )
      passes.push({
        id: "runner",
        level: "ok",
        code: "runner-can-notify",
        params: { runner: rule.runAs.name },
        fixes: [],
      });
    return { errors, warnings, passes };
  }

  detail(boardId: string, ruleId: string) {
    const rule = this.rule(boardId, ruleId);
    if (!rule) return undefined;
    const board = this.store.getBoard(boardId)!;
    const space = board.spaceId
      ? this.store.spaces.getSpace(board.spaceId)
      : null;
    const { errors, warnings, passes } = this.checks(boardId, rule);
    const enabled = this.enabledOf(boardId, rule);
    const status: "draft" | "on" | "off" = !rule.live
      ? "draft"
      : enabled
        ? "on"
        : "off";
    const clean = errors.length === 0;
    const checks =
      status === "on" && clean && warnings.length === 0
        ? [
            {
              id: "valid",
              level: "ok" as const,
              code: "rule-valid" as const,
              params: {},
              fixes: [],
            } satisfies CheckRecord,
          ]
        : [...errors, ...warnings, ...passes];
    const runs = this.state(boardId)
      .runs.filter((run) => run.ruleId === rule.id)
      .slice(0, 3);
    const subtitle = rule.live
      ? [`Phiên bản ${rule.version}`, rule.lastEdit, rule.activity]
          .filter((part): part is string => Boolean(part))
          .join(" · ")
      : rule.draftNote;
    return {
      boardId,
      id: rule.id,
      revision: rule.revision,
      status,
      definition: rule.definition,
      subtitle,
      version: rule.version,
      runAs: rule.runAs,
      scope: {
        boardTitle: board.title,
        detail: space
          ? `Phòng ${space.name} · chỉ việc trên board này kích hoạt quy tắc`
          : "Chỉ việc trên board này kích hoạt quy tắc",
      },
      versions: rule.versions,
      recentRuns: runs.map((run) => ({
        id: run.id,
        at: run.at,
        brief: run.brief,
        outcome: run.outcome,
      })),
      checks,
      blocked: !clean,
      checkedAt: this.store.getClock().isoNow(),
      nextRun: rule.nextRun,
      catalog: this.catalog(boardId),
    };
  }

  runs(boardId: string) {
    const state = this.state(boardId);
    const runAsOf = (run: RunRecord) => run.runAs;
    return {
      boardId,
      generatedAt: this.store.getClock().isoNow(),
      runs: state.runs.map((run) => {
        const rule = state.rules.find((r) => r.id === run.ruleId);
        return {
          id: run.id,
          ruleId: run.ruleId,
          ruleName: rule?.definition.name ?? "",
          at: run.at,
          trigger: run.trigger,
          outcome: run.outcome,
          summary: run.summary,
          warning: run.warning,
          stepsDone: run.stepsDone,
          stepsTotal: run.stepsTotal,
          runAs: runAsOf(run),
          version: run.version,
          versionIsCurrent: rule ? rule.version === run.version : false,
          firedBy: run.firedBy,
          condition: run.condition,
          steps: run.steps,
          fixes:
            run.outcome === "error"
              ? [
                  {
                    kind: "reauthorize" as const,
                    title: "Uỷ quyền lại kết nối Microsoft Teams",
                    detail:
                      "Người kết nối (Lê Hoàng Phúc) hoặc quản trị workspace, như bạn, làm được.",
                    blockedReason: null,
                  },
                  {
                    kind: "rerun-step" as const,
                    title: "Chạy lại bước lỗi",
                    detail: "",
                    blockedReason:
                      "Chưa bấm được: kết nối Microsoft Teams vẫn cần uỷ quyền lại.",
                  },
                ]
              : [],
        };
      }),
      rules: state.rules
        .filter((rule) => rule.live)
        .map((rule) => ({ id: rule.id, name: rule.definition.name })),
      usage: { month: 10, runs: 3880 },
    };
  }

  // ── Writes ──

  createDraft(boardId: string): AutomationWrite<{ id: string }> {
    const state = this.state(boardId);
    const user = this.store.getCurrentUser();
    const creator =
      PEOPLE.find((person) => person.name === user.name) ?? PEOPLE[0]!;
    const id = `sa-new-${state.nextRule}`;
    state.nextRule += 1;
    state.rules.push({
      id,
      definition: {
        name: "Quy tắc mới",
        trigger: { kind: "status-changed", optionId: null, schedule: null },
        conditions: [],
        steps: [],
      },
      revision: 1,
      version: 0,
      live: false,
      runAs: { ...creator, role: "người tạo" },
      versions: [],
      lastEdit: null,
      activity: "",
      draftNote: `Nháp · tạo bởi ${creator.name} · chưa chạy lần nào. Nháp được phép thiếu, chỉ cần đủ khi bật.`,
      nextRun: null,
    });
    return { result: "ok", value: { id } };
  }

  save(
    boardId: string,
    ruleId: string,
    input: {
      revision?: number;
      definition: RuleDocument;
      mode: "draft" | "enable" | "version";
    },
  ): AutomationWrite {
    const state = this.state(boardId);
    const rule = state.rules.find((entry) => entry.id === ruleId);
    if (!rule) return { result: "not-found" };
    if (input.revision !== undefined && input.revision !== rule.revision)
      return {
        result: "conflict",
        message: "The rule changed since it was read",
      };
    const name = input.definition.name.trim();
    if (!name) return { result: "invalid", message: "name" };
    const definition: RuleDocument = {
      ...input.definition,
      name,
      steps: input.definition.steps.map((step) => {
        if (step.id) return step;
        const id = `st-${state.nextStep}`;
        state.nextStep += 1;
        return { ...step, id };
      }),
    };
    const previous = rule.definition;
    rule.definition = definition;
    if (input.mode === "enable") {
      const { errors } = this.checks(boardId, rule);
      if (errors.length > 0 || definition.steps.length === 0) {
        rule.definition = previous;
        return { result: "invalid", message: "checks" };
      }
    }
    rule.revision += 1;
    const user = this.store.getCurrentUser().name;
    const today = this.store.getClock().isoNow();
    const summaryOf = () => [
      `Khi: ${this.describeTrigger(boardId, definition.trigger)}`,
      ...(definition.conditions.length > 0
        ? [`Nếu: ${this.describeConditions(boardId, definition.conditions)}`]
        : []),
      ...definition.steps.map(
        (step, index) =>
          `Thì ${index + 1}: ${this.describeStep(boardId, step)}`,
      ),
    ];
    const registry = this.store.settings.registry(boardId);
    if (input.mode === "enable" && !rule.live) {
      rule.live = true;
      rule.version = 1;
      rule.versions = [
        { number: 1, at: today, byName: user, summary: summaryOf() },
      ];
      rule.lastEdit = `bật ${isoDay(today)} bởi ${user}`;
      rule.activity = "chưa chạy lần nào";
      registry.automations.push({
        id: rule.id,
        name,
        enabled: true,
        when: this.describeTrigger(boardId, definition.trigger),
        condition:
          definition.conditions.length > 0
            ? this.describeConditions(boardId, definition.conditions)
            : undefined,
        then: definition.steps.map((step) => this.describeStep(boardId, step)),
        stats: "Chưa chạy lần nào",
        runAs: { name: rule.runAs.name, initials: rule.runAs.initials },
      });
      return { result: "ok", value: undefined };
    }
    if (rule.live && input.mode === "version") {
      rule.version += 1;
      rule.versions = [
        { number: rule.version, at: today, byName: user, summary: summaryOf() },
        ...rule.versions,
      ];
      rule.lastEdit = `sửa lần cuối ${isoDay(today)} bởi ${user}`;
    }
    const entry = registry.automations.find((item) => item.id === rule.id);
    if (entry) {
      entry.name = name;
      entry.when = this.describeTrigger(boardId, definition.trigger);
      entry.condition =
        definition.conditions.length > 0
          ? this.describeConditions(boardId, definition.conditions)
          : undefined;
      entry.then = definition.steps.map((step) =>
        this.describeStep(boardId, step),
      );
    }
    return { result: "ok", value: undefined };
  }

  changeRunner(
    boardId: string,
    ruleId: string,
    personId: string,
  ): AutomationWrite {
    const rule = this.rule(boardId, ruleId);
    const person = PEOPLE.find((entry) => entry.id === personId);
    if (!rule || !person) return { result: "not-found" };
    rule.runAs = { ...person, role: "Quản trị Phòng Công nghệ" };
    rule.revision += 1;
    const entry = this.store.settings
      .registry(boardId)
      .automations.find((item) => item.id === rule.id);
    if (entry) {
      entry.runAs = { name: person.name, initials: person.initials };
      if (entry.pausedReason && !entry.enabled) entry.pausedReason = undefined;
    }
    return { result: "ok", value: undefined };
  }

  rerunStep(boardId: string, runId: string): AutomationWrite {
    const run = this.state(boardId).runs.find((entry) => entry.id === runId);
    if (!run) return { result: "not-found" };
    return { result: "invalid", message: "connection" };
  }

  // ── Seed ──

  private optionId(
    boardId: string,
    fieldId: string,
    name: string,
  ): string | null {
    const field = this.catalog(boardId).fields.find((f) => f.id === fieldId);
    return (
      field?.options.find((option) => option.name === name)?.id ??
      field?.options[0]?.id ??
      null
    );
  }

  private seed(boardId: string): BoardAutomations {
    const registry = this.store.settings.registry(boardId);
    const [ha, phuc, , minh] = PEOPLE as [
      RulePerson,
      RulePerson,
      RulePerson,
      RulePerson,
    ];
    const done = this.optionId(boardId, "sf-status", "Hoàn thành");
    const review = this.optionId(boardId, "sf-status", "Chờ duyệt");
    const urgent = this.optionId(boardId, "sf-priority", "Khẩn");
    const accounting = this.optionId(boardId, "sf-module", "Kế toán");
    const warehouse = this.optionId(boardId, "sf-module", "Kho");
    const dataGroup =
      this.catalog(boardId).groups.find((g) => g.name === "Dữ liệu")?.id ??
      this.catalog(boardId).groups[0]?.id ??
      null;
    const runner = (person: RulePerson, role: string) => ({
      ...person,
      role,
    });
    const step = (
      id: string,
      kind: RuleStepKind,
      targetId: string | null,
      note: string | null = null,
    ): RuleStep => ({ id, kind, targetId, note });
    const trig = (
      kind: RuleTriggerKind,
      optionId: string | null = null,
      schedule: RuleSchedule | null = null,
    ): RuleTrigger => ({ kind, optionId, schedule });
    const day = (iso: string) => iso;
    const rules: RuleRecord[] = [
      {
        id: "sa-review",
        definition: {
          name: "Gửi duyệt khi chuyển sang Chờ duyệt",
          trigger: trig("status-changed", review),
          conditions: [],
          steps: [
            step("st-r1", "assign", ha.id),
            step("st-r2", "notify", ROLE_TARGETS.reviewer),
          ],
        },
        revision: 1,
        version: 3,
        live: true,
        runAs: runner(phuc, "Quản trị Phòng Công nghệ"),
        versions: [
          {
            number: 3,
            at: day("2026-10-02T03:00:00.000Z"),
            byName: "Lê Hoàng Phúc",
            summary: [
              "Khi: Trạng thái chuyển sang Chờ duyệt",
              "Thì 1: Giao Nguyễn Thu Hà duyệt",
              "Thì 2: Gửi thông báo cho người duyệt",
            ],
          },
          {
            number: 2,
            at: day("2026-09-18T03:00:00.000Z"),
            byName: null,
            summary: [
              "Khi: Trạng thái chuyển sang Chờ duyệt",
              "Thì 1: Giao Nguyễn Thu Hà duyệt",
            ],
          },
          {
            number: 1,
            at: day("2026-08-14T03:00:00.000Z"),
            byName: null,
            summary: [
              "Khi: Có việc mới",
              "Thì 1: Gửi thông báo cho người duyệt",
            ],
          },
        ],
        lastEdit: "sửa lần cuối 02/10 bởi Lê Hoàng Phúc",
        activity: "chạy 3 lần trong 7 ngày",
        draftNote: "",
        nextRun: null,
      },
      {
        id: "sa-overdue",
        definition: {
          name: "Nhắc việc quá hạn",
          trigger: trig("schedule", null, {
            time: "09:00",
            timeZone: "Asia/Ho_Chi_Minh",
            repeat: "daily",
            days: [1, 2, 3, 4, 5, 6, 7],
            date: null,
            missed: "catch-up",
          }),
          conditions: [
            {
              id: "c-o1",
              fieldId: "sf-due",
              operator: "passed",
              optionId: null,
            },
            {
              id: "c-o2",
              fieldId: "sf-status",
              operator: "is-not",
              optionId: done,
            },
          ],
          steps: [step("st-o1", "remind", ROLE_TARGETS.assignee)],
        },
        revision: 1,
        version: 2,
        live: true,
        runAs: runner(phuc, "Quản trị Phòng Công nghệ"),
        versions: [
          {
            number: 2,
            at: day("2026-09-18T03:00:00.000Z"),
            byName: "Lê Hoàng Phúc",
            summary: [
              "Khi: Theo lịch 09:00 hằng ngày",
              "Nếu: Hạn chót đã qua và Trạng thái không phải Hoàn thành",
              "Thì 1: Nhắc người phụ trách",
            ],
          },
          {
            number: 1,
            at: day("2026-08-20T03:00:00.000Z"),
            byName: null,
            summary: [
              "Khi: Theo lịch 09:00 hằng tuần",
              "Thì 1: Nhắc người phụ trách",
            ],
          },
        ],
        lastEdit: null,
        activity:
          "chạy 7 lần trong 7 ngày · lần cuối 09:00 hôm nay, thành công",
        draftNote: "",
        nextRun: "09:00 Thứ Ba 13/10",
      },
      {
        id: "sa-urgent",
        definition: {
          name: "Khẩn → báo Trưởng phòng",
          trigger: trig("priority-changed", urgent),
          conditions: [],
          steps: [
            step("st-u1", "notify", phuc.id),
            step("st-u2", "teams", "ch-erp"),
          ],
        },
        revision: 1,
        version: 1,
        live: true,
        runAs: runner(phuc, "Quản trị Phòng Công nghệ"),
        versions: [
          {
            number: 1,
            at: day("2026-09-30T03:00:00.000Z"),
            byName: "Lê Hoàng Phúc",
            summary: [
              "Khi: Mức ưu tiên đổi thành Khẩn",
              "Thì 1: Gửi thông báo cho Lê Hoàng Phúc",
              'Thì 2: Đăng tin vào kênh Microsoft Teams "ERP – Điều hành"',
            ],
          },
        ],
        lastEdit: "sửa lần cuối 30/09 bởi Lê Hoàng Phúc",
        activity: "chạy 1 lần trong 7 ngày",
        draftNote: "",
        nextRun: null,
      },
      {
        id: "sa-warehouse",
        definition: {
          name: "Gắn nhóm Dữ liệu cho việc kho",
          trigger: trig("item-created"),
          conditions: [
            {
              id: "c-w1",
              fieldId: "sf-module",
              operator: "is",
              optionId: warehouse,
            },
          ],
          steps: [step("st-w1", "move-group", dataGroup)],
        },
        revision: 1,
        version: 1,
        live: true,
        runAs: runner(minh, "đã rời Phòng Công nghệ"),
        versions: [
          {
            number: 1,
            at: day("2026-10-07T03:00:00.000Z"),
            byName: "Hồ Quang Minh",
            summary: ["Khi: Có việc mới", "Thì 1: Chuyển vào nhóm Dữ liệu"],
          },
        ],
        lastEdit: "tạm dừng từ 08/10",
        activity: "người chạy không còn quyền Sửa",
        draftNote: "",
        nextRun: null,
      },
      {
        id: "sa-draft",
        definition: {
          name: "Báo kế toán khi việc Kế toán hoàn thành",
          trigger: trig("status-changed", done),
          conditions: [
            {
              id: "c-d1",
              fieldId: "sf-module",
              operator: "is",
              optionId: accounting,
            },
          ],
          steps: [
            step(
              "st-d1",
              "notify",
              "t-ketoan",
              "tên việc, người đánh dấu hoàn thành và liên kết tới việc",
            ),
          ],
        },
        revision: 1,
        version: 0,
        live: false,
        runAs: runner(ha, "người tạo"),
        versions: [],
        lastEdit: null,
        activity: "",
        draftNote:
          "Nháp · tạo bởi Nguyễn Thu Hà · chưa chạy lần nào. Nháp được phép thiếu, chỉ cần đủ khi bật.",
        nextRun: null,
      },
      {
        id: "sa-urgent-copy",
        definition: {
          name: "Khẩn → báo Trưởng phòng (bản sao)",
          trigger: trig("priority-changed", urgent),
          conditions: [],
          steps: [
            step("st-c1", "set-today", "sf-handover"),
            step("st-c2", "teams", "ch-erp"),
          ],
        },
        revision: 1,
        version: 0,
        live: false,
        runAs: runner(ha, "người tạo bản sao"),
        versions: [],
        lastEdit: null,
        activity: "",
        draftNote:
          "Bản sao của Khẩn → báo Trưởng phòng · tạo bởi Nguyễn Thu Hà · chưa bật",
        nextRun: null,
      },
    ];
    void registry;
    const key = (
      rule: string,
      at: string,
      base: Partial<RunRecord>,
    ): RunRecord => ({
      id: "",
      ruleId: rule,
      at,
      trigger: { itemKey: null, text: "", detail: null, detailItemKey: null },
      outcome: "success",
      summary: "",
      brief: "",
      warning: false,
      stepsDone: 0,
      stepsTotal: 0,
      runAs: phuc,
      version: 1,
      firedBy: "",
      condition: "Không có",
      steps: [],
      ...base,
    });
    const overdue = (at: string, matched: boolean): Partial<RunRecord> => ({
      trigger: {
        itemKey: null,
        text: "Lịch 09:00",
        detail: matched ? "1 việc khớp: {key}" : "0 việc quá hạn",
        detailItemKey: matched ? "ERP-142" : null,
      },
      outcome: matched ? "success" : "skipped",
      summary: matched
        ? "Đã nhắc Trần Minh Quân"
        : "Không khớp điều kiện, không phải lỗi",
      brief: matched ? "1 việc khớp: ERP-142" : "Không khớp điều kiện",
      stepsDone: matched ? 1 : 0,
      stepsTotal: 1,
      version: 2,
      firedBy: "Lịch 09:00 hằng ngày",
      condition: "Hạn chót đã qua và Trạng thái không phải Hoàn thành",
      steps: matched
        ? [
            {
              index: 1,
              title: "Nhắc Trần Minh Quân",
              outcome: "success",
              at,
              error: null,
            },
          ]
        : [],
    });
    const review1 = (itemKey: string, at: string): Partial<RunRecord> => ({
      trigger: {
        itemKey,
        text: "Trạng thái → Chờ duyệt",
        detail: null,
        detailItemKey: null,
      },
      outcome: "success",
      summary: "2/2 bước",
      brief: "2/2 bước",
      stepsDone: 2,
      stepsTotal: 2,
      version: 3,
      firedBy: `Trạng thái đổi sang Chờ duyệt · {key}`,
      steps: [
        {
          index: 1,
          title: "Giao Nguyễn Thu Hà duyệt",
          outcome: "success",
          at,
          error: null,
        },
        {
          index: 2,
          title: "Gửi thông báo cho người duyệt",
          outcome: "success",
          at,
          error: null,
        },
      ],
    });
    const runs: RunRecord[] = [
      key(
        "sa-review",
        "2026-10-12T03:28:00.000Z",
        review1("ERP-138", "2026-10-12T03:28:00.000Z"),
      ),
      key(
        "sa-overdue",
        "2026-10-12T02:00:00.000Z",
        overdue("2026-10-12T02:00:00.000Z", true),
      ),
      key(
        "sa-overdue",
        "2026-10-11T02:00:00.000Z",
        overdue("2026-10-11T02:00:00.000Z", true),
      ),
      key("sa-urgent", "2026-10-10T11:05:00.000Z", {
        trigger: {
          itemKey: "ERP-142",
          text: "Mức ưu tiên → Khẩn",
          detail: null,
          detailItemKey: null,
        },
        outcome: "error",
        summary: "1/2 bước",
        brief: "1/2 bước",
        stepsDone: 1,
        stepsTotal: 2,
        version: 1,
        firedBy:
          "Mức ưu tiên đổi thành Khẩn · {key} · bởi Trần Minh Quân 18:05",
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
      }),
      key(
        "sa-overdue",
        "2026-10-10T02:00:00.000Z",
        overdue("2026-10-10T02:00:00.000Z", false),
      ),
      key(
        "sa-review",
        "2026-10-09T08:12:00.000Z",
        review1("ERP-140", "2026-10-09T08:12:00.000Z"),
      ),
      key(
        "sa-overdue",
        "2026-10-09T02:00:00.000Z",
        overdue("2026-10-09T02:00:00.000Z", false),
      ),
      key("sa-warehouse", "2026-10-08T04:20:00.000Z", {
        trigger: {
          itemKey: null,
          text: "Có việc mới",
          detail: null,
          detailItemKey: null,
        },
        outcome: "skipped",
        summary: "Không chạy: người chạy không còn quyền",
        brief: "Không chạy: người chạy không còn quyền",
        warning: true,
        stepsDone: 0,
        stepsTotal: 1,
        runAs: minh,
        version: 1,
        firedBy: "Có việc mới",
        condition: "Phân hệ ERP là Kho",
      }),
      key(
        "sa-overdue",
        "2026-10-08T02:00:00.000Z",
        overdue("2026-10-08T02:00:00.000Z", false),
      ),
    ].map((run, index) => ({ ...run, id: `run-${index + 1}` }));
    return { rules, runs, nextRule: 1, nextStep: 100 };
  }
}
