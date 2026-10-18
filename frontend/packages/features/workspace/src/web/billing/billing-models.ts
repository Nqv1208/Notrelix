import type { MarkToneId } from "../../core/model/navigation";
import type { ReportFormatters } from "../reports/reports-format";
import { dayLabel, timeLabel } from "../reports/reports-format";

/** What the Billing surfaces read (gap `CTR-GAP-BILLING`): money is whole VND, days are `YYYY-MM-DD`. */
export type BillingPlanTier = "free" | "pro" | "business" | "enterprise";
export type BillingInvoiceStatus = "paid" | "overdue";
export type BillingInvoiceKind = "plan-year" | "seats-added";

export interface BillingManagerDto {
  readonly id: string;
  readonly name: string;
  readonly tone: MarkToneId;
  readonly role: "owner" | "delegate";
  /** Owner: the job title; delegate: the `YYYY-MM` they were assigned. */
  readonly note: string;
  readonly isViewer: boolean;
}

export interface BillingInvoiceSummaryDto {
  readonly id: string;
  readonly number: string;
  readonly issuedAt: string;
  readonly dueAt: string | null;
  readonly status: BillingInvoiceStatus;
  readonly kind: BillingInvoiceKind;
  readonly quantity: number;
  readonly from: string;
  readonly to: string;
  readonly days: number;
}

export interface BillingSplitDto {
  readonly id: string;
  readonly name: string;
  readonly count: number;
}

export interface BillingOverviewDto {
  readonly serverTime: string;
  /** When these numbers were read (the "Cập nhật" time); `asOf` is the usage data's own time. */
  readonly refreshedAt: string;
  readonly asOf: string;
  readonly viewer: { readonly canManage: boolean };
  readonly account: {
    readonly name: string;
    readonly workspaces: readonly { id: string; name: string }[];
  };
  readonly plan: {
    readonly tier: BillingPlanTier;
    readonly status: "active" | "past_due";
    readonly cadence: "yearly";
    readonly periodStart: string;
    readonly periodEnd: string;
    readonly autoRenewAt: string;
    readonly unitPrice: number;
    readonly vatPercent: number;
    readonly pendingDowngrade: { to: BillingPlanTier; at: string } | null;
  };
  readonly usage: {
    readonly seats: {
      used: number;
      total: number;
      perWorkspace: readonly BillingSplitDto[];
    };
    readonly storage: { usedGb: number; totalGb: number };
    readonly runs: {
      used: number;
      total: number;
      month: string;
      perWorkspace: readonly BillingSplitDto[];
    };
    readonly guests: { used: number; total: number; inWorkspace: number };
    readonly integrations: { inWorkspace: number; limit: number | null };
    readonly workspace: {
      id: string;
      name: string;
      members: number;
      runs: number;
    };
  };
  readonly features: {
    readonly included: readonly BillingFeatureKey[];
    readonly upgrade: readonly BillingFeatureKey[];
  };
  readonly payment: {
    readonly transfer: boolean;
    readonly card: {
      brand: string;
      last4: string;
      expires: string;
      status: "ok" | "declined";
      declinedAt: string | null;
    };
  } | null;
  readonly invoiceProfile: BillingInvoiceProfileDto | null;
  readonly recentInvoices: readonly BillingInvoiceSummaryDto[] | null;
  readonly managers: readonly BillingManagerDto[];
  readonly pastDue: {
    readonly invoiceId: string;
    readonly number: string;
    readonly issuedAt: string;
    readonly dueAt: string;
    readonly quantity: number;
    readonly cardLast4: string;
    readonly graceUntil: string;
    readonly restrictedFrom: string;
  } | null;
  readonly pendingPurchase: BillingPurchaseDto | null;
}

export type BillingFeatureKey =
  | "advancedReports"
  | "webhooks"
  | "audit1y"
  | "sso"
  | "unlimitedIntegrations"
  | "powerBi"
  | "scim"
  | "audit7y";

export interface BillingInvoiceProfileDto {
  readonly company: string;
  readonly taxId: string;
  readonly address: string;
  readonly email: string;
}

export interface BillingPurchaseDto {
  readonly id: string;
  readonly quantity: number;
  readonly status?: "pending" | "succeeded";
  readonly submittedAt: string;
  readonly lastCheckedAt: string;
}

export interface BillingSeatQuoteDto {
  readonly quantity: number;
  readonly unitPrice: number;
  readonly from: string;
  readonly to: string;
  readonly days: number;
  readonly yearDays: number;
  readonly amount: number;
  readonly vat: number;
  readonly vatPercent: number;
  readonly total: number;
  readonly seatsAfter: number;
  readonly seatsUsed: number;
  readonly card: { brand: string; last4: string; expires: string };
  readonly email: string;
}

export interface BillingPlanRowDto {
  readonly tier: BillingPlanTier;
  readonly monthlyPrice: number | null;
  readonly people:
    { kind: "max"; value: number } | { kind: "seats" } | { kind: "contract" };
  readonly storageGb: number | null;
  readonly runs: number | null;
  readonly advancedReports: boolean;
  readonly webhooks: boolean;
  readonly auditDays: number | null;
  readonly sso: "none" | "sso" | "sso-scim";
  readonly powerBi: boolean;
}

export interface BillingPlansDto {
  readonly current: BillingPlanTier;
  readonly seatsOwned: number;
  readonly seatsUsed: number;
  readonly autoRenewAt: string;
  readonly periodEnd: string;
  readonly vatPercent: number;
  readonly workspaceCount: number;
  readonly pendingDowngrade: BillingPlanTier | null;
  readonly plans: readonly BillingPlanRowDto[];
}

export interface BillingInvoicesDto {
  readonly email: string;
  readonly invoices: readonly BillingInvoiceSummaryDto[];
}

export interface BillingInvoiceDetailDto extends BillingInvoiceSummaryDto {
  readonly paidAt: string | null;
  readonly method: { brand: string; last4: string } | null;
  readonly buyer: BillingInvoiceProfileDto;
  readonly line: {
    readonly kind: BillingInvoiceKind;
    readonly quantity: number;
    readonly unitPrice: number;
    readonly from: string;
    readonly to: string;
    readonly days: number;
    readonly yearDays: number;
    readonly months: number;
    readonly amount: number;
  };
  readonly subtotal: number;
  readonly vat: number;
  readonly vatPercent: number;
  readonly total: number;
  readonly emailedTo: string;
  readonly emailedAt: string;
}

export interface BillingDowngradeDto {
  readonly from: BillingPlanTier;
  readonly to: BillingPlanTier;
  readonly effectiveAt: string;
  readonly periodEnd: string;
  readonly newUnitPrice: number;
  readonly asOf: string;
  readonly workspaces: readonly string[];
  readonly pending: boolean;
  readonly runs: { from: number; to: number; used: number };
  readonly reports: { names: readonly string[] };
  readonly webhooks: { names: readonly string[] };
  readonly storage: {
    fromGb: number;
    toGb: number;
    usedGb: number;
    overGb: number;
  };
  readonly kept: { seatsOwned: number };
}

export interface BillingSeatSummaryDto {
  readonly used: number;
  readonly total: number;
  readonly sharedWith: readonly string[];
  readonly canBuy: boolean;
}

/** Where the Billing pages live; every href is real and every `onNavigate` is the in-app move. */
export interface BillingLink {
  readonly href: string;
  readonly onNavigate: () => void;
}
export interface BillingLinks {
  readonly overview: BillingLink;
  readonly compare: BillingLink;
  readonly invoices: BillingLink;
  readonly invoice: (invoiceId: string) => BillingLink;
  readonly downgrade: BillingLink;
  /** The overview with the buy-seats dialog open on `quantity` seats. */
  readonly buy: (quantity: number) => BillingLink;
  /** Integrations has no page yet: `null` draws the link inert. */
  readonly integrations: BillingLink | null;
}

export type BillingFormatters = ReportFormatters;
export const BILLING_TIME_ZONE = "Asia/Ho_Chi_Minh";

/** "05/09/2026" of a `YYYY-MM-DD` day. */
export const fullDay = (day: string): string =>
  `${day.slice(8, 10)}/${day.slice(5, 7)}/${day.slice(0, 4)}`;

/** "05/09" of a `YYYY-MM-DD` day. */
export const shortDay = (f: BillingFormatters, day: string): string =>
  dayLabel(f, day);

/** "10:38" of a moment, in Hanoi time. */
export const clockTime = (f: BillingFormatters, moment: string): string =>
  timeLabel(f, moment, BILLING_TIME_ZONE);

/** "08/2028" of a `YYYY-MM` month. */
export const monthYear = (month: string): string =>
  `${month.slice(5, 7)}/${month.slice(0, 4)}`;

/** "10" of a `YYYY-MM` month (the calendar month number). */
export const monthNumber = (month: string): string =>
  String(Number(month.slice(5, 7)));

/** Fills the locale's money template with a grouped amount. */
export function moneyLabel(
  f: BillingFormatters,
  template: string,
  amount: number,
): string {
  return template.replace("{{amount}}", f.number(amount));
}

/** A size in GB as "312 GB", or "1 TB" from 1000 GB up. */
export function sizeLabel(f: BillingFormatters, gb: number): string {
  return gb >= 1000 && gb % 1000 === 0
    ? `${f.number(gb / 1000)} TB`
    : `${f.number(gb)} GB`;
}
