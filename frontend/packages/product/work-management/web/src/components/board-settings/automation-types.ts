import type {
  AutomationRuleDetailDto,
  AutomationRuleDocumentDto,
  AutomationRunsDto,
  AutomationWriteResult,
  BoardSettingsWriteResult,
} from "@notrelix/work-management-core";

/** What the automation part of the settings page reads, per piece. */
export interface AutomationQueryState<T> {
  readonly status: "idle" | "loading" | "error" | "not-found" | "ready";
  readonly data: T | null;
}

/** The writes of the automation editor and the run log. */
export interface AutomationActions {
  save(
    definition: AutomationRuleDocumentDto,
    mode: "draft" | "enable" | "version",
  ): Promise<AutomationWriteResult>;
  /** Turn a saved rule on or off (the switch of the header and of the list). */
  setEnabled(
    ruleId: string,
    enabled: boolean,
  ): Promise<BoardSettingsWriteResult>;
  changeRunner(personId: string): Promise<AutomationWriteResult>;
  restoreField(fieldId: string): Promise<BoardSettingsWriteResult>;
  rerunStep(runId: string, stepIndex: number): Promise<AutomationWriteResult>;
}

/**
 * Where the automations section is and what it shows: the list of rules, one
 * rule in its editor, or the run log (with one run open). The address of the
 * page holds `rule`, `log` and `run`; the host maps them to these callbacks.
 */
export interface AutomationPanelProps {
  readonly ruleId: string | null;
  readonly log: boolean;
  readonly runId: string | null;
  readonly rule: AutomationQueryState<AutomationRuleDetailDto>;
  readonly runs: AutomationQueryState<AutomationRunsDto>;
  readonly onOpenRule: (ruleId: string) => void;
  /** Creates a draft and opens it. */
  readonly onCreateRule: () => Promise<boolean>;
  readonly onCloseRule: () => void;
  readonly onOpenLog: () => void;
  readonly onOpenRun: (runId: string | null) => void;
  readonly onReloadRule: () => void;
  readonly onReloadRuns: () => void;
  readonly actions: AutomationActions;
  /** For stories and tests: start with a picker of the editor open. */
  readonly defaultPicker?: "event" | "action";
  /** The Integrations page; absent until it exists. */
  readonly integrationsHref?: string;
  readonly billingHref?: string;
}
