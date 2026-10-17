import * as React from "react";
import { cn } from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
import type { SectionState } from "../home/home-models";
import {
  BILLING_BTN,
  BackLink,
  BillingGlyph,
  BOX,
  BOX_TEXT,
  CARD,
  InfoNote,
  MONO,
  NavAnchor,
  PageHead,
  Tag,
} from "./billing-parts";
import { CalcSub } from "./billing-dialogs";
import {
  fullDay,
  moneyLabel,
  shortDay,
  type BillingFormatters,
  type BillingInvoiceDetailDto,
  type BillingInvoiceSummaryDto,
  type BillingInvoicesDto,
  type BillingLinks,
} from "./billing-models";
import type { BillingCopy } from "./billing.copy";

export interface BillingInvoicesProps {
  readonly copy: BillingCopy;
  readonly formatters: BillingFormatters;
  readonly links: BillingLinks;
  readonly accountName: string;
  readonly state: SectionState<BillingInvoicesDto>;
  readonly selectedId: string | null;
  readonly detail: SectionState<BillingInvoiceDetailDto> | null;
  readonly downloading: boolean;
  readonly pdfFailed: boolean;
  readonly onDownload: (invoiceId: string) => void;
  readonly onRetry: () => void;
}

const SMALL =
  "block text-caption tracking-normal leading-[1.45] text-meta-foreground";

function rangeOf(f: BillingFormatters, from: string, to: string): string {
  return `${shortDay(f, from)} – ${fullDay(to)}`;
}

function contentOf(copy: BillingCopy, invoice: BillingInvoiceSummaryDto) {
  return invoice.kind === "plan-year"
    ? formatCopy(copy.invoices.planYear, { count: invoice.quantity })
    : formatCopy(copy.invoices.seatsAdded, { count: invoice.quantity });
}

function InvoiceDetail({
  copy,
  formatters: f,
  detail,
  downloading,
  pdfFailed,
  onDownload,
}: {
  copy: BillingCopy;
  formatters: BillingFormatters;
  detail: BillingInvoiceDetailDto;
  downloading: boolean;
  pdfFailed: boolean;
  onDownload: (invoiceId: string) => void;
}) {
  const d = copy.invoices.detail;
  const money = (amount: number) => moneyLabel(f, copy.money, amount);
  const line = detail.line;
  const seats = line.kind === "seats-added";
  const row = (label: React.ReactNode, value: string, total = false) => (
    <div
      role="row"
      className={cn(
        "flex items-baseline gap-3 border-t border-line px-3 py-[9px] text-compact leading-[1.45] first:border-t-0",
        total && "bg-background text-body leading-[1.45] font-[650]",
      )}
    >
      <span role="cell">{label}</span>
      <span
        role="cell"
        className="ml-auto whitespace-nowrap [font-variant-numeric:tabular-nums]"
      >
        {value}
      </span>
    </div>
  );
  return (
    <section
      data-f="invd"
      aria-labelledby="invd-t"
      className={cn(CARD, "flex flex-col gap-3 p-4")}
    >
      <div className="flex items-center gap-2">
        <h3
          id="invd-t"
          className={cn(
            MONO,
            "m-0 text-prose leading-[1.45] font-[650] text-foreground",
          )}
        >
          {detail.number}
        </h3>
        <Tag tone={detail.status === "overdue" ? "warn" : "ok"}>
          {detail.status === "overdue"
            ? copy.invoices.overdue
            : copy.invoices.paid}
        </Tag>
      </div>
      <div className="text-crumb leading-[1.5] text-foreground">
        <span className="block text-caption tracking-normal leading-[1.5] text-meta-foreground">
          {detail.paidAt && detail.method
            ? formatCopy(d.issuedPaid, {
                issued: fullDay(detail.issuedAt),
                paid: fullDay(detail.paidAt),
                method: `${detail.method.brand} •••• ${detail.method.last4}`,
              })
            : formatCopy(d.issuedDue, {
                issued: fullDay(detail.issuedAt),
                due: detail.dueAt ? fullDay(detail.dueAt) : "",
              })}
        </span>
      </div>
      <div className="text-crumb leading-[1.5] text-foreground">
        <span className="block text-caption tracking-normal leading-[1.5] text-meta-foreground">
          {d.buyer}
        </span>
        {formatCopy(d.buyerLine, {
          company: detail.buyer.company,
          taxId: detail.buyer.taxId,
        })}
        <br />
        {detail.buyer.address}
      </div>
      <div
        role="table"
        aria-label={formatCopy(d.calcLabel, { number: detail.number })}
        className="overflow-hidden rounded-[10px] border border-line"
      >
        {row(
          <>
            {formatCopy(seats ? d.seatsLine : d.planLine, {
              count: f.number(line.quantity),
            })}
            <CalcSub>
              {formatCopy(d.range, {
                from: shortDay(f, line.from),
                to: fullDay(line.to),
                days: line.days,
              })}
              <br />
              {formatCopy(seats ? d.formula : d.formulaYear, {
                count: f.number(line.quantity),
                price: money(line.unitPrice),
                days: line.days,
                year: line.yearDays,
              })}
            </CalcSub>
          </>,
          money(line.amount),
        )}
        {row(d.subtotal, money(detail.subtotal))}
        {row(
          formatCopy(d.vat, { percent: detail.vatPercent }),
          money(detail.vat),
        )}
        {row(d.total, money(detail.total), true)}
      </div>
      <div className="flex items-center gap-2.5">
        <span className="grid h-[26px] w-9 shrink-0 place-items-center rounded-[5px] border border-line-strong bg-background text-muted-foreground">
          <BillingGlyph name="mail" className="size-[15px]" />
        </span>
        <span>
          <b className="block text-nav leading-[1.45] font-semibold text-foreground">
            {d.sent}
          </b>
          <small className={SMALL}>
            {formatCopy(d.sentTo, {
              email: detail.emailedTo,
              day: fullDay(detail.emailedAt),
            })}
          </small>
        </span>
      </div>
      <div className="flex items-center gap-2">
        <button
          type="button"
          onClick={() => onDownload(detail.id)}
          aria-disabled={downloading || undefined}
          className={BILLING_BTN}
        >
          <BillingGlyph name="download" />
          {d.pdf}
        </button>
        {pdfFailed ? (
          <span role="alert" className="text-crumb text-danger-text">
            {d.pdfFailed}
          </span>
        ) : null}
      </div>
      <InfoNote>{d.locked}</InfoNote>
    </section>
  );
}

/** Invoices (Final-Billing `hoa-don`). */
export function BillingInvoicesSurface(props: BillingInvoicesProps) {
  const { copy, formatters: f, links, state } = props;
  const c = copy.invoices;
  const back = (
    <BackLink href={links.overview.href} onNavigate={links.overview.onNavigate}>
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
  const { invoices, email } = state.data;
  const selectedId = props.selectedId ?? invoices[0]?.id ?? null;
  return (
    <div className="flex flex-col gap-4" data-f="billing-invoices">
      {back}
      <PageHead
        id="invs-t"
        title={c.title}
        lede={formatCopy(c.lede, { account: props.accountName, email })}
      />
      <div className="grid grid-cols-[minmax(0,1fr)_392px] items-start gap-5">
        <div
          role="table"
          aria-labelledby="invs-t"
          data-f="list"
          className={cn(CARD)}
        >
          <div
            role="row"
            className="grid h-9 grid-cols-[150px_minmax(0,1fr)_112px] items-center gap-3 rounded-t-[12px] border-b border-line bg-background px-3.5 text-micro leading-[1.45] font-semibold text-meta-foreground"
          >
            <span role="columnheader">{c.columns.number}</span>
            <span role="columnheader">{c.columns.content}</span>
            <span role="columnheader">{c.columns.status}</span>
          </div>
          {invoices.length === 0 ? (
            <p className="m-0 px-3.5 py-4 text-compact text-meta-foreground">
              {c.empty}
            </p>
          ) : null}
          {invoices.map((invoice) => {
            const selected = invoice.id === selectedId;
            return (
              <div
                key={invoice.id}
                role="row"
                aria-selected={selected}
                className={cn(
                  "box-border grid min-h-14 grid-cols-[150px_minmax(0,1fr)_112px] items-center gap-3 border-t border-line px-3.5 py-2 text-compact leading-[1.45] first-of-type:border-t-0",
                  selected && "bg-state-selected",
                )}
              >
                <span role="cell">
                  <NavAnchor
                    link={links.invoice(invoice.id)}
                    aria-current={selected ? "true" : undefined}
                    className={MONO}
                  >
                    <b className="font-semibold">{invoice.number}</b>
                  </NavAnchor>
                  <small className={SMALL}>{fullDay(invoice.issuedAt)}</small>
                </span>
                <span role="cell">
                  {contentOf(copy, invoice)}
                  <small className={SMALL}>
                    {invoice.kind === "plan-year"
                      ? rangeOf(f, invoice.from, invoice.to)
                      : formatCopy(c.daysBilled, {
                          range: rangeOf(f, invoice.from, invoice.to),
                        })}
                  </small>
                </span>
                <span role="cell">
                  <Tag tone={invoice.status === "overdue" ? "warn" : "ok"}>
                    {invoice.status === "overdue" ? c.overdue : c.paid}
                  </Tag>
                </span>
              </div>
            );
          })}
        </div>
        {props.detail?.status === "ready" ? (
          <InvoiceDetail
            copy={copy}
            formatters={f}
            detail={props.detail.data}
            downloading={props.downloading}
            pdfFailed={props.pdfFailed}
            onDownload={props.onDownload}
          />
        ) : props.detail?.status === "error" ? (
          <p role="alert" className="m-0 text-compact text-danger-text">
            {c.detail.missing}
          </p>
        ) : null}
      </div>
    </div>
  );
}
