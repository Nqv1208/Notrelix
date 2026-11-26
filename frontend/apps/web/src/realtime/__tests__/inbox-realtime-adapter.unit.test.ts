import { describe, expect, it, vi } from "vitest";
import type { RealtimeEnvelope } from "@notrelix/realtime";
import {
  inboxQueryKey,
  inboxSummaryQueryKey,
} from "../../shell/workspace/work-feed-bridge";
import {
  createInboxRealtimeAdapter,
  INBOX_CHANGED_EVENT,
} from "../inbox-realtime-adapter";

const envelope = (over: Partial<RealtimeEnvelope> = {}): RealtimeEnvelope =>
  ({
    schemaVersion: 1,
    eventId: "evt-1",
    eventType: INBOX_CHANGED_EVENT,
    changeKind: INBOX_CHANGED_EVENT,
    subscriptionId: "user:me",
    streamKey: "user:notifications:me",
    streamVersion: 1,
    resourceKind: "notifications",
    resourceId: "me",
    aggregateId: "me",
    workspaceId: "ws-a",
    ...over,
  }) as RealtimeEnvelope;

describe("inbox realtime adapter (UC-COL-E02)", () => {
  it("takes only the Inbox fact", () => {
    const adapter = createInboxRealtimeAdapter("me");
    expect(adapter.supports(envelope())).toBe(true);
    expect(
      adapter.supports(envelope({ eventType: "chat.unread.changed" })),
    ).toBe(false);
    expect(adapter.supports(envelope({ eventType: "board.board" }))).toBe(
      false,
    );
  });

  it("reads the badge and the open workspace's Inbox again when that Inbox changed", async () => {
    const invalidateQueries = vi.fn();
    await createInboxRealtimeAdapter("me").validateAndHandle(envelope(), {
      workspaceId: "ws-a",
      invalidateQueries,
    });
    expect(invalidateQueries).toHaveBeenCalledWith([
      [...inboxSummaryQueryKey("me")],
      [...inboxQueryKey("ws-a")],
    ]);
  });

  it("only refreshes the badge for another workspace's Inbox, never the open one's list", async () => {
    const invalidateQueries = vi.fn();
    await createInboxRealtimeAdapter("me").validateAndHandle(
      envelope({ workspaceId: "ws-b" }),
      { workspaceId: "ws-a", invalidateQueries },
    );
    expect(invalidateQueries).toHaveBeenCalledWith([
      [...inboxSummaryQueryKey("me")],
    ]);
  });

  it("does nothing without a signed-in person", async () => {
    const invalidateQueries = vi.fn();
    await createInboxRealtimeAdapter(null).validateAndHandle(envelope(), {
      workspaceId: "ws-a",
      invalidateQueries,
    });
    expect(invalidateQueries).not.toHaveBeenCalled();
  });
});
