/**
 * Billing — the Plans and billing pages of the 2026-10 Billing mockup (gap
 * `CTR-GAP-BILLING`, see docs/superpowers/be-blockers-2026-10.md, "BE
 * requirement from FE: Billing"):
 *
 *   billing.overview          — GET    /workspaces/:workspaceId/billing
 *   billing.plans             — GET    /workspaces/:workspaceId/billing/plans
 *   billing.invoices          — GET    /workspaces/:workspaceId/billing/invoices
 *   billing.invoice           — GET    /workspaces/:workspaceId/billing/invoices/:invoiceId
 *   billing.invoicePdf        — GET    /workspaces/:workspaceId/billing/invoices/:invoiceId/pdf
 *   billing.invoicePay        — POST   /workspaces/:workspaceId/billing/invoices/:invoiceId/pay
 *   billing.downgradePreview  — GET    /workspaces/:workspaceId/billing/downgrade-preview
 *   billing.downgradeConfirm  — POST   /workspaces/:workspaceId/billing/downgrade
 *   billing.downgradeCancel   — DELETE /workspaces/:workspaceId/billing/downgrade
 *   billing.seatQuote         — GET    /workspaces/:workspaceId/billing/seat-quote
 *   billing.seatPurchase      — POST   /workspaces/:workspaceId/billing/seat-purchases
 *   billing.seatPurchaseCheck — POST   /workspaces/:workspaceId/billing/seat-purchases/:purchaseId/check
 *   billing.contactSales      — POST   /workspaces/:workspaceId/billing/contact-sales
 *   billing.seatSummary       — GET    /workspaces/:workspaceId/seat-summary
 *
 * and the one read the producer serves (a real contract, not a gap):
 *
 *   Billing.Capabilities.List — GET    /workspaces/:workspaceId/billing/capabilities
 */
import { defineMockOperation } from "../../operations/types";
import {
  created,
  forbidden,
  noContent,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import {
  BILLING_MANAGER_IDS,
  BILLING_PERIOD_END,
  BILLING_PERIOD_START,
  BILLING_UNIT_PRICE,
  BILLING_VAT_PERCENT,
  type StoredInvoice,
} from "../../state/mock-billing";
import { mockIds } from "../../state/mock-ids";
import { personOf } from "../../state/mock-reports";
import type { MockStore } from "../../state/mock-store";

const gap = { kind: "gap", gapId: "CTR-GAP-BILLING" } as const;

const ACCOUNT_NAME = "Tập đoàn Minh An";
const WORKSPACES = [
  {
    id: mockIds.workspaces.primary,
    name: "Minh An Group",
    seats: 86,
    runs: 3880,
  },
  {
    id: mockIds.workspaces.secondary,
    name: "Minh An Đà Nẵng",
    seats: 18,
    runs: 330,
  },
] as const;
const GUESTS = { used: 2, total: 50, inWorkspace: 2 } as const;
const STORAGE = { usedGb: 312, totalGb: 1000 } as const;
const RUNS_TOTAL = 25_000;
const INTEGRATIONS_IN_WORKSPACE = 4;
const PROFILE = {
  company: "Công ty CP Tập đoàn Minh An",
  taxId: "0101234567",
  address: "12 Láng Hạ, Ba Đình, Hà Nội",
  email: "ketoan@minhan.vn",
} as const;
const CARD = { brand: "Visa", last4: "4821", expires: "2028-08" } as const;
const QTY_MAX = 500;

const PLAN_ROWS = [
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
    monthlyPrice: BILLING_UNIT_PRICE,
    people: { kind: "seats" },
    storageGb: 1000,
    runs: RUNS_TOTAL,
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
] as const;

const isManager = (store: MockStore): boolean => {
  const id = store.getCurrentUser().id;
  return id === mockIds.users.owner || id === BILLING_MANAGER_IDS.executive;
};

function seatsUsed(): number {
  return WORKSPACES.reduce((sum, workspace) => sum + workspace.seats, 0);
}

function invoiceSummary(invoice: StoredInvoice) {
  return {
    id: invoice.id,
    number: invoice.number,
    issuedAt: invoice.issuedAt,
    dueAt: invoice.dueAt,
    status: invoice.status,
    kind: invoice.line.kind,
    quantity: invoice.line.quantity,
    from: invoice.line.from,
    to: invoice.line.to,
    days: invoice.line.days,
  };
}

function managersOf(store: MockStore) {
  const viewerId = store.getCurrentUser().id;
  const notes: Record<string, { role: string; note: string }> = {
    [BILLING_MANAGER_IDS.executive]: {
      role: "owner",
      note: "Tổng giám đốc",
    },
    [mockIds.users.owner]: { role: "delegate", note: "2026-03" },
  };
  return [BILLING_MANAGER_IDS.executive, mockIds.users.owner].flatMap((id) => {
    const person = personOf(store, id);
    return person
      ? [{ ...person, ...notes[id]!, isViewer: id === viewerId }]
      : [];
  });
}

function overviewOf(store: MockStore) {
  const billing = store.billing;
  const manage = isManager(store);
  const pastDue = billing.pastDueInvoice(store);
  const pending = billing.pending();
  const total = billing.seatsTotal();
  const invoices = billing.invoiceList(store);
  const pendingDowngrade = billing.pendingDowngrade();
  const card = pastDue
    ? { ...CARD, status: "declined", declinedAt: pastDue.issuedAt }
    : { ...CARD, status: "ok", declinedAt: null };
  return {
    serverTime: store.getClock().isoNow(),
    refreshedAt: store.getClock().offsetMinutes(-2),
    asOf: store.getClock().offsetMinutes(-3),
    viewer: { canManage: manage },
    account: {
      name: ACCOUNT_NAME,
      workspaces: WORKSPACES.map(({ id, name }) => ({ id, name })),
    },
    plan: {
      tier: "business",
      status: pastDue ? "past_due" : "active",
      cadence: "yearly",
      periodStart: BILLING_PERIOD_START,
      periodEnd: BILLING_PERIOD_END,
      autoRenewAt: "2027-01-01",
      unitPrice: BILLING_UNIT_PRICE,
      vatPercent: BILLING_VAT_PERCENT,
      pendingDowngrade: pendingDowngrade
        ? { to: pendingDowngrade, at: "2027-01-01" }
        : null,
    },
    usage: {
      seats: {
        used: seatsUsed(),
        total,
        perWorkspace: WORKSPACES.map(({ id, name, seats }) => ({
          id,
          name,
          count: seats,
        })),
      },
      storage: STORAGE,
      runs: {
        used: WORKSPACES.reduce((sum, workspace) => sum + workspace.runs, 0),
        total: RUNS_TOTAL,
        month: "2026-10",
        perWorkspace: WORKSPACES.map(({ id, name, runs }) => ({
          id,
          name,
          count: runs,
        })),
      },
      guests: GUESTS,
      integrations: { inWorkspace: INTEGRATIONS_IN_WORKSPACE, limit: null },
      workspace: {
        id: mockIds.workspaces.primary,
        name: WORKSPACES[0].name,
        members: WORKSPACES[0].seats,
        runs: WORKSPACES[0].runs,
      },
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
    payment: manage ? { transfer: true, card } : null,
    invoiceProfile: manage ? PROFILE : null,
    recentInvoices: manage ? invoices.slice(0, 4).map(invoiceSummary) : null,
    managers: managersOf(store),
    pastDue:
      manage && pastDue
        ? {
            invoiceId: pastDue.id,
            number: pastDue.number,
            issuedAt: pastDue.issuedAt,
            dueAt: pastDue.dueAt,
            quantity: pastDue.line.quantity,
            cardLast4: CARD.last4,
            graceUntil: "2026-10-19",
            restrictedFrom: "2026-10-20",
          }
        : null,
    pendingPurchase:
      manage && pending
        ? {
            id: pending.id,
            quantity: pending.quantity,
            submittedAt: pending.submittedAt,
            lastCheckedAt: pending.submittedAt,
          }
        : null,
  };
}

function detailOf(invoice: StoredInvoice) {
  return {
    ...invoiceSummary(invoice),
    paidAt: invoice.paidAt,
    method: invoice.paidAt ? { brand: CARD.brand, last4: CARD.last4 } : null,
    buyer: PROFILE,
    line: invoice.line,
    subtotal: invoice.subtotal,
    vat: invoice.vat,
    vatPercent: BILLING_VAT_PERCENT,
    total: invoice.total,
    emailedTo: PROFILE.email,
    emailedAt: invoice.issuedAt,
  };
}

const pdfOf = (invoice: StoredInvoice): string =>
  `%PDF-1.1\n% ${invoice.number} ${invoice.total}\n`;

type Ws = { workspaceId: string };
type WithInvoice = { workspaceId: string; invoiceId: string };

/** The seeded entitlement of the development stack, mirrored for the mock. */
const AUTOMATION_RULE_LIMIT = 50;

export const billingOperations = [
  defineMockOperation<Ws, never, unknown>({
    id: "billing.capabilities",
    contract: { kind: "openapi", operationId: "Billing.Capabilities.List" },
    method: "GET",
    route: "/workspaces/:workspaceId/billing/capabilities",
    async handle({ params, store }) {
      if (!store.getWorkspace(params.workspaceId))
        return notFound("Workspace not found");
      const role = store.getCurrentMemberRole(params.workspaceId);
      if (role !== "owner" && role !== "admin") return forbidden();
      const used = store.workspaceAutomations.list(params.workspaceId).length;
      return ok([
        {
          code: "AUTOMATION_RULE",
          isGranted: true,
          isUnlimited: false,
          limit: AUTOMATION_RULE_LIMIT,
          used,
          remaining: Math.max(0, AUTOMATION_RULE_LIMIT - used),
        },
      ]);
    },
  }),

  defineMockOperation<Ws, never, unknown>({
    id: "billing.overview",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/billing",
    async handle({ store }) {
      return ok(overviewOf(store));
    },
  }),

  defineMockOperation<Ws, never, unknown>({
    id: "billing.plans",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/billing/plans",
    async handle({ store }) {
      if (!isManager(store)) return forbidden();
      return ok({
        current: "business",
        seatsOwned: store.billing.seatsTotal(),
        seatsUsed: seatsUsed(),
        autoRenewAt: "2027-01-01",
        periodEnd: BILLING_PERIOD_END,
        vatPercent: BILLING_VAT_PERCENT,
        workspaceCount: WORKSPACES.length,
        pendingDowngrade: store.billing.pendingDowngrade(),
        plans: PLAN_ROWS,
      });
    },
  }),

  defineMockOperation<Ws, never, unknown>({
    id: "billing.invoices",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/billing/invoices",
    async handle({ store }) {
      if (!isManager(store)) return forbidden();
      return ok({
        email: PROFILE.email,
        invoices: store.billing.invoiceList(store).map(invoiceSummary),
      });
    },
  }),

  defineMockOperation<WithInvoice, never, unknown>({
    id: "billing.invoice",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/billing/invoices/:invoiceId",
    async handle({ params, store }) {
      if (!isManager(store)) return forbidden();
      const invoice = store.billing
        .invoiceList(store)
        .find((entry) => entry.id === params.invoiceId);
      return invoice ? ok(detailOf(invoice)) : notFound();
    },
  }),

  defineMockOperation<WithInvoice, never, unknown>({
    id: "billing.invoicePdf",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/billing/invoices/:invoiceId/pdf",
    async handle({ params, store }) {
      if (!isManager(store)) return forbidden();
      const invoice = store.billing
        .invoiceList(store)
        .find((entry) => entry.id === params.invoiceId);
      if (!invoice) return notFound();
      return ok({
        filename: `${invoice.number}.pdf`,
        downloadUrl: `data:application/pdf;charset=utf-8,${encodeURIComponent(pdfOf(invoice))}`,
      });
    },
  }),

  defineMockOperation<WithInvoice, never, unknown>({
    id: "billing.invoicePay",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/billing/invoices/:invoiceId/pay",
    async handle({ params, store }) {
      if (!isManager(store)) return forbidden();
      const due = store.billing.pastDueInvoice(store);
      if (!due || due.id !== params.invoiceId) return notFound();
      store.billing.payOverdue(store);
      return ok(overviewOf(store));
    },
  }),

  defineMockOperation<Ws, never, unknown>({
    id: "billing.downgradePreview",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/billing/downgrade-preview",
    async handle({ store }) {
      if (!isManager(store)) return forbidden();
      return ok({
        from: "business",
        to: "pro",
        effectiveAt: "2027-01-01",
        periodEnd: BILLING_PERIOD_END,
        newUnitPrice: 89_000,
        asOf: store.getClock().offsetMinutes(-3),
        workspaces: WORKSPACES.map(({ name }) => name),
        pending: store.billing.pendingDowngrade() === "pro",
        runs: { from: RUNS_TOTAL, to: 5000, used: 4210 },
        reports: {
          names: [
            "Tổng quan triển khai ERP Q4",
            "Đóng sổ tháng 10",
            "Báo cáo tuần Ban giám đốc",
          ],
        },
        webhooks: { names: ["Trạng thái việc sang ERP cũ"] },
        storage: { fromGb: 1000, toGb: 250, usedGb: 312, overGb: 62 },
        kept: { seatsOwned: store.billing.seatsTotal() },
      });
    },
  }),

  defineMockOperation<Ws, { to?: string }, unknown>({
    id: "billing.downgradeConfirm",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/billing/downgrade",
    async handle({ body, store }) {
      if (!isManager(store)) return forbidden();
      if (body?.to !== "pro") return validationError({ to: ["unsupported"] });
      store.billing.setDowngrade("pro");
      return ok(overviewOf(store));
    },
  }),

  defineMockOperation<Ws, never, unknown>({
    id: "billing.downgradeCancel",
    contract: gap,
    method: "DELETE",
    route: "/workspaces/:workspaceId/billing/downgrade",
    async handle({ store }) {
      if (!isManager(store)) return forbidden();
      store.billing.setDowngrade(null);
      return noContent();
    },
  }),

  defineMockOperation<Ws, never, unknown>({
    id: "billing.seatQuote",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/billing/seat-quote",
    async handle({ request, store }) {
      if (!isManager(store)) return forbidden();
      const quantity = Number(request.searchParams.get("quantity"));
      if (!Number.isInteger(quantity) || quantity < 1 || quantity > QTY_MAX) {
        return validationError({ quantity: ["out-of-range"] });
      }
      return ok({
        ...store.billing.quote(store, quantity),
        seatsAfter: store.billing.seatsTotal() + quantity,
        seatsUsed: seatsUsed(),
        vatPercent: BILLING_VAT_PERCENT,
        card: CARD,
        email: PROFILE.email,
      });
    },
  }),

  defineMockOperation<Ws, { quantity?: number }, unknown>({
    id: "billing.seatPurchase",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/billing/seat-purchases",
    async handle({ body, store }) {
      if (!isManager(store)) return forbidden();
      const quantity = Number(body?.quantity);
      if (!Number.isInteger(quantity) || quantity < 1 || quantity > QTY_MAX) {
        return validationError({ quantity: ["out-of-range"] });
      }
      const purchase = store.billing.startPurchase(store, quantity);
      return created({
        id: purchase.id,
        quantity: purchase.quantity,
        status: purchase.status,
        submittedAt: purchase.submittedAt,
        lastCheckedAt: purchase.submittedAt,
      });
    },
  }),

  defineMockOperation<
    { workspaceId: string; purchaseId: string },
    never,
    unknown
  >({
    id: "billing.seatPurchaseCheck",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/billing/seat-purchases/:purchaseId/check",
    async handle({ params, store }) {
      if (!isManager(store)) return forbidden();
      const purchase = store.billing.purchase(params.purchaseId);
      if (!purchase) return notFound();
      const next = store.billing.checkPurchase(store, purchase);
      return ok({
        id: next.id,
        quantity: next.quantity,
        status: next.status,
        submittedAt: next.submittedAt,
        lastCheckedAt: next.submittedAt,
      });
    },
  }),

  defineMockOperation<Ws, unknown, unknown>({
    id: "billing.contactSales",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/billing/contact-sales",
    async handle({ store }) {
      if (!isManager(store)) return forbidden();
      return created({ requestedAt: store.getClock().isoNow() });
    },
  }),

  defineMockOperation<Ws, never, unknown>({
    id: "billing.seatSummary",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/seat-summary",
    async handle({ store }) {
      return ok({
        used: seatsUsed(),
        total: store.billing.seatsTotal(),
        sharedWith: [WORKSPACES[1].name],
        canBuy: isManager(store),
      });
    },
  }),
];
