import { act, StrictMode } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it } from "vitest";
import type { KeyValueStorage } from "@notrelix/runtime-web";
import {
  SYSTEM_RAIL_STORAGE_KEY,
  useSystemRailPreference,
} from "../use-system-rail-preference";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

function memoryStorage(initial: Record<string, string> = {}) {
  const values = new Map(Object.entries(initial));
  const writes: [string, string][] = [];
  const storage: KeyValueStorage = {
    getItem: (key) => values.get(key) ?? null,
    setItem: (key, value) => {
      writes.push([key, value]);
      values.set(key, value);
    },
    removeItem: (key) => void values.delete(key),
    clear: () => values.clear(),
  };
  return { storage, values, writes };
}

let root: Root | null = null;
let host: HTMLDivElement | null = null;

afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = null;
  host = null;
});

function renderPreference(storage: KeyValueStorage, strict = false) {
  const result: { current: ReturnType<typeof useSystemRailPreference> } = {
    current: undefined as never,
  };
  function Probe() {
    result.current = useSystemRailPreference(storage);
    return null;
  }
  host = document.createElement("div");
  document.body.append(host);
  root = createRoot(host);
  act(() =>
    root!.render(
      strict ? (
        <StrictMode>
          <Probe />
        </StrictMode>
      ) : (
        <Probe />
      ),
    ),
  );
  return result;
}

describe("useSystemRailPreference", () => {
  it("starts hidden with nothing stored", () => {
    const { storage, writes } = memoryStorage();
    const rail = renderPreference(storage);
    expect(rail.current.presentation).toBe("hidden");
    expect(writes).toEqual([]);
  });

  it("opens the overlay without persisting it and closes it again", () => {
    const { storage, writes } = memoryStorage();
    const rail = renderPreference(storage);
    act(() => rail.current.open());
    expect(rail.current.presentation).toBe("overlay");
    act(() => rail.current.close());
    expect(rail.current.presentation).toBe("hidden");
    expect(writes).toEqual([]);
  });

  it("pins from the overlay and persists pinned", () => {
    const { storage, values } = memoryStorage();
    const rail = renderPreference(storage);
    act(() => rail.current.open());
    act(() => rail.current.togglePinned());
    expect(rail.current.presentation).toBe("pinned");
    expect(values.get(SYSTEM_RAIL_STORAGE_KEY)).toBe("pinned");
    // Opening while pinned keeps the rail pinned.
    act(() => rail.current.open());
    expect(rail.current.presentation).toBe("pinned");
  });

  it("unpins to hidden and persists hidden", () => {
    const { storage, values } = memoryStorage({
      [SYSTEM_RAIL_STORAGE_KEY]: "pinned",
    });
    const rail = renderPreference(storage);
    act(() => rail.current.togglePinned());
    expect(rail.current.presentation).toBe("hidden");
    expect(values.get(SYSTEM_RAIL_STORAGE_KEY)).toBe("hidden");
  });

  it("restores a stored pinned preference after a reload", () => {
    const { storage } = memoryStorage({ [SYSTEM_RAIL_STORAGE_KEY]: "pinned" });
    expect(renderPreference(storage).current.presentation).toBe("pinned");
  });

  it("treats an invalid or overlay value as hidden", () => {
    for (const stored of ["overlay", "open", ""]) {
      const { storage } = memoryStorage({ [SYSTEM_RAIL_STORAGE_KEY]: stored });
      const rail = renderPreference(storage);
      expect(rail.current.presentation, stored).toBe("hidden");
      act(() => root?.unmount());
      root = null;
    }
  });

  it("degrades to hidden when storage throws", () => {
    const storage: KeyValueStorage = {
      getItem: () => {
        throw new Error("blocked");
      },
      setItem: () => {
        throw new Error("blocked");
      },
      removeItem: () => undefined,
      clear: () => undefined,
    };
    const rail = renderPreference(storage);
    expect(rail.current.presentation).toBe("hidden");
    act(() => rail.current.togglePinned());
    expect(rail.current.presentation).toBe("pinned");
  });

  it("writes storage once per change under StrictMode (no writes in state updaters)", () => {
    const { storage, writes } = memoryStorage();
    const rail = renderPreference(storage, true);
    act(() => rail.current.togglePinned());
    expect(rail.current.presentation).toBe("pinned");
    expect(writes).toEqual([[SYSTEM_RAIL_STORAGE_KEY, "pinned"]]);
    act(() => rail.current.close());
    expect(rail.current.presentation).toBe("hidden");
    expect(writes).toEqual([
      [SYSTEM_RAIL_STORAGE_KEY, "pinned"],
      [SYSTEM_RAIL_STORAGE_KEY, "hidden"],
    ]);
  });
});
