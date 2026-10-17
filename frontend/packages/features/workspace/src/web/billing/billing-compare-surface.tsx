import * as React from "react";
import { cn } from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
import type { SectionState } from "../home/home-models";
import {
  BILLING_BTN,
  BILLING_BTN_SM,
  BILLING_BTN_SM_PRIMARY,
  BackLink,
  BillingGlyph,
  BOX,
  BOX_TEXT,
  NavAnchor,
  PageHead,
  Tag,
} from "./billing-parts";
import {
  fullDay,
  moneyLabel,
  sizeLabel,
  type BillingFormatters,
  type BillingLinks,
  type BillingPlanRowDto,
  type BillingPlansDto,
} from "./billing-models";
import type { BillingCopy } from "./billing.copy";

export interface BillingCompareProps {
  readonly copy: BillingCopy;
  readonly formatters: BillingFormatters;
  readonly links: BillingLinks;
  readonly accountName: string;
  readonly state: SectionState<BillingPlansDto>;
  readonly onContactSales: () => void;
  readonly onRetry: () => void;
}

const CELL = "border-t border-line px-3.5 py-2.5 text-left align-top";
const CUR = "bg-state-selected";
const SMALL =
  "block text-caption tracking-normal font-normal leading-[1.45] text-meta-foreground";

function Yes({ label }: { label: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 font-medium">
      <BillingGlyph name="check" className="size-3.5 text-success" />
      {label}
    </span>
  );
}

/** Compare plans (Final-Billing `so-sanh-goi`). */
export function BillingCompareSurface(props: BillingCompareProps) {
  const { copy, formatters: f, links, state } = props;
  const c = copy.compare;
  if (state.status !== "ready") {
    return (
      <div className="flex flex-col gap-4">
        <BackLink
          href={links.overview.href}
          onNavigate={links.overview.onNavigate}
        >
          {c.back}
        </BackLink>
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
  const data = state.data;
  const plans = data.plans;
  const money = (amount: number) => moneyLabel(f, copy.money, amount);
  const isCur = (row: BillingPlanRowDto) => row.tier === data.current;
  const cell = (
    row: BillingPlanRowDto,
    content: React.ReactNode,
    no = false,
  ) => (
    <td
      key={row.tier}
      className={cn(CELL, isCur(row) && CUR, no && "text-meta-foreground")}
    >
      {content}
    </td>
  );
  const flag = (row: BillingPlanRowDto, on: boolean) =>
    cell(row, on ? <Yes label={c.yes} /> : c.none, !on);
  const auditOf = (days: number | null): string =>
    days === null
      ? c.none
      : days >= 365 * 2
        ? formatCopy(c.years, { count: Math.round(days / 365) })
        : days === 365
          ? c.year
          : formatCopy(c.days, { count: days });
  const names = copy.plan.names;
  return (
    <div className="flex flex-col gap-4" data-f="billing-compare">
      <BackLink
        href={links.overview.href}
        onNavigate={links.overview.onNavigate}
      >
        {c.back}
      </BackLink>
      <PageHead
        id="cmp-t"
        title={c.title}
        lede={formatCopy(c.lede, {
          vat: data.vatPercent,
          account: props.accountName,
          count: data.workspaceCount,
        })}
      />
      <table
        aria-labelledby="cmp-t"
        data-f="cmp"
        className="w-full table-fixed border-separate border-spacing-0 rounded-[12px] border border-line bg-popover text-compact leading-[1.45] dark:bg-card"
      >
        <colgroup>
          <col style={{ width: 196 }} />
          {plans.map((row) => (
            <col key={row.tier} />
          ))}
        </colgroup>
        <thead>
          <tr>
            <th scope="col" className="px-3.5 pt-3.5 pb-3 text-left">
              <span className="sr-only">{c.criteria}</span>
            </th>
            {plans.map((row) => (
              <th
                key={row.tier}
                scope="col"
                className={cn(
                  "px-3.5 pt-3.5 pb-3 text-left text-item-heading leading-[1.45] font-[650]",
                  isCur(row) && CUR,
                )}
              >
                {names[row.tier]}
                {isCur(row) ? (
                  <Tag tone="acc" className="ml-1.5 align-[1px]">
                    {c.current}
                  </Tag>
                ) : null}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          <tr>
            <th
              scope="row"
              className={cn(CELL, "font-medium text-muted-foreground")}
            >
              {c.rows.price}
            </th>
            {plans.map((row) =>
              cell(
                row,
                row.monthlyPrice === null ? (
                  <>
                    <b className="font-[650]">{c.contact}</b>
                    <small className={SMALL}>{c.byContract}</small>
                  </>
                ) : row.monthlyPrice === 0 ? (
                  <b className="font-[650]">{c.free}</b>
                ) : (
                  <>
                    <b className="font-[650]">{money(row.monthlyPrice)}</b>
                    <small className={SMALL}>{c.perPersonMonth}</small>
                  </>
                ),
              ),
            )}
          </tr>
          <tr>
            <th
              scope="row"
              className={cn(CELL, "font-medium text-muted-foreground")}
            >
              {c.rows.people}
            </th>
            {plans.map((row) =>
              cell(
                row,
                row.people.kind === "max" ? (
                  formatCopy(c.maxPeople, { count: row.people.value })
                ) : row.people.kind === "contract" ? (
                  c.contractPeople
                ) : (
                  <>
                    {c.bySeats}
                    {isCur(row) ? (
                      <small className={SMALL}>
                        {formatCopy(c.haveSeats, {
                          count: f.number(data.seatsOwned),
                        })}
                      </small>
                    ) : null}
                  </>
                ),
              ),
            )}
          </tr>
          <tr>
            <th
              scope="row"
              className={cn(CELL, "font-medium text-muted-foreground")}
            >
              {c.rows.storage}
            </th>
            {plans.map((row) =>
              cell(
                row,
                row.storageGb === null ? c.custom : sizeLabel(f, row.storageGb),
              ),
            )}
          </tr>
          <tr>
            <th
              scope="row"
              className={cn(CELL, "font-medium text-muted-foreground")}
            >
              {c.rows.runs}
            </th>
            {plans.map((row) =>
              cell(row, row.runs === null ? c.unlimited : f.number(row.runs)),
            )}
          </tr>
          <tr>
            <th
              scope="row"
              className={cn(CELL, "font-medium text-muted-foreground")}
            >
              {c.rows.reports}
            </th>
            {plans.map((row) => flag(row, row.advancedReports))}
          </tr>
          <tr>
            <th
              scope="row"
              className={cn(CELL, "font-medium text-muted-foreground")}
            >
              {c.rows.webhooks}
            </th>
            {plans.map((row) => flag(row, row.webhooks))}
          </tr>
          <tr>
            <th
              scope="row"
              className={cn(CELL, "font-medium text-muted-foreground")}
            >
              {c.rows.audit}
            </th>
            {plans.map((row) =>
              cell(row, auditOf(row.auditDays), row.auditDays === null),
            )}
          </tr>
          <tr>
            <th
              scope="row"
              className={cn(CELL, "font-medium text-muted-foreground")}
            >
              {c.rows.sso}
            </th>
            {plans.map((row) =>
              cell(
                row,
                row.sso === "none"
                  ? c.none
                  : row.sso === "sso"
                    ? c.sso
                    : c.ssoScim,
                row.sso === "none",
              ),
            )}
          </tr>
          <tr>
            <th
              scope="row"
              className={cn(CELL, "font-medium text-muted-foreground")}
            >
              {c.rows.powerBi}
            </th>
            {plans.map((row) => flag(row, row.powerBi))}
          </tr>
        </tbody>
        <tfoot>
          <tr>
            <th scope="row" className={cn(CELL, "pt-3 pb-3.5")}>
              <span className="sr-only">{c.actions}</span>
            </th>
            {plans.map((row) => {
              const base = cn(CELL, "pt-3 pb-3.5", isCur(row) && CUR);
              if (isCur(row)) {
                return (
                  <td key={row.tier} className={base}>
                    <Tag tone="acc">{c.current}</Tag>
                    <small className={cn(SMALL, "mt-1.5")}>
                      {formatCopy(c.renews, { day: fullDay(data.autoRenewAt) })}
                    </small>
                  </td>
                );
              }
              if (row.tier === "enterprise") {
                return (
                  <td key={row.tier} className={base}>
                    <button
                      type="button"
                      onClick={props.onContactSales}
                      className={BILLING_BTN_SM_PRIMARY}
                    >
                      {c.contactSales}
                    </button>
                  </td>
                );
              }
              if (
                row.people.kind === "max" &&
                data.seatsUsed > row.people.value
              ) {
                return (
                  <td key={row.tier} className={base}>
                    <button
                      type="button"
                      disabled
                      aria-describedby="free-why"
                      className={BILLING_BTN_SM}
                    >
                      {c.cannotChoose}
                    </button>
                    <small id="free-why" className={cn(SMALL, "mt-1.5")}>
                      {formatCopy(c.cannotChooseWhy, { used: data.seatsUsed })}
                    </small>
                  </td>
                );
              }
              return (
                <td key={row.tier} className={base}>
                  <NavAnchor link={links.downgrade} className={BILLING_BTN_SM}>
                    {c.seeDowngrade}
                  </NavAnchor>
                </td>
              );
            })}
          </tr>
        </tfoot>
      </table>
      <p className="m-0 text-caption tracking-normal leading-[1.5] text-meta-foreground">
        {formatCopy(c.note, { day: fullDay(data.periodEnd) })}
      </p>
    </div>
  );
}
