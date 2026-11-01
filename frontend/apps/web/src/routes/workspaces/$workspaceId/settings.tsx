import { useMemo, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  describeWorkspaceFailure,
  isWorkspaceAdmin,
  type WorkspaceSettings,
} from "@notrelix/features-workspace/core";
import {
  createUseWorkspaceSettings,
  createUseWorkspaceSettingsMutations,
  formatCopy,
  useWorkspaceSettingsCopy,
  WorkspaceSettingsSurface,
  type SaveStatus,
  type WorkspaceProfileValues,
  type WorkspaceSettingsCopy,
} from "@notrelix/features-workspace/web";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { toastFailure } from "@/shell/workspace/failure-toast";
import { sectionState } from "@/shell/workspace/section-state";
import { useWorkspaceViewer } from "@/shell/workspace/use-workspace-viewer";

/** Maps a failed write to the section's status (inline, never toast-only). */
function failureStatus(
  error: unknown,
  copy: WorkspaceSettingsCopy,
): SaveStatus {
  const failure = describeWorkspaceFailure(error);
  if (failure.kind === "conflict") return { state: "conflict" };
  const fieldMessage = Object.values(failure.fieldErrors).flat()[0] ?? null;
  const message =
    failure.kind === "rule"
      ? (failure.detail ?? copy.toasts.saveFailed)
      : failure.kind === "validation"
        ? (fieldMessage ?? failure.detail ?? copy.toasts.saveFailed)
        : failure.kind === "forbidden"
          ? copy.toasts.forbidden
          : failure.kind === "network"
            ? copy.toasts.network
            : copy.toasts.unknown;
  return { state: "error", message };
}

/**
 * Workspace settings (WX-SET-001). Profile and defaults save separately and
 * carry the workspace version the member last read; without a reported
 * version the producer cannot accept a write, so saving is explained and off.
 */
export function SettingsPage() {
  const { workspaceId, workspace, isLoading, isError, refetch } =
    useWorkspaceContext();
  const copy = useWorkspaceSettingsCopy();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
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
  const [profileStatus, setProfileStatus] = useState<SaveStatus>({
    state: "idle",
  });
  const [defaultsStatus, setDefaultsStatus] = useState<SaveStatus>({
    state: "idle",
  });

  const version = workspace?.version;
  const profile = useMemo(
    () =>
      isError
        ? { status: "error" as const }
        : isLoading || !workspace
          ? { status: "loading" as const }
          : {
              status: "ready" as const,
              data: {
                name: workspace.name,
                description: workspace.description ?? "",
              },
            },
    [isError, isLoading, workspace],
  );

  const onSaveProfile = (values: WorkspaceProfileValues) => {
    if (version === undefined) return;
    setProfileStatus({ state: "saving" });
    mutations.updateProfile.mutate(
      { ...values, expectedVersion: version },
      {
        onSuccess: () => {
          setProfileStatus({ state: "saved" });
          toast.success(copy.toasts.profileSaved);
        },
        onError: (error) => setProfileStatus(failureStatus(error, copy)),
      },
    );
  };

  const onSaveDefaults = (next: WorkspaceSettings) => {
    if (version === undefined) return;
    setDefaultsStatus({ state: "saving" });
    mutations.updateSettings.mutate(
      { settings: next, expectedVersion: version },
      {
        onSuccess: () => {
          setDefaultsStatus({ state: "saved" });
          toast.success(copy.toasts.defaultsSaved);
        },
        onError: (error) => setDefaultsStatus(failureStatus(error, copy)),
      },
    );
  };

  const onArchive = () => {
    if (version === undefined || !workspace) return;
    const values = { workspace: workspace.name };
    mutations.archive.mutate(version, {
      onSuccess: () => {
        toast.success(formatCopy(copy.toasts.archived, values));
        void navigate({ to: "/home" });
      },
      onError: (error) =>
        toastFailure(
          formatCopy(copy.toasts.archiveFailed, values),
          error,
          (failure) =>
            failure.kind === "forbidden"
              ? copy.toasts.forbidden
              : copy.toasts.unknown,
        ),
    });
  };

  const onDelete = async () => {
    if (version === undefined || !workspace) return;
    const values = { workspace: workspace.name };
    const id = workspaceId;
    try {
      await mutations.remove.mutateAsync(version);
    } catch (error) {
      toastFailure(
        formatCopy(copy.toasts.deleteFailed, values),
        error,
        (failure) =>
          failure.kind === "conflict"
            ? formatCopy(copy.toasts.deleteConflict, values)
            : failure.kind === "rule"
              ? (failure.detail ?? copy.toasts.deletePersonal)
              : failure.kind === "forbidden"
                ? copy.toasts.forbidden
                : copy.toasts.unknown,
      );
      return;
    }
    // The workspace is gone for everyone, this person included: leave its scope first, then drop what the
    // old scope still has in flight so a late answer cannot land anywhere. The page may already be
    // unmounted by now, so this does not hang on the component.
    await navigate({ to: "/home" });
    await queryClient.cancelQueries({ queryKey: ["workspace", id] });
    queryClient.removeQueries({ queryKey: ["workspace", id] });
    toast.success(formatCopy(copy.toasts.deleted, values));
  };

  return (
    <WorkspaceSettingsSurface
      copy={copy}
      workspaceName={workspace?.name ?? null}
      canManage={isWorkspaceAdmin(viewer.role)}
      versionAvailable={version !== undefined}
      profile={profile}
      defaults={sectionState(settings, settings.data)}
      profileStatus={profileStatus}
      defaultsStatus={defaultsStatus}
      archiving={mutations.archive.isPending}
      canDelete={viewer.role === "owner"}
      isPersonal={workspace?.isPersonal === true}
      deleting={mutations.remove.isPending}
      onSaveProfile={onSaveProfile}
      onSaveDefaults={onSaveDefaults}
      onArchive={onArchive}
      onDelete={() => void onDelete()}
      onReload={() => {
        setProfileStatus({ state: "idle" });
        setDefaultsStatus({ state: "idle" });
        void refetch();
        void settings.refetch();
      }}
      onRetryDefaults={() => void settings.refetch()}
    />
  );
}
