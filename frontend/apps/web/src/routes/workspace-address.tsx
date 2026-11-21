import { useCallback, useMemo } from "react";
import { useNavigate, useParams, useSearch } from "@tanstack/react-router";
import {
  createWorkspaceAddressService,
  type WorkspaceAddressService,
} from "@notrelix/features-workspace";
import { WorkspaceAddressGate } from "@notrelix/features-workspace/web";
import { useAppRuntime } from "@notrelix/runtime-web";

/** Opens `/<address>?account=<accountId>`: the address is unique per Account, so the link carries both. */
export function WorkspaceAddressPage() {
  const { slug } = useParams({ from: "/$slug" });
  const { account } = useSearch({ from: "/$slug" });
  const navigate = useNavigate();
  const { api: runtimeClient } = useAppRuntime();
  const service: WorkspaceAddressService = useMemo(
    () =>
      createWorkspaceAddressService(runtimeClient.api, runtimeClient.endpoints),
    [runtimeClient],
  );
  const open = useCallback(
    (workspaceId: string) =>
      void navigate({
        to: "/workspaces/$workspaceId",
        params: { workspaceId },
        replace: true,
      }),
    [navigate],
  );

  return (
    <WorkspaceAddressGate
      service={service}
      accountId={account}
      slug={slug}
      onOpen={open}
      exitHref="/home"
      onExit={() => void navigate({ to: "/home" })}
    />
  );
}
