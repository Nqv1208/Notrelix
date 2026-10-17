import * as React from "react";
import { cn } from "@notrelix/ui-web";
import { PersonMark } from "../reports/reports-parts";
import { formatCopy } from "../shell/shell-models";
import type { SectionState } from "../home/home-models";
import {
  BILLING_BTN,
  BILLING_BTN_PRIMARY,
  BOX,
  BOX_TEXT,
  BOX_TITLE,
  BillingGlyph,
  CARD,
  Dot,
  InfoNote,
  LNKB,
  MONO,
  Meter,
  NavAnchor,
  PageHead,
  Rich,
  Tag,
} from "./billing-parts";
import {
  clockTime,
  fullDay,
  monthNumber,
  monthYear,
  moneyLabel,
  shortDay,
  sizeLabel,
  type BillingFeatureKey,
  type BillingFormatters,
  type BillingInvoiceSummaryDto,
  type BillingLinks,
  type BillingOverviewDto,
  type BillingSplitDto,
} from "./billing-models";
import type { BillingCopy } from "./billing.copy";

export interface BillingOverviewProps {
  readonly copy: BillingCopy;
  readonly formatters: BillingFormatters;
  readonly links: BillingLinks;
  readonly state: SectionState<BillingOverviewDto>;
  readonly reloading: boolean;
  readonly paying: boolean;
  readonly onReload: () => void;
  readonly onBuy: () => void;
  readonly onPayNow: () => void;
  /** Steps the payment gateway has no screen for yet (change card, bank transfer, invoice details). */
  readonly onUnavailable: () => void;
  readonly onContactSales: () => void;
}

const SPLIT_TONES = ["one", "two"] as const;

function Fresh({
  copy,
  time,
  onReload,
  reloading,
}: {
  copy: BillingCopy;
  time: string;
  onReload: () => void;
  reloading: boolean;
}) {
  return (
    <span
      data-f="fresh"
      className="inline-flex items-center gap-1.5 pt-1 text-crumb leading-[1.45] whitespace-nowrap text-meta-foreground"
    >
      {formatCopy(copy.page.fresh, { time })}
      <button
        type="button"
        onClick={onReload}
        aria-disabled={reloading || undefined}
        className={LNKB}
      >
        {copy.page.reload}
      </button>
    </span>
  );
}

function UsageRow({
  label,
  hint,
  children,
  value,
}: {
  label: string;
  hint: string;
  children: React.ReactNode;
  value?: React.ReactNode;
}) {
  return (
    <div
      data-f="ur"
      className="grid grid-cols-[148px_minmax(0,1fr)_128px] items-center gap-4 border-t border-line px-4 py-[11px] first:border-t-0"
    >
      <div>
        <b className="block text-nav leading-[1.45] font-semibold text-foreground">
          {label}
        </b>
        <small className="block text-caption tracking-normal leading-[1.45] text-meta-foreground">
          {hint}
        </small>
      </div>
      <div>{children}</div>
      <div className="text-right text-crumb leading-[1.45] whitespace-nowrap text-muted-foreground">
        {value}
      </div>
    </div>
  );
}

function Legend({ children }: { children: React.ReactNode }) {
  return (
    <div className="mt-[7px] flex flex-wrap gap-x-3.5 gap-y-[3px] text-caption tracking-normal leading-[1.45] text-muted-foreground">
      {children}
    </div>
  );
}

function SplitLegend({
  f,
  parts,
}: {
  f: BillingFormatters;
  parts: readonly BillingSplitDto[];
}) {
  return (
    <>
      {parts.map((part, index) => (
        <span key={part.id} className="inline-flex items-center gap-1.5">
          <Dot tone={SPLIT_TONES[index] ?? "two"} />
          {part.name} {f.number(part.count)}
        </span>
      ))}
    </>
  );
}

const splitText = (f: BillingFormatters, parts: readonly BillingSplitDto[]) =>
  parts.map((part) => `${part.name} ${f.number(part.count)}`).join(", ");

function Value({ now, of }: { now: string; of: string }) {
  return (
    <>
      <b className="text-item-heading leading-[1.45] font-[650] text-foreground">
        {now}
      </b>{" "}
      / {of}
    </>
  );
}

function FeatureList({
  copy,
  keys,
  plus,
}: {
  copy: BillingCopy;
  keys: readonly BillingFeatureKey[];
  plus?: boolean;
}) {
  return (
    <ul className="m-0 flex list-none flex-col gap-1.5 p-0 text-compact leading-[1.45]">
      {keys.map((key) => (
        <li key={key} className="flex items-start gap-2">
          <BillingGlyph
            name="check"
            className={cn(
              "mt-0.5 size-3.5",
              plus ? "text-meta-foreground" : "text-success",
            )}
          />
          {copy.features.items[key]}
        </li>
      ))}
    </ul>
  );
}

function usageSection(
  copy: BillingCopy,
  f: BillingFormatters,
  data: BillingOverviewDto,
  links: BillingLinks,
) {
  const u = data.usage;
  const share = (count: number, total: number) =>
    Math.round((count / total) * 1000) / 10;
  const seatsLeft = u.seats.total - u.seats.used;
  const runsMonth = monthNumber(u.runs.month);
  const guestText =
    u.guests.inWorkspace === u.guests.used
      ? copy.usage.guests.all
      : copy.usage.guests.some;
  return (
    <section data-f="card" aria-labelledby="use-t" className={CARD}>
      <div
        data-f="card-t"
        className="flex items-center gap-2 border-b border-line px-4 py-3 text-compact leading-[1.45] font-semibold"
      >
        <span id="use-t">{copy.usage.title}</span>
        <span className="ml-auto text-caption tracking-normal font-normal text-meta-foreground">
          {formatCopy(copy.usage.asOf, { time: clockTime(f, data.asOf) })}
        </span>
      </div>
      <UsageRow
        label={copy.usage.seats.label}
        hint={copy.usage.seats.hint}
        value={
          <Value
            now={f.number(u.seats.used)}
            of={`${f.number(u.seats.total)} ${copy.usage.seats.value}`}
          />
        }
      >
        <Meter
          max={u.seats.total}
          now={u.seats.used}
          label={formatCopy(copy.usage.meter.seats, {
            used: f.number(u.seats.used),
            total: f.number(u.seats.total),
            split: splitText(f, u.seats.perWorkspace),
          })}
          segments={u.seats.perWorkspace.map((part, index) => ({
            share: share(part.count, u.seats.total),
            tone: SPLIT_TONES[index] ?? "two",
          }))}
        />
        <Legend>
          <SplitLegend f={f} parts={u.seats.perWorkspace} />
          <span className="text-meta-foreground">
            {formatCopy(copy.usage.seats.left, { count: f.number(seatsLeft) })}
          </span>
        </Legend>
      </UsageRow>
      <UsageRow
        label={copy.usage.storage.label}
        hint={copy.usage.storage.hint}
        value={
          <Value
            now={sizeLabel(f, u.storage.usedGb)}
            of={sizeLabel(f, u.storage.totalGb)}
          />
        }
      >
        <Meter
          max={u.storage.totalGb}
          now={u.storage.usedGb}
          label={formatCopy(copy.usage.meter.storage, {
            used: sizeLabel(f, u.storage.usedGb),
            total: sizeLabel(f, u.storage.totalGb),
          })}
          segments={[
            { share: share(u.storage.usedGb, u.storage.totalGb), tone: "one" },
          ]}
        />
        <Legend>
          <span className="text-meta-foreground">
            {formatCopy(copy.usage.storage.shared, {
              count: data.account.workspaces.length,
            })}
          </span>
        </Legend>
      </UsageRow>
      <UsageRow
        label={copy.usage.runs.label}
        hint={formatCopy(copy.usage.runs.hint, { month: runsMonth })}
        value={
          <Value
            now={f.number(u.runs.used)}
            of={`${f.number(u.runs.total)} ${copy.usage.runs.value}`}
          />
        }
      >
        <Meter
          max={u.runs.total}
          now={u.runs.used}
          label={formatCopy(copy.usage.meter.runs, {
            month: runsMonth,
            used: f.number(u.runs.used),
            total: f.number(u.runs.total),
            split: splitText(f, u.runs.perWorkspace),
          })}
          segments={u.runs.perWorkspace.map((part, index) => ({
            share: share(part.count, u.runs.total),
            tone: SPLIT_TONES[index] ?? "two",
          }))}
        />
        <Legend>
          <SplitLegend f={f} parts={u.runs.perWorkspace} />
        </Legend>
      </UsageRow>
      <UsageRow
        label={copy.usage.guests.label}
        hint={copy.usage.guests.hint}
        value={
          <Value
            now={f.number(u.guests.used)}
            of={`${f.number(u.guests.total)} ${copy.usage.guests.value}`}
          />
        }
      >
        <Meter
          max={u.guests.total}
          now={u.guests.used}
          label={formatCopy(copy.usage.meter.guests, {
            used: f.number(u.guests.used),
            total: f.number(u.guests.total),
          })}
          segments={[
            { share: share(u.guests.used, u.guests.total), tone: "one" },
          ]}
        />
        <Legend>
          <span className="text-meta-foreground">
            {formatCopy(guestText, {
              count: f.number(u.guests.inWorkspace),
              workspace: u.workspace.name,
            })}
          </span>
        </Legend>
      </UsageRow>
      <UsageRow
        label={copy.usage.integrations.label}
        hint={copy.usage.integrations.hint}
      >
        <div className="text-compact leading-[1.45] font-semibold text-foreground">
          {copy.usage.integrations.unlimited}
          <small className="block text-caption tracking-normal font-normal text-meta-foreground">
            {formatCopy(copy.usage.integrations.inUse, {
              workspace: u.workspace.name,
              count: f.number(u.integrations.inWorkspace),
            })}
            {" · "}
            {links.integrations ? (
              <NavAnchor link={links.integrations} className={LNKB}>
                {copy.usage.integrations.open}
              </NavAnchor>
            ) : (
              <span aria-disabled="true" className={cn(LNKB, "opacity-70")}>
                {copy.usage.integrations.open}
              </span>
            )}
          </small>
        </div>
      </UsageRow>
    </section>
  );
}

function recentLine(
  copy: BillingCopy,
  f: BillingFormatters,
  invoice: BillingInvoiceSummaryDto,
): string {
  const day = shortDay(f, invoice.issuedAt);
  if (invoice.kind === "plan-year") {
    return formatCopy(copy.recent.year, { day, count: invoice.quantity });
  }
  return invoice.status === "overdue" && invoice.dueAt
    ? formatCopy(copy.recent.seatsDue, {
        day,
        count: invoice.quantity,
        due: shortDay(f, invoice.dueAt),
      })
    : formatCopy(copy.recent.seats, { day, count: invoice.quantity });
}

function AdminOverview(
  props: BillingOverviewProps & { data: BillingOverviewDto },
) {
  const { copy, formatters: f, links, data } = props;
  const pastDue = data.pastDue;
  const card = data.payment?.card ?? null;
  const planName = copy.plan.names[data.plan.tier];
  const downgrade = data.plan.pendingDowngrade;
  const names = data.account.workspaces.map((workspace) => workspace.name);
  return (
    <section
      aria-labelledby="bill-t"
      className="flex flex-col gap-4"
      data-f="billing-overview"
    >
      <PageHead
        id="bill-t"
        title={copy.page.title}
        lede={formatCopy(copy.page.lede, {
          account: data.account.name,
          count: names.length,
          names: names.join(", "),
        })}
        aside={
          <Fresh
            copy={copy}
            time={clockTime(f, data.refreshedAt)}
            onReload={props.onReload}
            reloading={props.reloading}
          />
        }
      />
      {pastDue ? (
        <div
          role="status"
          aria-labelledby="pd-t"
          data-f="banner"
          className="flex items-start gap-3 rounded-[12px] border border-warning-text/40 bg-warning-subtle px-4 py-3.5"
        >
          <BillingGlyph
            name="warn"
            className="mt-px size-[18px] text-warning-text"
          />
          <div className="min-w-0 flex-1">
            <b
              id="pd-t"
              className="block text-body leading-[1.45] font-[650] text-foreground"
            >
              {formatCopy(copy.banner.title, { number: pastDue.number })}
            </b>
            <p className="mt-1 mb-0 text-compact leading-[1.5] text-foreground">
              {formatCopy(copy.banner.reason, {
                card: `${card?.brand ?? ""} •••• ${pastDue.cardLast4}`,
                count: pastDue.quantity,
                issued: shortDay(f, pastDue.issuedAt),
                due: shortDay(f, pastDue.dueAt),
              })}
            </p>
            <ul className="mt-1.5 mb-0 list-disc pl-[18px] text-compact leading-[1.55] text-foreground">
              <li>
                <b className="font-semibold">
                  {formatCopy(copy.banner.graceLead, {
                    day: shortDay(f, pastDue.graceUntil),
                  })}
                </b>
                {copy.banner.graceRest}
              </li>
              <li>
                <b className="font-semibold">
                  {formatCopy(copy.banner.restrictedLead, {
                    day: shortDay(f, pastDue.restrictedFrom),
                  })}
                </b>
                {copy.banner.restrictedRest}
              </li>
            </ul>
            <p className="mt-1 mb-0 text-compact leading-[1.5] text-foreground">
              <b className="font-semibold">{copy.banner.safe}</b>{" "}
              {copy.banner.safeDetail}
            </p>
          </div>
          <div className="flex shrink-0 flex-col items-stretch gap-1.5">
            <button
              type="button"
              aria-haspopup="dialog"
              onClick={props.onPayNow}
              aria-disabled={props.paying || undefined}
              className={BILLING_BTN_PRIMARY}
            >
              {props.paying ? copy.banner.paying : copy.banner.payNow}
            </button>
            <button
              type="button"
              aria-haspopup="dialog"
              onClick={props.onUnavailable}
              className={BILLING_BTN}
            >
              {copy.banner.changeCard}
            </button>
            <button
              type="button"
              aria-haspopup="dialog"
              onClick={props.onUnavailable}
              className={BILLING_BTN}
            >
              {copy.banner.transfer}
            </button>
          </div>
        </div>
      ) : null}
      <div className="grid grid-cols-[minmax(0,1fr)_300px] items-start gap-5">
        <div className="flex min-w-0 flex-col gap-3.5">
          <section
            data-f="plan"
            aria-labelledby="plan-t"
            className={cn(CARD, "flex items-start gap-3.5 px-[18px] py-4")}
          >
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <h3
                  id="plan-t"
                  className="m-0 text-sheet-title leading-[1.45] font-[650] tracking-[-0.01em] text-foreground"
                >
                  {planName}
                </h3>
                <Tag tone={pastDue ? "warn" : "ok"}>
                  {pastDue ? copy.plan.pastDue : copy.plan.active}
                </Tag>
                <Tag>{copy.plan.yearly}</Tag>
                {downgrade ? (
                  <Tag tone="acc">
                    {formatCopy(copy.plan.pendingDowngrade, {
                      plan: copy.plan.names[downgrade.to],
                      day: fullDay(downgrade.at),
                    })}
                  </Tag>
                ) : null}
              </div>
              <p className="mt-1.5 mb-0 text-crumb leading-[1.55] text-muted-foreground">
                {formatCopy(copy.plan.period, {
                  from: fullDay(data.plan.periodStart),
                  to: fullDay(data.plan.periodEnd),
                  renew: fullDay(data.plan.autoRenewAt),
                })}
                <br />
                {formatCopy(copy.plan.price, {
                  price: moneyLabel(f, copy.money, data.plan.unitPrice),
                  vat: data.plan.vatPercent,
                })}
              </p>
              {pastDue ? (
                <p
                  id="seat-why"
                  className="mt-1 mb-0 text-crumb leading-[1.55] text-warning-text"
                >
                  {formatCopy(copy.plan.buyBlocked, { number: pastDue.number })}
                </p>
              ) : null}
            </div>
            <div className="flex shrink-0 flex-col items-end gap-1.5">
              <div className="flex gap-2">
                <NavAnchor link={links.compare} className={BILLING_BTN}>
                  {copy.plan.change}
                </NavAnchor>
                <button
                  type="button"
                  aria-haspopup="dialog"
                  disabled={Boolean(pastDue)}
                  aria-describedby={pastDue ? "seat-why" : undefined}
                  onClick={props.onBuy}
                  className={BILLING_BTN_PRIMARY}
                >
                  {pastDue ? null : <BillingGlyph name="plus" />}
                  {copy.plan.buy}
                </button>
              </div>
            </div>
          </section>
          {usageSection(copy, f, data, links)}
          <section data-f="card" aria-labelledby="feat-t" className={CARD}>
            <div className="flex items-center gap-2 border-b border-line px-4 py-3 text-compact leading-[1.45] font-semibold">
              <span id="feat-t">{copy.features.title}</span>
            </div>
            <div className="grid grid-cols-[minmax(0,1.1fr)_minmax(0,1fr)]">
              <div className="flex flex-col gap-2 px-4 py-3.5">
                <h4 className="m-0 text-crumb leading-[1.45] font-semibold text-muted-foreground">
                  {formatCopy(copy.features.included, { plan: planName })}
                </h4>
                <FeatureList copy={copy} keys={data.features.included} />
              </div>
              <div className="flex flex-col gap-2 rounded-br-[12px] border-l border-line bg-background px-4 py-3.5">
                <h4 className="m-0 text-crumb leading-[1.45] font-semibold text-muted-foreground">
                  {copy.features.upgrade}
                </h4>
                <FeatureList copy={copy} keys={data.features.upgrade} plus />
                <div className="mt-auto flex gap-3.5 pt-1">
                  <NavAnchor link={links.compare} className={LNKB}>
                    {copy.features.compare}
                  </NavAnchor>
                  <button
                    type="button"
                    onClick={props.onContactSales}
                    className={LNKB}
                  >
                    {copy.features.advise}
                  </button>
                </div>
              </div>
            </div>
          </section>
        </div>
        <div className="flex min-w-0 flex-col gap-3.5">
          <section data-f="box" aria-labelledby="pay-t" className={BOX}>
            <h3 id="pay-t" className={BOX_TITLE}>
              {copy.payment.title}
            </h3>
            <div className="flex items-center gap-2.5">
              <span className="grid h-[26px] w-9 shrink-0 place-items-center rounded-[5px] border border-line-strong bg-background text-muted-foreground">
                <BillingGlyph name="bank" className="size-[15px]" />
              </span>
              <span>
                <b className="block text-nav leading-[1.45] font-semibold text-foreground">
                  {copy.payment.transfer}
                </b>
                <small className="block text-caption tracking-normal leading-[1.45] text-meta-foreground">
                  {copy.payment.transferHint}
                </small>
              </span>
            </div>
            {card ? (
              <div className="flex items-center gap-2.5">
                <span className="grid h-[26px] w-9 shrink-0 place-items-center rounded-[5px] border border-line-strong bg-background text-mark-sm leading-none font-bold text-muted-foreground">
                  {card.brand}
                </span>
                <span>
                  <b className="block text-nav leading-[1.45] font-semibold text-foreground">
                    {formatCopy(copy.payment.card, {
                      brand: card.brand,
                      last4: card.last4,
                    })}
                  </b>
                  <small className="block text-caption tracking-normal leading-[1.45] text-meta-foreground">
                    {card.status === "declined" && card.declinedAt
                      ? formatCopy(copy.payment.cardDeclined, {
                          day: shortDay(f, card.declinedAt),
                          expires: monthYear(card.expires),
                        })
                      : formatCopy(copy.payment.cardOk, {
                          expires: monthYear(card.expires),
                        })}
                  </small>
                </span>
                <button
                  type="button"
                  onClick={props.onUnavailable}
                  aria-label={formatCopy(copy.payment.changeCardLabel, {
                    brand: card.brand,
                    last4: card.last4,
                  })}
                  className={cn(LNKB, "ml-auto")}
                >
                  {copy.payment.changeCard}
                </button>
              </div>
            ) : null}
            <p className="m-0 text-crumb leading-[1.55] text-meta-foreground">
              {copy.payment.note}
            </p>
            <div className="my-0.5 h-px bg-line" />
            {data.invoiceProfile ? (
              <>
                <div className="text-crumb leading-[1.5] text-foreground">
                  <span className="block text-caption tracking-normal leading-[1.5] text-meta-foreground">
                    {copy.payment.invoiceFor}
                  </span>
                  {data.invoiceProfile.company}
                  <br />
                  {formatCopy(copy.payment.invoiceTax, {
                    taxId: data.invoiceProfile.taxId,
                    address: data.invoiceProfile.address,
                  })}
                </div>
                <div className="text-crumb leading-[1.5] text-foreground">
                  <span className="block text-caption tracking-normal leading-[1.5] text-meta-foreground">
                    {copy.payment.emailLabel}
                  </span>
                  {data.invoiceProfile.email}
                </div>
              </>
            ) : null}
            <button
              type="button"
              onClick={props.onUnavailable}
              className={cn(LNKB, "self-start")}
            >
              {copy.payment.editProfile}
            </button>
          </section>
          <section data-f="box" aria-labelledby="inv-t" className={BOX}>
            <div className="flex items-center gap-2">
              <h3 id="inv-t" className={BOX_TITLE}>
                {copy.recent.title}
              </h3>
              <NavAnchor link={links.invoices} className={cn(LNKB, "ml-auto")}>
                {copy.recent.all}
              </NavAnchor>
            </div>
            <ul className="-mx-3.5 m-0 list-none p-0">
              {(data.recentInvoices ?? []).map((invoice) => (
                <li
                  key={invoice.id}
                  className="flex items-center gap-2.5 border-t border-line px-3.5 py-[9px]"
                >
                  <NavAnchor
                    link={links.invoice(invoice.id)}
                    className="group min-w-0 flex-1"
                  >
                    <b
                      className={cn(
                        MONO,
                        "block text-compact leading-[1.45] font-semibold group-hover:text-brand-ink",
                      )}
                    >
                      {invoice.number}
                    </b>
                    <small className="block text-caption tracking-normal leading-[1.45] text-meta-foreground">
                      {recentLine(copy, f, invoice)}
                    </small>
                  </NavAnchor>
                  <Tag tone={invoice.status === "overdue" ? "warn" : "ok"}>
                    {invoice.status === "overdue"
                      ? copy.recent.overdue
                      : copy.recent.paid}
                  </Tag>
                </li>
              ))}
            </ul>
          </section>
          <section data-f="box" aria-labelledby="mgr-t" className={BOX}>
            <h3 id="mgr-t" className={BOX_TITLE}>
              {copy.managers.title}
            </h3>
            {data.managers.map((manager) => (
              <div key={manager.id} className="flex items-center gap-2.5">
                <PersonMark person={manager} className="size-6 text-mark-xs" />
                <span>
                  <b className="block text-nav leading-[1.45] font-semibold text-foreground">
                    {manager.isViewer
                      ? formatCopy(copy.managers.you, { name: manager.name })
                      : manager.name}
                  </b>
                  <small className="block text-caption tracking-normal leading-[1.45] text-meta-foreground">
                    {manager.role === "owner"
                      ? formatCopy(copy.managers.owner, { note: manager.note })
                      : formatCopy(copy.managers.delegate, {
                          month: monthYear(manager.note),
                        })}
                  </small>
                </span>
              </div>
            ))}
            <p className={BOX_TEXT}>{copy.managers.note}</p>
          </section>
        </div>
      </div>
    </section>
  );
}

function ViewerOverview(
  props: BillingOverviewProps & { data: BillingOverviewDto },
) {
  const { copy, formatters: f, data } = props;
  const u = data.usage;
  const v = copy.viewer;
  const managerNames = data.managers.map((manager) => manager.name).join(", ");
  const row = (label: string, hint: string, value: string, note: string) => (
    <UsageRow label={label} hint={hint}>
      <div className="text-compact leading-[1.45] font-semibold text-foreground">
        {value}
        <small className="block text-caption tracking-normal font-normal text-meta-foreground">
          {note}
        </small>
      </div>
    </UsageRow>
  );
  const half = Math.ceil(data.features.included.length / 2);
  return (
    <section
      aria-labelledby="v-t"
      data-f="billing-viewer"
      className="flex max-w-[820px] flex-col gap-4"
    >
      <PageHead
        id="v-t"
        title={copy.page.title}
        lede={formatCopy(v.lede, {
          workspace: u.workspace.name,
          account: data.account.name,
        })}
        aside={
          <Fresh
            copy={copy}
            time={clockTime(f, data.refreshedAt)}
            onReload={props.onReload}
            reloading={props.reloading}
          />
        }
      />
      <InfoNote>
        <Rich text={formatCopy(v.notice, { names: managerNames })} />
      </InfoNote>
      <section
        data-f="plan"
        aria-labelledby="vp-t"
        className={cn(CARD, "flex items-start gap-3.5 px-[18px] py-4")}
      >
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <h3
              id="vp-t"
              className="m-0 text-sheet-title leading-[1.45] font-[650] tracking-[-0.01em] text-foreground"
            >
              {copy.plan.names[data.plan.tier]}
            </h3>
            <Tag tone="ok">{copy.plan.active}</Tag>
          </div>
          <p className="mt-1.5 mb-0 text-crumb leading-[1.55] text-muted-foreground">
            {formatCopy(v.period, { day: fullDay(data.plan.periodEnd) })}
          </p>
        </div>
      </section>
      <section data-f="card" aria-labelledby="vu-t" className={CARD}>
        <div className="flex items-center gap-2 border-b border-line px-4 py-3 text-compact leading-[1.45] font-semibold">
          <span id="vu-t">
            {formatCopy(v.usageTitle, { workspace: u.workspace.name })}
          </span>
          <span className="ml-auto text-caption tracking-normal font-normal text-meta-foreground">
            {formatCopy(copy.usage.asOf, { time: clockTime(f, data.asOf) })}
          </span>
        </div>
        {row(
          v.members.label,
          v.members.hint,
          formatCopy(v.members.value, { count: f.number(u.workspace.members) }),
          v.members.note,
        )}
        {row(
          v.runs.label,
          formatCopy(v.runs.hint, { month: monthNumber(u.runs.month) }),
          formatCopy(v.runs.value, { count: f.number(u.workspace.runs) }),
          formatCopy(v.runs.note, { limit: f.number(u.runs.total) }),
        )}
        <UsageRow
          label={v.guests.label}
          hint={v.guests.hint}
          value={
            <Value
              now={f.number(u.guests.used)}
              of={`${f.number(u.guests.total)} ${copy.usage.guests.value}`}
            />
          }
        >
          <Meter
            max={u.guests.total}
            now={u.guests.used}
            label={formatCopy(copy.usage.meter.guests, {
              used: f.number(u.guests.used),
              total: f.number(u.guests.total),
            })}
            segments={[
              {
                share: Math.round((u.guests.used / u.guests.total) * 1000) / 10,
                tone: "one",
              },
            ]}
          />
        </UsageRow>
        {row(
          v.integrations.label,
          v.integrations.hint,
          copy.usage.integrations.unlimited,
          formatCopy(v.integrations.note, {
            count: f.number(u.integrations.inWorkspace),
          }),
        )}
      </section>
      <section data-f="card" aria-labelledby="vf-t" className={CARD}>
        <div className="flex items-center gap-2 border-b border-line px-4 py-3 text-compact leading-[1.45] font-semibold">
          <span id="vf-t">{v.inPlan}</span>
        </div>
        <div className="grid grid-cols-2">
          <div className="px-4 py-3.5">
            <FeatureList
              copy={copy}
              keys={data.features.included.slice(0, half)}
            />
          </div>
          <div className="px-4 py-3.5">
            <ul className="m-0 flex list-none flex-col gap-1.5 p-0 text-compact leading-[1.45]">
              {data.features.included.slice(half).map((key) => (
                <li key={key} className="flex items-start gap-2">
                  <BillingGlyph
                    name="check"
                    className="mt-0.5 size-3.5 text-success"
                  />
                  {copy.features.items[key]}
                </li>
              ))}
              <li className="flex items-start gap-2">
                <BillingGlyph
                  name="check"
                  className="mt-0.5 size-3.5 text-success"
                />
                {formatCopy(v.runsFeature, { limit: f.number(u.runs.total) })}
              </li>
            </ul>
          </div>
        </div>
      </section>
    </section>
  );
}

/** Plans and billing, overview (Final-Billing `tong-quan`, `qua-han`, `chi-xem`). */
export function BillingOverviewSurface(props: BillingOverviewProps) {
  const { copy, state } = props;
  if (state.status === "loading") {
    return (
      <p role="status" className="m-0 text-compact text-meta-foreground">
        {copy.page.loading}
      </p>
    );
  }
  if (state.status === "error") {
    return (
      <div role="alert" className={cn(BOX, "max-w-[520px]")}>
        <p className={BOX_TEXT}>{copy.page.error}</p>
        <button
          type="button"
          onClick={props.onReload}
          className={cn(BILLING_BTN, "self-start")}
        >
          {copy.page.retry}
        </button>
      </div>
    );
  }
  return state.data.viewer.canManage ? (
    <AdminOverview {...props} data={state.data} />
  ) : (
    <ViewerOverview {...props} data={state.data} />
  );
}
