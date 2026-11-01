import { useCallback, useMemo, useState } from "react";
import { useRouter } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import {
  AccountWorkspacesSurface,
  createUseAccountWorkspaces,
  leaveFailureOf,
  useAccountWorkspacesCopy,
  type AccountLeaveFailure,
  type AccountSection,
  type AccountWorkspaceTone,
  type AccountWorkspaceView,
  type DeletedWorkspaceView,
} from "@notrelix/features-account";
import { workspaceQueryKeys } from "@notrelix/features-workspace";
import { useLocale } from "@notrelix/i18n";
import { useAccountId } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useRouteWorkspaceId, useToast } from "./use-account-pages";

const TONES: readonly AccountWorkspaceTone[] = [
  "blue",
  "sage",
  "gold",
  "coral",
  "ember",
];

/** The mark letters of a workspace: the first letters of its first two words. */
function initialsOf(name: string): string {
  const words = name
    .trim()
    .split(/\s+/)
    .filter((word) => /^\p{L}/u.test(word));
  const [first = "", second = ""] = words;
  return `${first.charAt(0)}${second.charAt(0)}`.toUpperCase();
}

export function AccountWorkspacesPage() {
  const copy = useAccountWorkspacesCopy();
  const { locale } = useLocale();
  const router = useRouter();
  const queryClient = useQueryClient();
  const accountId = useAccountId();
  const currentWorkspaceId = useRouteWorkspaceId();
  const { api: runtimeClient } = useAppRuntime();
  const useAccountWorkspaces = useMemo(
    () =>
      createUseAccountWorkspaces({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const list = useAccountWorkspaces();
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<
    AccountLeaveFailure | "restore" | null
  >(null);
  const [notice, showNotice] = useToast();

  const section = useMemo<AccountSection<readonly AccountWorkspaceView[]>>(
    () =>
      list.workspaces
        ? {
            status: "ready",
            data: list.workspaces.map((workspace, index) => {
              const to = `/workspaces/${workspace.id}`;
              return {
                ...workspace,
                initials: initialsOf(workspace.name),
                tone: TONES[index % TONES.length] ?? "blue",
                open: {
                  href: to,
                  onNavigate: () => void router.navigate({ to }),
                },
              };
            }),
          }
        : list.isError
          ? { status: "error" }
          : { status: "loading" },
    [list.workspaces, list.isError, router],
  );

  const deleted = useMemo<AccountSection<readonly DeletedWorkspaceView[]>>(
    () =>
      list.deleted
        ? {
            status: "ready",
            data: list.deleted.map((workspace, index) => ({
              id: workspace.id,
              name: workspace.name,
              initials: initialsOf(workspace.name),
              tone: TONES[index % TONES.length] ?? "blue",
              deletedOn: new Intl.DateTimeFormat(locale, {
                dateStyle: "medium",
              }).format(new Date(workspace.deletedAt)),
              daysLeft: Math.max(
                1,
                Math.ceil(
                  (new Date(workspace.purgeAfter).getTime() - Date.now()) /
                    86_400_000,
                ),
              ),
            })),
          }
        : list.deletedIsError
          ? { status: "error" }
          : { status: "loading" },
    [list.deleted, list.deletedIsError, locale],
  );

  const nameOf = (id: string) =>
    list.workspaces?.find((workspace) => workspace.id === id)?.name ?? "";

  const setDefault = useCallback(
    async (id: string, name: string) => {
      setBusy(true);
      setActionError(null);
      try {
        await list.setDefault(id);
        // The workspace list of the rail and the switcher changed too.
        await queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.all(accountId),
        });
        showNotice(copy.notice.defaultSet.replace("{{name}}", name));
      } catch {
        setActionError("failed");
      } finally {
        setBusy(false);
      }
    },
    [list, queryClient, accountId, showNotice, copy],
  );

  const unarchive = useCallback(
    async (id: string, name: string) => {
      setBusy(true);
      setActionError(null);
      try {
        await list.unarchive(id);
        // It is back in the rail and the switcher.
        await queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.all(accountId),
        });
        showNotice(copy.notice.unarchived.replace("{{name}}", name));
      } catch {
        setActionError("failed");
      } finally {
        setBusy(false);
      }
    },
    [list, queryClient, accountId, showNotice, copy],
  );

  const restore = useCallback(
    async (id: string) => {
      const entry = list.deleted?.find((workspace) => workspace.id === id);
      if (!entry) return;
      setBusy(true);
      setActionError(null);
      try {
        await list.restore({ workspaceId: id, expectedVersion: entry.version });
        // It is back in the rail and the switcher.
        await queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.all(accountId),
        });
        showNotice(copy.notice.restored.replace("{{name}}", entry.name));
      } catch {
        setActionError("restore");
      } finally {
        setBusy(false);
      }
    },
    [list, queryClient, accountId, showNotice, copy],
  );

  const leave = useCallback(
    async (id: string, name: string) => {
      setBusy(true);
      setActionError(null);
      try {
        const remaining = await list.leave(id);
        await queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.all(accountId),
        });
        if (id === currentWorkspaceId) {
          // Leaving the workspace this page is shown in is a scope
          // transition: go to another one first, then drop what the old
          // scope still has in flight so a late answer cannot land anywhere.
          // An archived workspace is read-only: it is not where to land.
          const openable = remaining.filter(
            (workspace) => !workspace.isArchived,
          );
          const next =
            openable.find((workspace) => workspace.isDefault) ?? openable[0];
          await router.navigate({
            to: next ? `/workspaces/${next.id}` : "/onboarding",
          });
          await queryClient.cancelQueries({ queryKey: ["workspace", id] });
          queryClient.removeQueries({ queryKey: ["workspace", id] });
        } else {
          showNotice(copy.notice.left.replace("{{name}}", name));
        }
      } catch (error) {
        setActionError(leaveFailureOf(error));
      } finally {
        setBusy(false);
      }
    },
    [
      list,
      queryClient,
      accountId,
      router,
      currentWorkspaceId,
      showNotice,
      copy,
    ],
  );

  return (
    <AccountWorkspacesSurface
      copy={copy}
      workspaces={section}
      busy={busy || list.isBusy}
      actionError={actionError}
      notice={notice}
      create={{
        href: "/onboarding",
        onNavigate: () => void router.navigate({ to: "/onboarding" }),
      }}
      deleted={deleted}
      onRestore={(id) => void restore(id)}
      onSetDefault={(id) => void setDefault(id, nameOf(id))}
      onUnarchive={(id) => void unarchive(id, nameOf(id))}
      onLeave={(id) => void leave(id, nameOf(id))}
      onRetry={() => void list.refetch()}
    />
  );
}
