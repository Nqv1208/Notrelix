import { createFormatters } from "@notrelix/i18n";
import type {
  BillingDowngradeDto,
  BillingInvoiceDetailDto,
  BillingInvoiceSummaryDto,
  BillingInvoicesDto,
  BillingLinks,
  BillingOverviewDto,
  BillingPlansDto,
  BillingSeatQuoteDto,
  BillingSeatSummaryDto,
} from "../web/billing/billing-models";
import { billingCopy } from "../web/billing/billing.copy";

/**
 * Mockup world of the Plans and billing pages (Final-Billing), for stories
 * and tests only: the Tập đoàn Minh An account on Monday 12/10/2026 10:40
 * (GMT+7), 104 of 110 seats in use. Deterministic: no clock, no random.
 */
const noop = () => undefined;
export const BILLING_FIXTURE_COPY = billingCopy("vi");
export const BILLING_FIXTURE_FORMATTERS = createFormatters("vi");

const link = { href: "#", onNavigate: noop };
export const BILLING_FIXTURE_LINKS: BillingLinks = {
  overview: link,
  compare: link,
  invoices: link,
  invoice: () => link,
  downgrade: link,
  buy: () => link,
  integrations: null,
};

const invoice = (
  number: string,
  issuedAt: string,
  quantity: number,
  extra: Partial<BillingInvoiceSummaryDto> = {},
): BillingInvoiceSummaryDto => ({
  id: `inv-${number}`,
  number,
  issuedAt,
  dueAt: null,
  status: "paid",
  kind: "seats-added",
  quantity,
  from: issuedAt,
  to: "2026-12-31",
  days: 118,
  ...extra,
});

const PAID = [
  invoice("HĐ-2026-0112", "2026-09-05", 4),
  invoice("HĐ-2026-0047", "2026-06-05", 6, { days: 210 }),
  invoice("HĐ-2026-0001", "2026-01-01", 100, {
    kind: "plan-year",
    from: "2026-01-01",
    days: 365,
  }),
];
const OVERDUE = invoice("HĐ-2026-0131", "2026-09-28", 4, {
  dueAt: "2026-10-05",
  status: "overdue",
  days: 95,
});

export function overviewFixture(
  options: { pastDue?: boolean; viewer?: boolean; pending?: boolean } = {},
): BillingOverviewDto {
  const manage = !options.viewer;
  return {
    serverTime: "2026-10-12T03:40:00.000Z",
    refreshedAt: "2026-10-12T03:38:00.000Z",
    asOf: "2026-10-12T03:37:00.000Z",
    viewer: { canManage: manage },
    account: {
      name: "Tập đoàn Minh An",
      workspaces: [
        { id: "ws-1", name: "Minh An Group" },
        { id: "ws-2", name: "Minh An Đà Nẵng" },
      ],
    },
    plan: {
      tier: "business",
      status: options.pastDue ? "past_due" : "active",
      cadence: "yearly",
      periodStart: "2026-01-01",
      periodEnd: "2026-12-31",
      autoRenewAt: "2027-01-01",
      unitPrice: 159_000,
      vatPercent: 8,
      pendingDowngrade: null,
    },
    usage: {
      seats: {
        used: 104,
        total: 110,
        perWorkspace: [
          { id: "ws-1", name: "Minh An Group", count: 86 },
          { id: "ws-2", name: "Minh An Đà Nẵng", count: 18 },
        ],
      },
      storage: { usedGb: 312, totalGb: 1000 },
      runs: {
        used: 4210,
        total: 25_000,
        month: "2026-10",
        perWorkspace: [
          { id: "ws-1", name: "Minh An Group", count: 3880 },
          { id: "ws-2", name: "Minh An Đà Nẵng", count: 330 },
        ],
      },
      guests: { used: 2, total: 50, inWorkspace: 2 },
      integrations: { inWorkspace: 4, limit: null },
      workspace: { id: "ws-1", name: "Minh An Group", members: 86, runs: 3880 },
    },
    features: {
      included: [
        "advancedReports",
        "webhooks",
        "audit1y",
        "sso",
        "unlimitedIntegrations",
      ],
      upgrade: ["powerBi", "scim", "audit7y"],
    },
    payment: manage
      ? {
          transfer: true,
          card: {
            brand: "Visa",
            last4: "4821",
            expires: "2028-08",
            status: options.pastDue ? "declined" : "ok",
            declinedAt: options.pastDue ? "2026-09-28" : null,
          },
        }
      : null,
    invoiceProfile: manage
      ? {
          company: "Công ty CP Tập đoàn Minh An",
          taxId: "0101234567",
          address: "12 Láng Hạ, Ba Đình, Hà Nội",
          email: "ketoan@minhan.vn",
        }
      : null,
    recentInvoices: manage
      ? options.pastDue
        ? [OVERDUE, ...PAID]
        : PAID
      : null,
    managers: [
      {
        id: "p-hung",
        name: "Trịnh Quốc Hưng",
        tone: "crimson",
        role: "owner",
        note: "Tổng giám đốc",
        isViewer: false,
      },
      {
        id: "p-ha",
        name: "Nguyễn Thu Hà",
        tone: "sage",
        role: "delegate",
        note: "2026-03",
        isViewer: manage,
      },
    ],
    pastDue:
      manage && options.pastDue
        ? {
            invoiceId: OVERDUE.id,
            number: OVERDUE.number,
            issuedAt: OVERDUE.issuedAt,
            dueAt: "2026-10-05",
            quantity: 4,
            cardLast4: "4821",
            graceUntil: "2026-10-19",
            restrictedFrom: "2026-10-20",
          }
        : null,
    pendingPurchase: options.pending
      ? {
          id: "purchase-1",
          quantity: 10,
          submittedAt: "2026-10-12T03:41:00.000Z",
          lastCheckedAt: "2026-10-12T03:41:00.000Z",
        }
      : null,
  };
}

export const PLANS_FIXTURE: BillingPlansDto = {
  current: "business",
  seatsOwned: 110,
  seatsUsed: 104,
  autoRenewAt: "2027-01-01",
  periodEnd: "2026-12-31",
  vatPercent: 8,
  workspaceCount: 2,
  pendingDowngrade: null,
  plans: [
    {
      tier: "free",
      monthlyPrice: 0,
      people: { kind: "max", value: 10 },
      storageGb: 5,
      runs: 500,
      advancedReports: false,
      webhooks: false,
      auditDays: null,
      sso: "none",
      powerBi: false,
    },
    {
      tier: "pro",
      monthlyPrice: 89_000,
      people: { kind: "seats" },
      storageGb: 250,
      runs: 5000,
      advancedReports: false,
      webhooks: false,
      auditDays: 90,
      sso: "none",
      powerBi: false,
    },
    {
      tier: "business",
      monthlyPrice: 159_000,
      people: { kind: "seats" },
      storageGb: 1000,
      runs: 25_000,
      advancedReports: true,
      webhooks: true,
      auditDays: 365,
      sso: "sso",
      powerBi: false,
    },
    {
      tier: "enterprise",
      monthlyPrice: null,
      people: { kind: "contract" },
      storageGb: null,
      runs: null,
      advancedReports: true,
      webhooks: true,
      auditDays: 2555,
      sso: "sso-scim",
      powerBi: true,
    },
  ],
};

export const INVOICES_FIXTURE: BillingInvoicesDto = {
  email: "ketoan@minhan.vn",
  invoices: PAID,
};

export const INVOICE_DETAIL_FIXTURE: BillingInvoiceDetailDto = {
  ...PAID[0]!,
  paidAt: "2026-09-05",
  method: { brand: "Visa", last4: "4821" },
  buyer: {
    company: "Công ty CP Tập đoàn Minh An",
    taxId: "0101234567",
    address: "12 Láng Hạ, Ba Đình, Hà Nội",
    email: "ketoan@minhan.vn",
  },
  line: {
    kind: "seats-added",
    quantity: 4,
    unitPrice: 159_000,
    from: "2026-09-05",
    to: "2026-12-31",
    days: 118,
    yearDays: 365,
    months: 12,
    amount: 2_467_332,
  },
  subtotal: 2_467_332,
  vat: 197_387,
  vatPercent: 8,
  total: 2_664_719,
  emailedTo: "ketoan@minhan.vn",
  emailedAt: "2026-09-05",
};

export const DOWNGRADE_FIXTURE: BillingDowngradeDto = {
  from: "business",
  to: "pro",
  effectiveAt: "2027-01-01",
  periodEnd: "2026-12-31",
  newUnitPrice: 89_000,
  asOf: "2026-10-12T03:37:00.000Z",
  workspaces: ["Minh An Group", "Minh An Đà Nẵng"],
  pending: false,
  runs: { from: 25_000, to: 5000, used: 4210 },
  reports: {
    names: [
      "Tổng quan triển khai ERP Q4",
      "Đóng sổ tháng 10",
      "Báo cáo tuần Ban giám đốc",
    ],
  },
  webhooks: { names: ["Trạng thái việc sang ERP cũ"] },
  storage: { fromGb: 1000, toGb: 250, usedGb: 312, overGb: 62 },
  kept: { seatsOwned: 110 },
};

export const QUOTE_FIXTURE: BillingSeatQuoteDto = {
  quantity: 10,
  unitPrice: 159_000,
  from: "2026-10-12",
  to: "2026-12-31",
  days: 81,
  yearDays: 365,
  amount: 4_234_192,
  vat: 338_735,
  vatPercent: 8,
  total: 4_572_927,
  seatsAfter: 120,
  seatsUsed: 104,
  card: { brand: "Visa", last4: "4821", expires: "2028-08" },
  email: "ketoan@minhan.vn",
};

export const SEATS_FIXTURE: BillingSeatSummaryDto = {
  used: 104,
  total: 110,
  sharedWith: ["Minh An Đà Nẵng"],
  canBuy: true,
};

export const INVITE_EMAILS = [
  "lan.do@minhan.vn",
  "son.pham@minhan.vn",
  "hieu.ngo@minhan.vn",
  "trang.le@minhan.vn",
  "duc.vo@minhan.vn",
  "mai.hoang@minhan.vn",
  "nam.dinh@minhan.vn",
] as const;
