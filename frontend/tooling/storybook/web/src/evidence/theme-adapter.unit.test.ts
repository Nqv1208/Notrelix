// @vitest-environment jsdom
import { describe, expect, it } from "vitest";
import { applyFuiTheme } from "../../.storybook/theme-adapter";

function createRoot(): HTMLElement {
  const root = document.createElement("html");
  document.documentElement.replaceWith(root);
  return root;
}

describe("Storybook theme adapter (FUIR-WU-051)", () => {
  it("applies the Web root class contract for light and dark (TST-092)", () => {
    const root = createRoot();
    applyFuiTheme("?id=x--default&fui-theme=light", root);
    expect(root.classList.contains("light")).toBe(true);
    expect(root.classList.contains("dark")).toBe(false);
    expect(root.dataset.fuiTheme).toBe("light");

    applyFuiTheme("?id=x--default&fui-theme=dark", root);
    expect(root.classList.contains("light")).toBe(false);
    expect(root.classList.contains("dark")).toBe(true);
    expect(root.dataset.fuiTheme).toBe("dark");
  });

  it("does not use backgrounds alone and rejects unknown themes (TST-093)", () => {
    const root = createRoot();
    root.classList.add("light");
    applyFuiTheme("?id=x--default&fui-theme=midnight", root);
    expect(root.classList.contains("light")).toBe(true);
    expect(root.dataset.fuiTheme).toBeUndefined();

    applyFuiTheme("?id=x--default", root);
    expect(root.classList.contains("light")).toBe(true);
    expect(root.dataset.fuiTheme).toBeUndefined();
  });
});
