import * as React from "react";
import { cn } from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
import type { SectionState } from "../home/home-models";
import {
  BILLING_BTN,
  BILLING_BTN_PRIMARY,
  BackLink,
  BillingGlyph,
  BOX,
  BOX_TEXT,
  BOX_TITLE,
  CARD,
  InfoNote,
  NavAnchor,
  PageHead,
  Rich,
} from "./billing-parts";
import {
  clockTime,
  fullDay,
  moneyLabel,
  sizeLabel,
  type BillingDowngradeDto,
  type BillingFormatters,
  type BillingLinks,
} from "./billing-models";
import type { BillingCopy } from "./billing.copy";

export interface BillingDowngradeProps {
  readonly copy: BillingCopy;
  readonly formatters: BillingFormatters;
  readonly links: BillingLinks;
  readonly state: SectionState<BillingDowngradeDto>;
  readonly saving: boolean;
  readonly onConfirm: () => void;
  readonly onCancelRequest: () => void;
  readonly onRetry: () => void;
}

function Change({
  label,
  change,
  children,
  detail,
}: {
  label: string;
  change: string;
  children: React.ReactNode;
  detail: string;
}) {
  return (
    <div
      data-f="chg"
      className="grid grid-cols-[176px_minmax(0,1fr)] gap-3.5 border-t border-line px-4 py-[13px] first:border-t-0"
    >
      <div className="text-nav leading-[1.45] font-semibold">
        {label}
        <small className="mt-0.5 block text-caption tracking-normal leading-[1.45] font-medium text-meta-foreground">
          {change}
        </small>
      </div>
      <p className="m-0 text-compact leading-[1.55] text-foreground">
        {children}
        <small className="mt-[3px] block text-caption tracking-normal leading-[1.55] text-meta-foreground">
          {detail}
        </small>
      </p>
    </div>
  );
}

/** Downgrade from Business to Pro (Final-Billing `ha-goi`). */
export function BillingDowngradeSurface(props: BillingDowngradeProps) {
  const { copy, formatters: f, links, state } = props;
  const c = copy.downgrade;
  const back = (
    <BackLink href={links.compare.href} onNavigate={links.compare.onNavigate}>
      {c.back}
    </BackLink>
  );
  if (state.status !== "ready") {
    return (
      <div className="flex flex-col gap-4">
        {back}
        {state.status === "loading" ? (
          <p role="status" className="m-0 text-compact text-meta-foreground">
            {copy.page.loading}
          </p>
        ) : (
          <div role="alert" className={cn(BOX, "max-w-[520px]")}>
            <p className={BOX_TEXT}>{copy.page.error}</p>
            <button
              type="button"
              onClick={props.onRetry}
              className={cn(BILLING_BTN, "self-start")}
            >
              {copy.page.retry}
            </button>
          </div>
        )}
      </div>
    );
  }
  const d = state.data;
  const effective = fullDay(d.effectiveAt);
  const keeps = [
    c.keep.data,
    c.keep.access,
    formatCopy(c.keep.seats, { count: f.number(d.kept.seatsOwned) }),
    c.keep.back,
  ];
  return (
    <div className="flex flex-col gap-4" data-f="billing-downgrade">
      {back}
      <PageHead
        id="dg-t"
        title={c.title}
        lede={
          <Rich
            text={formatCopy(c.lede, {
              day: fullDay(d.periodEnd),
              from: effective,
              price: moneyLabel(f, copy.money, d.newUnitPrice),
              count: d.workspaces.length,
              names: d.workspaces.join(", "),
            })}
          />
        }
      />
      <div className="grid grid-cols-[minmax(0,1fr)_316px] items-start gap-5">
        <section data-f="card" aria-labelledby="chg-t" className={CARD}>
          <div className="flex items-center gap-2 border-b border-line px-4 py-3 text-compact leading-[1.45] font-semibold">
            <span id="chg-t">
              {formatCopy(c.changesTitle, { day: effective })}
            </span>
            <span className="ml-auto text-caption tracking-normal font-normal text-meta-foreground">
              {formatCopy(c.usageAsOf, { time: clockTime(f, d.asOf) })}
            </span>
          </div>
          <Change
            label={c.runs.label}
            change={formatCopy(c.runs.change, {
              from: f.number(d.runs.from),
              to: f.number(d.runs.to),
            })}
            detail={formatCopy(c.runs.used, { used: f.number(d.runs.used) })}
          >
            {formatCopy(c.runs.body, { to: f.number(d.runs.to) })}
          </Change>
          <Change
            label={c.reports.label}
            change={c.reports.change}
            detail={c.reports.detail}
          >
            {formatCopy(c.reports.body, {
              count: d.reports.names.length,
              names: d.reports.names.join(", "),
            })}
          </Change>
          <Change
            label={c.webhooks.label}
            change={c.webhooks.change}
            detail={c.webhooks.detail}
          >
            {formatCopy(c.webhooks.body, {
              names: d.webhooks.names.join(", "),
            })}
          </Change>
          <Change
            label={c.storage.label}
            change={formatCopy(c.storage.change, {
              from: sizeLabel(f, d.storage.fromGb),
              to: sizeLabel(f, d.storage.toGb),
            })}
            detail={c.storage.detail}
          >
            {formatCopy(c.storage.body, {
              used: sizeLabel(f, d.storage.usedGb),
              over: sizeLabel(f, d.storage.overGb),
              to: sizeLabel(f, d.storage.toGb),
            })}
          </Change>
        </section>
        <div className="flex min-w-0 flex-col gap-3.5">
          <section data-f="box" aria-labelledby="keep-t" className={BOX}>
            <h3 id="keep-t" className={BOX_TITLE}>
              {c.keepTitle}
            </h3>
            <ul className="m-0 flex list-none flex-col gap-1.5 p-0 text-compact leading-[1.45]">
              {keeps.map((text, index) => (
                <li key={text} className="flex items-start gap-2">
                  <BillingGlyph
                    name="check"
                    className="mt-0.5 size-3.5 text-success"
                  />
                  {index === 0 ? <b className="font-semibold">{text}</b> : text}
                </li>
              ))}
            </ul>
          </section>
          <InfoNote>
            {formatCopy(c.cancelInfo, {
              day: fullDay(d.periodEnd),
              from: effective,
            })}
          </InfoNote>
          <div className="flex items-center gap-2">
            <NavAnchor link={links.overview} className={BILLING_BTN_PRIMARY}>
              {c.keepBusiness}
            </NavAnchor>
            {d.pending ? (
              <button
                type="button"
                onClick={props.onCancelRequest}
                aria-disabled={props.saving || undefined}
                className={BILLING_BTN}
              >
                {c.cancelRequest}
              </button>
            ) : (
              <button
                type="button"
                onClick={props.onConfirm}
                aria-disabled={props.saving || undefined}
                className={BILLING_BTN}
              >
                {props.saving ? c.confirming : c.confirm}
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
