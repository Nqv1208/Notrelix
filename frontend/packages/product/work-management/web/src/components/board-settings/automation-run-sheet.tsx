import { useEffect, useRef, useState } from "react";
import { Check, ExternalLink, Info, RotateCw, X, Zap } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type {
  AutomationRunDto,
  AutomationWriteResult,
} from "@notrelix/work-management-core";
import { BUTTON_CLASS, Callout } from "./board-settings-parts";
import { fillCopy } from "./board-settings-format";
import type { AutomationEditorCopy } from "./board-automation.copy";
import { ConnectLink } from "./automation-pickers";
import { PersonDot, StatusTag } from "./automation-parts";
import { dayLabel, WithKey } from "./automation-log-parts";
import { timeOf } from "./automation-format";

/** One run in a sheet over the right edge of the log (mockup `.sheet`). */

export function RunSheet({
  copy,
  run,
  nowIso,
  timeZone,
  integrationsHref,
  onClose,
  onRerun,
}: {
  copy: AutomationEditorCopy;
  run: AutomationRunDto;
  nowIso: string;
  timeZone: string;
  integrationsHref?: string;
  onClose: () => void;
  onRerun: (stepIndex: number) => Promise<AutomationWriteResult>;
}) {
  const t = copy.run;
  const closeRef = useRef<HTMLButtonElement>(null);
  const [rerunFailed, setRerunFailed] = useState(false);
  useEffect(() => {
    closeRef.current?.focus();
  }, []);
  const failedStep = run.steps.find((step) => step.outcome === "error");
  const doneSteps = run.steps
    .filter((step) => step.outcome === "success")
    .map((step) => step.index);
  const reauthorize = run.fixes.find((fix) => fix.kind === "reauthorize");
  const rerun = run.fixes.find((fix) => fix.kind === "rerun-step");
  const rowClass =
    "grid grid-cols-[132px_minmax(0,1fr)] gap-2.5 border-t border-line px-3 py-[7px] text-item-small items-center first:border-t-0";
  return (
    <>
      <div
        aria-hidden="true"
        onClick={onClose}
        className="absolute inset-0 z-24 bg-modal-scrim/40 dark:bg-modal-scrim/70"
      />
      <aside
        role="dialog"
        aria-modal="true"
        aria-labelledby="rd-t"
        onKeyDown={(event) => {
          if (event.key === "Escape") {
            event.stopPropagation();
            onClose();
          }
        }}
        className="absolute inset-y-0 right-0 z-25 flex w-[471px] flex-col border-l border-line bg-popover shadow-sheet"
      >
        <div className="flex items-center gap-2.5 border-b border-line px-[18px] py-3.5">
          <span
            aria-hidden="true"
            className="grid size-[30px] shrink-0 place-items-center rounded-lg bg-danger-subtle text-danger-text"
          >
            <Zap className="size-4" />
          </span>
          <div className="min-w-0 flex-1">
            <b
              id="rd-t"
              className="block text-[15px] leading-[1.45] font-[650]"
            >
              {run.ruleName}
            </b>
            <small className="block text-item-note text-meta-foreground">
              {fillCopy(t.updated, {
                when: `${dayLabel(copy, run.at, nowIso, timeZone)} ${timeOf(run.at, timeZone)}`,
                time: timeOf(nowIso, timeZone),
              })}
            </small>
          </div>
          <StatusTag
            tone={
              run.outcome === "error"
                ? "err"
                : run.outcome === "success"
                  ? "ok"
                  : "neutral"
            }
          >
            {run.outcome === "error"
              ? run.stepsDone > 0
                ? copy.log.partial
                : copy.log.failed
              : run.outcome === "success"
                ? copy.log.success
                : copy.log.skippedTag}
          </StatusTag>
          <button
            ref={closeRef}
            type="button"
            aria-label={t.close}
            onClick={onClose}
            className="grid size-[30px] shrink-0 cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent text-muted-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <X aria-hidden="true" className="size-4" />
          </button>
        </div>
        <div className="flex flex-1 flex-col gap-3.5 overflow-auto px-[18px] pt-4 pb-[18px] [&>*]:shrink-0">
          <dl className="m-0 flex flex-col overflow-hidden rounded-[9px] border border-line">
            <div className={rowClass}>
              <dt className="text-crumb font-semibold text-muted-foreground">
                {t.firedBy}
              </dt>
              <dd className="m-0 block">
                <WithKey text={run.firedBy} itemKey={run.trigger.itemKey} />
              </dd>
            </div>
            <div className={rowClass}>
              <dt className="text-crumb font-semibold text-muted-foreground">
                {t.condition}
              </dt>
              <dd className="m-0 flex flex-wrap items-center gap-1.5">
                {run.condition}
              </dd>
            </div>
            <div className={rowClass}>
              <dt className="text-crumb font-semibold text-muted-foreground">
                {t.runner}
              </dt>
              <dd className="m-0 flex flex-wrap items-center gap-1.5">
                <PersonDot person={run.runAs} size={20} />
                {run.runAs.name}
              </dd>
            </div>
            <div className={rowClass}>
              <dt className="text-crumb font-semibold text-muted-foreground">
                {t.version}
              </dt>
              <dd className="m-0 flex flex-wrap items-center gap-1.5">
                {run.version}{" "}
                <small className="text-item-note text-meta-foreground">
                  {run.versionIsCurrent ? t.currentVersion : t.olderVersion}
                </small>
              </dd>
            </div>
          </dl>

          {run.steps.length > 0 ? (
            <div className="flex flex-col gap-[7px]">
              <span className="text-crumb font-semibold text-muted-foreground">
                {t.steps}
              </span>
              <ol className="m-0 flex list-none flex-col gap-2 p-0">
                {run.steps.map((step) => (
                  <li
                    key={step.index}
                    className={cn(
                      "grid grid-cols-[26px_minmax(0,1fr)] gap-[9px] rounded-[9px] border px-3 py-2.5",
                      step.outcome === "error"
                        ? "border-danger"
                        : "border-line",
                    )}
                  >
                    <span
                      aria-hidden="true"
                      className={cn(
                        "grid size-6 place-items-center rounded-full",
                        step.outcome === "error"
                          ? "bg-danger-subtle text-danger-text"
                          : "bg-success-subtle text-success-text",
                      )}
                    >
                      {step.outcome === "error" ? (
                        <X className="size-3.5" />
                      ) : (
                        <Check className="size-3.5" />
                      )}
                    </span>
                    <div>
                      <b className="block text-item-small font-semibold">
                        {fillCopy(t.step, { n: step.index, title: step.title })}
                      </b>
                      <div className="mt-1 flex flex-wrap items-center gap-1.5 text-crumb text-muted-foreground">
                        <StatusTag
                          tone={step.outcome === "error" ? "err" : "ok"}
                        >
                          {step.outcome === "error" ? t.stepFailed : t.stepOk}
                        </StatusTag>
                        {step.error ?? timeOf(step.at, timeZone)}
                      </div>
                    </div>
                  </li>
                ))}
              </ol>
            </div>
          ) : null}

          {run.fixes.length > 0 ? (
            <div className="flex flex-col gap-[7px]">
              <span className="text-crumb font-semibold text-muted-foreground">
                {t.fix}
              </span>
              <div className="flex flex-col overflow-hidden rounded-[9px] border border-line">
                {reauthorize ? (
                  <div className="grid grid-cols-[22px_minmax(0,1fr)_auto] items-center gap-2.5 px-3 py-2.5 text-item-small">
                    <span
                      aria-hidden="true"
                      className="box-border grid size-[22px] place-items-center rounded-full border border-line-strong bg-popover text-[11.5px] font-[650] text-muted-foreground"
                    >
                      1
                    </span>
                    <div>
                      <b className="font-semibold">{reauthorize.title}</b>
                      <small className="mt-0.5 block text-item-note text-meta-foreground">
                        {reauthorize.detail}
                      </small>
                    </div>
                    <ConnectButton
                      href={integrationsHref}
                      label={t.reauthorize}
                      unavailable={copy.integrationsUnavailable}
                    />
                  </div>
                ) : null}
                {rerun ? (
                  <div className="grid grid-cols-[22px_minmax(0,1fr)_auto] items-center gap-2.5 border-t border-line px-3 py-2.5 text-item-small">
                    <span
                      aria-hidden="true"
                      className="box-border grid size-[22px] place-items-center rounded-full border border-line-strong bg-popover text-[11.5px] font-[650] text-muted-foreground"
                    >
                      2
                    </span>
                    <div>
                      <b className="font-semibold">{rerun.title}</b>
                      <small
                        id="rerun-why"
                        className="mt-0.5 block text-item-note text-meta-foreground"
                      >
                        {rerunFailed
                          ? t.rerunFailed
                          : (rerun.blockedReason ?? rerun.detail)}
                      </small>
                    </div>
                    <button
                      type="button"
                      disabled={rerun.blockedReason !== null}
                      aria-describedby="rerun-why"
                      onClick={async () => {
                        if (!failedStep) return;
                        const result = await onRerun(failedStep.index);
                        setRerunFailed(!result.ok);
                      }}
                      className={cn(
                        BUTTON_CLASS.outline,
                        "h-7 px-2.5 text-crumb",
                      )}
                    >
                      <RotateCw aria-hidden="true" className="!size-3.5" />
                      {t.rerun}
                    </button>
                  </div>
                ) : null}
              </div>
              {failedStep ? (
                <Callout
                  icon={Info}
                  className="!border-transparent !bg-brand-subtle !text-foreground [&_svg]:!text-brand-subtle-foreground"
                >
                  <span>
                    {doneSteps.length > 0
                      ? fillCopy(t.rerunInfo, {
                          failed: failedStep.index,
                          done: doneSteps.join(", "),
                        })
                      : fillCopy(t.rerunInfoNoDone, {
                          failed: failedStep.index,
                        })}
                  </span>
                </Callout>
              ) : null}
            </div>
          ) : null}
        </div>
      </aside>
    </>
  );
}

function ConnectButton({
  href,
  label,
  unavailable,
}: {
  href?: string;
  label: string;
  unavailable: string;
}) {
  const cls = cn(BUTTON_CLASS.primary, "h-7 px-2.5 text-crumb");
  if (href)
    return (
      <a href={href} className={cls}>
        {label}
        <ExternalLink aria-hidden="true" className="!size-3.5" />
      </a>
    );
  return (
    <ConnectLink label={label} unavailable={unavailable} className={cls} />
  );
}
