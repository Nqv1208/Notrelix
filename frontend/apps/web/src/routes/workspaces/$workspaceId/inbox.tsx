import { useCallback, useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import {
  createUseMarkRead,
  createUseNotifications,
} from "@notrelix/features-notifications";
import {
  useWorkspaceInboxCopy,
  WorkspaceInboxSurface,
  type InboxItemView,
  type SectionState,
} from "@notrelix/features-workspace/web";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import {
  toInboxItems,
  type InboxNotification,
} from "@/shell/workspace/inbox-view";
import { useNow } from "@/shell/workspace/use-now";

/**
 * Inbox (WX-SHELL-001, Final-Inbox): the member's notifications by day with
 * the selected one in the reading pane. The read is the notification list
 * (title, text, read flag, time): it is not scoped to a workspace and has no
 * type, person or item, so the page says nothing about scope and draws only
 * the All tab and the Mentions and Assigned tabs, which the notification
 * `type` filters; "Đã xử lý" and "Đánh dấu tất cả đã đọc" are the real mark-read
 * writes. Snooze, mute, approvals and replies have no producer and are drawn
 * inert with the reason.
 */
export function InboxPage() {
  const { workspaceId } = useWorkspaceContext();
  const copy = useWorkspaceInboxCopy();
  const navigate = useNavigate();
  const now = useNow();
  const { api: runtimeClient } = useAppRuntime();

  const hooks = useMemo(() => {
    const deps = { api: runtimeClient.api, endpoints: runtimeClient.endpoints };
    return {
      useNotifications: createUseNotifications(deps),
      useMarkRead: createUseMarkRead(deps),
    };
  }, [runtimeClient]);

  const query = hooks.useNotifications();
  const { markRead, markAllRead } = hooks.useMarkRead();

  const state = useMemo<SectionState<readonly InboxItemView[]>>(() => {
    if (query.isError) return { status: "error" };
    if (!query.data) return { status: "loading" };
    return {
      status: "ready",
      data: toInboxItems(
        query.data as unknown as readonly InboxNotification[],
        copy,
        now,
      ),
    };
  }, [copy, now, query.data, query.isError]);

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
      scope={null}
      tabsAvailable={{ mention: true, assigned: true }}
      settingsHref={settingsHref}
      onOpenSettings={openSettings}
      onMarkRead={(id) =>
        markRead(id, {
          onError: () => toast.error(copy.toasts.markFailed),
        })
      }
      onMarkAllRead={() =>
        markAllRead(undefined, {
          onSuccess: () => toast.success(copy.toasts.allRead),
          onError: () => toast.error(copy.toasts.markFailed),
        })
      }
      onRetry={() => void query.refetch()}
    />
  );
}
