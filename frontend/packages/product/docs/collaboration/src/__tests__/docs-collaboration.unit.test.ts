import { describe, it, expect, vi } from "vitest";
import { createDocsCollaborationAdapter } from "../docs-collaboration";
import type { RealtimeTransport } from "@notrelix/realtime";

describe("createDocsCollaborationAdapter", () => {
  it("subscribes and disposes document channel events", () => {
    const unsubscribeMock = vi.fn();
    let handler: ((env: any) => void) | undefined;

    const mockRealtime: RealtimeTransport = {
      connect: vi.fn(),
      disconnect: vi.fn(),
      subscribe: vi.fn((_filter, cb) => {
        handler = cb;
        return unsubscribeMock;
      }),
      subscribeState: vi.fn(),
      subscribeRecovery: vi.fn(),
      getState: vi.fn().mockReturnValue("connected"),
      dispose: vi.fn(),
    };

    const adapter = createDocsCollaborationAdapter(mockRealtime);
    const onEvent = vi.fn();

    const sub = adapter.subscribeToDocument({
      workspaceId: "ws-1",
      documentId: "doc-1",
      onEvent,
    });

    expect(mockRealtime.subscribe).toHaveBeenCalledWith(
      {
        workspaceId: "ws-1",
        subscriptionId: "doc-1",
      },
      expect.any(Function),
    );

    handler?.({ payload: { type: "user-joined", userId: "u-1" } });
    expect(onEvent).toHaveBeenCalledWith({
      type: "user-joined",
      userId: "u-1",
    });

    sub.dispose();
    expect(unsubscribeMock).toHaveBeenCalled();
  });
});
