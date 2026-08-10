import { describe, it, expect, vi } from "vitest";
import { createDocsCollaborationAdapter } from "../docs-collaboration";
import type { RealtimeTransport } from "@notrelix/realtime";

function makeMockRealtime() {
  const unsubscribeMock = vi.fn();
  let capturedHandler: ((env: unknown) => void) | undefined;

  const mockRealtime: RealtimeTransport = {
    connect: vi.fn(),
    disconnect: vi.fn(),
    subscribe: vi.fn((_filter, cb) => {
      capturedHandler = cb;
      return unsubscribeMock;
    }),
    subscribeState: vi.fn(),
    subscribeRecovery: vi.fn(),
    getState: vi.fn().mockReturnValue("connected"),
    dispose: vi.fn(),
  };

  return { mockRealtime, unsubscribeMock, getHandler: () => capturedHandler };
}

describe("createDocsCollaborationAdapter", () => {
  it("subscribes and disposes document channel events", () => {
    const { mockRealtime, unsubscribeMock, getHandler } = makeMockRealtime();
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

    getHandler()?.({ payload: { type: "user-joined", userId: "u-1" } });
    expect(onEvent).toHaveBeenCalledWith({
      type: "user-joined",
      userId: "u-1",
    });

    sub.dispose();
    expect(unsubscribeMock).toHaveBeenCalled();
  });

  it("disconnectDocument disposes the active subscription for the given document", () => {
    const { mockRealtime, unsubscribeMock } = makeMockRealtime();
    const adapter = createDocsCollaborationAdapter(mockRealtime);
    const onEvent = vi.fn();

    adapter.subscribeToDocument({
      workspaceId: "ws-1",
      documentId: "doc-2",
      onEvent,
    });

    expect(unsubscribeMock).not.toHaveBeenCalled();

    adapter.disconnectDocument({ workspaceId: "ws-1", documentId: "doc-2" });

    expect(unsubscribeMock).toHaveBeenCalledTimes(1);
  });

  it("disconnectDocument is a no-op for a document that has no active subscription", () => {
    const { mockRealtime, unsubscribeMock } = makeMockRealtime();
    const adapter = createDocsCollaborationAdapter(mockRealtime);

    // Should not throw and should not call unsubscribe
    adapter.disconnectDocument({ workspaceId: "ws-99", documentId: "doc-99" });
    expect(unsubscribeMock).not.toHaveBeenCalled();
  });

  it("re-subscribing to the same document disposes the previous subscription first", () => {
    const unsubscribe1 = vi.fn();
    const unsubscribe2 = vi.fn();
    let callCount = 0;

    const mockRealtime: RealtimeTransport = {
      connect: vi.fn(),
      disconnect: vi.fn(),
      subscribe: vi.fn(() => (callCount++ === 0 ? unsubscribe1 : unsubscribe2)),
      subscribeState: vi.fn(),
      subscribeRecovery: vi.fn(),
      getState: vi.fn().mockReturnValue("connected"),
      dispose: vi.fn(),
    };

    const adapter = createDocsCollaborationAdapter(mockRealtime);

    adapter.subscribeToDocument({
      workspaceId: "ws-1",
      documentId: "doc-3",
      onEvent: vi.fn(),
    });
    expect(unsubscribe1).not.toHaveBeenCalled();

    // Re-subscribe — old sub must be disposed first
    adapter.subscribeToDocument({
      workspaceId: "ws-1",
      documentId: "doc-3",
      onEvent: vi.fn(),
    });
    expect(unsubscribe1).toHaveBeenCalledTimes(1);
  });
});
