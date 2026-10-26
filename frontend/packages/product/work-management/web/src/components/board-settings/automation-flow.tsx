import { useState, type ReactNode } from "react";
import {
  ArrowRight,
  CalendarDays,
  Clock,
  Ellipsis,
  Filter,
  Plus,
  TriangleAlert,
  Users,
  X,
  CircleAlert,
  Zap,
} from "lucide-react";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  cn,
} from "@notrelix/ui-web";
import type {
  AutomationCatalogDto,
  AutomationConditionDto,
  AutomationOperator,
  AutomationRuleDocumentDto,
  AutomationScheduleDto,
  AutomationStepDto,
  AutomationStepKind,
  AutomationTriggerDto,
  AutomationTriggerKind,
} from "@notrelix/work-management-core";
import { BUTTON_CLASS, INPUT_CLASS } from "./board-settings-parts";
import { fillCopy } from "./board-settings-format";
import type { AutomationEditorCopy } from "./board-automation.copy";
import {
  ActionPicker,
  ConnectLink,
  EventPicker,
  eventKeyOf,
  type EventKey,
} from "./automation-pickers";
import {
  Chip,
  ChoiceChip,
  PersonDot,
  type ChoiceGroup,
} from "./automation-parts";

/** The "Khi / Nếu / Thì" flow of a rule (mockup `.flow`). */

export interface FlowProps {
  readonly copy: AutomationEditorCopy;
  readonly definition: AutomationRuleDocumentDto;
  readonly catalog: AutomationCatalogDto;
  readonly nextRun: string | null;
  readonly integrationsHref?: string;
  readonly defaultPicker?: "event" | "action";
  /** The step whose field list the "Chọn trường khác" button opens. */
  readonly chooseFieldStepId: string | null;
  readonly onChooseField: (stepId: string | null) => void;
  readonly onChange: (definition: AutomationRuleDocumentDto) => void;
  readonly onRestoreField: (fieldId: string) => void;
}

const LINK = "ml-7 h-3 w-0.5 bg-line-strong";
const BLOCK =
  "grid grid-cols-[58px_minmax(0,1fr)] gap-2.5 rounded-[10px] border border-line bg-popover px-3.5 py-3";
const SENTENCE = "m-0 flex min-h-7 flex-wrap items-center gap-1.5 text-item-ui";
const OP = "text-muted-foreground";

const DAY_KEYS = ["mon", "tue", "wed", "thu", "fri", "sat", "sun"] as const;
const REMOVE_ID = "__remove";
const SELECT_TYPES = ["select", "status", "multiselect"];

function newId(): string {
  return crypto.randomUUID();
}

function BlockHead({
  id,
  icon: Icon,
  children,
}: {
  id: string;
  icon: typeof Zap;
  children: ReactNode;
}) {
  return (
    <div
      id={id}
      className="flex h-7 items-center gap-[7px] text-item-ui font-[650]"
    >
      <span className="grid size-[22px] shrink-0 place-items-center rounded-[6px] bg-brand-subtle text-brand-subtle-foreground">
        <Icon aria-hidden="true" className="size-[13px]" />
      </span>
      {children}
    </div>
  );
}

function operatorsFor(type: string): readonly AutomationOperator[] {
  if (type === "date") return ["passed", "is-empty"];
  if (SELECT_TYPES.includes(type)) return ["is", "is-not", "is-empty"];
  return ["is-empty"];
}

export function AutomationFlow(props: FlowProps) {
  const { copy, definition, catalog, onChange } = props;
  const [eventOpen, setEventOpen] = useState(props.defaultPicker === "event");
  const [actionOpen, setActionOpen] = useState(
    props.defaultPicker === "action",
  );
  const [changingStep, setChangingStep] = useState<string | null>(null);
  const schedule = definition.trigger.kind === "schedule";
  const t = copy;

  const setTrigger = (trigger: AutomationTriggerDto) =>
    onChange({ ...definition, trigger });
  const setConditions = (conditions: readonly AutomationConditionDto[]) =>
    onChange({ ...definition, conditions });
  const setSteps = (steps: readonly AutomationStepDto[]) =>
    onChange({ ...definition, steps });

  function chooseEvent(key: EventKey) {
    setEventOpen(false);
    if (key === "daily" || key === "weekly" || key === "date") {
      const previous = definition.trigger.schedule;
      setTrigger({
        kind: "schedule",
        optionId: null,
        schedule: {
          time: previous?.time ?? "09:00",
          timeZone: previous?.timeZone ?? "Asia/Ho_Chi_Minh",
          repeat: key,
          days: previous?.days ?? [1, 2, 3, 4, 5, 6, 7],
          date: previous?.date ?? null,
          missed: previous?.missed ?? "catch-up",
        },
      });
      return;
    }
    setTrigger({
      kind: key as AutomationTriggerKind,
      optionId: null,
      schedule: null,
    });
  }

  const optionGroups = (
    options: readonly { id: string; name: string }[],
  ): ChoiceGroup[] => [
    { items: options.map((o) => ({ id: o.id, label: o.name })) },
  ];
  const nameOf = (
    options: readonly { id: string; name: string }[],
    id: string | null,
    placeholder: string,
  ) => options.find((o) => o.id === id)?.name ?? placeholder;

  const eventKey = eventKeyOf(definition.trigger);
  const triggerLabel = schedule
    ? t.pickers.groups.schedule
    : t.pickers.events[eventKey].label;

  // ── Khi ──
  const whenRow = (
    <p className={SENTENCE}>
      <EventPicker
        copy={copy}
        trigger={definition.trigger}
        hasEmailAddress={catalog.boardHasEmailAddress}
        integrationsHref={props.integrationsHref}
        onSelect={chooseEvent}
        open={eventOpen}
        onOpenChange={setEventOpen}
      >
        <Chip tone="key" aria-haspopup="dialog" aria-expanded={eventOpen}>
          {triggerLabel}
        </Chip>
      </EventPicker>
      {definition.trigger.kind === "status-changed" ? (
        <>
          <span className={OP}>{t.sentence.toStatus}</span>
          <ChoiceChip
            label={nameOf(
              catalog.statuses,
              definition.trigger.optionId,
              t.pick.status,
            )}
            groups={optionGroups(catalog.statuses)}
            currentId={definition.trigger.optionId}
            empty={t.pick.noneFound}
            onSelect={(id) =>
              setTrigger({ ...definition.trigger, optionId: id })
            }
          />
        </>
      ) : null}
      {definition.trigger.kind === "priority-changed" ? (
        <>
          <span className={OP}>{t.sentence.toPriority}</span>
          <ChoiceChip
            label={nameOf(
              catalog.priorities,
              definition.trigger.optionId,
              t.pick.priority,
            )}
            groups={optionGroups(catalog.priorities)}
            currentId={definition.trigger.optionId}
            empty={t.pick.noneFound}
            onSelect={(id) =>
              setTrigger({ ...definition.trigger, optionId: id })
            }
          />
        </>
      ) : null}
      {schedule && definition.trigger.schedule ? (
        <span className={OP}>
          {fillCopy(t.sentence.schedule, {
            time: definition.trigger.schedule.time,
            repeat:
              definition.trigger.schedule.repeat === "weekly"
                ? t.sentence.repeatWeekly
                : definition.trigger.schedule.repeat === "date"
                  ? t.sentence.repeatDate
                  : t.sentence.repeatDaily,
          })}
        </span>
      ) : (
        <span className={OP}>{t.sentence.onThisBoard}</span>
      )}
    </p>
  );

  // ── Nếu ──
  const fieldGroups = (types?: readonly string[]): ChoiceGroup[] => [
    {
      items: catalog.fields
        .filter((field) => !types || types.includes(field.type))
        .map((field) => ({
          id: field.id,
          label: field.name,
          hint: field.archived ? t.checks.error : undefined,
        })),
    },
  ];
  const conditionRows = definition.conditions.map((condition, index) => {
    const field = catalog.fields.find((f) => f.id === condition.fieldId);
    const operators = operatorsFor(field?.type ?? "");
    const showsValue =
      condition.operator === "is" || condition.operator === "is-not";
    const text = [
      field?.name ?? "",
      t.operators[
        condition.operator === "is-not"
          ? "isNot"
          : condition.operator === "is-empty"
            ? "isEmpty"
            : condition.operator
      ],
      field?.options.find((o) => o.id === condition.optionId)?.name ?? "",
    ]
      .join(" ")
      .trim();
    const removeCondition = () =>
      setConditions(definition.conditions.filter((c) => c.id !== condition.id));
    const update = (patch: Partial<AutomationConditionDto>) =>
      setConditions(
        definition.conditions.map((c) =>
          c.id === condition.id ? { ...c, ...patch } : c,
        ),
      );
    return (
      <span key={condition.id} className="contents">
        {index > 0 ? <span className={OP}>{t.sentence.and}</span> : null}
        <ChoiceChip
          label={field?.name ?? t.pick.field}
          groups={
            definition.conditions.length > 1
              ? [
                  ...fieldGroups(),
                  {
                    items: [
                      {
                        id: REMOVE_ID,
                        label: fillCopy(t.removeCondition, { text }),
                      },
                    ],
                  },
                ]
              : fieldGroups()
          }
          currentId={condition.fieldId}
          empty={t.pick.noneFound}
          tone={field?.archived ? "bad" : "plain"}
          onSelect={(id) => {
            if (id === REMOVE_ID) {
              removeCondition();
              return;
            }
            const next = catalog.fields.find((f) => f.id === id);
            update({
              fieldId: id,
              operator: operatorsFor(next?.type ?? "")[0] ?? "is-empty",
              optionId: null,
            });
          }}
        />
        <ChoiceChip
          label={
            t.operators[
              condition.operator === "is-not"
                ? "isNot"
                : condition.operator === "is-empty"
                  ? "isEmpty"
                  : condition.operator
            ]
          }
          groups={[
            {
              items: operators.map((operator) => ({
                id: operator,
                label:
                  t.operators[
                    operator === "is-not"
                      ? "isNot"
                      : operator === "is-empty"
                        ? "isEmpty"
                        : operator
                  ],
              })),
            },
          ]}
          currentId={condition.operator}
          empty={t.pick.noneFound}
          onSelect={(id) =>
            update({
              operator: id as AutomationOperator,
              optionId:
                id === "is" || id === "is-not" ? condition.optionId : null,
            })
          }
        />
        {showsValue ? (
          <ChoiceChip
            label={nameOf(
              field?.options ?? [],
              condition.optionId,
              t.pick.value,
            )}
            groups={optionGroups(field?.options ?? [])}
            currentId={condition.optionId}
            empty={t.pick.noneFound}
            onSelect={(id) => update({ optionId: id })}
          />
        ) : null}
        {definition.conditions.length === 1 ? (
          <>
            <span className="flex-1" />
            <button
              type="button"
              aria-label={fillCopy(t.removeCondition, { text })}
              onClick={removeCondition}
              className="grid size-[30px] shrink-0 cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent text-muted-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring"
            >
              <X aria-hidden="true" className="size-3.5" />
            </button>
          </>
        ) : null}
      </span>
    );
  });

  function addCondition() {
    const field =
      catalog.fields.find(
        (f) => !f.archived && SELECT_TYPES.includes(f.type),
      ) ?? catalog.fields.find((f) => !f.archived);
    if (!field) return;
    setConditions([
      ...definition.conditions,
      {
        id: newId(),
        fieldId: field.id,
        operator: operatorsFor(field.type)[0] ?? "is-empty",
        optionId: null,
      },
    ]);
  }

  // ── Thì ──
  const peopleGroups = (peopleOnly: boolean): ChoiceGroup[] => [
    {
      label: t.pick.peopleGroup,
      items: catalog.people.map((p) => ({
        id: p.id,
        label: p.name,
        leading: <PersonDot person={p} size={18} />,
      })),
    },
    ...(peopleOnly
      ? []
      : [
          {
            label: t.pick.teamsGroup,
            items: catalog.teams.map((team) => ({
              id: team.id,
              label: team.name,
              hint: fillCopy(t.pick.teamMembers, { count: team.memberCount }),
            })),
          },
          {
            label: t.pick.rolesGroup,
            items: [
              { id: "role:reviewer", label: t.pick.reviewer },
              { id: "role:assignee", label: t.pick.assignee },
            ],
          },
        ]),
  ];
  const targetLabel = (id: string | null, fallback: string) => {
    if (id === "role:reviewer") return t.pick.reviewer;
    if (id === "role:assignee") return t.pick.assignee;
    return (
      catalog.people.find((p) => p.id === id)?.name ??
      catalog.teams.find((team) => team.id === id)?.name ??
      fallback
    );
  };
  const targetLeading = (id: string | null) => {
    const person = catalog.people.find((p) => p.id === id);
    if (person) return <PersonDot person={person} size={18} />;
    if (catalog.teams.some((team) => team.id === id))
      return <Users aria-hidden="true" className="!text-muted-foreground" />;
    return undefined;
  };

  function moveStep(index: number, by: -1 | 1) {
    const next = [...definition.steps];
    const target = index + by;
    if (target < 0 || target >= next.length) return;
    const [step] = next.splice(index, 1);
    if (step) next.splice(target, 0, step);
    setSteps(next);
  }

  const stepRows = definition.steps.map((step, index) => {
    const update = (patch: Partial<AutomationStepDto>) =>
      setSteps(
        definition.steps.map((s) =>
          s.id === step.id ? { ...s, ...patch } : s,
        ),
      );
    const field =
      step.kind === "set-field" || step.kind === "set-today"
        ? catalog.fields.find((f) => f.id === step.targetId)
        : undefined;
    const archived = field?.archived === true;
    const teamsWarn =
      step.kind === "teams" && catalog.teamsNeedsReauthorization;
    const fieldChip = (
      <ChoiceChip
        label={field?.name ?? t.pick.field}
        groups={fieldGroups(step.kind === "set-today" ? ["date"] : undefined)}
        currentId={step.targetId}
        empty={t.pick.noneFound}
        tone={archived ? "bad" : "plain"}
        invalid={archived}
        describedBy={archived ? `step-err-${step.id}` : undefined}
        leading={archived ? <CircleAlert aria-hidden="true" /> : undefined}
        open={props.chooseFieldStepId === step.id ? true : undefined}
        onOpenChange={(open) => {
          if (!open && props.chooseFieldStepId === step.id)
            props.onChooseField(null);
        }}
        onSelect={(id) => update({ targetId: id })}
      />
    );
    let params: ReactNode = null;
    switch (step.kind) {
      case "notify":
      case "email":
        params = (
          <>
            <span className={OP}>{t.sentence.for}</span>
            <ChoiceChip
              label={targetLabel(step.targetId, t.pick.person)}
              leading={targetLeading(step.targetId)}
              groups={peopleGroups(false)}
              currentId={step.targetId}
              empty={t.pick.noneFound}
              onSelect={(id) => update({ targetId: id })}
            />
          </>
        );
        break;
      case "assign":
        params = (
          <>
            <ChoiceChip
              label={targetLabel(step.targetId, t.pick.person)}
              leading={targetLeading(step.targetId)}
              groups={peopleGroups(true)}
              currentId={step.targetId}
              empty={t.pick.noneFound}
              onSelect={(id) => update({ targetId: id })}
            />
            <span className={OP}>{t.sentence.approve}</span>
          </>
        );
        break;
      case "remind":
        params = (
          <>
            <ChoiceChip
              label={targetLabel(step.targetId, t.pick.person)}
              groups={peopleGroups(false)}
              currentId={step.targetId}
              empty={t.pick.noneFound}
              onSelect={(id) => update({ targetId: id })}
            />
            <span className={OP}>{t.sentence.ofEachMatch}</span>
          </>
        );
        break;
      case "set-today":
        params = (
          <>
            <span className={OP}>{t.sentence.inField}</span>
            {fieldChip}
          </>
        );
        break;
      case "set-field":
        params = fieldChip;
        break;
      case "set-status":
        params = (
          <>
            <span className={OP}>{t.sentence.toStatus}</span>
            <ChoiceChip
              label={nameOf(catalog.statuses, step.targetId, t.pick.status)}
              groups={optionGroups(catalog.statuses)}
              currentId={step.targetId}
              empty={t.pick.noneFound}
              onSelect={(id) => update({ targetId: id })}
            />
          </>
        );
        break;
      case "move-group":
        params = (
          <>
            <span className={OP}>{t.sentence.toGroup}</span>
            <ChoiceChip
              label={nameOf(catalog.groups, step.targetId, t.pick.group)}
              groups={optionGroups(catalog.groups)}
              currentId={step.targetId}
              empty={t.pick.noneFound}
              onSelect={(id) => update({ targetId: id })}
            />
          </>
        );
        break;
      case "teams":
        params = (
          <>
            <span className={OP}>{t.sentence.inChannel}</span>
            <ChoiceChip
              label={nameOf(catalog.channels, step.targetId, t.pick.channel)}
              groups={optionGroups(catalog.channels)}
              currentId={step.targetId}
              empty={t.pick.noneFound}
              onSelect={(id) => update({ targetId: id })}
            />
          </>
        );
        break;
      case "webhook":
        params = (
          <>
            <span className={OP}>{t.sentence.to}</span>
            <ChoiceChip
              label={nameOf(catalog.webhooks, step.targetId, t.pick.webhook)}
              groups={optionGroups(catalog.webhooks)}
              currentId={step.targetId}
              empty={t.pick.noneFound}
              onSelect={(id) => update({ targetId: id })}
            />
          </>
        );
        break;
      default:
        break;
    }
    const kindLabel = t.stepLabels[step.kind];
    return (
      <li
        key={step.id}
        className={cn(
          "grid grid-cols-[24px_minmax(0,1fr)_30px] items-start gap-2 rounded-[9px] border bg-background py-[7px] pr-1.5 pl-[9px]",
          archived ? "border-danger" : "border-line",
        )}
      >
        <span
          aria-hidden="true"
          className="mt-[3px] box-border grid size-[22px] place-items-center rounded-full border border-line-strong bg-popover text-[11.5px] font-[650] text-muted-foreground"
        >
          {index + 1}
        </span>
        <div>
          <p className={SENTENCE}>
            <ActionPicker
              copy={copy}
              teamsNeedsReauthorization={catalog.teamsNeedsReauthorization}
              integrationsHref={props.integrationsHref}
              side="bottom"
              open={changingStep === step.id}
              onOpenChange={(open) => setChangingStep(open ? step.id : null)}
              onSelect={(kind) => {
                setChangingStep(null);
                update({ kind, targetId: null });
              }}
            >
              <Chip aria-haspopup="dialog">{kindLabel}</Chip>
            </ActionPicker>
            {params}
          </p>
          {step.note ? (
            <div className="mt-1 text-item-note leading-[1.45] text-meta-foreground">
              {fillCopy(t.sentence.content, { note: step.note })}
            </div>
          ) : null}
          {archived ? (
            <>
              <div
                id={`step-err-${step.id}`}
                className="mt-1 flex flex-wrap items-center gap-1.5 text-item-note leading-[1.45] font-medium text-danger-text"
              >
                {t.stepArchived}
              </div>
              <div className="mt-2 flex gap-1.5">
                <button
                  type="button"
                  className={cn(BUTTON_CLASS.outline, "h-7 px-2.5 text-crumb")}
                  onClick={() => field && props.onRestoreField(field.id)}
                >
                  {t.checks.restoreField}
                </button>
                <button
                  type="button"
                  className={cn(BUTTON_CLASS.outline, "h-7 px-2.5 text-crumb")}
                  onClick={() => props.onChooseField(step.id)}
                >
                  {t.checks.chooseField}
                </button>
              </div>
            </>
          ) : null}
          {teamsWarn ? (
            <div className="mt-1 flex items-center gap-1.5 text-item-note leading-[1.45] text-warning-text">
              <TriangleAlert aria-hidden="true" className="size-3.5 shrink-0" />
              <span>
                {t.stepTeamsReauth}{" "}
                <ConnectLink
                  href={props.integrationsHref}
                  label={t.pickers.viewConnection}
                  unavailable={t.integrationsUnavailable}
                />
              </span>
            </div>
          ) : null}
        </div>
        <StepMenu
          copy={copy}
          index={index}
          last={definition.steps.length - 1}
          onMove={moveStep}
          onRemove={() =>
            setSteps(definition.steps.filter((s) => s.id !== step.id))
          }
        />
      </li>
    );
  });

  return (
    <div className="flex flex-col">
      <section aria-labelledby="b-khi" className={BLOCK}>
        <BlockHead id="b-khi" icon={schedule ? Clock : Zap}>
          {t.blocks.when}
        </BlockHead>
        <div className="flex min-w-0 flex-col gap-2">
          {whenRow}
          {schedule && definition.trigger.schedule ? (
            <SchedulePanel
              copy={copy}
              schedule={definition.trigger.schedule}
              nextRun={props.nextRun}
              onChange={(next) =>
                setTrigger({ ...definition.trigger, schedule: next })
              }
            />
          ) : null}
        </div>
      </section>
      <div aria-hidden="true" className={LINK} />
      <section aria-labelledby="b-neu" className={BLOCK}>
        <BlockHead id="b-neu" icon={Filter}>
          {t.blocks.if}
        </BlockHead>
        <div className="flex min-w-0 flex-col gap-2">
          {definition.conditions.length === 0 ? (
            <p className={SENTENCE}>
              <span className={OP}>{t.sentence.noCondition}</span>
            </p>
          ) : (
            <p className={SENTENCE}>{conditionRows}</p>
          )}
          <button
            type="button"
            onClick={addCondition}
            className="-ml-2 inline-flex h-7 cursor-pointer items-center gap-1.5 self-start rounded-[7px] border-0 bg-transparent px-2 text-item-small font-medium text-brand-subtle-foreground outline-none hover:bg-brand-subtle focus-visible:ring-2 focus-visible:ring-focus-ring [&_svg]:size-3.5"
          >
            <Plus aria-hidden="true" />
            {t.addCondition}
          </button>
        </div>
      </section>
      <div aria-hidden="true" className={LINK} />
      <section aria-labelledby="b-thi" className={BLOCK}>
        <BlockHead id="b-thi" icon={ArrowRight}>
          {t.blocks.then}
        </BlockHead>
        <div className="flex min-w-0 flex-col gap-2">
          {definition.steps.length > 0 ? (
            <ol
              aria-label={t.stepsAria}
              className="m-0 flex list-none flex-col gap-2 p-0"
            >
              {stepRows}
            </ol>
          ) : null}
          <ActionPicker
            copy={copy}
            teamsNeedsReauthorization={catalog.teamsNeedsReauthorization}
            integrationsHref={props.integrationsHref}
            open={actionOpen}
            onOpenChange={setActionOpen}
            onSelect={(kind: AutomationStepKind) => {
              setActionOpen(false);
              setSteps([
                ...definition.steps,
                { id: newId(), kind, targetId: null, note: null },
              ]);
            }}
          >
            <button
              type="button"
              aria-haspopup="dialog"
              aria-expanded={actionOpen}
              onClick={() => setActionOpen(true)}
              className="-ml-2 inline-flex h-7 cursor-pointer items-center gap-1.5 self-start rounded-[7px] border-0 bg-transparent px-2 text-item-small font-medium text-brand-subtle-foreground outline-none hover:bg-brand-subtle focus-visible:ring-2 focus-visible:ring-focus-ring aria-expanded:bg-brand-subtle [&_svg]:size-3.5"
            >
              <Plus aria-hidden="true" />
              {t.addAction}
            </button>
          </ActionPicker>
        </div>
      </section>
    </div>
  );
}

function StepMenu({
  copy,
  index,
  last,
  onMove,
  onRemove,
}: {
  copy: AutomationEditorCopy;
  index: number;
  last: number;
  onMove: (index: number, by: -1 | 1) => void;
  onRemove: () => void;
}) {
  const [open, setOpen] = useState(false);
  const item =
    "flex h-8 w-full cursor-pointer items-center rounded-[7px] border-0 bg-transparent px-2.5 text-left text-item-small text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-not-allowed disabled:opacity-50";
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          aria-haspopup="menu"
          aria-label={fillCopy(copy.stepMenu, { n: index + 1 })}
          className="grid size-[30px] shrink-0 cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent text-muted-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring"
        >
          <Ellipsis aria-hidden="true" className="size-3.5" />
        </button>
      </PopoverTrigger>
      <PopoverContent
        align="end"
        role="menu"
        className="flex w-[180px] flex-col gap-0.5 rounded-[10px] border border-line-strong bg-popover p-1.5 shadow-lg ring-0"
      >
        <button
          type="button"
          role="menuitem"
          disabled={index === 0}
          className={item}
          onClick={() => {
            onMove(index, -1);
            setOpen(false);
          }}
        >
          {copy.stepMoveUp}
        </button>
        <button
          type="button"
          role="menuitem"
          disabled={index === last}
          className={item}
          onClick={() => {
            onMove(index, 1);
            setOpen(false);
          }}
        >
          {copy.stepMoveDown}
        </button>
        <button
          type="button"
          role="menuitem"
          className={cn(item, "text-danger-text")}
          onClick={() => {
            onRemove();
            setOpen(false);
          }}
        >
          {copy.stepRemove}
        </button>
      </PopoverContent>
    </Popover>
  );
}

function SchedulePanel({
  copy,
  schedule,
  nextRun,
  onChange,
}: {
  copy: AutomationEditorCopy;
  schedule: AutomationScheduleDto;
  nextRun: string | null;
  onChange: (schedule: AutomationScheduleDto) => void;
}) {
  const t = copy.schedule;
  const row =
    "grid grid-cols-[108px_minmax(0,1fr)] items-center gap-2.5 text-item-small";
  const label = "text-crumb font-semibold text-muted-foreground";
  const inp = cn(INPUT_CLASS, "h-8 w-auto text-item-small");
  const repeats: AutomationScheduleDto["repeat"][] = [
    "daily",
    "weekly",
    "date",
  ];
  return (
    <div
      role="group"
      aria-label={t.group}
      className="flex flex-col gap-2 rounded-[9px] border border-line bg-background px-3 py-2.5"
    >
      <div className={row}>
        <label className={label} htmlFor="s-time">
          {t.time}
        </label>
        <span className="flex flex-wrap items-center gap-2">
          <input
            id="s-time"
            type="text"
            value={schedule.time}
            onChange={(event) =>
              onChange({ ...schedule, time: event.target.value })
            }
            className={cn(inp, "w-[72px] font-mono")}
          />
          <label className="sr-only" htmlFor="s-tz">
            {t.zone}
          </label>
          <Select
            value={schedule.timeZone}
            onValueChange={(value) =>
              onChange({ ...schedule, timeZone: value })
            }
          >
            <SelectTrigger id="s-tz" className="h-8 w-[170px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Asia/Ho_Chi_Minh">{t.zoneHanoi}</SelectItem>
              <SelectItem value="Asia/Singapore">{t.zoneSingapore}</SelectItem>
            </SelectContent>
          </Select>
        </span>
      </div>
      <div className={row}>
        <span />
        <span className="-mt-1 text-item-note leading-[1.45] text-meta-foreground">
          {t.seasons}
        </span>
      </div>
      <div className={cn(row, "items-start")}>
        <span id="s-rep" className={cn(label, "pt-1.5")}>
          {t.repeat}
        </span>
        <div
          role="group"
          aria-labelledby="s-rep"
          className="flex flex-col gap-2"
        >
          <div className="inline-flex self-start gap-0.5 rounded-lg border border-line-strong bg-popover p-0.5">
            {repeats.map((repeat) => (
              <button
                key={repeat}
                type="button"
                aria-pressed={schedule.repeat === repeat}
                onClick={() => onChange({ ...schedule, repeat })}
                className={cn(
                  "h-[26px] cursor-pointer rounded-[6px] border-0 bg-transparent px-2.5 text-crumb font-medium whitespace-nowrap text-muted-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                  schedule.repeat === repeat &&
                    "bg-brand-subtle font-semibold text-brand-subtle-foreground",
                )}
              >
                {t[repeat]}
              </button>
            ))}
          </div>
          {schedule.repeat === "date" ? (
            <input
              type="date"
              aria-label={t.dateLabel}
              value={schedule.date ?? ""}
              onChange={(event) =>
                onChange({ ...schedule, date: event.target.value || null })
              }
              className={cn(inp, "w-[170px]")}
            />
          ) : (
            <div aria-label={t.days} className="flex flex-wrap gap-1">
              {DAY_KEYS.map((dayKey, index) => {
                const short = t.dayShort[dayKey];
                const day = index + 1;
                const on = schedule.days.includes(day);
                return (
                  <button
                    key={day}
                    type="button"
                    aria-pressed={on}
                    aria-label={t.dayLong[dayKey]}
                    onClick={() =>
                      onChange({
                        ...schedule,
                        days: on
                          ? schedule.days.filter((d) => d !== day)
                          : [...schedule.days, day].sort((a, b) => a - b),
                      })
                    }
                    className={cn(
                      "h-7 min-w-[34px] cursor-pointer rounded-[7px] border border-line-strong bg-popover px-1 text-crumb font-semibold text-muted-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                      on &&
                        "border-transparent bg-brand-subtle text-brand-subtle-foreground",
                    )}
                  >
                    {short}
                  </button>
                );
              })}
            </div>
          )}
        </div>
      </div>
      <div className={cn(row, "items-start")}>
        <span id="s-miss" className={cn(label, "pt-1.5")}>
          {t.missed}
        </span>
        <div
          role="radiogroup"
          aria-labelledby="s-miss"
          className="flex flex-col gap-1.5"
        >
          {(["catch-up", "skip"] as const).map((missed) => (
            <label
              key={missed}
              className="flex cursor-pointer items-start gap-2 text-item-small"
            >
              <input
                type="radio"
                name="miss"
                checked={schedule.missed === missed}
                onChange={() => onChange({ ...schedule, missed })}
                className="mt-[3px] mr-0 ml-0 accent-primary"
              />
              <span>
                {missed === "catch-up" ? t.catchUp : t.skip}
                <small className="block text-item-note text-meta-foreground">
                  {missed === "catch-up"
                    ? t.catchUpHint
                    : fillCopy(t.skipHint, { time: schedule.time })}
                </small>
              </span>
            </label>
          ))}
        </div>
      </div>
      {nextRun ? (
        <div className="flex items-center gap-2 rounded-lg border border-line bg-popover px-2.5 py-2 text-item-small">
          <CalendarDays
            aria-hidden="true"
            className="size-3.5 text-brand-subtle-foreground"
          />
          {t.nextRun} <b className="font-semibold">{nextRun}</b>
        </div>
      ) : null}
    </div>
  );
}
