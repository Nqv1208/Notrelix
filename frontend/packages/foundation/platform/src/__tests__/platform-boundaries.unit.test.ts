import { describe, expect, it } from "vitest";
import { hasPermission, permissions } from "../permissions/core";
import type { ClockPort, KeyValueStorage } from "../ports";

describe("platform boundaries", () => {
  it("evaluates permissions without React or browser globals", () => {
    expect(hasPermission("owner", permissions.workspace.manage)).toBe(true);
    expect(hasPermission("member", permissions.workspace.manage)).toBe(false);
    expect(hasPermission("guest", permissions.comment.create)).toBe(true);
    expect(hasPermission(undefined, permissions.board.create)).toBe(false);
  });

  it("loads platform core in a Node environment without window", async () => {
    expect(typeof globalThis.window).toBe("undefined");

    const core = await import("../permissions/core");

    expect(core.hasPermission("admin", core.permissions.board.create)).toBe(
      true,
    );
  });

  it("FND-020 platform root exposes only neutral ports and pure helpers", async () => {
    const platform = await import("../index");

    const keys = Object.keys(platform).sort();

    expect(keys).toEqual(
      expect.arrayContaining([
        "applyServerValidationErrors",
        "createMockModeChecker",
        "hasPermission",
        "isMockModeEnabled",
        "permissions",
        "permissionValues",
      ]),
    );

    expect("useNavigate" in platform).toBe(false);
    expect("useSearchParams" in platform).toBe(false);
    expect("usePathname" in platform).toBe(false);
    expect("useLink" in platform).toBe(false);
    expect("useCan" in platform).toBe(false);
    expect("NavigationProvider" in platform).toBe(false);
    expect("routes" in platform).toBe(false);
  });

  it("FND-020 platform ports carry no runtime implementation", async () => {
    const portsModule = await import("../ports");

    expect(Object.keys(portsModule)).toEqual([]);
  });

  it("FND-020 platform port contracts are structurally usable from runtime adapters", () => {
    const clock: ClockPort = {
      now: () => new Date(),
      isoNow: () => new Date().toISOString(),
    };

    const storage: KeyValueStorage = {
      getItem: () => null,
      setItem: () => {},
      removeItem: () => {},
      clear: () => {},
    };

    expect(clock.now()).toBeInstanceOf(Date);
    expect(storage.getItem("x")).toBeNull();
  });
});
