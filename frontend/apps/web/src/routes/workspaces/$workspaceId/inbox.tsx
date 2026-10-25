import { useCallback, useMemo, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import {
  formatCopy,
  useWorkspaceInboxCopy,
  WorkspaceInboxSurface,
  type InboxActions,
  type InboxItemView,
  type SectionState,
} from "@notrelix/features-workspace/web";
import { useFormatters } from "@notrelix/i18n";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { toastFailure } from "@/shell/workspace/failure-toast";
import { useDepartmentsById } from "@/shell/workspace/home-feed-view";
import { snoozeUntil, toInboxEntries } from "@/shell/workspace/inbox-view";
import { useNow } from "@/shell/workspace/use-now";
import { useShellNavigation } from "@/shell/workspace/use-shell-navigation";
import {
  useWorkFeedWrites,
  useWorkspaceInbox,
} from "@/shell/workspace/work-feed-bridge";
import { sectionState } from "@/shell/workspace/section-state";

/**
 * Inbox (Final-Inbox): the member's own notifications in the workspace with
 * the selected entry in the reading pane. The member marks one or all of them
 * read, marks one done (the Done filter lists those), snoozes it until a time,
 * mutes or unmutes its thread and replies on its item. Approving has no
 * contract here yet, so those buttons stay inert.
 */
export function InboxPage() {
  const { workspaceId, workspace } = useWorkspaceContext();
  const copy = useWorkspaceInboxCopy();
  const formatters = useFormatters();
  const navigate = useNavigate();
  const navigation = useShellNavigation();
  const now = useNow();
  const [doneView, setDoneView] = useState(false);
  const query = useWorkspaceInbox(workspaceId, doneView ? "done" : "inbox");
  const [pending, setPending] = useState<ReadonlySet<string>>(new Set());
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

  const act = useCallback(
    (id: string, job: () => Promise<void>, success: string) => {
      setPending((previous) => new Set(previous).add(id));
      void job()
        .then(() => toast.success(success))
        .catch((error: unknown) =>
          toastFailure(copy.toasts2.failed, error, copy.toasts2.failedHint),
        )
        .finally(() =>
          setPending((previous) => {
            const next = new Set(previous);
            next.delete(id);
            return next;
          }),
        );
    },
    [copy],
  );

  const actions = useMemo<InboxActions>(
    () => ({
      pendingIds: pending,
      // Approving has no contract here yet.
      onApprove: () => undefined,
      onRequestChanges: () => undefined,
      onReply: (item, text) => {
        if (!item.itemId) return;
        const itemId = item.itemId;
        act(
          item.id,
          () => writes.inbox.reply(itemId, text),
          formatCopy(copy.toasts2.replied, { name: item.person?.name ?? "" }),
        );
      },
      onHandled: (item) =>
        act(
          item.id,
          () => writes.inbox.markRead(item.id),
          copy.toasts2.handled,
        ),
      onDone: (item) =>
        act(item.id, () => writes.inbox.done(item.id), copy.toasts2.done),
      onReopen: (item) =>
        act(item.id, () => writes.inbox.reopen(item.id), copy.toasts2.reopened),
      onSnooze: (item, option) =>
        act(
          item.id,
          () => writes.inbox.snooze(item.id, snoozeUntil(option, now)),
          formatCopy(copy.toasts2.snoozed, {
            when: copy.read.snoozeOptions[option].toLowerCase(),
          }),
        ),
      onMute: (item) =>
        act(
          item.id,
          () => writes.inbox.mute(item.id, item.muted !== true),
          item.muted === true ? copy.toasts2.unmuted : copy.toasts2.muted,
        ),
    }),
    [act, copy, now, pending, writes],
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
      tabsAvailable={{ need: true, mention: true }}
      done={{
        active: doneView,
        onToggle: () => setDoneView((value) => !value),
      }}
      actions={actions}
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
