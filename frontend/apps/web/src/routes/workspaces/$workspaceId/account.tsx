import { useMemo } from "react";
import { Outlet, useLocation, useParams } from "@tanstack/react-router";
import {
  AccountFrame,
  createUseProfile,
  useAccountNavCopy,
  type AccountTab,
} from "@notrelix/features-account";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useAccountLinks } from "./account/use-account-pages";

const TABS: readonly AccountTab[] = [
  "profile",
  "appearance",
  "security",
  "notifications",
  "workspaces",
];

/** The tab of the account page the path shows. */
function activeTab(pathname: string): AccountTab {
  const last = pathname.replace(/\/+$/, "").split("/").pop() ?? "";
  return TABS.find((tab) => tab === last) ?? "profile";
}

export function AccountLayout() {
  const { workspaceId } = useParams({
    from: "/workspaces/$workspaceId/account",
  });
  const location = useLocation();
  const copy = useAccountNavCopy();
  const links = useAccountLinks(workspaceId);
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

  return (
    <AccountFrame
      copy={copy}
      identity={{
        name: profile?.name ?? "",
        email: profile?.email ?? "",
        avatarUrl: profile?.avatarUrl ?? null,
      }}
      active={activeTab(location.pathname)}
      links={links}
    >
      <Outlet />
    </AccountFrame>
  );
}
