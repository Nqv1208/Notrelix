import type { RealtimeEnvelope, ModuleRealtimeAdapter, ModuleRealtimeContext } from '@notrelix/realtime';

export class ModuleAdapterRegistry {
  private readonly adapters: ModuleRealtimeAdapter[] = [];

  public register(adapter: ModuleRealtimeAdapter): void {
    if (!this.adapters.some((a) => a.id === adapter.id)) {
      this.adapters.push(adapter);
    }
  }

  public async dispatch(
    envelope: RealtimeEnvelope<unknown>,
    context: ModuleRealtimeContext
  ): Promise<void> {
    const supportingAdapters = this.adapters.filter((a) => a.supports(envelope));
    for (const adapter of supportingAdapters) {
      await adapter.validateAndHandle(envelope, context);
    }
  }
}
