import type { RealtimeTransport } from "@notrelix/realtime";

/** The realtime transport a host hands to the document page. */
export type DocsRealtime = RealtimeTransport;

export interface DocsCollaborationSubscription {
  dispose(): void;
}

export interface DocsCollaborationAdapter {
  subscribeToDocument(input: {
    workspaceId: string;
    documentId: string;
    onEvent: (event: unknown) => void;
  }): DocsCollaborationSubscription;

  disconnectDocument(input: { workspaceId: string; documentId: string }): void;
}

export function createDocsCollaborationAdapter(
  realtime: RealtimeTransport,
): DocsCollaborationAdapter {
  const activeSubscriptions = new Map<string, DocsCollaborationSubscription>();

  function subscriptionKey(workspaceId: string, documentId: string): string {
    return `${workspaceId}:${documentId}`;
  }

  return {
    subscribeToDocument({ workspaceId, documentId, onEvent }) {
      const key = subscriptionKey(workspaceId, documentId);

      // Disconnect any existing subscription for this document before subscribing.
      const existing = activeSubscriptions.get(key);
      if (existing) {
        existing.dispose();
        activeSubscriptions.delete(key);
      }

      // The gateway has no document scope: listen on the Workspace stream and
      // keep only frames about this document. Frames carry no payload, so the
      // consumer receives the envelope as a change notification.
      const unsubscribe = realtime.subscribe({ workspaceId }, (envelope) => {
        if (envelope.resourceId !== documentId) return;
        onEvent(envelope);
      });

      const subscription: DocsCollaborationSubscription = {
        dispose() {
          unsubscribe();
          activeSubscriptions.delete(key);
        },
      };

      activeSubscriptions.set(key, subscription);
      return subscription;
    },

    disconnectDocument({ workspaceId, documentId }) {
      const key = subscriptionKey(workspaceId, documentId);
      const subscription = activeSubscriptions.get(key);
      if (subscription) {
        subscription.dispose();
      }
    },
  };
}

/**
 * Calls `onChange` whenever the open document may have changed on the server:
 * a frame about it arrived, the gateway asked for recovery (subscription ack,
 * stream gap or reset, permission loss), or the connection came back after a
 * drop. Frames carry no payload, so the consumer refetches authoritative state.
 */
export function watchDocumentChanges(
  realtime: RealtimeTransport,
  input: { workspaceId: string; documentId: string; onChange: () => void },
): () => void {
  const { workspaceId, documentId, onChange } = input;
  const adapter = createDocsCollaborationAdapter(realtime);
  const subscription = adapter.subscribeToDocument({
    workspaceId,
    documentId,
    onEvent: onChange,
  });
  const unsubscribeRecovery = realtime.subscribeRecovery((signal) => {
    if (signal.workspaceId === workspaceId) onChange();
  });
  let dropped = realtime.getState() !== "connected";
  const unsubscribeState = realtime.subscribeState((state) => {
    if (state !== "connected") {
      dropped = true;
      return;
    }
    if (dropped) {
      dropped = false;
      onChange();
    }
  });
  return () => {
    subscription.dispose();
    unsubscribeRecovery();
    unsubscribeState();
  };
}
