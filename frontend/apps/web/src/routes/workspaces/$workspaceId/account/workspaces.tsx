import { useCallback, useMemo, useState } from "react";
import { useRouter } from "@tanstack/react-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AccountWorkspacesSurface,
  useAccountWorkspacesCopy,
  type AccountSection,
  type AccountWorkspaceTone,
  type AccountWorkspaceView,
} from "@notrelix/features-account";
import {
  accountPagesKeys,
  useAccountPagesApi,
  useToast,
} from "./use-account-pages";

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
  const api = useAccountPagesApi();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState(false);
  const [notice, showNotice] = useToast();

  const list = useQuery({
    queryKey: accountPagesKeys.workspaces,
    queryFn: () => api.getWorkspaces(),
    retry: false,
  });

  const section = useMemo<AccountSection<readonly AccountWorkspaceView[]>>(
    () =>
      list.data
        ? {
            status: "ready",
            data: list.data.workspaces.map((workspace, index) => {
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
    [list.data, list.isError, router],
  );

  const run = useCallback(
    async (action: () => Promise<unknown>, message: string) => {
      setBusy(true);
      setActionError(false);
      try {
        await action();
        await queryClient.invalidateQueries({
          queryKey: accountPagesKeys.workspaces,
        });
        // The workspace list of the rail and the switcher changed too.
        await queryClient.invalidateQueries({ queryKey: ["workspaces"] });
        showNotice(message);
      } catch {
        setActionError(true);
      } finally {
        setBusy(false);
      }
    },
    [queryClient, showNotice],
  );

  const nameOf = (id: string) =>
    list.data?.workspaces.find((workspace) => workspace.id === id)?.name ?? "";

  return (
    <AccountWorkspacesSurface
      copy={copy}
      workspaces={section}
      busy={busy}
      actionError={actionError}
      notice={notice}
      create={{
        href: "/onboarding",
        onNavigate: () => void router.navigate({ to: "/onboarding" }),
      }}
      onSetDefault={(id) =>
        void run(
          () => api.setDefaultWorkspace(id),
          copy.notice.defaultSet.replace("{{name}}", nameOf(id)),
        )
      }
      onLeave={(id) =>
        void run(
          () => api.leaveWorkspace(id),
          copy.notice.left.replace("{{name}}", nameOf(id)),
        )
      }
      onRetry={() => void list.refetch()}
    />
  );
}
