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
  return {
    subscribeToDocument({ workspaceId, documentId, onEvent }) {
      const unsubscribe = realtime.subscribe(
        {
          workspaceId,
          subscriptionId: documentId,
        },
        (envelope) => {
          onEvent(envelope.payload);
        },
      );

      return {
        dispose() {
          unsubscribe();
        },
      };
    },

    disconnectDocument() {
      // Subscription lifecycle is managed via dispose() on DocsCollaborationSubscription.
    },
  };
}
