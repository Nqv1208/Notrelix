import { useCallback, useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import {
  useWorkspaceInboxCopy,
  WorkspaceInboxSurface,
  type InboxItemView,
  type SectionState,
} from "@notrelix/features-workspace/web";
import { useFormatters } from "@notrelix/i18n";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { toastFailure } from "@/shell/workspace/failure-toast";
import { useDepartmentsById } from "@/shell/workspace/home-feed-view";
import { toInboxEntries } from "@/shell/workspace/inbox-view";
import { useNow } from "@/shell/workspace/use-now";
import { useShellNavigation } from "@/shell/workspace/use-shell-navigation";
import {
  useWorkFeedWrites,
  useWorkspaceInbox,
} from "@/shell/workspace/work-feed-bridge";
import { sectionState } from "@/shell/workspace/section-state";

/**
 * Inbox (Final-Inbox): the member's own notifications in the workspace with
 * the selected entry in the reading pane. The member lists them and marks one
 * or all of them read; approving, replying, handled, snooze and mute have no
 * contract yet, so the page offers none of them.
 */
export function InboxPage() {
  const { workspaceId, workspace } = useWorkspaceContext();
  const copy = useWorkspaceInboxCopy();
  const formatters = useFormatters();
  const navigate = useNavigate();
  const navigation = useShellNavigation();
  const now = useNow();
  const query = useWorkspaceInbox(workspaceId);
  const departments = useDepartmentsById(workspaceId);
  const writes = useWorkFeedWrites(workspaceId);
  const state = useMemo<SectionState<readonly InboxItemView[]>>(() => {
    const base = sectionState(query, query.data);
    if (base.status !== "ready") return base;
    return {
      status: "ready",
      data: toInboxEntries(base.data.items, {
        copy,
        now,
        departments,
        hrefFor: (target) =>
          target.kind === "item" && target.boardId && target.id
            ? navigation.getHref({
                section: "board",
                boardId: target.boardId,
                item: target.id,
              })
            : target.kind === "doc" && target.id
              ? navigation.getHref({ section: "doc", docId: target.id })
              : null,
        weekday: (iso) => {
          const text = formatters.date(iso, { weekday: "long" });
          return text.charAt(0).toLowerCase() + text.slice(1);
        },
      }),
    };
  }, [copy, departments, formatters, navigation, now, query]);

  const markRead = useCallback(
    (id: string) =>
      void writes.inbox
        .markRead(id)
        .catch((error: unknown) =>
          toastFailure(copy.toasts2.failed, error, copy.toasts2.failedHint),
        ),
    [copy, writes],
  );

  const settingsHref = `/workspaces/${encodeURIComponent(workspaceId)}/account/notifications`;
  const openSettings = useCallback(() => {
    void navigate({
      to: "/workspaces/$workspaceId/account/notifications",
      params: { workspaceId },
    } as never);
  }, [navigate, workspaceId]);

  return (
    <WorkspaceInboxSurface
      copy={copy}
      now={now}
      state={state}
      scope={{
        workspaceName: query.data?.workspace.name || workspace?.name || "",
        allHref: navigation.getHref({ section: "inbox" }),
      }}
      tabsAvailable={{ mention: true }}
      counts={
        query.data
          ? {
              need: query.data.counts.need,
              mention: query.data.counts.mention,
              assigned: query.data.counts.assigned,
            }
          : undefined
      }
      settingsHref={settingsHref}
      onOpenSettings={openSettings}
      onMarkRead={markRead}
      onMarkAllRead={() =>
        void writes.inbox
          .readAll()
          .then(() => toast.success(copy.toasts.allRead))
          .catch(() => toast.error(copy.toasts.markFailed))
      }
      onRetry={() => void query.refetch()}
      loadMore={
        query.hasNextPage
          ? {
              onLoadMore: () => void query.fetchNextPage(),
              loading: query.isFetchingNextPage,
            }
          : null
      }
    />
  );
}
