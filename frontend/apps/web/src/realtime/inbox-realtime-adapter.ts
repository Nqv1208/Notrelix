import type {
  ModuleRealtimeAdapter,
  RealtimeEnvelope,
} from "@notrelix/realtime";
import {
  inboxQueryKey,
  inboxSummaryQueryKey,
} from "../shell/workspace/work-feed-bridge";

/** The fact the backend sends on a person's own stream when their Inbox changed in one workspace (UC-COL-E02). */
export const INBOX_CHANGED_EVENT = "inbox.changed";

/**
 * Inbox realtime adapter (UC-COL-E02). A frame says only that the person's
 * Inbox changed in a workspace: the adapter never patches a cache, it marks
 * the badge counts stale (every workspace's, one account-wide read) and, when
 * the frame is about the open workspace, its Inbox lists. The mounted reads
 * refetch over REST, where the recipient check runs again.
 *
 * A duplicate or out-of-order frame costs one extra refetch and never
 * overwrites newer state; a frame of another workspace leaves the open
 * workspace's lists alone (NRX-014).
 */
export function createInboxRealtimeAdapter(
  accountId: string | null,
): ModuleRealtimeAdapter {
  return {
    id: "inbox-adapter",
    supports(envelope: RealtimeEnvelope): boolean {
      return envelope.eventType === INBOX_CHANGED_EVENT;
    },
    async validateAndHandle(envelope, context): Promise<void> {
      if (!accountId) return;
      const keys: unknown[][] = [[...inboxSummaryQueryKey(accountId)]];
      if (envelope.workspaceId === context.workspaceId)
        keys.push([...inboxQueryKey(context.workspaceId)]);
      await context.invalidateQueries(keys);
    },
  };
}
