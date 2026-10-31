import type { RealtimeEnvelope } from "./envelope";

export interface ModuleRealtimeContext {
  readonly workspaceId: string;
  /** Set when the frame arrived through a board subscription: the open board. */
  readonly boardId?: string;
  readonly invalidateQueries: (
    queryKeys: readonly unknown[][],
  ) => Promise<void> | void;
}

/**
 * Product/feature adapters interpret an event frame as an invalidation signal
 * only: frames carry no payload, so adapters must not patch caches.
 */
export interface ModuleRealtimeAdapter {
  readonly id: string;
  supports(envelope: RealtimeEnvelope): boolean;
  validateAndHandle(
    envelope: RealtimeEnvelope,
    context: ModuleRealtimeContext,
  ): Promise<void> | void;
}
