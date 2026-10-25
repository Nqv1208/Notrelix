import { useCallback, useRef, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  BillingBuyDialog,
  BillingCompareSurface,
  BillingDowngradeSurface,
  BillingInvoicesSurface,
  BillingOverviewSurface,
  WorkspaceSetupFrame,
  useBillingCopy,
  useWorkspaceTeamsCopy,
  type BillingPurchaseDto,
  type SectionState,
} from "@notrelix/features-workspace/web";
import { useFormatters } from "@notrelix/i18n";
import { backendCapabilities } from "@/config/backend-capabilities";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useSetupFrame } from "@/shell/workspace/use-setup-frame";
import type { BillingSearch } from "../../../router/schemas/billing-search-schema";
import { BillingCapabilitiesPage } from "./billing/billing-capabilities-page";
import {
  billingKeys,
  useBillingApi,
  useBillingLinks,
} from "./billing/use-billing";

const ROUTE = "/workspaces/$workspaceId/billing";

function stateOf<T>(query: {
  data: T | undefined;
  isError: boolean;
}): SectionState<T> {
  return query.data !== undefined
    ? { status: "ready", data: query.data }
    : query.isError
      ? { status: "error" }
      : { status: "loading" };
}

/**
 * Plans and billing (Final-Billing): the overview, the plan comparison, the
 * invoices and the downgrade preview share one path and are told apart by
 * `view`, inside the frame of "Thiết lập workspace". `buy` opens the buy-seats
 * dialog over the overview.
 */
export function BillingPage() {
  return backendCapabilities.billingPlans ? (
    <BillingPlansPage />
  ) : (
    <BillingCapabilitiesPage />
  );
}

function BillingPlansPage() {
  const { workspaceId } = useWorkspaceContext();
  const copy = useBillingCopy();
  const teamsCopy = useWorkspaceTeamsCopy();
  const formatters = useFormatters();
  const api = useBillingApi();
  const links = useBillingLinks();
  const frame = useSetupFrame("billing");
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const search: BillingSearch = useSearch({ from: ROUTE });

  const overview = useQuery({
    queryKey: billingKeys.overview(workspaceId),
    queryFn: () => api.overview(workspaceId),
    retry: false,
  });
  const canManage = overview.data?.viewer.canManage ?? true;
  const view = canManage ? search.view : undefined;
  const refreshAll = useCallback(
    () =>
      queryClient.invalidateQueries({ queryKey: billingKeys.all(workspaceId) }),
    [queryClient, workspaceId],
  );

  const plans = useQuery({
    queryKey: billingKeys.plans(workspaceId),
    queryFn: () => api.plans(workspaceId),
    enabled: view === "compare",
    retry: false,
  });
  const invoices = useQuery({
    queryKey: billingKeys.invoices(workspaceId),
    queryFn: () => api.invoices(workspaceId),
    enabled: view === "invoices",
    retry: false,
  });
  const selectedInvoice =
    search.invoice ?? invoices.data?.invoices[0]?.id ?? null;
  const invoice = useQuery({
    queryKey: billingKeys.invoice(workspaceId, selectedInvoice ?? ""),
    queryFn: () => api.invoice(workspaceId, selectedInvoice ?? ""),
    enabled: view === "invoices" && selectedInvoice !== null,
    retry: false,
  });
  const downgrade = useQuery({
    queryKey: billingKeys.downgrade(workspaceId),
    queryFn: () => api.downgradePreview(workspaceId),
    enabled: view === "downgrade",
    retry: false,
  });

  const unavailable = useCallback(
    () => toast.message(copy.page.unavailable),
    [copy.page.unavailable],
  );
  const contact = useMutation({
    mutationFn: () => api.contactSales(workspaceId),
    onSuccess: () => toast.success(copy.page.contactSent),
    onError: () => toast.error(copy.page.actionFailed),
  });
  const pay = useMutation({
    mutationFn: (invoiceId: string) => api.payInvoice(workspaceId, invoiceId),
    onSuccess: async (_data, invoiceId) => {
      await refreshAll();
      const number =
        overview.data?.pastDue?.invoiceId === invoiceId
          ? overview.data.pastDue.number
          : invoiceId;
      toast.success(copy.page.paid.replace("{{number}}", number));
    },
    onError: () => toast.error(copy.page.payFailed),
  });
  const confirmDowngrade = useMutation({
    mutationFn: () => api.confirmDowngrade(workspaceId),
    onSuccess: async () => {
      await refreshAll();
      toast.success(
        copy.page.downgradeSaved
          .replace("{{plan}}", copy.plan.names.pro)
          .replace("{{day}}", "01/01/2027"),
      );
      links.overview.onNavigate();
    },
    onError: () => toast.error(copy.page.actionFailed),
  });
  const cancelDowngrade = useMutation({
    mutationFn: () => api.cancelDowngrade(workspaceId),
    onSuccess: async () => {
      await refreshAll();
      toast.success(copy.page.downgradeCancelled);
      links.overview.onNavigate();
    },
    onError: () => toast.error(copy.page.actionFailed),
  });
  const [pdfFailed, setPdfFailed] = useState(false);
  const pdf = useMutation({
    mutationFn: (invoiceId: string) => api.invoicePdf(workspaceId, invoiceId),
    onSuccess: (file) => {
      setPdfFailed(false);
      const anchor = document.createElement("a");
      anchor.href = file.downloadUrl;
      anchor.download = file.filename;
      anchor.click();
    },
    onError: () => setPdfFailed(true),
  });

  // ── Buy seats ──
  const quantity = search.buy ?? null;
  const buyOpen = quantity !== null && canManage;
  const quote = useQuery({
    queryKey: billingKeys.quote(workspaceId, quantity ?? 0),
    queryFn: () => api.seatQuote(workspaceId, quantity ?? 1),
    enabled: buyOpen && (quantity ?? 0) >= 1,
    retry: false,
  });
  const [started, setStarted] = useState<BillingPurchaseDto | null>(null);
  const [buyFailed, setBuyFailed] = useState(false);
  const idempotencyKey = useRef<string | null>(null);
  const pending: BillingPurchaseDto | null =
    started ?? overview.data?.pendingPurchase ?? null;
  const setSearch = useCallback(
    (next: BillingSearch) =>
      void navigate({
        to: ROUTE,
        params: { workspaceId },
        search: next,
        replace: true,
      } as never),
    [navigate, workspaceId],
  );
  const closeBuy = useCallback(() => {
    setBuyFailed(false);
    idempotencyKey.current = null;
    setSearch({});
  }, [setSearch]);
  const buy = useMutation({
    mutationFn: () => {
      idempotencyKey.current ??= crypto.randomUUID();
      return api.buySeats(workspaceId, quantity ?? 1, idempotencyKey.current);
    },
    onSuccess: async (purchase) => {
      setBuyFailed(false);
      setStarted(purchase);
      await queryClient.invalidateQueries({
        queryKey: billingKeys.overview(workspaceId),
      });
    },
    onError: () => setBuyFailed(true),
  });
  const recheck = useMutation({
    mutationFn: () => api.checkPurchase(workspaceId, pending?.id ?? ""),
    onSuccess: async (purchase) => {
      if (purchase.status === "succeeded") {
        setStarted(null);
        idempotencyKey.current = null;
        await refreshAll();
        toast.success(
          copy.buy.done.replace("{{count}}", String(purchase.quantity)),
        );
        setSearch({});
      } else {
        setStarted(purchase);
      }
    },
    onError: () => toast.error(copy.page.actionFailed),
  });
  const openBuy = useCallback(
    () => setSearch({ buy: pending?.quantity ?? 10 }),
    [pending?.quantity, setSearch],
  );

  const overviewState = stateOf(overview);
  const accountName = overview.data?.account.name ?? "";

  let body: React.ReactNode;
  if (view === "compare") {
    body = (
      <BillingCompareSurface
        copy={copy}
        formatters={formatters}
        links={links}
        accountName={accountName}
        state={stateOf(plans)}
        onContactSales={() => contact.mutate()}
        onRetry={() => void plans.refetch()}
      />
    );
  } else if (view === "invoices") {
    body = (
      <BillingInvoicesSurface
        copy={copy}
        formatters={formatters}
        links={links}
        accountName={accountName}
        state={stateOf(invoices)}
        selectedId={selectedInvoice}
        detail={selectedInvoice ? stateOf(invoice) : null}
        downloading={pdf.isPending}
        pdfFailed={pdfFailed}
        onDownload={(invoiceId) => pdf.mutate(invoiceId)}
        onRetry={() => void invoices.refetch()}
      />
    );
  } else if (view === "downgrade") {
    body = (
      <BillingDowngradeSurface
        copy={copy}
        formatters={formatters}
        links={links}
        state={stateOf(downgrade)}
        saving={confirmDowngrade.isPending || cancelDowngrade.isPending}
        onConfirm={() => confirmDowngrade.mutate()}
        onCancelRequest={() => cancelDowngrade.mutate()}
        onRetry={() => void downgrade.refetch()}
      />
    );
  } else {
    body = (
      <BillingOverviewSurface
        copy={copy}
        formatters={formatters}
        links={links}
        state={overviewState}
        reloading={overview.isFetching}
        paying={pay.isPending}
        onReload={() => void overview.refetch()}
        onBuy={openBuy}
        onPayNow={() => {
          const due = overview.data?.pastDue;
          if (due) pay.mutate(due.invoiceId);
        }}
        onUnavailable={unavailable}
        onContactSales={() => contact.mutate()}
      />
    );
  }

  return (
    <WorkspaceSetupFrame
      copy={teamsCopy}
      frame={frame}
      hideNav={overview.data ? !overview.data.viewer.canManage : false}
    >
      {body}
      <BillingBuyDialog
        copy={copy}
        formatters={formatters}
        open={buyOpen}
        quantity={quantity ?? 10}
        quote={stateOf(quote)}
        pending={pending}
        submitting={buy.isPending}
        checking={recheck.isPending}
        failed={buyFailed}
        onQuantity={(next) => setSearch({ buy: Math.max(1, next) })}
        onConfirm={() => buy.mutate()}
        onRecheck={() => recheck.mutate()}
        onChangeCard={unavailable}
        onClose={closeBuy}
      />
    </WorkspaceSetupFrame>
  );
}
