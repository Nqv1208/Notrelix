import { beforeEach, describe, expect, test } from "vitest";
import { PluginRegistry, type BoardPlugin } from "../registry/plugin-registry";

describe("PluginRegistry", () => {
  let registry: PluginRegistry;

  beforeEach(() => {
    registry = PluginRegistry.getInstance();
    registry.clear();
  });

  test("registers and retrieves a plugin", () => {
    const plugin: BoardPlugin = {
      id: "custom-number-formatter",
      name: "Custom Number Formatter",
      version: "1.0.0",
    };

    registry.registerPlugin(plugin);
    expect(registry.getPlugin("custom-number-formatter")).toBe(plugin);
    expect(registry.listPlugins()).toHaveLength(1);
  });

  test("prevents duplicate plugin registration", () => {
    const plugin: BoardPlugin = {
      id: "duplicate-plugin",
      name: "Duplicate Plugin",
      version: "1.0.0",
    };

    registry.registerPlugin(plugin);
    expect(() => registry.registerPlugin(plugin)).toThrowError(
      'Plugin with id "duplicate-plugin" is already registered.',
    );
  });
});
