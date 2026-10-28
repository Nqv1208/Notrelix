import { useMemo, useState } from "react";
import { ChevronLeft, CircleAlert, RotateCw } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import type {
  AutomationRunDto,
  AutomationRunsDto,
} from "@notrelix/work-management-core";
import { BUTTON_CLASS } from "./board-settings-parts";
import { fillCopy } from "./board-settings-format";
import type { AutomationEditorCopy } from "./board-automation.copy";
import type { AutomationPanelProps } from "./automation-types";
import { PersonDot, StatusTag } from "./automation-parts";
import { RunSheet } from "./automation-run-sheet";
import { dayLabel, ItemKey, WithKey } from "./automation-log-parts";
import { timeOf } from "./automation-format";

/** Radix items cannot carry an empty value, so "all rules" is a sentinel. */
const ALL_RULES = "__all__";

/** The run log of the board's automations (mockup `.logw`) and one run's sheet. */

const COLUMNS =
  "grid grid-cols-[96px_minmax(0,1fr)_176px_184px_144px_58px] items-center gap-3 border-t border-line px-3.5 py-1.5 text-item-small box-border";

type Tab = "all" | "success" | "error" | "skipped";

export function AutomationLog({
  copy,
  panel,
  timeZone,
  billingHref,
}: {
  copy: AutomationEditorCopy;
  panel: AutomationPanelProps;
  timeZone: string;
  billingHref?: string;
}) {
  const t = copy.log;
  const state = panel.runs;
  const data = state.data;
  const [tab, setTab] = useState<Tab>("all");
  const [ruleId, setRuleId] = useState("");
  const counts = useMemo(() => {
    const all = data?.runs ?? [];
    return {
      all: all.length,
      success: all.filter((run) => run.outcome === "success").length,
      error: all.filter((run) => run.outcome === "error").length,
      skipped: all.filter((run) => run.outcome === "skipped").length,
    };
  }, [data]);
  const rows = (data?.runs ?? []).filter(
    (run) =>
      (tab === "all" || run.outcome === tab) &&
      (ruleId === "" || run.ruleId === ruleId),
  );
  const open = data?.runs.find((run) => run.id === panel.runId) ?? null;

  return (
    <>
      <section
        aria-labelledby="automation-log-h"
        className="min-w-0 flex-1 overflow-auto px-6 pt-4 pb-5"
      >
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
          <span>{t.crumb}</span>
        </div>
        <div className="mb-1.5 flex items-start gap-3">
          <div>
            <h2 id="automation-log-h" className="m-0 text-[17px] font-[650]">
              {t.title}
            </h2>
            <p className="mt-0.5 mb-0 max-w-[60ch] text-item-small text-meta-foreground">
              {t.intro}
            </p>
          </div>
          <span className="flex-1" />
          <div className="flex shrink-0 items-center gap-2">
            {data ? (
              <span className="text-item-note whitespace-nowrap text-meta-foreground">
                {fillCopy(t.updated, {
                  time: timeOf(data.generatedAt, timeZone),
                })}
              </span>
            ) : null}
            <button
              type="button"
              onClick={panel.onReloadRuns}
              className={cn(BUTTON_CLASS.outline, "h-7 px-2.5 text-crumb")}
            >
              <RotateCw aria-hidden="true" className="!size-3.5" />
              {t.reload}
            </button>
          </div>
        </div>

        {state.status === "loading" || state.status === "idle" ? (
          <div
            role="status"
            aria-label={t.loading}
            className="mt-4 flex flex-col gap-2"
          >
            <Skeleton className="h-9 w-80" />
            <Skeleton className="h-64 rounded-[10px]" />
          </div>
        ) : state.status === "error" || !data ? (
          <div role="alert" className="mt-6 flex flex-col items-start gap-3">
            <p className="m-0 text-item-ui text-muted-foreground">
              {t.loadError}
            </p>
            <button
              type="button"
              onClick={panel.onReloadRuns}
              className={BUTTON_CLASS.outline}
            >
              {t.retry}
            </button>
          </div>
        ) : (
          <>
            <div className="mt-2.5 mb-3 flex items-end gap-0.5 border-b border-line">
              <div
                role="tablist"
                aria-label={t.filterAria}
                className="flex gap-0.5"
              >
                {(["all", "success", "error", "skipped"] as const).map(
                  (key) => (
                    <button
                      key={key}
                      type="button"
                      role="tab"
                      aria-selected={tab === key}
                      onClick={() => setTab(key)}
                      className={cn(
                        "-mb-px inline-flex h-[38px] cursor-pointer items-center gap-1.5 border-0 border-b-2 border-transparent bg-transparent px-2.5 text-item-small font-medium text-muted-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                        tab === key &&
                          "border-primary font-semibold text-foreground",
                      )}
                    >
                      {t[key]}{" "}
                      <span className="text-item-note font-medium text-meta-foreground">
                        {counts[key]}
                      </span>
                    </button>
                  ),
                )}
              </div>
              <span className="flex-1" />
              <label className="sr-only" htmlFor="lg-rule">
                {t.ruleFilter}
              </label>
              <Select
                value={ruleId === "" ? ALL_RULES : ruleId}
                onValueChange={(value) =>
                  setRuleId(value === ALL_RULES ? "" : value)
                }
              >
                <SelectTrigger
                  id="lg-rule"
                  size="sm"
                  className="mb-[5px] w-[210px]"
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={ALL_RULES}>{t.allRules}</SelectItem>
                  {data.rules.map((rule) => (
                    <SelectItem key={rule.id} value={rule.id}>
                      {rule.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div
              role="table"
              aria-label={t.table}
              className="overflow-hidden rounded-[10px] border border-line bg-popover"
            >
              <div
                role="row"
                className={cn(
                  COLUMNS,
                  "min-h-[34px] border-t-0 bg-background text-item-note font-semibold text-meta-foreground",
                )}
              >
                <span role="columnheader">{t.columns.time}</span>
                <span role="columnheader">{t.columns.rule}</span>
                <span role="columnheader">{t.columns.trigger}</span>
                <span role="columnheader">{t.columns.result}</span>
                <span role="columnheader">{t.columns.runner}</span>
                <span role="columnheader">{t.columns.version}</span>
              </div>
              {rows.map((run) => (
                <RunRow
                  key={run.id}
                  run={run}
                  copy={copy}
                  nowIso={data.generatedAt}
                  timeZone={timeZone}
                  selected={run.id === panel.runId}
                  onOpen={() => panel.onOpenRun(run.id)}
                />
              ))}
              {rows.length === 0 ? (
                <p className="m-0 border-t border-line px-3.5 py-4 text-item-small text-meta-foreground">
                  {data.runs.length === 0 ? t.empty : t.emptyFilter}
                </p>
              ) : null}
            </div>
            <LogFoot copy={copy} data={data} billingHref={billingHref} />
          </>
        )}
      </section>

      {open ? (
        <RunSheet
          copy={copy}
          run={open}
          nowIso={data?.generatedAt ?? open.at}
          timeZone={timeZone}
          integrationsHref={panel.integrationsHref}
          onClose={() => panel.onOpenRun(null)}
          onRerun={(stepIndex) => panel.actions.rerunStep(open.id, stepIndex)}
        />
      ) : null}
    </>
  );
}

function LogFoot({
  copy,
  data,
  billingHref,
}: {
  copy: AutomationEditorCopy;
  data: AutomationRunsDto;
  billingHref?: string;
}) {
  const t = copy.log;
  return (
    <div className="mt-3 flex items-start gap-4 text-item-note leading-normal text-meta-foreground">
      <span className="flex-1">{t.footnote}</span>
      <span>
        {fillCopy(t.usage, {
          runs: new Intl.NumberFormat("vi-VN").format(data.usage.runs),
        })}{" "}
        {billingHref ? (
          <a
            href={billingHref}
            className="font-medium text-brand-subtle-foreground hover:underline"
          >
            {t.plans}
          </a>
        ) : (
          <span className="font-medium text-brand-subtle-foreground">
            {t.plans}
          </span>
        )}
      </span>
    </div>
  );
}

function RunRow({
  run,
  copy,
  nowIso,
  timeZone,
  selected,
  onOpen,
}: {
  run: AutomationRunDto;
  copy: AutomationEditorCopy;
  nowIso: string;
  timeZone: string;
  selected: boolean;
  onOpen: () => void;
}) {
  const t = copy.log;
  const failed = run.outcome === "error";
  return (
    <div
      role="row"
      aria-selected={selected}
      className={cn(
        COLUMNS,
        "min-h-14",
        selected &&
          "bg-state-selected shadow-[inset_0_0_0_1.5px_var(--primary)]",
      )}
    >
      <span role="cell">
        <b className="block font-semibold">{timeOf(run.at, timeZone)}</b>
        <small className="block text-item-note leading-[1.35] text-meta-foreground">
          {dayLabel(copy, run.at, nowIso, timeZone)}
        </small>
      </span>
      <span role="cell" className="leading-[1.35] font-medium">
        {run.ruleName}
      </span>
      <span role="cell">
        {run.trigger.itemKey ? (
          <>
            <ItemKey>{run.trigger.itemKey}</ItemKey>
            <small className="block text-item-note leading-[1.35] text-meta-foreground">
              {run.trigger.text}
            </small>
          </>
        ) : (
          <>
            <span>{run.trigger.text}</span>
            {run.trigger.detail ? (
              <small className="block text-item-note leading-[1.35] text-meta-foreground">
                <WithKey
                  text={run.trigger.detail}
                  itemKey={run.trigger.detailItemKey}
                />
              </small>
            ) : null}
          </>
        )}
      </span>
      <span role="cell" className="flex flex-col items-start gap-[3px]">
        {failed ? (
          <>
            <StatusTag tone="err" icon={CircleAlert}>
              {run.stepsDone > 0 ? t.partial : t.failed}
            </StatusTag>
            <button
              type="button"
              aria-haspopup="dialog"
              aria-label={fillCopy(t.stepsAria, {
                rule: run.ruleName,
                time: timeOf(run.at, timeZone),
              })}
              onClick={onOpen}
              className="-ml-1.5 inline-flex h-6 cursor-pointer items-center rounded-[7px] border-0 bg-transparent px-1.5 text-item-note font-medium text-brand-subtle-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring"
            >
              {fillCopy(t.steps, {
                done: run.stepsDone,
                total: run.stepsTotal,
              })}
            </button>
          </>
        ) : (
          <>
            {run.outcome === "success" ? (
              <StatusTag tone="ok">{t.success}</StatusTag>
            ) : (
              <StatusTag tone={run.warning ? "warn" : "neutral"}>
                {t.skippedTag}
              </StatusTag>
            )}
            <small className="block text-item-note leading-[1.35] text-meta-foreground">
              {run.summary}
            </small>
          </>
        )}
      </span>
      <span role="cell" className="flex min-w-0 items-center gap-[7px]">
        <PersonDot person={run.runAs} size={20} />
        <span className="truncate whitespace-nowrap">{run.runAs.name}</span>
      </span>
      <span role="cell" className="text-muted-foreground tabular-nums">
        {run.version}
      </span>
    </div>
  );
}
