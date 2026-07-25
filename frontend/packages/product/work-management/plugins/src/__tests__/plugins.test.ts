import { describe, it, expect } from 'vitest';

type ViewType = 'table' | 'kanban' | 'calendar' | 'timeline';

interface ViewPlugin {
  type: ViewType;
  name: string;
}

class PluginRegistry {
  private plugins = new Map<ViewType, ViewPlugin>();

  register(plugin: ViewPlugin) {
    this.plugins.set(plugin.type, plugin);
  }

  get(type: ViewType): ViewPlugin | undefined {
    return this.plugins.get(type);
  }
}

describe('Work Management Plugin Registry Invariants', () => {
  it('registers and retrieves view plugins', () => {
    const registry = new PluginRegistry();
    registry.register({ type: 'kanban', name: 'Kanban View Plugin' });

    expect(registry.get('kanban')).toEqual({ type: 'kanban', name: 'Kanban View Plugin' });
    expect(registry.get('table')).toBeUndefined();
  });
});
