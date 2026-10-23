export interface RealtimeEnvelopeLike {
  readonly eventType: string;
  readonly workspaceId: string;
}

export interface ModuleRealtimeContextLike {
  readonly workspaceId: string;
  readonly invalidateQueries: (
    keys: readonly unknown[][],
  ) => Promise<void> | void;
}

export const workspaceRealtimeAdapter = {
  id: "workspace-adapter",
  supports(envelope: RealtimeEnvelopeLike): boolean {
    return envelope.eventType.startsWith("workspace.");
  },
  async validateAndHandle(
    envelope: RealtimeEnvelopeLike,
    context: ModuleRealtimeContextLike,
  ): Promise<void> {
    if (envelope.workspaceId === context.workspaceId) {
      // Frames carry no payload: revalidate the Workspace-scoped queries
      // (root of `workspaceQueryKey(workspaceId, ...)`) instead of patching.
      await context.invalidateQueries([["workspace", context.workspaceId]]);
    }
  },
};
