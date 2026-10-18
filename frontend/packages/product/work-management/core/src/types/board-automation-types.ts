/**
 * The automation rules of a board: the editor ("Khi / Nếu / Thì"), the run
 * log and one run. Gap `CTR-GAP-WM-BOARD-AUTOMATIONS`: the generated contract
 * has none of these reads, so the shapes are the ones the frontend needs.
 */

export type AutomationTriggerKind =
  | "item-created"
  | "status-changed"
  | "priority-changed"
  | "assignee-changed"
  | "due-passed"
  | "comment-added"
  | "schedule"
  | "manual";

export type AutomationScheduleRepeat = "daily" | "weekly" | "date";

export interface AutomationScheduleDto {
  /** "09:00". */
  readonly time: string;
  /** "Asia/Ho_Chi_Minh". */
  readonly timeZone: string;
  readonly repeat: AutomationScheduleRepeat;
  /** ISO weekdays that run, 1 = Monday .. 7 = Sunday. */
  readonly days: readonly number[];
  /** The day of a "date" schedule (yyyy-mm-dd). */
  readonly date: string | null;
  /** What happens when the hour was missed. */
  readonly missed: "catch-up" | "skip";
}

export interface AutomationTriggerDto {
  readonly kind: AutomationTriggerKind;
  /** The target status or priority option a change trigger waits for. */
  readonly optionId: string | null;
  readonly schedule: AutomationScheduleDto | null;
}

export type AutomationOperator = "is" | "is-not" | "passed" | "is-empty";

export interface AutomationConditionDto {
  readonly id: string;
  readonly fieldId: string;
  readonly operator: AutomationOperator;
  readonly optionId: string | null;
}

export type AutomationStepKind =
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

export interface AutomationStepDto {
  readonly id: string;
  readonly kind: AutomationStepKind;
  /**
   * What the step acts on: a person, a team or a role (notify, assign,
   * email), a status option, a field, a group, a channel or a webhook.
   */
  readonly targetId: string | null;
  /** The text of a notification or message, when the step has one. */
  readonly note: string | null;
}

export interface AutomationRuleDocumentDto {
  readonly name: string;
  readonly trigger: AutomationTriggerDto;
  readonly conditions: readonly AutomationConditionDto[];
  readonly steps: readonly AutomationStepDto[];
}

export type AutomationCheckLevel = "ok" | "error" | "warning";

/** What a check says; the page words each code. */
export type AutomationCheckCode =
  | "trigger-valid"
  | "condition-valid"
  | "step-field-archived"
  | "step-teams-reauthorization"
  | "team-can-see-board"
  | "runner-can-notify"
  | "rule-valid"
  | "no-steps";

export interface AutomationCheckDto {
  readonly id: string;
  readonly level: AutomationCheckLevel;
  readonly code: AutomationCheckCode;
  /** The names and numbers the wording needs ("event", "field", "step"). */
  readonly params: Readonly<Record<string, string | number>>;
  /** Ways to fix it, drawn as buttons under the message. */
  readonly fixes: readonly {
    readonly kind: "restore-field" | "choose-field" | "reauthorize";
    readonly fieldId?: string;
    readonly stepId?: string;
  }[];
}

export interface AutomationPersonDto {
  readonly id: string;
  readonly name: string;
  readonly initials: string;
  readonly color: string;
}

export interface AutomationOptionDto {
  readonly id: string;
  readonly name: string;
}

export interface AutomationFieldChoiceDto {
  readonly id: string;
  readonly name: string;
  readonly type: string;
  readonly archived: boolean;
  /** Values a condition may compare with (select and status fields). */
  readonly options: readonly AutomationOptionDto[];
}

export interface AutomationCatalogDto {
  readonly statuses: readonly AutomationOptionDto[];
  readonly priorities: readonly AutomationOptionDto[];
  readonly fields: readonly AutomationFieldChoiceDto[];
  readonly groups: readonly AutomationOptionDto[];
  readonly people: readonly AutomationPersonDto[];
  readonly teams: readonly (AutomationOptionDto & {
    readonly memberCount: number;
  })[];
  readonly channels: readonly AutomationOptionDto[];
  readonly webhooks: readonly AutomationOptionDto[];
  /** The Teams connection needs the workspace to authorise it again. */
  readonly teamsNeedsReauthorization: boolean;
  /** The board has no email address yet (the "email to board" trigger). */
  readonly boardHasEmailAddress: boolean;
}

export interface AutomationVersionDto {
  readonly number: number;
  readonly at: string;
  readonly byName: string | null;
  /** The one-line summary the "Xem" button shows. */
  readonly summary: readonly string[];
}

export interface AutomationRecentRunDto {
  readonly id: string;
  readonly at: string;
  readonly brief: string;
  readonly outcome: AutomationRunOutcome;
}

export interface AutomationRuleDetailDto {
  readonly boardId: string;
  readonly id: string;
  /** Version of the rule: a save sends it back to detect a conflict. */
  readonly revision: number;
  readonly status: "draft" | "on" | "off";
  readonly definition: AutomationRuleDocumentDto;
  /** The line under the name ("Phiên bản 3 · sửa lần cuối 02/10 …"). */
  readonly subtitle: string;
  /** Version number of the saved rule (0 for a draft). */
  readonly version: number;
  readonly runAs: AutomationPersonDto & { readonly role: string };
  readonly scope: { readonly boardTitle: string; readonly detail: string };
  readonly versions: readonly AutomationVersionDto[];
  readonly recentRuns: readonly AutomationRecentRunDto[];
  readonly checks: readonly AutomationCheckDto[];
  /** The checks cannot pass until a blocking error is fixed. */
  readonly blocked: boolean;
  readonly checkedAt: string;
  /** "Lần chạy tới" of a schedule rule. */
  readonly nextRun: string | null;
  readonly catalog: AutomationCatalogDto;
}

export type AutomationRunOutcome = "success" | "error" | "skipped";

export interface AutomationRunStepDto {
  readonly index: number;
  readonly title: string;
  readonly outcome: "success" | "error";
  readonly at: string;
  readonly error: string | null;
}

export interface AutomationRunFixDto {
  readonly kind: "reauthorize" | "rerun-step";
  readonly title: string;
  readonly detail: string;
  /** Why the button cannot be pressed yet. */
  readonly blockedReason: string | null;
}

export interface AutomationRunDto {
  readonly id: string;
  readonly ruleId: string;
  readonly ruleName: string;
  readonly at: string;
  /** Trigger of the row: an item key and a text, or a text alone. */
  readonly trigger: {
    readonly itemKey: string | null;
    readonly text: string;
    /** A second line; "{key}" stands for the item key link. */
    readonly detail: string | null;
    readonly detailItemKey: string | null;
  };
  readonly outcome: AutomationRunOutcome;
  /** "2/2 bước", "Đã nhắc Trần Minh Quân", "Không khớp điều kiện…". */
  readonly summary: string;
  /** The run was skipped because the runner lost access: a warning, not a note. */
  readonly warning: boolean;
  readonly stepsDone: number;
  readonly stepsTotal: number;
  readonly runAs: AutomationPersonDto;
  readonly version: number;
  readonly versionIsCurrent: boolean;
  /** What fired the run; "{key}" stands for the item key link. */
  readonly firedBy: string;
  readonly condition: string;
  readonly steps: readonly AutomationRunStepDto[];
  readonly fixes: readonly AutomationRunFixDto[];
}

export interface AutomationRunsDto {
  readonly boardId: string;
  /** The read time (ISO) the log's dates are measured from. */
  readonly generatedAt: string;
  readonly runs: readonly AutomationRunDto[];
  readonly rules: readonly AutomationOptionDto[];
  readonly usage: { readonly month: number; readonly runs: number };
}

export type AutomationWriteResult =
  | { ok: true; id?: string }
  | { ok: false; reason: "conflict" | "invalid" | "failed"; field?: string };
