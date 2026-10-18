import { useMemo, useState, type ReactNode } from "react";
import { ChevronLeft, CircleAlert, Info, Clock } from "lucide-react";
import { Skeleton, cn } from "@notrelix/ui-web";
import type {
  AutomationRuleDetailDto,
  AutomationRuleDocumentDto,
} from "@notrelix/work-management-core";
import { Callout, BUTTON_CLASS, SwitchControl } from "./board-settings-parts";
import { fillCopy } from "./board-settings-format";
import type { AutomationEditorCopy } from "./board-automation.copy";
import type { AutomationPanelProps } from "./automation-types";
import { AutomationAside } from "./automation-aside";
import { AutomationFlow } from "./automation-flow";
import { StatusTag } from "./automation-parts";
import { dayMonthOf } from "./automation-format";

/** The editor of one rule: its flow, its footer and the column on the right. */

export function AutomationEditor({
  copy,
  panel,
  timeZone,
  scopeMark,
}: {
  copy: AutomationEditorCopy;
  panel: AutomationPanelProps;
  timeZone: string;
  scopeMark: ReactNode;
}) {
  const state = panel.rule;
  if (state.status === "loading" || state.status === "idle")
    return (
      <div
        role="status"
        aria-label={copy.loading}
        className="flex min-w-0 flex-1 flex-col gap-3 p-6"
      >
        <Skeleton className="h-6 w-64" />
        <Skeleton className="h-40 rounded-[10px]" />
        <Skeleton className="h-32 rounded-[10px]" />
      </div>
    );
  if (state.status === "error" || state.status === "not-found" || !state.data)
    return (
      <section
        role="alert"
        className="grid min-w-0 flex-1 place-items-center p-6"
      >
        <div className="flex max-w-[420px] flex-col items-center gap-3 text-center">
          <p className="m-0 text-item-ui text-muted-foreground">
            {state.status === "not-found" ? copy.notFound : copy.error}
          </p>
          {state.status === "error" ? (
            <button
              type="button"
              onClick={panel.onReloadRule}
              className={BUTTON_CLASS.outline}
            >
              {copy.retry}
            </button>
          ) : (
            <button
              type="button"
              onClick={panel.onCloseRule}
              className={BUTTON_CLASS.outline}
            >
              {copy.back}
            </button>
          )}
        </div>
      </section>
    );
  return (
    <RuleEditor
      key={`${state.data.id}:${state.data.revision}`}
      copy={copy}
      panel={panel}
      rule={state.data}
      timeZone={timeZone}
      scopeMark={scopeMark}
    />
  );
}

function RuleEditor({
  copy,
  panel,
  rule,
  timeZone,
  scopeMark,
}: {
  copy: AutomationEditorCopy;
  panel: AutomationPanelProps;
  rule: AutomationRuleDetailDto;
  timeZone: string;
  scopeMark: ReactNode;
}) {
  const [doc, setDoc] = useState<AutomationRuleDocumentDto>(rule.definition);
  const [busy, setBusy] = useState(false);
  const [feedback, setFeedback] = useState<
    "conflict" | "invalid" | "failed" | null
  >(null);
  const [chooseFieldStepId, setChooseFieldStepId] = useState<string | null>(
    null,
  );
  const dirty = useMemo(
    () => JSON.stringify(doc) !== JSON.stringify(rule.definition),
    [doc, rule.definition],
  );
  const live = rule.status !== "draft";
  const on = rule.status === "on";
  const blocked = rule.blocked && !on;
  const schedule = doc.trigger.kind === "schedule";
  const firstError = rule.checks.find(
    (check) => check.level === "error" && check.code === "step-field-archived",
  );

  async function submit(mode: "draft" | "enable" | "version") {
    setBusy(true);
    const result = await panel.actions.save(doc, mode);
    setBusy(false);
    setFeedback(result.ok ? null : result.reason);
  }

  async function toggle(next: boolean) {
    if (!live) {
      await submit("enable");
      return;
    }
    setBusy(true);
    const result = await panel.actions.setEnabled(rule.id, next);
    setBusy(false);
    setFeedback(result.ok ? null : result.reason);
  }

  const tagTone = on ? "ok" : blocked ? "err" : "neutral";
  const tagText = on
    ? copy.status.on
    : blocked
      ? copy.status.blocked
      : rule.status === "draft"
        ? copy.status.draft
        : copy.status.off;
  const hint = !live
    ? blocked
      ? copy.footer.blocked
      : copy.footer.draft
    : dirty
      ? copy.footer.dirty
      : blocked
        ? copy.footer.blocked
        : copy.footer.clean;
  const errorField = firstError?.params;

  return (
    <>
      <section
        aria-labelledby="automation-crumb-tail"
        className="flex min-h-0 min-w-0 flex-1 flex-col bg-background"
      >
        <div className="flex flex-1 flex-col overflow-auto px-6 pt-4 pb-[18px] [&>*]:shrink-0">
          <div className="mb-1.5 flex items-center gap-1.5 text-crumb text-meta-foreground">
            <a
              href="#automations"
              aria-label={copy.backAria}
              onClick={(event) => {
                event.preventDefault();
                panel.onCloseRule();
              }}
              className="inline-flex items-center gap-[3px] font-medium text-brand-subtle-foreground hover:underline"
            >
              <ChevronLeft aria-hidden="true" className="size-3.5" />
              {copy.back}
            </a>
            <span>/</span>
            <span id="automation-crumb-tail">
              {live ? copy.editTail : copy.newTail}
            </span>
          </div>
          <div className="flex items-center gap-2.5">
            <label className="sr-only" htmlFor="rule-name">
              {copy.nameLabel}
            </label>
            <input
              id="rule-name"
              type="text"
              value={doc.name}
              onChange={(event) => setDoc({ ...doc, name: event.target.value })}
              className="-ml-[7px] min-w-0 flex-1 rounded-[7px] border border-transparent bg-transparent px-1.5 py-0.5 text-[18px] leading-[1.45] font-[650] tracking-[-0.01em] text-foreground outline-none hover:border-line-strong hover:bg-popover focus-visible:ring-2 focus-visible:ring-focus-ring"
            />
            <StatusTag tone={tagTone}>{tagText}</StatusTag>
            <SwitchControl
              checked={on}
              disabled={busy || blocked}
              describedBy={blocked ? "bad-msg" : undefined}
              onChange={(next) => void toggle(next)}
              label={fillCopy(copy.toggle, { name: doc.name })}
            />
          </div>
          <p className="mt-0.5 mb-3.5 text-crumb text-meta-foreground">
            {rule.subtitle}
          </p>

          {on && !rule.blocked ? (
            <Callout
              icon={Info}
              role="status"
              className="mb-3 !bg-brand-subtle [&_svg]:!text-brand-subtle-foreground"
            >
              <span>
                <b className="font-semibold">{copy.onNote.lead}</b>
                {fillCopy(copy.onNote.rest, { version: rule.version })}
              </span>
            </Callout>
          ) : null}
          {rule.blocked && errorField ? (
            <div
              role="alert"
              className="mb-3 flex items-start gap-[9px] rounded-lg border border-transparent bg-danger-subtle px-3 py-2.5 text-crumb leading-normal text-foreground"
            >
              <CircleAlert
                aria-hidden="true"
                className="mt-0.5 size-3.5 shrink-0 text-danger-text"
              />
              <span className="flex-1">
                <b id="bad-msg" className="font-semibold">
                  {fillCopy(copy.blockedNote.lead, {
                    field: String(errorField.field),
                  })}
                </b>
                <br />
                {fillCopy(
                  Number(errorField.step) > 0
                    ? copy.blockedNote.restWrite
                    : copy.blockedNote.restRead,
                  {
                    step: Number(errorField.step),
                    by: String(errorField.by),
                    date: dayMonthOf(String(errorField.at), timeZone),
                  },
                )}
              </span>
            </div>
          ) : null}
          {feedback ? (
            <Callout
              icon={CircleAlert}
              tone="warning"
              role="alert"
              className="mb-3 items-start"
            >
              <span className="flex flex-1 flex-col gap-1">
                <b className="font-semibold">
                  {feedback === "conflict"
                    ? copy.feedback.conflictTitle
                    : feedback === "invalid"
                      ? copy.feedback.invalid
                      : copy.feedback.failed}
                </b>
                {feedback === "conflict" ? (
                  <span>{copy.feedback.conflictBody}</span>
                ) : null}
                <span className="mt-1 flex gap-2">
                  {feedback === "conflict" ? (
                    <button
                      type="button"
                      className={cn(BUTTON_CLASS.outline, "h-7")}
                      onClick={panel.onReloadRule}
                    >
                      {copy.feedback.reload}
                    </button>
                  ) : null}
                  <button
                    type="button"
                    className={cn(BUTTON_CLASS.ghost, "h-7")}
                    onClick={() => setFeedback(null)}
                  >
                    {copy.feedback.close}
                  </button>
                </span>
              </span>
            </Callout>
          ) : null}

          <AutomationFlow
            copy={copy}
            definition={doc}
            catalog={rule.catalog}
            nextRun={rule.nextRun}
            integrationsHref={panel.integrationsHref}
            defaultPicker={panel.defaultPicker}
            chooseFieldStepId={chooseFieldStepId}
            onChooseField={setChooseFieldStepId}
            onChange={setDoc}
            onRestoreField={(fieldId) =>
              void panel.actions.restoreField(fieldId)
            }
          />

          <p className="mt-3 mb-0 flex items-center gap-[7px] text-item-note text-meta-foreground">
            <Clock aria-hidden="true" className="size-3.5 shrink-0" />
            {schedule ? copy.flowNoteSchedule : copy.flowNote}
          </p>
        </div>

        <div className="relative z-[2] flex items-center gap-2 border-t border-line bg-popover px-6 py-3">
          <span className="flex-1 text-item-note text-meta-foreground">
            {hint}
          </span>
          <button
            type="button"
            className={BUTTON_CLASS.outline}
            onClick={() => {
              if (dirty) setDoc(rule.definition);
              else panel.onCloseRule();
            }}
          >
            {copy.footer.cancel}
          </button>
          {!live ? (
            <>
              <button
                type="button"
                className={BUTTON_CLASS.outline}
                disabled={busy}
                onClick={() => void submit("draft")}
              >
                {copy.footer.saveDraft}
              </button>
              <button
                type="button"
                className={BUTTON_CLASS.primary}
                disabled={busy || blocked}
                aria-describedby={blocked ? "bad-msg" : undefined}
                onClick={() => void submit("enable")}
              >
                {copy.footer.enable}
              </button>
            </>
          ) : (
            <button
              type="button"
              className={BUTTON_CLASS.primary}
              disabled={busy || !dirty}
              onClick={() => void submit("version")}
            >
              {fillCopy(copy.footer.saveVersion, { version: rule.version + 1 })}
            </button>
          )}
        </div>
      </section>

      <AutomationAside
        copy={copy}
        rule={rule}
        timeZone={timeZone}
        scopeMark={scopeMark}
        integrationsHref={panel.integrationsHref}
        onChangeRunner={(personId) => void panel.actions.changeRunner(personId)}
        onRestoreField={(fieldId) => void panel.actions.restoreField(fieldId)}
        onChooseField={setChooseFieldStepId}
        onOpenLog={panel.onOpenLog}
      />
    </>
  );
}
