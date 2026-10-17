import { useNavigate } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import {
  GettingStartedCard,
  shortCompanyName,
  useOnboardingCopy,
  type GettingStartedItem,
} from "@notrelix/features-workspace/web";
import { useOnboardingApi } from "./use-onboarding-api";

/**
 * The getting-started list on Home of a workspace made through onboarding
 * (gap `CTR-GAP-ONBOARD-FLOW`): it shows until its three tasks are done or
 * the member hides it, and nothing at all in any other workspace.
 */
export function HomeGettingStarted({
  workspaceId,
  workspaceName,
}: {
  workspaceId: string;
  workspaceName: string;
}) {
  const copy = useOnboardingCopy();
  const api = useOnboardingApi();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const key = workspaceQueryKey(workspaceId, "getting-started");
  const list = useQuery({
    queryKey: key,
    queryFn: () => api.gettingStarted(workspaceId),
    retry: false,
    staleTime: 30_000,
  });
  const refresh = () => void queryClient.invalidateQueries({ queryKey: key });
  const dismiss = useMutation({
    mutationFn: () => api.dismissGettingStarted(workspaceId),
    onSuccess: refresh,
  });
  const complete = useMutation({
    mutationFn: (itemKey: string) =>
      api.completeGettingStarted(workspaceId, itemKey),
    onSuccess: refresh,
  });

  const data = list.data;
  if (!data?.visible) return null;
  const t = copy.done.items;
  const items: GettingStartedItem[] = data.items.map((item) => {
    if (item.key === "first-item")
      return {
        key: item.key,
        title: t.firstItem,
        note: item.boardTitle
          ? t.firstItemNote.replace("{{board}}", item.boardTitle)
          : t.firstItemNoBoard,
        done: item.done,
        href: item.boardId
          ? `/workspaces/${workspaceId}/boards/${item.boardId}`
          : null,
      };
    if (item.key === "invite-more")
      return {
        key: item.key,
        title: t.inviteMore,
        note:
          item.pendingInvites && item.pendingInvites > 0
            ? t.inviteMoreWaiting.replace(
                "{{count}}",
                String(item.pendingInvites),
              )
            : t.inviteMoreNoneNote,
        done: item.done,
        href: `/workspaces/${workspaceId}/members`,
      };
    return {
      key: item.key,
      title: t.mobile,
      note: t.mobileNote,
      done: item.done,
      href: null,
    };
  });
  return (
    <GettingStartedCard
      copy={copy}
      name={shortCompanyName(workspaceName)}
      items={items}
      onToggle={(itemKey) => complete.mutate(itemKey)}
      onHide={() => dismiss.mutate()}
      onOpen={(item) => {
        if (item.href) void navigate({ to: item.href });
      }}
    />
  );
}
