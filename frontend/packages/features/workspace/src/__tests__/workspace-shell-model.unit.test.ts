import { describe, expect, it, vi } from "vitest";
import { buildWorkspaceBreadcrumb } from "../core/model/breadcrumb";
import {
  NAVIGATION_RESOURCE_KIND,
  identityToneFor,
  initialsFor,
  isDestinationActive,
  resolveNavigationEntries,
  type NavigationReference,
} from "../core/model/navigation";
import {
  createShortcutMatcher,
  type ShortcutKeyEvent,
} from "../core/model/shortcuts";
import { createNavigationService } from "../core/api/navigation.service";
import { endpoints } from "@notrelix/contracts";
import {
  formatCopy,
  workspaceBreadcrumbLabels,
} from "../web/shell/shell-models";
import { workspaceShellCatalog } from "../web/shell/workspace-shell.catalog";
import { workspaceShellCopy } from "../web/shell/workspace-shell.copy";
import { findCatalogIssues } from "@notrelix/i18n/catalog";

const board = NAVIGATION_RESOURCE_KIND.board;
const doc = NAVIGATION_RESOURCE_KIND.doc;

function ref(
  kind: NavigationReference["kind"],
  resourceId: string,
): NavigationReference {
  return { kind, resourceId, at: "2026-10-09T08:00:00.000Z" };
}

describe("resolveNavigationEntries (WX-DEC-011)", () => {
  const catalog = {
    boards: [{ id: "b1", name: "Roadmap" }],
    docs: [{ id: "d1", name: "Spec" }],
  };

  it("joins names from the authorized lists and keeps reference order", () => {
    const entries = resolveNavigationEntries(
      [ref(doc, "d1"), ref(board, "b1")],
      catalog,
    );
    expect(entries.map((entry) => entry.name)).toEqual(["Spec", "Roadmap"]);
  });

  it("drops references without a match instead of leaking an id", () => {
    const entries = resolveNavigationEntries(
      [ref(board, "b1"), ref(board, "b-revoked"), ref(doc, "b1")],
      catalog,
    );
    expect(entries).toHaveLength(1);
    expect(entries[0]?.resourceId).toBe("b1");
  });

  it("marks pinned entries and removes duplicates", () => {
    const entries = resolveNavigationEntries(
      [ref(board, "b1"), ref(board, "b1"), ref(doc, "d1")],
      catalog,
      [ref(board, "b1")],
    );
    expect(entries.map((entry) => [entry.resourceId, entry.pinned])).toEqual([
      ["b1", true],
      ["d1", false],
    ]);
  });
});

describe("destinations and identity", () => {
  it("matches resources by id and sections by name", () => {
    expect(
      isDestinationActive(
        { section: "board", boardId: "a" },
        { section: "board", boardId: "a", view: "table" },
      ),
    ).toBe(true);
    expect(
      isDestinationActive(
        { section: "board", boardId: "a" },
        { section: "board", boardId: "b" },
      ),
    ).toBe(false);
    expect(
      isDestinationActive({ section: "home" }, { section: "members" }),
    ).toBe(false);
    expect(isDestinationActive({ section: "home" }, null)).toBe(false);
  });

  it("derives a stable identity tone and initials", () => {
    expect(identityToneFor("ws-product")).toBe(identityToneFor("ws-product"));
    expect(initialsFor("Linh Tran")).toBe("LT");
    expect(initialsFor("  ")).toBe("?");
    expect(initialsFor("Đội ngũ")).toBe("ĐN");
    expect(initialsFor("Alex Rivera (Admin)")).toBe("AR");
  });
});

describe("buildWorkspaceBreadcrumb (WX-SHELL-002)", () => {
  const labels = workspaceBreadcrumbLabels(workspaceShellCopy("en"));

  it("builds Workspace › Board › View from data", () => {
    const items = buildWorkspaceBreadcrumb({
      workspaceName: "Product",
      location: { section: "board", boardId: "b1", view: "kanban" },
      boards: [{ id: "b1", name: "Roadmap" }],
      docs: [],
      labels,
    });
    expect(items.map((item) => item.label)).toEqual([
      "Product",
      "Roadmap",
      "Kanban",
    ]);
    expect(items[0]?.destination).toEqual({ section: "home" });
    expect(items[1]?.destination).toEqual({ section: "board", boardId: "b1" });
    expect(items[2]?.destination).toBeUndefined();
  });

  it("marks names pending while lists load and never falls back to the id", () => {
    const pending = buildWorkspaceBreadcrumb({
      workspaceName: null,
      location: { section: "doc", docId: "d-9" },
      boards: null,
      docs: null,
      labels,
    });
    expect(pending.every((item) => item.pending)).toBe(true);
    expect(pending.map((item) => item.label)).not.toContain("d-9");

    const missing = buildWorkspaceBreadcrumb({
      workspaceName: "Product",
      location: { section: "doc", docId: "d-9" },
      boards: [],
      docs: [],
      labels,
    });
    expect(missing[1]?.label).toBe(labels.unavailable);
  });

  it("uses section labels for workspace sections", () => {
    const items = buildWorkspaceBreadcrumb({
      workspaceName: "Product",
      location: { section: "my-work" },
      boards: [],
      docs: [],
      labels,
    });
    expect(items.map((item) => item.label)).toEqual(["Product", "My work"]);
  });
});

describe("createShortcutMatcher (WX-SHELL-005)", () => {
  const bindings = [
    { id: "go-home", keys: ["g", "h"] },
    { id: "go-inbox", keys: ["g", "i"] },
    { id: "palette", keys: ["k"], mod: true, whileTyping: true },
    { id: "help", keys: ["?"] },
  ];
  const key = (
    value: string,
    overrides: Partial<ShortcutKeyEvent> = {},
  ): ShortcutKeyEvent => ({
    key: value,
    metaKey: false,
    ctrlKey: false,
    altKey: false,
    timeStamp: 0,
    typing: false,
    ...overrides,
  });

  it("matches two-key sequences", () => {
    const matcher = createShortcutMatcher(bindings);
    expect(matcher.handle(key("g", { timeStamp: 10 }))).toBeNull();
    expect(matcher.handle(key("h", { timeStamp: 20 }))).toBe("go-home");
    expect(matcher.handle(key("G", { timeStamp: 30 }))).toBeNull();
    expect(matcher.handle(key("I", { timeStamp: 40 }))).toBe("go-inbox");
  });

  it("expires a sequence after the timeout", () => {
    const matcher = createShortcutMatcher(bindings, 500);
    matcher.handle(key("g", { timeStamp: 0 }));
    expect(matcher.handle(key("h", { timeStamp: 900 }))).toBeNull();
  });

  it("skips sequences while typing but keeps the palette chord", () => {
    const matcher = createShortcutMatcher(bindings);
    expect(matcher.handle(key("g", { typing: true }))).toBeNull();
    expect(matcher.handle(key("h", { typing: true }))).toBeNull();
    expect(matcher.handle(key("?", { typing: true }))).toBeNull();
    expect(matcher.handle(key("k", { metaKey: true, typing: true }))).toBe(
      "palette",
    );
    expect(matcher.handle(key("k", { ctrlKey: true }))).toBe("palette");
  });

  it("matches single keys and ignores unrelated keys", () => {
    const matcher = createShortcutMatcher(bindings);
    expect(matcher.handle(key("x"))).toBeNull();
    expect(matcher.handle(key("?"))).toBe("help");
    expect(matcher.handle(key("k"))).toBeNull();
  });

  it("restarts a sequence on a fresh prefix key", () => {
    const matcher = createShortcutMatcher(bindings);
    matcher.handle(key("g", { timeStamp: 0 }));
    matcher.handle(key("x", { timeStamp: 10 }));
    expect(matcher.handle(key("h", { timeStamp: 20 }))).toBeNull();
    matcher.handle(key("g", { timeStamp: 30 }));
    matcher.handle(key("g", { timeStamp: 40 }));
    expect(matcher.handle(key("h", { timeStamp: 50 }))).toBe("go-home");
  });
});

describe("createNavigationService (WX-BE-001/002 adapter)", () => {
  function api() {
    return {
      get: vi.fn(),
      post: vi.fn().mockResolvedValue(null),
      put: vi.fn().mockResolvedValue(null),
      delete: vi.fn().mockResolvedValue(null),
    };
  }

  it("maps recents and pins and drops unknown kinds", async () => {
    const client = api();
    client.get.mockResolvedValueOnce([
      { kind: board, resourceId: "b1", lastOpenedAt: "2026-10-09T08:00:00Z" },
      {
        kind: "automation.rule",
        resourceId: "r1",
        lastOpenedAt: "2026-10-09T07:00:00Z",
      },
      { kind: doc, resourceId: "", lastOpenedAt: "2026-10-09T06:00:00Z" },
    ]);
    client.get.mockResolvedValueOnce(null);
    const service = createNavigationService(client, endpoints);

    await expect(service.getRecents("ws-1")).resolves.toEqual([
      { kind: board, resourceId: "b1", at: "2026-10-09T08:00:00Z" },
    ]);
    await expect(service.getPins("ws-1")).resolves.toEqual([]);
    expect(client.get).toHaveBeenNthCalledWith(1, "/workspaces/ws-1/recents");
    expect(client.get).toHaveBeenNthCalledWith(2, "/workspaces/ws-1/pins");
  });

  it("records an open with an idempotency key and pins by kind and id", async () => {
    const client = api();
    const service = createNavigationService(client, endpoints);

    await service.recordRecent("ws-1", doc, "d1", "idem-1");
    expect(client.post).toHaveBeenCalledWith(
      "/workspaces/ws-1/recents",
      { kind: doc, resourceId: "d1" },
      { idempotencyKey: "idem-1", skipGlobalErrorToast: true },
    );

    await service.pin("ws-1", board, "b1");
    await service.unpin("ws-1", board, "b1");
    expect(client.put).toHaveBeenCalledWith(
      "/workspaces/ws-1/pins/work-management.board/b1",
    );
    expect(client.delete).toHaveBeenCalledWith(
      "/workspaces/ws-1/pins/work-management.board/b1",
    );
  });
});

describe("workspace shell catalog (WX-DEC-008)", () => {
  it("is complete in vi and en", () => {
    expect(findCatalogIssues(workspaceShellCatalog)).toEqual([]);
  });

  it("fills placeholders and leaves unknown ones visible", () => {
    expect(formatCopy("Open {{name}}", { name: "Roadmap" })).toBe(
      "Open Roadmap",
    );
    expect(formatCopy("Open {{name}}", {})).toBe("Open {{name}}");
  });
});
