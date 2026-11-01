import { useState } from "react";
import { History, Plus, TriangleAlert, Zap } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { BoardSettingsAutomationDto } from "@notrelix/work-management-core";
import { PersonMark } from "../item-page/item-page-parts";
import type { AutomationPanelProps } from "./automation-types";
import { useSettings } from "./board-settings-context";
import { fillCopy } from "./board-settings-format";
import {
  BUTTON_CLASS,
  Callout,
  SectionHeader,
  SectionLine,
  SwitchControl,
  Tag_,
  TypeMark,
} from "./board-settings-parts";

const CHIP_CLASS =
  "inline-flex h-[26px] items-center rounded-[6px] border border-line bg-background px-2 text-crumb font-semibold whitespace-nowrap";

/** The "Tự động hoá" section: the rules of the board, running and paused. */
export function AutomationsSection({
  panel,
}: {
  panel?: AutomationPanelProps;
}) {
  const { copy, settings } = useSettings();
  const t = copy.automations;
  const running = settings.automations.filter((rule) => rule.enabled);
  const paused = settings.automations.filter(
    (rule) => !rule.enabled && !rule.draft,
  );
  const drafts = settings.automations.filter((rule) => rule.draft);
  const [creating, setCreating] = useState(false);

  return (
    <section
      aria-labelledby="bs-auto-h"
      className="min-w-0 flex-1 overflow-auto px-6 pt-5 pb-6"
    >
      <div className="max-w-[780px]">
        <SectionHeader
          id="bs-auto-h"
          title={t.title}
          intro={t.intro}
          actions={
            <button
              type="button"
              disabled={!panel || creating}
              onClick={() => {
                if (!panel) return;
                setCreating(true);
                void panel.onCreateRule().finally(() => setCreating(false));
              }}
              className={BUTTON_CLASS.primary}
            >
              <Plus aria-hidden="true" />
              {t.create}
            </button>
          }
        />
        {settings.automations.length === 0 ? (
          <p className="m-0 text-item-small text-meta-foreground">{t.empty}</p>
        ) : null}
        {running.length > 0 ? (
          <>
            <SectionLine title={t.running} count={running.length} />
            <div className="flex flex-col gap-2.5">
              {running.map((rule) => (
                <RuleCard key={rule.id} rule={rule} panel={panel} />
              ))}
            </div>
          </>
        ) : null}
        {paused.length > 0 ? (
          <>
            <SectionLine
              title={t.paused}
              count={paused.length}
              note={t.pausedNote}
            />
            <div className="flex flex-col gap-2.5">
              {paused.map((rule) => (
                <RuleCard key={rule.id} rule={rule} panel={panel} />
              ))}
            </div>
          </>
        ) : null}
        {drafts.length > 0 ? (
          <>
            <SectionLine
              title={t.editor.list.drafts}
              count={drafts.length}
              note={t.editor.list.draftsNote}
            />
            <div className="flex flex-col gap-2.5">
              {drafts.map((rule) => (
                <RuleCard key={rule.id} rule={rule} panel={panel} />
              ))}
            </div>
          </>
        ) : null}
        <div className="mt-3.5 flex items-center gap-2.5 text-crumb text-meta-foreground">
          <span>{t.footer}</span>
          <span className="flex-1" />
          <button
            type="button"
            disabled={!panel}
            onClick={panel?.onOpenLog}
            className={cn(
              BUTTON_CLASS.ghost,
              "text-brand-subtle-foreground hover:text-brand-subtle-foreground",
            )}
          >
            <History aria-hidden="true" className="!size-3.5" />
            {t.log}
          </button>
        </div>
      </div>
    </section>
  );
}

function RuleCard({
  rule,
  panel,
}: {
  rule: BoardSettingsAutomationDto;
  panel?: AutomationPanelProps;
}) {
  const { copy, actions, run } = useSettings();
  const t = copy.automations;
  const blocked = rule.pausedReason !== undefined;
  return (
    <article
      aria-label={rule.name}
      className="flex flex-col gap-2.5 rounded-[10px] border border-line bg-popover px-3.5 py-3"
    >
      <div className="flex items-center gap-2.5">
        <TypeMark icon={Zap} />
        <b
          className={cn(
            "text-[14px] font-semibold",
            !rule.enabled && "text-muted-foreground",
          )}
        >
          {rule.name}
        </b>
        <span className="flex-1" />
        {rule.draft ? (
          <Tag_ tone="warning">{t.editor.list.draftTag}</Tag_>
        ) : null}
        <button
          type="button"
          disabled={!panel}
          onClick={() => panel?.onOpenRule(rule.id)}
          className={cn(BUTTON_CLASS.ghost, "h-7")}
        >
          {t.edit}
        </button>
        {rule.draft ? null : (
          <SwitchControl
            checked={rule.enabled}
            disabled={blocked && !rule.enabled}
            onChange={(next) =>
              void run(() => actions.setAutomationEnabled(rule.id, next))
            }
            label={fillCopy(t.toggle, { name: rule.name })}
          />
        )}
      </div>
      <p
        className={cn(
          "m-0 flex flex-wrap items-center gap-1.5 text-item-small",
          !rule.enabled && "text-muted-foreground",
        )}
      >
        <span className="text-meta-foreground">{t.when}</span>
        <span
          className={cn(
            CHIP_CLASS,
            "border-transparent bg-brand-subtle text-brand-subtle-foreground",
          )}
        >
          {rule.when}
        </span>
        {rule.condition ? (
          <>
            <span className="text-meta-foreground">{t.if}</span>
            <span className={CHIP_CLASS}>{rule.condition}</span>
          </>
        ) : null}
        {rule.then.map((action, index) => (
          <span key={action} className="contents">
            <span className="text-meta-foreground">
              {index === 0 ? t.then : t.and}
            </span>
            <span className={CHIP_CLASS}>{action}</span>
          </span>
        ))}
      </p>
      {rule.pausedReason ? (
        <Callout
          icon={TriangleAlert}
          tone="warning"
          role="status"
          className="items-center"
        >
          <span className="flex-1">
            <b className="font-semibold">{rule.pausedReason.lead}</b>
            {rule.pausedReason.rest}
          </span>
          <button
            type="button"
            disabled={!panel}
            onClick={() => panel?.onOpenRule(rule.id)}
            className={cn(BUTTON_CLASS.outline, "h-7")}
          >
            {rule.pausedReason.action}
          </button>
        </Callout>
      ) : null}
      <div className="flex flex-wrap items-center gap-3.5 text-crumb text-meta-foreground">
        <span>
          {rule.stats}
          {rule.partialFailure ? (
            <>
              <span className="font-semibold text-danger-text">
                {rule.partialFailure}
              </span>
              {rule.failureDetail}
            </>
          ) : null}
        </span>
        <span className="inline-flex items-center gap-1.5">
          {t.runAs}
          <PersonMark
            person={{
              id: rule.runAs.initials,
              userId: rule.runAs.initials,
              initials: rule.runAs.initials,
            }}
            className="size-5 text-mark-xs"
            title={rule.runAs.name}
          />
          <b className="font-semibold text-foreground">{rule.runAs.name}</b>
        </span>
      </div>
    </article>
  );
}
