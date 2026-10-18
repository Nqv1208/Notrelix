/**
 * What the Billing pages of the 2026-10 Billing mockup need (gap
 * `CTR-GAP-BILLING`, see docs/superpowers/be-blockers-2026-10.md, "BE
 * requirement from FE: Billing"): the plan and usage of the company account
 * Minh An Group belongs to, its invoices, the downgrade preview and the seat
 * purchase with its payment confirmation. Money is whole VND, days are
 * `YYYY-MM-DD`; the handlers compose the store with it.
 */
import { isoOfMs } from "./clock";
import type { MockStore } from "./mock-store";

export const BILLING_UNIT_PRICE = 159_000;
export const BILLING_VAT_PERCENT = 8;
export const BILLING_PERIOD_START = "2026-01-01";
export const BILLING_PERIOD_END = "2026-12-31";
const YEAR_DAYS = 365;
const SEATS_BASE = 110;

export const BILLING_MANAGER_IDS = {
  executive: "mock-person-quoc-hung",
} as const;

export type InvoiceStatus = "paid" | "overdue";

export interface InvoiceLine {
  readonly kind: "plan-year" | "seats-added";
  readonly quantity: number;
  readonly unitPrice: number;
  readonly from: string;
  readonly to: string;
  readonly days: number;
  readonly yearDays: number;
  readonly months: number;
  readonly amount: number;
}

export interface StoredInvoice {
  readonly id: string;
  readonly number: string;
  readonly issuedAt: string;
  readonly dueAt: string | null;
  readonly paidAt: string | null;
  readonly status: InvoiceStatus;
  readonly line: InvoiceLine;
  readonly subtotal: number;
  readonly vat: number;
  readonly total: number;
}

export interface StoredPurchase {
  readonly id: string;
  readonly quantity: number;
  readonly submittedAt: string;
  checks: number;
  status: "pending" | "succeeded";
}

const roundHalfUp = (value: number) => Math.floor(value + 0.5);

/** Whole days between two `YYYY-MM-DD` days, both ends counted. */
export function inclusiveDays(from: string, to: string): number {
  return (
    Math.round(
      (Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)) /
        86_400_000,
    ) + 1
  );
}

function priced(line: Omit<InvoiceLine, "amount">) {
  const amount = roundHalfUp(
    (line.quantity * line.unitPrice * line.months * line.days) / line.yearDays,
  );
  const vat = roundHalfUp((amount * BILLING_VAT_PERCENT) / 100);
  return {
    line: { ...line, amount },
    subtotal: amount,
    vat,
    total: amount + vat,
  };
}

function seatsInvoice(
  number: string,
  issuedAt: string,
  quantity: number,
  extra: Pick<StoredInvoice, "dueAt" | "paidAt" | "status">,
): StoredInvoice {
  const days = inclusiveDays(issuedAt, BILLING_PERIOD_END);
  return {
    id: `mock-invoice-${number.slice(-9)}`,
    number,
    issuedAt,
    ...extra,
    ...priced({
      kind: "seats-added",
      quantity,
      unitPrice: BILLING_UNIT_PRICE,
      from: issuedAt,
      to: BILLING_PERIOD_END,
      days,
      yearDays: YEAR_DAYS,
      months: 12,
    }),
  };
}

function seedInvoices(pastDue: boolean): StoredInvoice[] {
  const paid = (day: string) => ({
    dueAt: null,
    paidAt: day,
    status: "paid" as const,
  });
  const invoices: StoredInvoice[] = [
    seatsInvoice("HĐ-2026-0112", "2026-09-05", 4, paid("2026-09-05")),
    seatsInvoice("HĐ-2026-0047", "2026-06-05", 6, paid("2026-06-05")),
    {
      id: "mock-invoice-2026-0001",
      number: "HĐ-2026-0001",
      issuedAt: "2026-01-01",
      dueAt: null,
      paidAt: "2026-01-01",
      status: "paid",
      ...priced({
        kind: "plan-year",
        quantity: 100,
        unitPrice: BILLING_UNIT_PRICE,
        from: BILLING_PERIOD_START,
        to: BILLING_PERIOD_END,
        days: YEAR_DAYS,
        yearDays: YEAR_DAYS,
        months: 12,
      }),
    },
  ];
  if (pastDue) {
    invoices.unshift(
      seatsInvoice("HĐ-2026-0131", "2026-09-28", 4, {
        dueAt: "2026-10-05",
        paidAt: null,
        status: "overdue",
      }),
    );
  }
  return invoices;
}

export interface SeatQuote {
  readonly quantity: number;
  readonly unitPrice: number;
  readonly from: string;
  readonly to: string;
  readonly days: number;
  readonly yearDays: number;
  readonly amount: number;
  readonly vat: number;
  readonly total: number;
}

export class MockBilling {
  private invoices: StoredInvoice[] | null = null;
  private purchases = new Map<string, StoredPurchase>();
  private extraSeats = 0;
  private sequence = 0;
  private downgradeTo: "pro" | null = null;
  private paidOverdue = false;

  reset(): void {
    this.invoices = null;
    this.purchases.clear();
    this.extraSeats = 0;
    this.sequence = 0;
    this.downgradeTo = null;
    this.paidOverdue = false;
  }

  private isPastDueWorld(store: MockStore): boolean {
    return store.getConfig().state === "billing-past-due" && !this.paidOverdue;
  }

  invoiceList(store: MockStore): readonly StoredInvoice[] {
    if (!this.invoices) {
      this.invoices = seedInvoices(
        store.getConfig().state === "billing-past-due",
      );
    }
    return this.invoices;
  }

  today(store: MockStore): string {
    return isoOfMs(store.getClock().now().getTime() + 7 * 3_600_000).slice(
      0,
      10,
    );
  }

  seatsTotal(): number {
    return SEATS_BASE + this.extraSeats;
  }

  pastDueInvoice(store: MockStore): StoredInvoice | null {
    if (!this.isPastDueWorld(store)) return null;
    return (
      this.invoiceList(store).find((invoice) => invoice.status === "overdue") ??
      null
    );
  }

  pending(): StoredPurchase | null {
    return (
      [...this.purchases.values()].find(
        (purchase) => purchase.status === "pending",
      ) ?? null
    );
  }

  purchase(id: string): StoredPurchase | undefined {
    return this.purchases.get(id);
  }

  quote(store: MockStore, quantity: number): SeatQuote {
    const from = this.today(store);
    const days = inclusiveDays(from, BILLING_PERIOD_END);
    const { line, vat, total } = priced({
      kind: "seats-added",
      quantity,
      unitPrice: BILLING_UNIT_PRICE,
      from,
      to: BILLING_PERIOD_END,
      days,
      yearDays: YEAR_DAYS,
      months: 12,
    });
    return {
      quantity,
      unitPrice: BILLING_UNIT_PRICE,
      from,
      to: BILLING_PERIOD_END,
      days,
      yearDays: YEAR_DAYS,
      amount: line.amount,
      vat,
      total,
    };
  }

  startPurchase(store: MockStore, quantity: number): StoredPurchase {
    const existing = this.pending();
    if (existing) return existing;
    this.sequence += 1;
    const purchase: StoredPurchase = {
      id: `mock-seat-purchase-${this.sequence}`,
      quantity,
      submittedAt: store.getClock().offsetMinutes(1),
      checks: 1,
      status: "pending",
    };
    this.purchases.set(purchase.id, purchase);
    return purchase;
  }

  /** The bank confirms on the third look: the seats are added and invoiced. */
  checkPurchase(store: MockStore, purchase: StoredPurchase): StoredPurchase {
    if (purchase.status === "succeeded") return purchase;
    purchase.checks += 1;
    if (purchase.checks >= 3) {
      purchase.status = "succeeded";
      this.extraSeats += purchase.quantity;
      const today = this.today(store);
      const list = [...this.invoiceList(store)];
      list.unshift(
        seatsInvoice(
          `HĐ-2026-0${140 + this.sequence}`,
          today,
          purchase.quantity,
          {
            dueAt: null,
            paidAt: today,
            status: "paid",
          },
        ),
      );
      this.invoices = list;
    }
    return purchase;
  }

  payOverdue(store: MockStore): boolean {
    const invoice = this.pastDueInvoice(store);
    if (!invoice) return false;
    this.paidOverdue = true;
    this.invoices = this.invoiceList(store).map((entry) =>
      entry.id === invoice.id
        ? {
            ...entry,
            status: "paid" as const,
            paidAt: this.today(store),
            dueAt: null,
          }
        : entry,
    );
    return true;
  }

  pendingDowngrade(): "pro" | null {
    return this.downgradeTo;
  }

  setDowngrade(to: "pro" | null): void {
    this.downgradeTo = to;
  }
}
