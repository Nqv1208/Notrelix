import { useId, useState } from "react";
import {
  History,
  Lock,
  Pencil,
  Plus,
  Trash2,
  TriangleAlert,
  Zap,
} from "lucide-react";
import { Skeleton } from "@notrelix/ui-web";
import type {
  AutomationActionInput,
  AutomationConditionInput,
  WorkspaceAutomationRuleDto,
  WorkspaceAutomationRunStatus,
  WorkspaceAutomationWriteResult,
} from "@notrelix/work-management-core";
import { useSettings } from "./board-settings-context";
import { fillCopy } from "./board-settings-format";
import {
  BUTTON_CLASS,
  Callout,
  SectionHeader,
  SectionLine,
  SwitchControl,
  Tag_,
} from "./board-settings-parts";
import type {
  WorkspaceRuleChoice,
  WorkspaceRulesPanelProps,
} from "./board-settings-types";
import { WorkspaceRuleEditor } from "./workspace-rule-editor";
import type { WorkspaceRulesCopy } from "./workspace-rules.copy";

type Lookup = (
  list: readonly WorkspaceRuleChoice[],
  id: string,
  copy: WorkspaceRulesCopy,
) => string;

/** The name of a choice in quotes, or the words for something that is gone. */
const named: Lookup = (list, id, copy) => {
  const choice = list.find((candidate) => candidate.id === id);
  return choice ? `“${choice.name}”` : copy.unknown;
};

function actionText(
  action: AutomationActionInput,
  copy: WorkspaceRulesCopy,
  panel: WorkspaceRulesPanelProps,
): string {
  const t = copy.actionLabels;
  switch (action.type) {
    case "MoveItem":
      return fillCopy(t.MoveItem, {
        group: named(panel.groups, action.groupId, copy),
      });
    case "ChangeStatus":
      return fillCopy(t.ChangeStatus, {
        status: named(panel.statuses, action.optionId, copy),
      });
    case "AssignPerson":
      return fillCopy(t.AssignPerson, {
        person: named(panel.people, action.userId, copy),
      });
    case "UpdateField":
      return fillCopy(t.UpdateField, {
        field: named(panel.fields, action.fieldId, copy),
        value: `“${action.value}”`,
      });
    case "NotifyMember":
      return fillCopy(t.NotifyMember, {
        person: named(panel.people, action.userId, copy),
      });
    case "SendEmail":
      return fillCopy(t.SendEmail, {
        person: named(panel.people, action.userId, copy),
      });
  }
}

function conditionText(
  condition: AutomationConditionInput,
  copy: WorkspaceRulesCopy,
  panel: WorkspaceRulesPanelProps,
): string {
  const t = copy.conditionLabels;
  switch (condition.type) {
    case "group":
      return fillCopy(t.group, {
        group: named(panel.groups, condition.groupId, copy),
      });
    case "status":
      return fillCopy(t.status, {
        status: named(panel.statuses, condition.optionId, copy),
      });
    case "field":
      return fillCopy(t.field, {
        field: named(panel.fields, condition.fieldId, copy),
        value: `“${condition.value}”`,
      });
  }
}

/** "When an item is created and its status is X, set Y, then notify Z" in the words of the copy. */
function ruleSentence(
  rule: WorkspaceAutomationRuleDto,
  copy: WorkspaceRulesCopy,
  panel: WorkspaceRulesPanelProps,
): string {
  const triggers: Readonly<Record<string, string>> = copy.triggers;
  const trigger =
    rule.trigger === "ItemMovedToGroup" && rule.triggerGroupId
      ? fillCopy(copy.triggerMovedTo, {
          group: named(panel.groups, rule.triggerGroupId, copy),
        })
      : (triggers[rule.trigger] ?? rule.trigger);
  const conditions =
    rule.conditions.length > 0
      ? fillCopy(copy.ifPart, {
          conditions: rule.conditions
            .map((condition) => conditionText(condition, copy, panel))
            .join(copy.andJoin),
        })
      : "";
  const actions = rule.actions
    .map((action) => actionText(action, copy, panel))
    .join(copy.thenJoin);
  return fillCopy(copy.line, { trigger, conditions, actions });
}

function feedbackText(
  copy: WorkspaceRulesCopy,
  result: WorkspaceAutomationWriteResult,
): string | null {
  return result.ok ? null : copy.feedback[result.reason];
}

/** The "Tự động hoá" section when the backend's workspace rules are shown. */
export function WorkspaceRulesSection({
  panel,
}: {
  panel: WorkspaceRulesPanelProps;
}) {
  const { copy: board } = useSettings();
  const copy = board.automations.rules;
  const [creating, setCreating] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);

  const running = panel.rules.filter((rule) => rule.enabled);
  const off = panel.rules.filter((rule) => !rule.enabled);

  return (
    <section
      aria-labelledby="bs-rules-h"
      className="min-w-0 flex-1 overflow-auto px-6 pt-5 pb-6"
    >
      <div className="max-w-[780px]">
        <SectionHeader
          id="bs-rules-h"
          title={copy.title}
          intro={copy.intro}
          actions={
            panel.status === "ready" ? (
              <button
                type="button"
                aria-expanded={creating}
                onClick={() => setCreating((open) => !open)}
                className={BUTTON_CLASS.primary}
              >
                <Plus aria-hidden="true" />
                {copy.create}
              </button>
            ) : null
          }
        />
        {panel.status === "loading" ? (
          <div
            role="status"
            aria-label={copy.loading}
            className="flex flex-col gap-2.5"
          >
            <Skeleton className="h-16 rounded-[10px]" />
            <Skeleton className="h-16 rounded-[10px]" />
          </div>
        ) : null}
        {panel.status === "error" ? (
          <div role="alert" className="flex flex-col items-start gap-2.5">
            <Callout icon={TriangleAlert} tone="warning">
              {copy.error}
            </Callout>
            <button
              type="button"
              onClick={panel.onReload}
              className={BUTTON_CLASS.outline}
            >
              {copy.retry}
            </button>
          </div>
        ) : null}
        {panel.status === "forbidden" ? (
          <Callout icon={Lock} role="status">
            <span>
              <strong>{copy.forbidden.title}.</strong> {copy.forbidden.body}
            </span>
          </Callout>
        ) : null}
        {panel.status === "ready" ? (
          <>
            {feedback ? (
              <Callout
                icon={TriangleAlert}
                tone="warning"
                role="alert"
                className="mb-3"
              >
                {feedback}
              </Callout>
            ) : null}
            {creating ? (
              <WorkspaceRuleEditor
                panel={panel}
                copy={copy}
                onDone={(result) => {
                  setFeedback(feedbackText(copy, result));
                  if (result.ok) setCreating(false);
                }}
                onCancel={() => setCreating(false)}
              />
            ) : null}
            {panel.rules.length === 0 ? (
              <p className="m-0 text-item-small text-meta-foreground">
                {copy.empty}
              </p>
            ) : null}
            {running.length > 0 ? (
              <>
                <SectionLine
                  title={copy.running}
                  count={running.length}
                  note={copy.scope}
                />
                <RuleList
                  rules={running}
                  panel={panel}
                  copy={copy}
                  onResult={(result) => setFeedback(feedbackText(copy, result))}
                />
              </>
            ) : null}
            {off.length > 0 ? (
              <>
                <SectionLine
                  title={copy.off}
                  count={off.length}
                  note={running.length === 0 ? copy.scope : undefined}
                />
                <RuleList
                  rules={off}
                  panel={panel}
                  copy={copy}
                  onResult={(result) => setFeedback(feedbackText(copy, result))}
                />
              </>
            ) : null}
          </>
        ) : null}
      </div>
    </section>
  );
}

function RuleList({
  rules,
  panel,
  copy,
  onResult,
}: {
  rules: readonly WorkspaceAutomationRuleDto[];
  panel: WorkspaceRulesPanelProps;
  copy: WorkspaceRulesCopy;
  onResult: (result: WorkspaceAutomationWriteResult) => void;
}) {
  return (
    <ul className="m-0 flex list-none flex-col gap-2.5 p-0">
      {rules.map((rule) => (
        <RuleCard
          key={rule.id}
          rule={rule}
          panel={panel}
          copy={copy}
          onResult={onResult}
        />
      ))}
    </ul>
  );
}

function RuleCard({
  rule,
  panel,
  copy,
  onResult,
}: {
  rule: WorkspaceAutomationRuleDto;
  panel: WorkspaceRulesPanelProps;
  copy: WorkspaceRulesCopy;
  onResult: (result: WorkspaceAutomationWriteResult) => void;
}) {
  const [busy, setBusy] = useState(false);
  const open = panel.openRuleId === rule.id;
  const sentenceId = useId();
  const [editing, setEditing] = useState(false);
  const [confirming, setConfirming] = useState(false);
  if (editing)
    return (
      <li>
        <WorkspaceRuleEditor
          panel={panel}
          copy={copy}
          rule={rule}
          onDone={(result) => {
            onResult(result);
            if (result.ok) setEditing(false);
          }}
          onCancel={() => setEditing(false)}
        />
      </li>
    );
  return (
    <li
      aria-labelledby={`${sentenceId}-n`}
      className="rounded-[10px] border border-line bg-popover px-3.5 py-3"
    >
      <div className="flex items-start gap-3">
        <span
          aria-hidden="true"
          className="mt-0.5 grid size-7 shrink-0 place-items-center rounded-lg bg-brand-subtle text-brand-subtle-foreground"
        >
          <Zap className="size-3.5" />
        </span>
        <div className="min-w-0 flex-1">
          <p
            id={`${sentenceId}-n`}
            className="m-0 text-item-ui font-semibold break-words"
          >
            {rule.name}
          </p>
          <p
            id={sentenceId}
            className="mt-0.5 mb-0 text-item-small text-meta-foreground"
          >
            {ruleSentence(rule, copy, panel)}
          </p>
          <p className="mt-1 mb-0 text-crumb text-meta-foreground">
            {fillCopy(copy.version, { version: rule.version })}
          </p>
          {rule.disabledReason ? (
            <p className="mt-1.5 mb-0">
              <Tag_ tone="warning">
                {fillCopy(copy.disabledBy, { reason: rule.disabledReason })}
              </Tag_>
            </p>
          ) : null}
        </div>
        <SwitchControl
          checked={rule.enabled}
          disabled={busy}
          label={fillCopy(copy.toggle, { name: rule.name })}
          describedBy={sentenceId}
          onChange={(next) => {
            setBusy(true);
            void panel
              .onSetEnabled(rule.id, next)
              .then(onResult)
              .finally(() => setBusy(false));
          }}
        />
      </div>
      <div className="mt-2 flex flex-wrap items-center gap-1">
        <button
          type="button"
          aria-expanded={open}
          onClick={() => panel.onOpenRuns(open ? null : rule.id)}
          className={BUTTON_CLASS.ghostBrand}
        >
          <History aria-hidden="true" />
          {open ? copy.history.hide : copy.history.show}
        </button>
        <button
          type="button"
          aria-label={fillCopy(copy.editName, { name: rule.name })}
          onClick={() => setEditing(true)}
          className={BUTTON_CLASS.ghost}
        >
          <Pencil aria-hidden="true" />
          {copy.edit}
        </button>
        <button
          type="button"
          aria-label={fillCopy(copy.deleteName, { name: rule.name })}
          aria-expanded={confirming}
          onClick={() => setConfirming((value) => !value)}
          className={BUTTON_CLASS.ghost}
        >
          <Trash2 aria-hidden="true" />
          {copy.delete}
        </button>
      </div>
      {confirming ? (
        <div
          role="alertdialog"
          aria-label={copy.confirmDelete.title}
          className="mt-2 flex flex-col gap-2 rounded-lg border border-line bg-popover px-3 py-2.5"
        >
          <p className="m-0 text-item-small font-semibold">
            {copy.confirmDelete.title}
          </p>
          <p className="m-0 text-crumb text-meta-foreground">
            {copy.confirmDelete.body}
          </p>
          <div className="flex items-center gap-2">
            <button
              type="button"
              disabled={busy}
              onClick={() => {
                setBusy(true);
                void panel
                  .onDelete(rule.id)
                  .then((result) => {
                    onResult(result);
                    if (!result.ok) setConfirming(false);
                  })
                  .finally(() => setBusy(false));
              }}
              className={BUTTON_CLASS.danger}
            >
              {copy.confirmDelete.confirm}
            </button>
            <button
              type="button"
              onClick={() => setConfirming(false)}
              className={BUTTON_CLASS.outline}
            >
              {copy.confirmDelete.cancel}
            </button>
          </div>
        </div>
      ) : null}
      {open ? <RuleRuns panel={panel} copy={copy} /> : null}
    </li>
  );
}

const RUN_TONE: Readonly<
  Record<WorkspaceAutomationRunStatus, "brand" | "warning">
> = {
  queued: "brand",
  running: "brand",
  succeeded: "brand",
  failed: "warning",
  cancelled: "warning",
};

/** The name of an action type in the editor's words, or the type itself. */
function stepType(copy: WorkspaceRulesCopy, type: string): string {
  const types: Readonly<Record<string, string>> = copy.form.actionTypes;
  return types[type] ?? type;
}

function RuleRuns({
  panel,
  copy,
}: {
  panel: WorkspaceRulesPanelProps;
  copy: WorkspaceRulesCopy;
}) {
  const t = copy.history;
  const { runs } = panel;
  if (runs.status === "loading")
    return (
      <p role="status" className="mt-2 mb-0 text-crumb text-meta-foreground">
        {t.loading}
      </p>
    );
  if (runs.status === "error")
    return (
      <p role="alert" className="mt-2 mb-0 text-crumb text-danger-text">
        {t.error}
      </p>
    );
  if (runs.data.length === 0)
    return (
      <p className="mt-2 mb-0 text-crumb text-meta-foreground">{t.empty}</p>
    );
  return (
    <ul
      aria-label={t.show}
      className="mt-2 mb-0 flex list-none flex-col gap-1.5 border-t border-line p-0 pt-2"
    >
      {runs.data.map((run) => (
        <li
          key={run.id}
          className="flex flex-wrap items-center gap-2 text-crumb text-muted-foreground"
        >
          <Tag_ tone={RUN_TONE[run.status]}>{t.status[run.status]}</Tag_>
          <time dateTime={run.createdAt}>
            {new Date(run.createdAt).toLocaleString()}
          </time>
          {run.ruleVersion ? (
            <span>{fillCopy(t.version, { version: run.ruleVersion })}</span>
          ) : null}
          {run.attempts > 1 ? (
            <span>{fillCopy(t.attempts, { count: run.attempts })}</span>
          ) : null}
          {run.error ? (
            <span className="text-danger-text">{run.error}</span>
          ) : null}
          {run.steps.length > 0 ? (
            <ol className="m-0 flex basis-full list-none flex-col gap-1 p-0 pl-2">
              {run.steps.map((step) => (
                <li key={step.index} className="flex flex-wrap gap-2">
                  <Tag_ tone={step.status === "failed" ? "warning" : "brand"}>
                    {t.stepStatus[step.status]}
                  </Tag_>
                  <span>
                    {fillCopy(t.step, {
                      index: step.index + 1,
                      type: stepType(copy, step.type),
                    })}
                  </span>
                  {step.error ? (
                    <span className="text-danger-text">{step.error}</span>
                  ) : null}
                </li>
              ))}
            </ol>
          ) : null}
        </li>
      ))}
    </ul>
  );
}
