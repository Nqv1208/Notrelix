export interface BoardPlugin {
  readonly id: string;
  readonly name: string;
  readonly version: string;
  readonly description?: string;
  readonly initialize?: (context: unknown) => void;
  readonly renderCell?: (props: unknown) => unknown;
}

export class PluginRegistry {
  private static instance: PluginRegistry | null = null;
  private plugins = new Map<string, BoardPlugin>();

  public static getInstance(): PluginRegistry {
    if (!PluginRegistry.instance) {
      PluginRegistry.instance = new PluginRegistry();
    }
    return PluginRegistry.instance;
  }

  public registerPlugin(plugin: BoardPlugin): void {
    if (this.plugins.has(plugin.id)) {
      throw new Error(`Plugin with id "${plugin.id}" is already registered.`);
    }
    this.plugins.set(plugin.id, plugin);
  }

  public getPlugin(id: string): BoardPlugin | undefined {
    return this.plugins.get(id);
  }

  public listPlugins(): ReadonlyArray<BoardPlugin> {
    return Array.from(this.plugins.values());
  }

  public clear(): void {
    this.plugins.clear();
  }
}
