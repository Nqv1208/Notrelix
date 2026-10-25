import { useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import type { BillingLinks } from "@notrelix/features-workspace/web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { createBillingApi, type BillingApiClient } from "./billing-api";

/** The billing calls on the app's runtime client. */
export function useBillingApi() {
  const { api: runtimeClient } = useAppRuntime();
  return useMemo(
    () => createBillingApi(runtimeClient.api as unknown as BillingApiClient),
    [runtimeClient],
  );
}

/** Workspace-scoped query keys of the Plans and billing pages. */
export const billingKeys = {
  all: (workspaceId: string) => workspaceQueryKey(workspaceId, "billing"),
  capabilities: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "billing", "capabilities"),
  overview: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "billing", "overview"),
  plans: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "billing", "plans"),
  invoices: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "billing", "invoices"),
  invoice: (workspaceId: string, invoiceId: string) =>
    workspaceQueryKey(workspaceId, "billing", "invoice", invoiceId),
  downgrade: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "billing", "downgrade"),
  quote: (workspaceId: string, quantity: number) =>
    workspaceQueryKey(workspaceId, "billing", "quote", quantity),
  seats: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "billing", "seat-summary"),
} as const;

const ROUTE = "/workspaces/$workspaceId/billing";

/** Where the pages of Plans and billing live. */
export function useBillingLinks(): BillingLinks {
  const { workspaceId } = useWorkspaceContext();
  const navigate = useNavigate();
  return useMemo(() => {
    const base = `/workspaces/${encodeURIComponent(workspaceId)}`;
    const page = (
      search: Record<string, string> | undefined,
    ): { href: string; onNavigate: () => void } => {
      const query = search ? `?${new URLSearchParams(search).toString()}` : "";
      return {
        href: `${base}/billing${query}`,
        onNavigate: () =>
          void navigate({
            to: ROUTE,
            params: { workspaceId },
            search,
          } as never),
      };
    };
    return {
      overview: page(undefined),
      compare: page({ view: "compare" }),
      invoices: page({ view: "invoices" }),
      invoice: (invoiceId) => page({ view: "invoices", invoice: invoiceId }),
      downgrade: page({ view: "downgrade" }),
      buy: (quantity) => page({ buy: String(quantity) }),
      integrations: {
        href: `${base}/integrations`,
        onNavigate: () =>
          void navigate({
            to: "/workspaces/$workspaceId/integrations",
            params: { workspaceId },
          } as never),
      },
    };
  }, [navigate, workspaceId]);
}
