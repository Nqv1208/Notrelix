import { useCallback, useMemo, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import {
  formatCopy,
  useWorkspaceInboxCopy,
  WorkspaceInboxSurface,
  type InboxActions,
  type InboxItemView,
  type InboxSnooze,
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
  type InboxEntryDto,
} from "@/shell/workspace/work-feed-bridge";
import { sectionState } from "@/shell/workspace/section-state";

const HOUR = 60 * 60 * 1000;

/** When a snooze option brings the notification back (local time). */
function snoozeUntil(option: InboxSnooze, now: number): Date {
  if (option === "hour") return new Date(now + HOUR);
  const date = new Date(now);
  date.setHours(8, 0, 0, 0);
  if (option === "tomorrow") {
    date.setDate(date.getDate() + 1);
  } else {
    // The next Monday.
    date.setDate(date.getDate() + ((8 - date.getDay()) % 7 || 7));
  }
  return date;
}

/**
 * Inbox (Final-Inbox): the member's workspace inbox with its tabs, the
 * selected entry in the reading pane, and the writes the design draws:
 * approve, request changes, reply, handled, snooze, mute. The read and the
 * writes are the work feed's gap (`CTR-GAP-WS-INBOX`, work-feed-bridge.ts).
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
  const [pendingIds, setPendingIds] = useState<ReadonlySet<string>>(
    () => new Set(),
  );

  const dtoById = useMemo(
    () => new Map((query.data?.items ?? []).map((entry) => [entry.id, entry])),
    [query.data],
  );

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

  const run = useCallback(
    async (item: InboxItemView, job: () => Promise<void>, success: string) => {
      setPendingIds((ids) => new Set(ids).add(item.id));
      try {
        await job();
        toast.success(success);
      } catch (error) {
        toastFailure(copy.toasts2.failed, error, copy.toasts2.failedHint);
      } finally {
        setPendingIds((ids) => {
          const next = new Set(ids);
          next.delete(item.id);
          return next;
        });
      }
    },
    [copy],
  );

  const actions = useMemo<InboxActions>(() => {
    const dto = (item: InboxItemView): InboxEntryDto | undefined =>
      dtoById.get(item.id);
    const label = (item: InboxItemView) =>
      dto(item)?.target.key ?? dto(item)?.target.title ?? "";
    const decide = (
      item: InboxItemView,
      decision: "approve" | "request-changes",
    ) => {
      const target = dto(item)?.target;
      if (!target?.id || !target.boardId) return;
      void run(
        item,
        () =>
          writes.decideApproval({
            itemId: target.id!,
            boardId: target.boardId!,
            decision,
          }),
        formatCopy(
          decision === "approve"
            ? copy.toasts2.approved
            : copy.toasts2.changesRequested,
          { name: label(item) },
        ),
      );
    };
    return {
      pendingIds,
      onApprove: (item) => decide(item, "approve"),
      onRequestChanges: (item) => decide(item, "request-changes"),
      onReply: (item, text) =>
        void run(
          item,
          () => writes.inbox.reply(item.id, text),
          formatCopy(copy.toasts2.replied, {
            name: item.detail?.replyTo ?? "",
          }),
        ),
      onHandled: (item) =>
        void run(
          item,
          () => writes.inbox.handled(item.id),
          copy.toasts2.handled,
        ),
      onSnooze: (item, option) => {
        const until = snoozeUntil(option, now);
        void run(
          item,
          () => writes.inbox.snooze(item.id, until.toISOString()),
          formatCopy(copy.toasts2.snoozed, {
            when: copy.read.snoozeOptions[option].toLowerCase(),
          }),
        );
      },
      onMute: (item) =>
        void run(item, () => writes.inbox.mute(item.id), copy.toasts2.muted),
    };
  }, [copy, dtoById, now, pendingIds, run, writes]);

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
      tabsAvailable={{
        need: true,
        mention: true,
        assigned: true,
        follow: true,
      }}
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
      onMarkRead={null}
      onMarkAllRead={() =>
        void writes.inbox
          .readAll()
          .then(() => toast.success(copy.toasts.allRead))
          .catch(() => toast.error(copy.toasts.markFailed))
      }
      actions={actions}
      onRetry={() => void query.refetch()}
    />
  );
}
