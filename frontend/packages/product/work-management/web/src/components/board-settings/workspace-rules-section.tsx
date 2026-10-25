import { useId, useState, type FormEvent } from "react";
import { History, Lock, Plus, TriangleAlert, Zap } from "lucide-react";
import { Skeleton } from "@notrelix/ui-web";
import type {
  WorkspaceAutomationRuleDto,
  WorkspaceAutomationRunStatus,
  WorkspaceAutomationTrigger,
  WorkspaceAutomationWriteResult,
} from "@notrelix/work-management-core";
import { useSettings } from "./board-settings-context";
import { fillCopy } from "./board-settings-format";
import {
  BUTTON_CLASS,
  Callout,
  FormGroup,
  INPUT_CLASS,
  SectionHeader,
  SectionLine,
  SwitchControl,
  Tag_,
} from "./board-settings-parts";
import type {
  WorkspaceRuleChoice,
  WorkspaceRulesPanelProps,
} from "./board-settings-types";
import type { WorkspaceRulesCopy } from "./workspace-rules.copy";

const TRIGGERS: readonly WorkspaceAutomationTrigger[] = [
  "ItemMovedToGroup",
  "ItemCreated",
];

/** "When an item is created, move X to Y" in the words of the copy. */
function ruleSentence(
  rule: WorkspaceAutomationRuleDto,
  copy: WorkspaceRulesCopy,
  items: readonly WorkspaceRuleChoice[],
  groups: readonly WorkspaceRuleChoice[],
): string {
  const triggers: Readonly<Record<string, string>> = copy.triggers;
  const triggerGroup = rule.triggerGroupId
    ? groups.find((choice) => choice.id === rule.triggerGroupId)
    : undefined;
  const trigger =
    rule.trigger === "ItemMovedToGroup" && triggerGroup
      ? fillCopy(copy.triggerMovedTo, { group: `“${triggerGroup.name}”` })
      : (triggers[rule.trigger] ?? rule.trigger);
  let action: string;
  if (rule.action === "MoveItem" && rule.targetGroupId) {
    const group = groups.find((choice) => choice.id === rule.targetGroupId);
    action = fillCopy(copy.actions.move, {
      group: group ? `“${group.name}”` : copy.groupUnknown,
    });
  } else if (rule.action === "MoveItem") {
    action = copy.actions.moveUnknown;
  } else {
    action = fillCopy(copy.actions.other, { action: rule.action });
  }
  return fillCopy(copy.line, { trigger, action });
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
              <NewRuleForm
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
            {ruleSentence(rule, copy, panel.items, panel.groups)}
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
      <div className="mt-2 flex items-center">
        <button
          type="button"
          aria-expanded={open}
          onClick={() => panel.onOpenRuns(open ? null : rule.id)}
          className={BUTTON_CLASS.ghostBrand}
        >
          <History aria-hidden="true" />
          {open ? copy.history.hide : copy.history.show}
        </button>
      </div>
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
          {run.attempts > 1 ? (
            <span>{fillCopy(t.attempts, { count: run.attempts })}</span>
          ) : null}
          {run.error ? (
            <span className="text-danger-text">{run.error}</span>
          ) : null}
        </li>
      ))}
    </ul>
  );
}

function NewRuleForm({
  panel,
  copy,
  onDone,
  onCancel,
}: {
  panel: WorkspaceRulesPanelProps;
  copy: WorkspaceRulesCopy;
  onDone: (result: WorkspaceAutomationWriteResult) => void;
  onCancel: () => void;
}) {
  const t = copy.form;
  const id = useId();
  const [name, setName] = useState("");
  const [trigger, setTrigger] =
    useState<WorkspaceAutomationTrigger>("ItemMovedToGroup");
  const [triggerGroupId, setTriggerGroupId] = useState("");
  const [groupId, setGroupId] = useState("");
  const [busy, setBusy] = useState(false);
  const needsTriggerGroup = trigger === "ItemMovedToGroup";
  const ready =
    name.trim() !== "" &&
    groupId !== "" &&
    (!needsTriggerGroup || triggerGroupId !== "");

  function submit(event: FormEvent) {
    event.preventDefault();
    if (!ready || busy) return;
    setBusy(true);
    void panel
      .onCreate({
        name: name.trim(),
        trigger,
        boardId: panel.boardId,
        ...(needsTriggerGroup ? { triggerGroupId } : {}),
        targetGroupId: groupId,
      })
      .then(onDone)
      .finally(() => setBusy(false));
  }

  return (
    <form
      onSubmit={submit}
      aria-label={t.title}
      className="mb-3.5 flex flex-col gap-3 rounded-[10px] border border-line bg-popover p-3.5"
    >
      <FormGroup label={t.name} htmlFor={`${id}-name`}>
        <input
          id={`${id}-name`}
          value={name}
          maxLength={160}
          placeholder={t.namePlaceholder}
          onChange={(event) => setName(event.target.value)}
          className={INPUT_CLASS}
        />
      </FormGroup>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <FormGroup label={t.trigger} htmlFor={`${id}-trigger`}>
          <select
            id={`${id}-trigger`}
            value={trigger}
            onChange={(event) =>
              setTrigger(event.target.value as WorkspaceAutomationTrigger)
            }
            className={INPUT_CLASS}
          >
            {TRIGGERS.map((key) => (
              <option key={key} value={key}>
                {copy.triggers[key]}
              </option>
            ))}
          </select>
        </FormGroup>
        <FormGroup label={t.action} htmlFor={`${id}-action`}>
          <select id={`${id}-action`} disabled className={INPUT_CLASS}>
            <option>{t.actionMove}</option>
          </select>
        </FormGroup>
        {needsTriggerGroup ? (
          <FormGroup label={t.triggerGroup} htmlFor={`${id}-trigger-group`}>
            <select
              id={`${id}-trigger-group`}
              value={triggerGroupId}
              onChange={(event) => setTriggerGroupId(event.target.value)}
              className={INPUT_CLASS}
            >
              <option value="">{t.choose}</option>
              {panel.groups.map((group) => (
                <option key={group.id} value={group.id}>
                  {group.name}
                </option>
              ))}
            </select>
          </FormGroup>
        ) : null}
        <FormGroup label={t.group} htmlFor={`${id}-group`}>
          <select
            id={`${id}-group`}
            value={groupId}
            onChange={(event) => setGroupId(event.target.value)}
            className={INPUT_CLASS}
          >
            <option value="">{t.choose}</option>
            {panel.groups.map((group) => (
              <option key={group.id} value={group.id}>
                {group.name}
              </option>
            ))}
          </select>
        </FormGroup>
      </div>
      <p className="m-0 text-crumb text-meta-foreground">{t.hint}</p>
      <div className="flex items-center gap-2">
        <button
          type="submit"
          disabled={!ready || busy}
          className={BUTTON_CLASS.primary}
        >
          {t.submit}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className={BUTTON_CLASS.outline}
        >
          {t.cancel}
        </button>
      </div>
    </form>
  );
}
