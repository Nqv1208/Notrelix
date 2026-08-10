import type {
  RealtimeEnvelope,
  ModuleRealtimeAdapter,
  ModuleRealtimeContext,
} from "@notrelix/realtime";

export class ModuleAdapterRegistry {
  private readonly adapters: ModuleRealtimeAdapter[] = [];

  public register(adapter: ModuleRealtimeAdapter): void {
    if (!this.adapters.some((a) => a.id === adapter.id)) {
      this.adapters.push(adapter);
    }
  }

  public async dispatch(
    envelope: RealtimeEnvelope<unknown>,
    context: ModuleRealtimeContext,
  ): Promise<{ handled: boolean; adapterIds: readonly string[] }> {
    const supportingAdapters = this.adapters.filter((a) =>
      a.supports(envelope),
    );
    const adapterIds = supportingAdapters.map((adapter) => adapter.id);
    for (const adapter of supportingAdapters) {
      await adapter.validateAndHandle(envelope, context);
    }
    return { handled: supportingAdapters.length > 0, adapterIds };
  }
}
