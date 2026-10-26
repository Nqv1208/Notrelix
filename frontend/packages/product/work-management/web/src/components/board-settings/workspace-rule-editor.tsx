import { useId, useState, type FormEvent } from "react";
import { ArrowDown, ArrowUp, Plus, X } from "lucide-react";
import {
  MAX_AUTOMATION_ACTIONS,
  type AutomationActionInput,
  type AutomationActionType,
  type AutomationConditionInput,
  type WorkspaceAutomationDefinition,
  type WorkspaceAutomationRuleDto,
  type WorkspaceAutomationTrigger,
  type WorkspaceAutomationWriteResult,
} from "@notrelix/work-management-core";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@notrelix/ui-web";
import { fillCopy } from "./board-settings-format";
import { BUTTON_CLASS, FormGroup, INPUT_CLASS } from "./board-settings-parts";
import type {
  WorkspaceRuleChoice,
  WorkspaceRulesPanelProps,
} from "./board-settings-types";
import type { WorkspaceRulesCopy } from "./workspace-rules.copy";

const TRIGGERS: readonly WorkspaceAutomationTrigger[] = [
  "ItemMovedToGroup",
  "ItemCreated",
  "ItemAssigned",
];

const ACTION_TYPES: readonly AutomationActionType[] = [
  "MoveItem",
  "ChangeStatus",
  "AssignPerson",
  "UpdateField",
  "NotifyMember",
  "SendEmail",
];

const CONDITION_TYPES = ["group", "status", "field"] as const;

/** An action as the editor holds it while it is being filled in: ids may still be empty. */
function blankAction(type: AutomationActionType): AutomationActionInput {
  switch (type) {
    case "MoveItem":
      return { type, groupId: "" };
    case "ChangeStatus":
      return { type, optionId: "" };
    case "AssignPerson":
      return { type, userId: "" };
    case "UpdateField":
      return { type, fieldId: "", value: "" };
    case "NotifyMember":
      return { type, userId: "", message: "" };
    case "SendEmail":
      return { type, userId: "", subject: "", body: "" };
  }
}

function blankCondition(
  type: (typeof CONDITION_TYPES)[number],
): AutomationConditionInput {
  switch (type) {
    case "group":
      return { type, groupId: "" };
    case "status":
      return { type, optionId: "" };
    case "field":
      return { type, fieldId: "", value: "" };
  }
}

function actionReady(action: AutomationActionInput): boolean {
  switch (action.type) {
    case "MoveItem":
      return action.groupId !== "";
    case "ChangeStatus":
      return action.optionId !== "";
    case "AssignPerson":
    case "NotifyMember":
      return action.userId !== "";
    case "UpdateField":
      return action.fieldId !== "" && action.value.trim() !== "";
    case "SendEmail":
      return action.userId !== "" && action.subject.trim() !== "";
  }
}

function conditionReady(condition: AutomationConditionInput): boolean {
  switch (condition.type) {
    case "group":
      return condition.groupId !== "";
    case "status":
      return condition.optionId !== "";
    case "field":
      return condition.fieldId !== "" && condition.value.trim() !== "";
  }
}

let draftKey = 0;
interface Keyed<T> {
  readonly key: number;
  readonly value: T;
}
const keyed = <T,>(value: T): Keyed<T> => ({ key: (draftKey += 1), value });

function ChoiceSelect({
  id,
  label,
  value,
  choices,
  choose,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  choices: readonly WorkspaceRuleChoice[];
  choose: string;
  onChange: (value: string) => void;
}) {
  return (
    <FormGroup label={label} htmlFor={id}>
      <Select value={value} onValueChange={onChange}>
        <SelectTrigger id={id} className="w-full">
          <SelectValue placeholder={choose} />
        </SelectTrigger>
        <SelectContent>
          {choices.map((choice) => (
            <SelectItem key={choice.id} value={choice.id}>
              {choice.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </FormGroup>
  );
}

function TextInput({
  id,
  label,
  value,
  maxLength = 200,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  maxLength?: number;
  onChange: (value: string) => void;
}) {
  return (
    <FormGroup label={label} htmlFor={id}>
      <input
        id={id}
        value={value}
        maxLength={maxLength}
        onChange={(event) => onChange(event.target.value)}
        className={INPUT_CLASS}
      />
    </FormGroup>
  );
}

/** The fields one action asks for. */
function ActionParams({
  id,
  action,
  panel,
  copy,
  onChange,
}: {
  id: string;
  action: AutomationActionInput;
  panel: WorkspaceRulesPanelProps;
  copy: WorkspaceRulesCopy;
  onChange: (next: AutomationActionInput) => void;
}) {
  const t = copy.form;
  switch (action.type) {
    case "MoveItem":
      return (
        <ChoiceSelect
          id={`${id}-group`}
          label={t.group}
          value={action.groupId}
          choices={panel.groups}
          choose={t.choose}
          onChange={(groupId) => onChange({ ...action, groupId })}
        />
      );
    case "ChangeStatus":
      return (
        <ChoiceSelect
          id={`${id}-status`}
          label={t.status}
          value={action.optionId}
          choices={panel.statuses}
          choose={t.choose}
          onChange={(optionId) => onChange({ ...action, optionId })}
        />
      );
    case "AssignPerson":
      return (
        <ChoiceSelect
          id={`${id}-person`}
          label={t.person}
          value={action.userId}
          choices={panel.people}
          choose={t.choose}
          onChange={(userId) => onChange({ ...action, userId })}
        />
      );
    case "UpdateField":
      return (
        <>
          <ChoiceSelect
            id={`${id}-field`}
            label={t.field}
            value={action.fieldId}
            choices={panel.fields}
            choose={t.choose}
            onChange={(fieldId) => onChange({ ...action, fieldId })}
          />
          <TextInput
            id={`${id}-value`}
            label={t.value}
            value={action.value}
            onChange={(value) => onChange({ ...action, value })}
          />
        </>
      );
    case "NotifyMember":
      return (
        <>
          <ChoiceSelect
            id={`${id}-person`}
            label={t.person}
            value={action.userId}
            choices={panel.people}
            choose={t.choose}
            onChange={(userId) => onChange({ ...action, userId })}
          />
          <TextInput
            id={`${id}-message`}
            label={t.message}
            value={action.message}
            onChange={(message) => onChange({ ...action, message })}
          />
        </>
      );
    case "SendEmail":
      return (
        <>
          <ChoiceSelect
            id={`${id}-person`}
            label={t.person}
            value={action.userId}
            choices={panel.people}
            choose={t.choose}
            onChange={(userId) => onChange({ ...action, userId })}
          />
          <TextInput
            id={`${id}-subject`}
            label={t.subject}
            value={action.subject}
            onChange={(subject) => onChange({ ...action, subject })}
          />
          <TextInput
            id={`${id}-body`}
            label={t.body}
            value={action.body}
            maxLength={1000}
            onChange={(body) => onChange({ ...action, body })}
          />
        </>
      );
  }
}

function ConditionParams({
  id,
  condition,
  panel,
  copy,
  onChange,
}: {
  id: string;
  condition: AutomationConditionInput;
  panel: WorkspaceRulesPanelProps;
  copy: WorkspaceRulesCopy;
  onChange: (next: AutomationConditionInput) => void;
}) {
  const t = copy.form;
  switch (condition.type) {
    case "group":
      return (
        <ChoiceSelect
          id={`${id}-group`}
          label={t.group}
          value={condition.groupId}
          choices={panel.groups}
          choose={t.choose}
          onChange={(groupId) => onChange({ ...condition, groupId })}
        />
      );
    case "status":
      return (
        <ChoiceSelect
          id={`${id}-status`}
          label={t.status}
          value={condition.optionId}
          choices={panel.statuses}
          choose={t.choose}
          onChange={(optionId) => onChange({ ...condition, optionId })}
        />
      );
    case "field":
      return (
        <>
          <ChoiceSelect
            id={`${id}-field`}
            label={t.field}
            value={condition.fieldId}
            choices={panel.fields}
            choose={t.choose}
            onChange={(fieldId) => onChange({ ...condition, fieldId })}
          />
          <TextInput
            id={`${id}-value`}
            label={t.value}
            value={condition.value}
            onChange={(value) => onChange({ ...condition, value })}
          />
        </>
      );
  }
}

/** The editor of a rule: new, or an edit of `rule` (which saves a new version). */
export function WorkspaceRuleEditor({
  panel,
  copy,
  rule,
  onDone,
  onCancel,
}: {
  panel: WorkspaceRulesPanelProps;
  copy: WorkspaceRulesCopy;
  rule?: WorkspaceAutomationRuleDto;
  onDone: (result: WorkspaceAutomationWriteResult) => void;
  onCancel: () => void;
}) {
  const t = copy.form;
  const id = useId();
  const [name, setName] = useState(rule?.name ?? "");
  const [trigger, setTrigger] = useState<WorkspaceAutomationTrigger>(
    (rule?.trigger as WorkspaceAutomationTrigger | undefined) ??
      "ItemMovedToGroup",
  );
  const [triggerGroupId, setTriggerGroupId] = useState(
    rule?.triggerGroupId ?? "",
  );
  const [conditions, setConditions] = useState<
    readonly Keyed<AutomationConditionInput>[]
  >((rule?.conditions ?? []).map(keyed));
  const [actions, setActions] = useState<
    readonly Keyed<AutomationActionInput>[]
  >((rule?.actions ?? [blankAction("MoveItem")]).map(keyed));
  const [busy, setBusy] = useState(false);

  const needsTriggerGroup = trigger === "ItemMovedToGroup";
  const ready =
    name.trim() !== "" &&
    (!needsTriggerGroup || triggerGroupId !== "") &&
    actions.length > 0 &&
    actions.every((entry) => actionReady(entry.value)) &&
    conditions.every((entry) => conditionReady(entry.value));

  const patchAction = (key: number, next: AutomationActionInput) =>
    setActions((list) =>
      list.map((entry) => (entry.key === key ? { key, value: next } : entry)),
    );
  const moveAction = (index: number, by: -1 | 1) =>
    setActions((list) => {
      const target = index + by;
      if (target < 0 || target >= list.length) return list;
      const next = [...list];
      [next[index], next[target]] = [next[target]!, next[index]!];
      return next;
    });

  function submit(event: FormEvent) {
    event.preventDefault();
    if (!ready || busy) return;
    setBusy(true);
    const definition: WorkspaceAutomationDefinition = {
      name: name.trim(),
      trigger,
      ...(needsTriggerGroup ? { triggerGroupId } : {}),
      conditions: conditions.map((entry) => entry.value),
      actions: actions.map((entry) => entry.value),
    };
    const write = rule
      ? panel.onUpdate(rule.id, definition)
      : panel.onCreate({ ...definition, boardId: panel.boardId });
    void write.then(onDone).finally(() => setBusy(false));
  }

  return (
    <form
      onSubmit={submit}
      aria-label={rule ? t.editTitle : t.title}
      className="mb-3.5 flex flex-col gap-3.5 rounded-[10px] border border-line bg-popover p-3.5"
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
          <Select
            value={trigger}
            onValueChange={(value) =>
              setTrigger(value as WorkspaceAutomationTrigger)
            }
          >
            <SelectTrigger id={`${id}-trigger`} className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {TRIGGERS.map((key) => (
                <SelectItem key={key} value={key}>
                  {copy.triggers[key]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </FormGroup>
        {needsTriggerGroup ? (
          <ChoiceSelect
            id={`${id}-trigger-group`}
            label={t.triggerGroup}
            value={triggerGroupId}
            choices={panel.groups}
            choose={t.choose}
            onChange={setTriggerGroupId}
          />
        ) : null}
      </div>

      <fieldset className="m-0 flex flex-col gap-2.5 border-0 p-0">
        <legend className="mb-1.5 p-0 text-item-small font-semibold">
          {t.conditionsTitle}
        </legend>
        {conditions.length === 0 ? (
          <p className="m-0 text-crumb text-meta-foreground">
            {t.conditionsNone}
          </p>
        ) : null}
        {conditions.map((entry, index) => (
          <div
            key={entry.key}
            className="flex flex-wrap items-end gap-2.5 rounded-lg border border-line px-3 py-2.5"
          >
            <span className="pb-2 text-item-small font-semibold">
              {t.conditionTypes[entry.value.type]}
            </span>
            <ConditionParams
              id={`${id}-c${entry.key}`}
              condition={entry.value}
              panel={panel}
              copy={copy}
              onChange={(next) =>
                setConditions((list) =>
                  list.map((candidate) =>
                    candidate.key === entry.key
                      ? { key: entry.key, value: next }
                      : candidate,
                  ),
                )
              }
            />
            <button
              type="button"
              aria-label={fillCopy(t.removeCondition, { index: index + 1 })}
              onClick={() =>
                setConditions((list) =>
                  list.filter((candidate) => candidate.key !== entry.key),
                )
              }
              className={BUTTON_CLASS.ghost}
            >
              <X aria-hidden="true" />
            </button>
          </div>
        ))}
        <div className="flex flex-wrap gap-2">
          {CONDITION_TYPES.map((type) => (
            <button
              key={type}
              type="button"
              onClick={() =>
                setConditions((list) => [...list, keyed(blankCondition(type))])
              }
              className={BUTTON_CLASS.outline}
            >
              <Plus aria-hidden="true" />
              {t.conditionTypes[type]}
            </button>
          ))}
        </div>
      </fieldset>

      <fieldset className="m-0 flex flex-col gap-2.5 border-0 p-0">
        <legend className="mb-1.5 p-0 text-item-small font-semibold">
          {t.actionsTitle}
        </legend>
        {actions.map((entry, index) => (
          <div
            key={entry.key}
            role="group"
            aria-label={fillCopy(t.action, { index: index + 1 })}
            className="flex flex-col gap-2.5 rounded-lg border border-line px-3 py-2.5"
          >
            <div className="flex flex-wrap items-end gap-2.5">
              <FormGroup
                label={fillCopy(t.action, { index: index + 1 })}
                htmlFor={`${id}-a${entry.key}-type`}
              >
                <Select
                  value={entry.value.type}
                  onValueChange={(value) =>
                    patchAction(
                      entry.key,
                      blankAction(value as AutomationActionType),
                    )
                  }
                >
                  <SelectTrigger
                    id={`${id}-a${entry.key}-type`}
                    className="w-full"
                  >
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {ACTION_TYPES.map((type) => (
                      <SelectItem key={type} value={type}>
                        {t.actionTypes[type]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </FormGroup>
              <ActionParams
                id={`${id}-a${entry.key}`}
                action={entry.value}
                panel={panel}
                copy={copy}
                onChange={(next) => patchAction(entry.key, next)}
              />
              <span className="ml-auto flex gap-1">
                <button
                  type="button"
                  disabled={index === 0}
                  aria-label={fillCopy(t.moveUp, { index: index + 1 })}
                  onClick={() => moveAction(index, -1)}
                  className={BUTTON_CLASS.ghost}
                >
                  <ArrowUp aria-hidden="true" />
                </button>
                <button
                  type="button"
                  disabled={index === actions.length - 1}
                  aria-label={fillCopy(t.moveDown, { index: index + 1 })}
                  onClick={() => moveAction(index, 1)}
                  className={BUTTON_CLASS.ghost}
                >
                  <ArrowDown aria-hidden="true" />
                </button>
                <button
                  type="button"
                  disabled={actions.length === 1}
                  aria-label={fillCopy(t.remove, { index: index + 1 })}
                  onClick={() =>
                    setActions((list) =>
                      list.filter((candidate) => candidate.key !== entry.key),
                    )
                  }
                  className={BUTTON_CLASS.ghost}
                >
                  <X aria-hidden="true" />
                </button>
              </span>
            </div>
          </div>
        ))}
        <div className="flex items-center gap-3">
          <button
            type="button"
            disabled={actions.length >= MAX_AUTOMATION_ACTIONS}
            onClick={() =>
              setActions((list) => [
                ...list,
                keyed(blankAction("ChangeStatus")),
              ])
            }
            className={BUTTON_CLASS.outline}
          >
            <Plus aria-hidden="true" />
            {t.addAction}
          </button>
          {actions.length >= MAX_AUTOMATION_ACTIONS ? (
            <span className="text-crumb text-meta-foreground">
              {t.actionLimit}
            </span>
          ) : null}
        </div>
      </fieldset>

      <p className="m-0 text-crumb text-meta-foreground">{t.hint}</p>
      <div className="flex items-center gap-2">
        <button
          type="submit"
          disabled={!ready || busy}
          className={BUTTON_CLASS.primary}
        >
          {rule ? t.save : t.submit}
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
