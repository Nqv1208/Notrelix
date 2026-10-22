import type { RealtimeTransport } from "@notrelix/realtime";

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
