import { useCallback, useMemo, useState } from "react";
import { Navigate, useRouter } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import {
  WorkspacesHomeSurface,
  createUseAccountWorkspaces,
  createUseProfile,
  useWorkspacesHomeCopy,
  type AccountSection,
  type AccountWorkspaceTone,
  type AccountWorkspaceView,
  type DeletedWorkspaceView,
} from "@notrelix/features-account";
import { workspaceQueryKeys } from "@notrelix/features-workspace";
import { useLocale } from "@notrelix/i18n";
import { useAccountId } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import { NotrelixLogoMark } from "@notrelix/ui-web";
import { AuthGuard } from "@/shell/guards/auth-guard";
import { useToast } from "./workspaces/$workspaceId/account/use-account-pages";

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

/**
 * Home (`/home`): the account-level overview of the person's workspaces. It is
 * a launcher outside any workspace: a card opens one workspace, the default is
 * marked, and archived and recently deleted ones can be brought back from
 * here. A person with no workspace and nothing to restore starts onboarding.
 */
function HomeLanding() {
  const copy = useWorkspacesHomeCopy();
  const { locale } = useLocale();
  const router = useRouter();
  const queryClient = useQueryClient();
  const accountId = useAccountId();
  const { api: runtimeClient } = useAppRuntime();
  const useAccountWorkspaces = useMemo(
    () =>
      createUseAccountWorkspaces({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const useProfile = useMemo(
    () =>
      createUseProfile({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const list = useAccountWorkspaces();
  const { profile } = useProfile();
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<"failed" | "restore" | null>(
    null,
  );
  const [notice, showNotice] = useToast();

  const workspaces = useMemo<AccountSection<readonly AccountWorkspaceView[]>>(
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

  const run = useCallback(
    async (
      action: () => Promise<unknown>,
      failure: "failed" | "restore",
      done: string,
    ) => {
      setBusy(true);
      setActionError(null);
      try {
        await action();
        // The rail and the switcher read the same list.
        await queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.all(accountId),
        });
        showNotice(done);
      } catch {
        setActionError(failure);
      } finally {
        setBusy(false);
      }
    },
    [queryClient, accountId, showNotice],
  );

  const nameOf = (id: string) =>
    list.workspaces?.find((workspace) => workspace.id === id)?.name ?? "";

  // With no workspace the Recently deleted list decides what to show, so wait for it
  // (a failed read falls through to onboarding).
  const settled = list.deleted !== undefined || list.deletedIsError;
  if (list.workspaces?.length === 0 && settled && !list.deleted?.length) {
    return <Navigate to="/onboarding" replace />;
  }

  const account = {
    href: "/account/workspaces",
    onNavigate: () => void router.navigate({ to: "/account/workspaces" }),
  };

  return (
    <main className="flex min-h-svh flex-col bg-surface-canvas">
      <header className="flex h-12 shrink-0 items-center border-b border-border px-4">
        <NotrelixLogoMark className="h-5 w-auto" aria-hidden="true" />
        <span className="ml-2 text-label text-foreground">Notrelix</span>
      </header>
      <WorkspacesHomeSurface
        copy={copy}
        name={profile?.name ?? ""}
        workspaces={workspaces}
        deleted={deleted}
        busy={busy || list.isBusy}
        actionError={actionError}
        notice={notice}
        create={{
          href: "/onboarding",
          onNavigate: () => void router.navigate({ to: "/onboarding" }),
        }}
        account={account}
        onSetDefault={(id) =>
          void run(
            () => list.setDefault(id),
            "failed",
            copy.notice.defaultSet.replace("{{name}}", nameOf(id)),
          )
        }
        onUnarchive={(id) =>
          void run(
            () => list.unarchive(id),
            "failed",
            copy.notice.unarchived.replace("{{name}}", nameOf(id)),
          )
        }
        onRestore={(id) => {
          const entry = list.deleted?.find((workspace) => workspace.id === id);
          if (!entry) return;
          void run(
            () =>
              list.restore({ workspaceId: id, expectedVersion: entry.version }),
            "restore",
            copy.notice.restored.replace("{{name}}", entry.name),
          );
        }}
        onRetry={() => void list.refetch()}
      />
    </main>
  );
}

export function HomePage() {
  return (
    <AuthGuard>
      <HomeLanding />
    </AuthGuard>
  );
}
