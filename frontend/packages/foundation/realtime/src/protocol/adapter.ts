import type { RealtimeEnvelope } from "./envelope";

export interface ModuleRealtimeContext {
  readonly workspaceId: string;
  readonly invalidateQueries: (
    queryKeys: readonly unknown[][],
  ) => Promise<void> | void;
}

export interface ModuleRealtimeAdapter {
  readonly id: string;
  supports(envelope: RealtimeEnvelope<unknown>): boolean;
  validateAndHandle(
    envelope: RealtimeEnvelope<unknown>,
    context: ModuleRealtimeContext,
  ): Promise<void> | void;
}
