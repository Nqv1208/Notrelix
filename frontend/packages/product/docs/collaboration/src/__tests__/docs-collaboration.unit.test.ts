import { describe, it, expect, vi } from "vitest";
import {
  createDocsCollaborationAdapter,
  watchDocumentChanges,
} from "../docs-collaboration";
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
      { workspaceId: "ws-1" },
      expect.any(Function),
    );

    const forDocument = { eventId: "evt-1", resourceId: "doc-1" };
    getHandler()?.(forDocument);
    expect(onEvent).toHaveBeenCalledWith(forDocument);

    // Frames about other resources of the Workspace stream are not delivered.
    getHandler()?.({ eventId: "evt-2", resourceId: "doc-other" });
    expect(onEvent).toHaveBeenCalledTimes(1);

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

describe("watchDocumentChanges", () => {
  function makeRealtime(initial: string) {
    let onEvent: ((env: unknown) => void) | undefined;
    let onRecovery: ((signal: unknown) => void) | undefined;
    let onState: ((state: string) => void) | undefined;
    const disposers = [vi.fn(), vi.fn(), vi.fn()];
    const realtime = {
      subscribe: vi.fn((_filter, cb) => {
        onEvent = cb;
        return disposers[0];
      }),
      subscribeRecovery: vi.fn((cb) => {
        onRecovery = cb;
        return disposers[1];
      }),
      subscribeState: vi.fn((cb) => {
        onState = cb;
        return disposers[2];
      }),
      getState: vi.fn().mockReturnValue(initial),
    } as unknown as RealtimeTransport;
    return {
      realtime,
      disposers,
      event: (env: unknown) => onEvent?.(env),
      recovery: (signal: unknown) => onRecovery?.(signal),
      state: (state: string) => onState?.(state),
    };
  }

  it("reports a frame about the document, a recovery of the workspace and a reconnect", () => {
    const { realtime, event, recovery, state, disposers } =
      makeRealtime("connected");
    const onChange = vi.fn();
    const stop = watchDocumentChanges(realtime, {
      workspaceId: "ws-1",
      documentId: "doc-1",
      onChange,
    });

    event({ resourceId: "doc-other" });
    expect(onChange).not.toHaveBeenCalled();
    event({ resourceId: "doc-1" });
    expect(onChange).toHaveBeenCalledTimes(1);

    recovery({ workspaceId: "ws-other" });
    expect(onChange).toHaveBeenCalledTimes(1);
    recovery({ workspaceId: "ws-1" });
    expect(onChange).toHaveBeenCalledTimes(2);

    state("connected");
    expect(onChange).toHaveBeenCalledTimes(2);
    state("reconnecting");
    state("connected");
    expect(onChange).toHaveBeenCalledTimes(3);

    stop();
    for (const dispose of disposers) expect(dispose).toHaveBeenCalled();
  });

  it("refreshes when the first connection completes after the page opened offline", () => {
    const { realtime, state } = makeRealtime("connecting");
    const onChange = vi.fn();
    watchDocumentChanges(realtime, {
      workspaceId: "ws-1",
      documentId: "doc-1",
      onChange,
    });
    state("connected");
    expect(onChange).toHaveBeenCalledTimes(1);
  });
});
