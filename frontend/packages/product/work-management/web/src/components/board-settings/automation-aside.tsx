import type { ReactNode } from "react";
import { CircleCheck, CircleX, TriangleAlert } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger, cn } from "@notrelix/ui-web";
import type {
  AutomationCheckDto,
  AutomationRuleDetailDto,
  AutomationRunOutcome,
} from "@notrelix/work-management-core";
import { BUTTON_CLASS } from "./board-settings-parts";
import { fillCopy } from "./board-settings-format";
import type { AutomationEditorCopy } from "./board-automation.copy";
import { ConnectLink } from "./automation-pickers";
import {
  ChoicePopover,
  PersonDot,
  StatusTag,
  TriggerButton,
} from "./automation-parts";
import { dayMonthOf, isSameDay, timeOf } from "./automation-format";

/** The right-hand column of the rule editor (mockup `aside.ed`). */

const LABEL =
  "flex items-center gap-2 text-crumb font-semibold text-muted-foreground";
const HINT = "text-item-note text-meta-foreground";
const BOX =
  "flex items-center gap-2.5 rounded-[9px] border border-line px-3 py-2.5";

function Group({
  label,
  hint,
  children,
}: {
  label: string;
  hint?: string;
  children: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-[7px]">
      <span className={LABEL}>
        {label}
        {hint ? (
          <>
            <span className="flex-1" />
            <span className={cn(HINT, "font-normal")}>{hint}</span>
          </>
        ) : null}
      </span>
      {children}
    </div>
  );
}

export function AutomationAside({
  copy,
  rule,
  timeZone,
  scopeMark,
  integrationsHref,
  onChangeRunner,
  onRestoreField,
  onChooseField,
  onOpenLog,
}: {
  copy: AutomationEditorCopy;
  rule: AutomationRuleDetailDto;
  timeZone: string;
  scopeMark: ReactNode;
  integrationsHref?: string;
  onChangeRunner: (personId: string) => void;
  onRestoreField: (fieldId: string) => void;
  onChooseField: (stepId: string) => void;
  onOpenLog: () => void;
}) {
  const t = copy.aside;
  const schedule = rule.definition.trigger.kind === "schedule";
  const live = rule.status !== "draft";
  return (
    <aside
      aria-label={t.aria}
      className="flex min-h-0 w-[401px] shrink-0 flex-col border-l border-line bg-popover"
    >
      <div className="flex flex-1 flex-col gap-[18px] overflow-auto px-[18px] pt-4 pb-[18px] [&>*]:shrink-0">
        <Group label={t.runner}>
          <div className={BOX}>
            <PersonDot person={rule.runAs} size={30} />
            <span className="min-w-0 flex-1">
              <b className="block text-item-ui font-semibold">
                {rule.runAs.name}
              </b>
              <small className="block text-item-note text-meta-foreground">
                {rule.runAs.role}
              </small>
            </span>
            <ChoicePopover
              align="end"
              currentId={rule.runAs.id}
              empty={copy.pick.noneFound}
              groups={[
                {
                  items: rule.catalog.people.map((person) => ({
                    id: person.id,
                    label: person.name,
                    leading: <PersonDot person={person} size={18} />,
                  })),
                },
              ]}
              onSelect={onChangeRunner}
              trigger={(open) => (
                <TriggerButton
                  aria-haspopup="listbox"
                  aria-expanded={open}
                  aria-label={t.runnerAria}
                  className={cn(BUTTON_CLASS.outline, "h-7 px-2.5 text-crumb")}
                ></TriggerButton>
              )}
            />
          </div>
          <span className={HINT}>{t.runnerHint}</span>
        </Group>

        <Group label={t.scope}>
          <div className={BOX}>
            {scopeMark}
            <span className="min-w-0 flex-1">
              <b className="block text-item-ui font-semibold">
                {fillCopy(t.scopeBoard, { title: rule.scope.boardTitle })}
              </b>
              <small className="block text-item-note text-meta-foreground">
                {rule.scope.detail}
              </small>
            </span>
          </div>
        </Group>

        {live && !schedule && rule.versions.length > 0 ? (
          <Group label={t.versions} hint={t.versionsHint}>
            <div
              role="list"
              className="flex flex-col overflow-hidden rounded-[9px] border border-line"
            >
              {rule.versions.map((version, index) => {
                const current = version.number === rule.version;
                return (
                  <div
                    key={version.number}
                    role="listitem"
                    className={cn(
                      "box-content flex min-h-11 items-center gap-2.5 border-t border-line py-1 pr-2 pl-3 text-item-small first:border-t-0",
                      current && "bg-state-selected",
                    )}
                    data-index={index}
                  >
                    <span className="flex-1">
                      <b className="font-semibold">
                        {fillCopy(t.version, { n: version.number })}
                      </b>
                      <small className="block text-item-note text-meta-foreground">
                        {dayMonthOf(version.at, timeZone)}
                        {version.byName ? ` · ${version.byName}` : ""}
                      </small>
                    </span>
                    {current ? (
                      <StatusTag tone="brand">{t.current}</StatusTag>
                    ) : (
                      <Popover>
                        <PopoverTrigger asChild>
                          <button
                            type="button"
                            aria-label={fillCopy(t.viewAria, {
                              n: version.number,
                            })}
                            className={cn(
                              BUTTON_CLASS.ghost,
                              "h-7 px-2.5 text-crumb",
                            )}
                          >
                            {t.view}
                          </button>
                        </PopoverTrigger>
                        <PopoverContent
                          align="end"
                          role="dialog"
                          aria-label={fillCopy(t.viewTitle, {
                            n: version.number,
                          })}
                          className="flex w-[320px] flex-col gap-2 rounded-[10px] border border-line-strong bg-popover p-3 shadow-lg ring-0"
                        >
                          <b className="text-item-small font-semibold">
                            {fillCopy(t.viewTitle, { n: version.number })}
                          </b>
                          <ul className="m-0 flex list-none flex-col gap-1 p-0 text-crumb text-muted-foreground">
                            {version.summary.map((line) => (
                              <li key={line}>{line}</li>
                            ))}
                          </ul>
                        </PopoverContent>
                      </Popover>
                    )}
                  </div>
                );
              })}
            </div>
          </Group>
        ) : null}

        {live && schedule ? (
          <Group
            label={t.recent}
            hint={fillCopy(t.recentVersion, { n: rule.version })}
          >
            <div className="rounded-[9px] border border-line px-3 py-0.5">
              {rule.recentRuns.map((run) => (
                <div
                  key={run.id}
                  className="box-content flex min-h-9 items-center gap-2.5 border-t border-line text-crumb first:border-t-0"
                >
                  <span className="w-24 shrink-0 text-muted-foreground">
                    {isSameDay(run.at, rule.checkedAt, timeZone)
                      ? fillCopy(t.today, { time: timeOf(run.at, timeZone) })
                      : `${dayMonthOf(run.at, timeZone)} ${timeOf(run.at, timeZone)}`}
                  </span>
                  <span className="min-w-0 flex-1 text-muted-foreground">
                    {run.brief}
                  </span>
                  <OutcomeTag outcome={run.outcome} copy={copy} />
                </div>
              ))}
            </div>
            <button
              type="button"
              onClick={onOpenLog}
              className={cn(
                BUTTON_CLASS.ghostBrand,
                "-ml-2 h-7 self-start px-2.5 text-crumb",
              )}
            >
              {t.openLog}
            </button>
          </Group>
        ) : null}

        <Group
          label={t.checks}
          hint={fillCopy(t.checkedAt, {
            time: timeOf(rule.checkedAt, timeZone),
          })}
        >
          <div
            role="list"
            className="flex flex-col overflow-hidden rounded-[9px] border border-line"
          >
            {rule.checks.map((check) => (
              <CheckRow
                key={check.id}
                check={check}
                copy={copy}
                timeZone={timeZone}
                integrationsHref={integrationsHref}
                onRestoreField={onRestoreField}
                onChooseField={onChooseField}
              />
            ))}
          </div>
          {rule.status === "draft" && !rule.blocked ? (
            <span className={HINT}>{t.draftHint}</span>
          ) : null}
        </Group>
      </div>
    </aside>
  );
}

function OutcomeTag({
  outcome,
  copy,
}: {
  outcome: AutomationRunOutcome;
  copy: AutomationEditorCopy;
}) {
  return outcome === "success" ? (
    <StatusTag tone="ok">{copy.aside.success}</StatusTag>
  ) : outcome === "error" ? (
    <StatusTag tone="err">{copy.aside.error}</StatusTag>
  ) : (
    <StatusTag tone="neutral">{copy.aside.skipped}</StatusTag>
  );
}

function CheckRow({
  check,
  copy,
  timeZone,
  integrationsHref,
  onRestoreField,
  onChooseField,
}: {
  check: AutomationCheckDto;
  copy: AutomationEditorCopy;
  timeZone: string;
  integrationsHref?: string;
  onRestoreField: (fieldId: string) => void;
  onChooseField: (stepId: string) => void;
}) {
  const t = copy.checks;
  const params = { ...check.params };
  if (typeof params.at === "string")
    params.date = dayMonthOf(params.at, timeZone);
  const key =
    check.code === "step-field-archived" && params.step === 0
      ? "condition-field-archived"
      : check.code;
  const Icon =
    check.level === "ok"
      ? CircleCheck
      : check.level === "error"
        ? CircleX
        : TriangleAlert;
  return (
    <div
      role="listitem"
      className={cn(
        "flex gap-[9px] border-t border-line px-3 py-[9px] text-crumb leading-[1.45] text-foreground first:border-t-0",
        check.level === "error" && "bg-danger-subtle",
      )}
    >
      <Icon
        role="img"
        aria-label={t[check.level]}
        className={cn(
          "mt-px size-3.5 shrink-0",
          check.level === "ok" && "text-success-text",
          check.level === "error" && "text-danger-text",
          check.level === "warning" && "text-warning-text",
        )}
      />
      <span>
        {check.level === "error" ? (
          <>
            <b className="font-semibold">{fillCopy(t[key], params)}</b>
            <small className="block text-item-note text-muted-foreground">
              {t.blocks}
            </small>
            <span className="mt-1.5 flex flex-wrap gap-1.5">
              {check.fixes.map((fix) =>
                fix.kind === "restore-field" && fix.fieldId ? (
                  <button
                    key={fix.kind}
                    type="button"
                    className={cn(
                      BUTTON_CLASS.outline,
                      "h-7 px-2.5 text-crumb",
                    )}
                    onClick={() => onRestoreField(fix.fieldId!)}
                  >
                    {t.restoreField}
                  </button>
                ) : fix.kind === "choose-field" && fix.stepId ? (
                  <button
                    key={fix.kind}
                    type="button"
                    className={cn(
                      BUTTON_CLASS.outline,
                      "h-7 px-2.5 text-crumb",
                    )}
                    onClick={() => onChooseField(fix.stepId!)}
                  >
                    {t.chooseField}
                  </button>
                ) : null,
              )}
            </span>
          </>
        ) : check.level === "warning" ? (
          <>
            {fillCopy(t[key], params)}
            <small className="block text-item-note text-muted-foreground">
              {fillCopy(t.warnBefore, params)}
              <ConnectLink
                href={integrationsHref}
                label={t.warnLink}
                unavailable={copy.integrationsUnavailable}
              />
              {t.warnAfter}
            </small>
          </>
        ) : (
          fillCopy(t[key], params)
        )}
      </span>
    </div>
  );
}
