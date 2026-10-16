import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import {
  describeWorkspaceFailure,
  isWorkspaceAdmin,
} from "@notrelix/features-workspace/core";
import {
  createUseWorkspaceSettings,
  createUseWorkspaceSettingsMutations,
  useWorkspaceTeamsCopy,
  WorkspaceSecuritySurface,
  type WorkspaceSecurityView,
} from "@notrelix/features-workspace/web";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useSetupFrame } from "@/shell/workspace/use-setup-frame";
import { useSecurityExt } from "@/shell/workspace/use-teams-ext";
import { useWorkspaceViewer } from "@/shell/workspace/use-workspace-viewer";

function dayOf(iso: string): string {
  const date = new Date(iso);
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
}

/**
 * Sharing and security of the workspace (Final-Teams `chia-se`): the sharing
 * links switch writes the workspace policy (`allowPublicSharing`, versioned
 * with the workspace); guests, the default end of their access and the company
 * domain come from the producer's extras and are shown.
 */
export function SecurityPage() {
  const { workspaceId, workspace, refetch } = useWorkspaceContext();
  const copy = useWorkspaceTeamsCopy();
  const frame = useSetupFrame("sharing");
  const { api: runtimeClient } = useAppRuntime();
  const hooks = useMemo(() => {
    const deps = { api: runtimeClient.api, endpoints: runtimeClient.endpoints };
    return {
      useSettings: createUseWorkspaceSettings(deps),
      useMutations: createUseWorkspaceSettingsMutations(deps),
    };
  }, [runtimeClient]);
  const viewer = useWorkspaceViewer(workspaceId);
  const settings = hooks.useSettings(workspaceId);
  const mutations = hooks.useMutations(workspaceId);
  const [error, setError] = useState<string | null>(null);
  const ext = useSecurityExt(
    workspaceId,
    String(settings.data?.allowPublicSharing ?? "unknown"),
  );

  const view = useMemo(() => {
    if (settings.isError || ext.isError) return { status: "error" as const };
    if (!settings.data || !ext.data) return { status: "loading" as const };
    const data: WorkspaceSecurityView = {
      linksOn: settings.data.allowPublicSharing,
      offSince: ext.data.linksOffSince ? dayOf(ext.data.linksOffSince) : null,
      guestCount: ext.data.guestCount,
      guestExpiryDays: ext.data.guestExpiryDays,
      companyDomain: ext.data.companyDomain,
    };
    return { status: "ready" as const, data };
  }, [ext.data, ext.isError, settings.data, settings.isError]);

  const onToggle = useCallback(
    (next: boolean) => {
      const version = workspace?.version;
      if (!settings.data || version === undefined) return;
      setError(null);
      mutations.updateSettings.mutate(
        {
          settings: { ...settings.data, allowPublicSharing: next },
          expectedVersion: version,
        },
        {
          onSuccess: () => void refetch(),
          onError: (failure) => {
            const described = describeWorkspaceFailure(failure);
            setError(described.detail ?? copy.security.link.failed);
            toast.error(copy.security.link.failed);
          },
        },
      );
    },
    [
      copy,
      mutations.updateSettings,
      refetch,
      settings.data,
      workspace?.version,
    ],
  );

  return (
    <WorkspaceSecuritySurface
      copy={copy}
      frame={frame}
      view={view}
      canManage={isWorkspaceAdmin(viewer.role)}
      saving={mutations.updateSettings.isPending}
      error={error}
      onToggleLinks={onToggle}
      onRetry={() => {
        void settings.refetch();
        ext.refetch();
      }}
    />
  );
}
