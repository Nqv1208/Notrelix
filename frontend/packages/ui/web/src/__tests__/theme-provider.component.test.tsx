import {
  act,
  fireEvent,
  render,
  renderHook,
  screen,
} from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { ThemePicker, type ThemePickerLabels } from "../theme/theme-picker";
import { ThemeProvider, useTheme } from "../theme/theme-provider";
import {
  THEME_ACCENTS,
  THEME_MODES,
  THEME_STORAGE_KEYS,
  type KeyValueStorage,
} from "../theme/theme-model";

function memoryStorage(initial: Record<string, string> = {}) {
  const data = new Map(Object.entries(initial));
  const storage: KeyValueStorage = {
    getItem: (key) => data.get(key) ?? null,
    setItem: (key, value) => void data.set(key, value),
    removeItem: (key) => void data.delete(key),
  };
  return { storage, data };
}

/** Controllable prefers-color-scheme media query. */
function fakeMedia(initialDark: boolean) {
  const listeners = new Set<(event: MediaQueryListEvent) => void>();
  const query = {
    matches: initialDark,
    addEventListener: (_: string, listener: (e: MediaQueryListEvent) => void) =>
      listeners.add(listener),
    removeEventListener: (
      _: string,
      listener: (e: MediaQueryListEvent) => void,
    ) => listeners.delete(listener),
  };
  return {
    matchMedia: () => query as unknown as MediaQueryList,
    setDark(dark: boolean) {
      query.matches = dark;
      for (const listener of listeners) {
        listener({ matches: dark } as MediaQueryListEvent);
      }
    },
    listenerCount: () => listeners.size,
  };
}

function setup(
  options: {
    storage?: KeyValueStorage;
    dark?: boolean;
  } = {},
) {
  const target = document.createElement("div");
  const media = fakeMedia(options.dark ?? false);
  const wrapper = ({ children }: { children: ReactNode }) => (
    <ThemeProvider
      storage={options.storage}
      target={target}
      matchMedia={media.matchMedia}
    >
      {children}
    </ThemeProvider>
  );
  const hook = renderHook(() => useTheme(), { wrapper });
  return { target, media, hook };
}

describe("ThemeProvider", () => {
  it("applies the light/blue default to the target element", () => {
    const { target, hook } = setup();
    expect(hook.result.current.mode).toBe("light");
    expect(hook.result.current.accent).toBe("blue");
    expect(hook.result.current.appearance).toBe("light");
    expect(target.classList.contains("light")).toBe(true);
    expect(target.getAttribute("data-accent")).toBe("blue");
    expect(target.hasAttribute("data-contrast")).toBe(false);
  });

  it("exposes both axes independently", () => {
    const { hook } = setup();
    expect(hook.result.current.modes).toEqual(THEME_MODES);
    expect(hook.result.current.accents).toEqual(THEME_ACCENTS);
  });

  it("changes mode without touching accent, and persists both", () => {
    const { storage, data } = memoryStorage();
    const { target, hook } = setup({ storage });
    act(() => hook.result.current.setAccent("coral"));
    act(() => hook.result.current.setMode("dark"));
    expect(hook.result.current.accent).toBe("coral");
    expect(target.classList.contains("dark")).toBe(true);
    expect(target.classList.contains("light")).toBe(false);
    expect(target.getAttribute("data-accent")).toBe("coral");
    expect(data.get(THEME_STORAGE_KEYS.mode)).toBe("dark");
    expect(data.get(THEME_STORAGE_KEYS.accent)).toBe("coral");
  });

  it("applies high contrast as a complete mode", () => {
    const { target, hook } = setup();
    act(() => hook.result.current.setMode("high-contrast"));
    expect(hook.result.current.appearance).toBe("high-contrast");
    expect(target.getAttribute("data-contrast")).toBe("high");
    expect(target.classList.contains("light")).toBe(true);
  });

  it("follows OS changes only while mode is system (FE-UI-012)", () => {
    const { target, media, hook } = setup({ dark: false });
    act(() => hook.result.current.setMode("system"));
    expect(hook.result.current.appearance).toBe("light");
    act(() => media.setDark(true));
    expect(hook.result.current.appearance).toBe("dark");
    expect(target.classList.contains("dark")).toBe(true);

    act(() => hook.result.current.setMode("light"));
    expect(media.listenerCount()).toBe(0);
    act(() => media.setDark(false));
    act(() => media.setDark(true));
    expect(hook.result.current.appearance).toBe("light");
  });

  it("migrates an old accent theme id and re-persists under the new keys", () => {
    const { storage, data } = memoryStorage({
      theme: "dark",
      "notrelix-color-theme": "sunset",
    });
    const { target, hook } = setup({ storage });
    expect(hook.result.current.mode).toBe("dark");
    expect(hook.result.current.accent).toBe("coral");
    expect(target.getAttribute("data-accent")).toBe("coral");
    expect(data.get(THEME_STORAGE_KEYS.accent)).toBe("coral");
    expect(data.has("notrelix-color-theme")).toBe(false);
    expect(data.has("theme")).toBe(false);
  });

  it("keeps working in memory when storage throws", () => {
    const throwing: KeyValueStorage = {
      getItem: () => {
        throw new Error("blocked");
      },
      setItem: () => {
        throw new Error("blocked");
      },
    };
    const { hook } = setup({ storage: throwing });
    act(() => hook.result.current.setMode("dark"));
    expect(hook.result.current.appearance).toBe("dark");
  });

  it("fails clearly when used without a provider (FE-UI-066)", () => {
    expect(() => renderHook(() => useTheme())).toThrow(/ThemeProvider/);
  });
});

const labels: ThemePickerLabels = {
  mode: {
    legend: "Mode",
    options: {
      light: { label: "Light", description: "Light surfaces" },
      dark: { label: "Dark", description: "Dark surfaces" },
      system: { label: "System", description: "Follow the device" },
      "high-contrast": {
        label: "High contrast",
        description: "Maximum legibility",
      },
    },
  },
  accent: {
    legend: "Accent",
    options: {
      blue: { label: "Notrelix Blue", description: "Default" },
      coral: { label: "Coral", description: "Warm" },
      ember: { label: "Ember", description: "Bold" },
      sage: { label: "Sage", description: "Calm" },
      amber: { label: "Amber", description: "Bright" },
    },
  },
};

describe("ThemePicker", () => {
  it("renders two labelled radio groups and switches each axis independently", () => {
    const target = document.createElement("div");
    render(
      <ThemeProvider target={target} matchMedia={fakeMedia(false).matchMedia}>
        <ThemePicker labels={labels} />
      </ThemeProvider>,
    );

    expect(screen.getByRole("group", { name: "Mode" })).toBeDefined();
    expect(screen.getByRole("group", { name: "Accent" })).toBeDefined();
    const radio = (name: RegExp) =>
      screen.getByRole("radio", { name }) as HTMLInputElement;
    expect(radio(/Light/).checked).toBe(true);
    expect(radio(/Notrelix Blue/).checked).toBe(true);

    fireEvent.click(screen.getByRole("radio", { name: /High contrast/ }));
    fireEvent.click(screen.getByRole("radio", { name: /Amber/ }));

    expect(target.getAttribute("data-contrast")).toBe("high");
    expect(target.getAttribute("data-accent")).toBe("amber");
    expect(radio(/High contrast/).checked).toBe(true);
  });
});
