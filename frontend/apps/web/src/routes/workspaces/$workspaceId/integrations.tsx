import { useCallback, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  IntegrationConnectDialog,
  IntegrationDetailSurface,
  IntegrationDisconnectDialog,
  IntegrationUpgradeSheet,
  IntegrationsListSurface,
  WorkspaceSetupFrame,
  formatCopy,
  useIntegrationsCopy,
  useWorkspaceTeamsCopy,
  type SectionState,
} from "@notrelix/features-workspace/web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useSetupFrame } from "@/shell/workspace/use-setup-frame";
import type { IntegrationsSearch } from "../../../router/schemas/integrations-search-schema";
import {
  integrationsKeys,
  useIntegrationsApi,
  useIntegrationsLinks,
} from "./integrations/use-integrations";

const ROUTE = "/workspaces/$workspaceId/integrations";

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
 * Integrations (Final-Integrations): the connections in use and the catalog, or
 * one connection when `item` names it; `connect` opens the connect dialog (or
 * the plan sheet of an Enterprise entry) and `disconnect` the confirmation.
 */
export function IntegrationsPage() {
  const { workspaceId } = useWorkspaceContext();
  const copy = useIntegrationsCopy();
  const teamsCopy = useWorkspaceTeamsCopy();
  const frame = useSetupFrame("integrations");
  const api = useIntegrationsApi();
  const links = useIntegrationsLinks();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const search: IntegrationsSearch = useSearch({ from: ROUTE });
  const [scope, setScope] = useState<"workspace" | "board">("workspace");

  const list = useQuery({
    queryKey: integrationsKeys.list(workspaceId),
    queryFn: () => api.list(workspaceId),
    retry: false,
  });
  const canManage = list.data?.canManage ?? true;
  const itemKey = canManage ? search.item : undefined;
  const detail = useQuery({
    queryKey: integrationsKeys.detail(workspaceId, itemKey ?? ""),
    queryFn: () => api.get(workspaceId, itemKey ?? ""),
    enabled: itemKey !== undefined,
    retry: false,
  });
  const refresh = useCallback(
    () =>
      queryClient.invalidateQueries({
        queryKey: integrationsKeys.all(workspaceId),
      }),
    [queryClient, workspaceId],
  );
  const setSearch = useCallback(
    (next: IntegrationsSearch, replace = true) =>
      void navigate({
        to: ROUTE,
        params: { workspaceId },
        search: next,
        replace,
      } as never),
    [navigate, workspaceId],
  );
  const failed = () => toast.error(copy.unavailable);
  const detailName = detail.data?.name ?? "";

  const reauthorize = useMutation({
    mutationFn: () => api.reauthorize(workspaceId, itemKey ?? ""),
    onSuccess: async () => {
      await refresh();
      toast.success(copy.reauthorized);
    },
    onError: failed,
  });
  const pause = useMutation({
    mutationFn: (paused: boolean) =>
      paused
        ? api.pause(workspaceId, itemKey ?? "")
        : api.resume(workspaceId, itemKey ?? ""),
    onSuccess: async (_data, paused) => {
      await refresh();
      toast.success(paused ? copy.paused : copy.resumed);
    },
    onError: failed,
  });
  const test = useMutation({
    mutationFn: () => api.test(workspaceId, itemKey ?? ""),
    onSuccess: (result) =>
      toast.message(formatCopy(copy.testSent, { result: result.result })),
    onError: failed,
  });
  const regenerate = useMutation({
    mutationFn: () => api.regenerateSecret(workspaceId, itemKey ?? ""),
    onSuccess: async (result) => {
      await refresh();
      toast.message(formatCopy(copy.secretShown, { secret: result.secret }));
    },
    onError: failed,
  });
  const disconnect = useMutation({
    mutationFn: () => api.disconnect(workspaceId, itemKey ?? ""),
    onSuccess: async () => {
      toast.success(formatCopy(copy.disconnected, { name: detailName }));
      await refresh();
      setSearch({}, false);
    },
    onError: failed,
  });
  const connectItem =
    list.data?.catalog.find((entry) => entry.key === search.connect) ?? null;
  const connect = useMutation({
    mutationFn: () => api.connect(workspaceId, connectItem?.key ?? "", scope),
    onSuccess: async () => {
      toast.success(
        formatCopy(copy.connected, { name: connectItem?.name ?? "" }),
      );
      await refresh();
      setSearch({});
    },
    onError: failed,
  });
  const contact = useMutation({
    mutationFn: () => api.contactSales(workspaceId),
    onSuccess: () => toast.success(copy.contactSent),
    onError: failed,
  });

  const body = itemKey ? (
    <IntegrationDetailSurface
      copy={copy}
      links={links}
      state={stateOf(detail)}
      busy={false}
      onReload={() => void detail.refetch()}
      onReauthorize={() => reauthorize.mutate()}
      onDisconnect={() => setSearch({ item: itemKey, disconnect: true })}
      onTest={() => test.mutate()}
      onPause={(paused) => pause.mutate(paused)}
      onRegenerate={() => regenerate.mutate()}
    />
  ) : (
    <IntegrationsListSurface
      copy={copy}
      links={links}
      state={stateOf(list)}
      onReload={() => void list.refetch()}
      onConnect={(key) => setSearch({ connect: key }, false)}
    />
  );

  return (
    <WorkspaceSetupFrame
      copy={teamsCopy}
      frame={frame}
      only={list.data && !list.data.canManage ? "integrations" : undefined}
      title={list.data && !list.data.canManage ? copy.memberTitle : undefined}
    >
      {body}
      <IntegrationConnectDialog
        copy={copy}
        links={links}
        open={canManage && connectItem !== null && !connectItem.enterprise}
        item={connectItem}
        workspaceName={list.data?.workspaceName ?? ""}
        scope={scope}
        submitting={connect.isPending}
        onScope={setScope}
        onNext={() => connect.mutate()}
        onClose={() => setSearch({})}
      />
      <IntegrationUpgradeSheet
        copy={copy}
        links={links}
        open={canManage && connectItem !== null && connectItem.enterprise}
        item={connectItem}
        plan={
          list.data?.plan ?? { name: "business", account: "", workspaces: [] }
        }
        onContact={() => contact.mutate()}
        onClose={() => setSearch({})}
      />
      <IntegrationDisconnectDialog
        copy={copy}
        links={links}
        open={canManage && search.disconnect === true && itemKey !== undefined}
        detail={detail.data ?? null}
        submitting={disconnect.isPending}
        onConfirm={() => disconnect.mutate()}
        onClose={() => setSearch({ item: itemKey })}
      />
    </WorkspaceSetupFrame>
  );
}
