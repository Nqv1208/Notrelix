import { useMemo } from "react";
import { useRouter } from "@tanstack/react-router";
import {
  AccountFrame,
  createUseProfile,
  useAccountNavCopy,
  type AccountFrameLink,
  type AccountTab,
} from "@notrelix/features-account";
import { useAppRuntime } from "@notrelix/runtime-web";
import { NotrelixLogoMark } from "@notrelix/ui-web";
import { AuthGuard } from "@/shell/guards/auth-guard";
import { AccountWorkspacesPage } from "../workspaces/$workspaceId/account/workspaces";

const ONLY_WORKSPACES: readonly AccountTab[] = ["workspaces"];

/**
 * "Your workspaces" outside any workspace (`/account/workspaces`): where a person
 * with no workspace left still finds Recently deleted to restore one. It is the
 * account frame with its one page that needs no workspace; the other four open
 * inside a workspace.
 */
export function AccountOutsideWorkspacePage() {
  return (
    <AuthGuard>
      <Frame />
    </AuthGuard>
  );
}

function Frame() {
  const router = useRouter();
  const copy = useAccountNavCopy();
  const { api: runtimeClient } = useAppRuntime();
  const useProfile = useMemo(
    () =>
      createUseProfile({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const { profile } = useProfile();
  const links = useMemo(() => {
    const to = "/account/workspaces";
    const link: AccountFrameLink = {
      href: to,
      onNavigate: () => void router.navigate({ to }),
    };
    return {
      profile: link,
      appearance: link,
      security: link,
      notifications: link,
      workspaces: link,
    };
  }, [router]);

  return (
    <main className="flex h-svh flex-col bg-surface-canvas">
      <header className="flex h-12 shrink-0 items-center border-b border-border px-4">
        <NotrelixLogoMark className="h-5 w-auto" aria-hidden="true" />
        <span className="ml-2 text-label text-foreground">Notrelix</span>
      </header>
      <div className="min-h-0 flex-1">
        <AccountFrame
          copy={copy}
          identity={{
            name: profile?.name ?? "",
            email: profile?.email ?? "",
            avatarUrl: profile?.avatarUrl ?? null,
          }}
          active="workspaces"
          tabs={ONLY_WORKSPACES}
          links={links}
        >
          <AccountWorkspacesPage />
        </AccountFrame>
      </div>
    </main>
  );
}
