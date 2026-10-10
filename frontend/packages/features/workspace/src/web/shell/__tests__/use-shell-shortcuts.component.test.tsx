import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, renderHook } from "@notrelix/testing";
import {
  commandShortcutLabel,
  useShellShortcuts,
} from "../use-shell-shortcuts";

describe("useShellShortcuts (WX-SHELL-005)", () => {
  it("runs G-sequences, ? and ⌘K from one window registration", () => {
    const onShortcut = vi.fn();
    const add = vi.spyOn(window, "addEventListener");
    renderHook(() => useShellShortcuts(onShortcut));
    expect(add.mock.calls.filter(([type]) => type === "keydown")).toHaveLength(
      1,
    );

    fireEvent.keyDown(window, { key: "g" });
    fireEvent.keyDown(window, { key: "h" });
    fireEvent.keyDown(window, { key: "g" });
    fireEvent.keyDown(window, { key: "m" });
    fireEvent.keyDown(window, { key: "g" });
    fireEvent.keyDown(window, { key: "i" });
    fireEvent.keyDown(window, { key: "?" });
    fireEvent.keyDown(window, { key: "k", metaKey: true });
    fireEvent.keyDown(window, { key: "k", ctrlKey: true });

    expect(onShortcut.mock.calls.map(([id]) => id)).toEqual([
      "go-home",
      "go-my-work",
      "go-inbox",
      "show-shortcuts",
      "open-palette",
      "open-palette",
    ]);
  });

  it("skips sequences while typing in a field but still opens the palette", () => {
    const onShortcut = vi.fn();
    renderHook(() => useShellShortcuts(onShortcut));
    const { getByRole } = render(<input aria-label="Name" />);
    const input = getByRole("textbox");

    fireEvent.keyDown(input, { key: "g" });
    fireEvent.keyDown(input, { key: "h" });
    fireEvent.keyDown(input, { key: "?" });
    expect(onShortcut).not.toHaveBeenCalled();

    fireEvent.keyDown(input, { key: "k", metaKey: true });
    expect(onShortcut).toHaveBeenCalledWith("open-palette");
  });

  it("can be disabled while another dialog owns the keyboard", () => {
    const onShortcut = vi.fn();
    renderHook(() => useShellShortcuts(onShortcut, { enabled: false }));
    fireEvent.keyDown(window, { key: "?" });
    expect(onShortcut).not.toHaveBeenCalled();
  });

  it("labels the palette chord per platform", () => {
    expect(commandShortcutLabel("MacIntel")).toBe("⌘K");
    expect(commandShortcutLabel("Win32")).toBe("Ctrl K");
    expect(commandShortcutLabel(undefined)).toBe("Ctrl K");
  });
});
