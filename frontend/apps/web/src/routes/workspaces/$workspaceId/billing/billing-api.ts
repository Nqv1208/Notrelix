import type {
  BillingDowngradeDto,
  BillingInvoiceDetailDto,
  BillingInvoicesDto,
  BillingOverviewDto,
  BillingPlansDto,
  BillingPurchaseDto,
  BillingSeatQuoteDto,
  BillingSeatSummaryDto,
} from "@notrelix/features-workspace/web";

/**
 * The calls behind the Plans and billing pages (gap `CTR-GAP-BILLING`, see
 * docs/superpowers/be-blockers-2026-10.md, "BE requirement from FE: Billing").
 * `api` is the runtime client; every call is quiet: the pages say what went
 * wrong themselves.
 */
export interface BillingApiClient {
  get<T>(url: string, options?: { skipGlobalErrorToast?: boolean }): Promise<T>;
  post<T>(
    url: string,
    body?: unknown,
    options?: { skipGlobalErrorToast?: boolean; idempotencyKey?: string },
  ): Promise<T>;
  delete<T>(
    url: string,
    options?: { skipGlobalErrorToast?: boolean },
  ): Promise<T>;
}

const QUIET = { skipGlobalErrorToast: true } as const;

const root = (workspaceId: string) =>
  `/workspaces/${encodeURIComponent(workspaceId)}`;
const billing = (workspaceId: string) => `${root(workspaceId)}/billing`;

export function createBillingApi(api: BillingApiClient) {
  return {
    overview(workspaceId: string) {
      return api.get<BillingOverviewDto>(billing(workspaceId), QUIET);
    },
    plans(workspaceId: string) {
      return api.get<BillingPlansDto>(`${billing(workspaceId)}/plans`, QUIET);
    },
    invoices(workspaceId: string) {
      return api.get<BillingInvoicesDto>(
        `${billing(workspaceId)}/invoices`,
        QUIET,
      );
    },
    invoice(workspaceId: string, invoiceId: string) {
      return api.get<BillingInvoiceDetailDto>(
        `${billing(workspaceId)}/invoices/${encodeURIComponent(invoiceId)}`,
        QUIET,
      );
    },
    invoicePdf(workspaceId: string, invoiceId: string) {
      return api.get<{ filename: string; downloadUrl: string }>(
        `${billing(workspaceId)}/invoices/${encodeURIComponent(invoiceId)}/pdf`,
        QUIET,
      );
    },
    payInvoice(workspaceId: string, invoiceId: string) {
      return api.post<BillingOverviewDto>(
        `${billing(workspaceId)}/invoices/${encodeURIComponent(invoiceId)}/pay`,
        {},
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
    downgradePreview(workspaceId: string) {
      return api.get<BillingDowngradeDto>(
        `${billing(workspaceId)}/downgrade-preview`,
        QUIET,
      );
    },
    confirmDowngrade(workspaceId: string) {
      return api.post<BillingOverviewDto>(
        `${billing(workspaceId)}/downgrade`,
        { to: "pro" },
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
    cancelDowngrade(workspaceId: string) {
      return api.delete<void>(`${billing(workspaceId)}/downgrade`, QUIET);
    },
    seatQuote(workspaceId: string, quantity: number) {
      return api.get<BillingSeatQuoteDto>(
        `${billing(workspaceId)}/seat-quote?quantity=${quantity}`,
        QUIET,
      );
    },
    buySeats(workspaceId: string, quantity: number, idempotencyKey: string) {
      return api.post<BillingPurchaseDto>(
        `${billing(workspaceId)}/seat-purchases`,
        { quantity },
        { ...QUIET, idempotencyKey },
      );
    },
    checkPurchase(workspaceId: string, purchaseId: string) {
      return api.post<BillingPurchaseDto>(
        `${billing(workspaceId)}/seat-purchases/${encodeURIComponent(purchaseId)}/check`,
        {},
        QUIET,
      );
    },
    contactSales(workspaceId: string) {
      return api.post<{ requestedAt: string }>(
        `${billing(workspaceId)}/contact-sales`,
        {},
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
    seatSummary(workspaceId: string) {
      return api.get<BillingSeatSummaryDto>(
        `${root(workspaceId)}/seat-summary`,
        QUIET,
      );
    },
  };
}
