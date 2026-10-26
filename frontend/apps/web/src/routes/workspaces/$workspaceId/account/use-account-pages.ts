import { useCallback, useMemo, useRef, useState } from "react";
import { useParams, useRouter } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import type { AccountFrameLink, AccountTab } from "@notrelix/features-account";

/** The workspace of the route the account pages are shown in. */
export function useRouteWorkspaceId(): string {
  const { workspaceId = "" } = useParams({ strict: false }) as {
    workspaceId?: string;
  };
  return workspaceId;
}

/** The name of the route's workspace, for the pages' copy; `""` until it loads. */
export function useRouteWorkspaceName(): string {
  const workspaceId = useRouteWorkspaceId();
  const { api: runtimeClient } = useAppRuntime();
  const query = useQuery({
    queryKey: workspaceQueryKey(workspaceId, "account-pages", "name"),
    queryFn: async () => {
      const wire = await runtimeClient.api.get<{ name?: string | null }>(
        runtimeClient.endpoints.workspaces.detail(workspaceId),
      );
      return wire.name ?? "";
    },
    enabled: workspaceId !== "",
    retry: false,
  });
  return query.data ?? "";
}

const PATHS: Record<AccountTab, string> = {
  profile: "profile",
  appearance: "appearance",
  security: "security",
  notifications: "notifications",
  workspaces: "workspaces",
};

/** Where each account page lives, as links the frame can use. */
export function useAccountLinks(
  workspaceId: string,
): Record<AccountTab, AccountFrameLink> {
  const router = useRouter();
  return useMemo(() => {
    const entries = (Object.keys(PATHS) as AccountTab[]).map((tab) => {
      const to = `/workspaces/${workspaceId}/account/${PATHS[tab]}`;
      return [
        tab,
        {
          href: to,
          onNavigate: () => void router.navigate({ to }),
        },
      ] as const;
    });
    return Object.fromEntries(entries) as Record<AccountTab, AccountFrameLink>;
  }, [router, workspaceId]);
}

/**
 * A toast text that goes away by itself: `show` sets it, a new call replaces
 * it and restarts the timer.
 */
export function useToast(milliseconds = 3200) {
  const [text, setText] = useState<string | null>(null);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const show = useCallback(
    (next: string) => {
      setText(next);
      if (timer.current) clearTimeout(timer.current);
      timer.current = setTimeout(() => setText(null), milliseconds);
    },
    [milliseconds],
  );
  return [text, show] as const;
}
