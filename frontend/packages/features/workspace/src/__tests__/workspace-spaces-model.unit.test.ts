import { describe, expect, it } from "vitest";
import {
  appendPlacement,
  applySpaceMove,
  archiveBlocked,
  buildSpaceIndex,
  canCreateRootSpace,
  defaultVisibilityFor,
  filterSpaceIndex,
  isSpaceMoveNoop,
  moveTargets,
  spaceAncestors,
  spaceCapabilities,
  spaceDescendantIds,
  spaceMemberCapabilities,
  spacePathLabel,
  toSpaceTeams,
  visibleSpaceRows,
} from "../core/model/spaces";
import { parseSpaceMembers, parseSpaceTree } from "../core/api/spaces.service";
import { parseTeamList } from "../core/api/teams.service";
import {
  buildLibraryEntries,
  DEFAULT_LIBRARY_QUERY,
  queryLibrary,
} from "../core/model/library";
import {
  spaceMembersFixture,
  spaceNode,
  spaceTreeFixture,
} from "../verification/workspace-spaces-fixtures";

const index = () => buildSpaceIndex(spaceTreeFixture());

describe("Space tree model (WSP-021)", () => {
  it("orders siblings by ordinal position, then id, and treats a hidden parent as root", () => {
    const nodes = [
      spaceNode({ id: "b", name: "B", position: "a1" }),
      spaceNode({ id: "a", name: "A", position: "a1" }),
      spaceNode({ id: "c", name: "C", position: "a0" }),
      spaceNode({
        id: "orphan",
        name: "Orphan",
        parentId: "hidden",
        position: "Z",
      }),
    ];
    const roots = visibleSpaceRows(buildSpaceIndex(nodes), new Set());
    expect(roots.map((row) => row.node.id)).toEqual(["orphan", "c", "a", "b"]);
    expect(roots.every((row) => row.level === 1 && row.setSize === 4)).toBe(
      true,
    );
  });

  it("renders only expanded subtrees with levels and set positions", () => {
    const rows = visibleSpaceRows(index(), new Set(["sp-it"]));
    expect(
      rows.map((row) => [row.node.id, row.level, row.posInSet, row.setSize]),
    ).toEqual([
      ["sp-it", 1, 1, 4],
      ["sp-it-platform", 2, 1, 2],
      ["sp-it-security", 2, 2, 2],
      ["sp-accounting", 1, 2, 4],
      ["sp-marketing", 1, 3, 4],
      ["sp-archive", 1, 4, 4],
    ]);
    expect(rows[1]!.hasChildren).toBe(true);
    expect(rows[1]!.expanded).toBe(false);
  });

  it("walks ancestors, descendants and the path label", () => {
    const tree = index();
    expect(spaceAncestors(tree, "sp-it-infra").map((n) => n.id)).toEqual([
      "sp-it",
      "sp-it-platform",
    ]);
    expect([...spaceDescendantIds(tree, "sp-it")].sort()).toEqual([
      "sp-it-infra",
      "sp-it-platform",
      "sp-it-security",
    ]);
    expect(spacePathLabel(tree, "sp-it-infra")).toBe(
      "IT / Platform / Infrastructure",
    );
  });

  it("survives a cyclic cache without looping", () => {
    const tree = buildSpaceIndex([
      spaceNode({ id: "x", name: "X", parentId: "y" }),
      spaceNode({ id: "y", name: "Y", parentId: "x" }),
    ]);
    expect(spaceAncestors(tree, "x").length).toBeLessThan(3);
    expect(visibleSpaceRows(tree, new Set(["x", "y"]))).toEqual([]);
  });

  it("filters diacritic-insensitively and keeps the ancestors open", () => {
    const tree = buildSpaceIndex([
      spaceNode({ id: "root", name: "Phòng Kế toán" }),
      spaceNode({ id: "kid", name: "Lương", parentId: "root" }),
    ]);
    const match = filterSpaceIndex(tree, "luong")!;
    expect([...match.ids].sort()).toEqual(["kid", "root"]);
    expect([...match.expanded]).toEqual(["root"]);
    expect(filterSpaceIndex(tree, "  ")).toBeNull();
  });
});

describe("Space placement (WSP-021, GOV-028)", () => {
  it("never offers a Space its own subtree, an archived Space or one it does not manage", () => {
    const targets = moveTargets(
      index(),
      { kind: "space", spaceId: "sp-it-platform" },
      "member",
    );
    expect(targets.spaceIds.has("sp-it-platform")).toBe(false);
    expect(targets.spaceIds.has("sp-it-infra")).toBe(false);
    expect(targets.spaceIds.has("sp-archive")).toBe(false);
    expect(targets.spaceIds.has("sp-accounting")).toBe(false); // Editor only
    expect(targets.spaceIds.has("sp-it-security")).toBe(true);
    expect(targets.root).toBe(false); // root needs workspace Owner/Admin
    expect(
      moveTargets(index(), { kind: "space", spaceId: "sp-it" }, "admin").root,
    ).toBe(true);
  });

  it("lets a Board go to any Space with Editor access, or out to the workspace", () => {
    const targets = moveTargets(index(), { kind: "board" }, "member");
    expect(targets.spaceIds.has("sp-accounting")).toBe(true);
    expect(targets.spaceIds.has("sp-marketing")).toBe(false); // Viewer
    expect(targets.root).toBe(true);
    expect(moveTargets(index(), { kind: "board" }, "guest").root).toBe(false);
  });

  it("appends after the last sibling and moves optimistically to the end", () => {
    const tree = index();
    expect(appendPlacement(tree, "sp-it")).toEqual({
      previousSiblingId: "sp-it-security",
      nextSiblingId: null,
    });
    expect(
      appendPlacement(tree, "sp-it", "sp-it-security").previousSiblingId,
    ).toBe("sp-it-platform");
    expect(appendPlacement(tree, "sp-accounting-payroll")).toEqual({
      previousSiblingId: null,
      nextSiblingId: null,
    });
    const moved = buildSpaceIndex(
      applySpaceMove(spaceTreeFixture(), "sp-marketing", "sp-it"),
    );
    expect(
      visibleSpaceRows(moved, new Set(["sp-it"]))
        .slice(0, 4)
        .map((r) => r.node.id),
    ).toEqual(["sp-it", "sp-it-platform", "sp-it-security", "sp-marketing"]);
    expect(isSpaceMoveNoop(tree, "sp-it-security", "sp-it")).toBe(true);
    expect(isSpaceMoveNoop(tree, "sp-it-platform", "sp-it")).toBe(false);
  });
});

describe("Space access (GOV-027) and membership (WSP-024)", () => {
  it("derives what to offer from the access level", () => {
    const tree = index();
    expect(spaceCapabilities(tree.byId.get("sp-it")!)).toEqual({
      canManage: true,
      canCreateResources: true,
      canCreateChild: true,
      canMove: true,
    });
    expect(spaceCapabilities(tree.byId.get("sp-accounting")!).canManage).toBe(
      false,
    );
    expect(
      spaceCapabilities(tree.byId.get("sp-accounting")!).canCreateResources,
    ).toBe(true);
    expect(
      spaceCapabilities(tree.byId.get("sp-marketing")!).canCreateResources,
    ).toBe(false);
    expect(
      spaceCapabilities(tree.byId.get("sp-archive")!).canCreateResources,
    ).toBe(false);
    expect(canCreateRootSpace("owner")).toBe(true);
    expect(canCreateRootSpace("member")).toBe(false);
  });

  it("protects the last Admin entry and only lets managers edit", () => {
    const entries = spaceMembersFixture();
    const [admin, team] = entries;
    expect(spaceMemberCapabilities(admin!, entries, true)).toEqual({
      roles: ["Admin"],
      canRemove: false,
      blockedBy: "last-admin",
    });
    expect(spaceMemberCapabilities(team!, entries, true).canRemove).toBe(true);
    const twoAdmins = entries.map((entry) => ({
      ...entry,
      role: "Admin" as const,
    }));
    expect(
      spaceMemberCapabilities(twoAdmins[0]!, twoAdmins, true).canRemove,
    ).toBe(true);
    expect(spaceMemberCapabilities(team!, entries, false).blockedBy).toBe(
      "not-manager",
    );
  });

  it("defaults a Department to Private and other types to open (WSP-023)", () => {
    expect(defaultVisibilityFor("Department")).toBe("Private");
    expect(defaultVisibilityFor("Folder")).toBe("Workspace");
  });

  it("blocks archive while visible content remains (WSP-022)", () => {
    expect(archiveBlocked({ childSpaces: 0, boards: 0, docs: 0 })).toBe(false);
    expect(archiveBlocked({ childSpaces: 0, boards: 1, docs: 0 })).toBe(true);
  });
});

describe("Space adapters", () => {
  it("parses the tree, dropping rows without identity or known enums", () => {
    const nodes = parseSpaceTree([
      {
        id: "s1",
        parentSpaceId: null,
        name: "IT",
        spaceType: "department",
        visibility: "Private",
        isArchived: false,
        position: "a0",
        version: 4,
        createdAt: "2026-01-01T00:00:00Z",
        accessLevel: "Manager",
      },
      { id: "s2", name: "Bad", spaceType: "Galaxy", visibility: "Private" },
      { name: "No id", spaceType: "Folder", visibility: "Workspace" },
      {
        id: "s3",
        name: "Odd level",
        spaceType: "Folder",
        visibility: "Workspace",
        accessLevel: "Owner",
      },
    ]);
    expect(nodes.map((node) => node.id)).toEqual(["s1", "s3"]);
    expect(nodes[0]).toMatchObject({
      type: "Department",
      version: 4,
      access: "Manager",
    });
    expect(nodes[1]!.access).toBe("Viewer"); // unknown level = least privilege
    expect(parseSpaceTree(null)).toEqual([]);
  });

  it("parses members and keeps only active teams", () => {
    expect(
      parseSpaceMembers([
        {
          subjectType: "User",
          subjectId: "u1",
          role: "Admin",
          displayName: "A",
        },
        { subjectType: "Robot", subjectId: "r1", role: "Admin" },
      ]).map((entry) => entry.subjectId),
    ).toEqual(["u1"]);
    expect(
      toSpaceTeams(
        parseTeamList([
          { id: "t1", name: "Ops", status: "Active", memberCount: 3 },
          { id: "t2", name: "Old", status: "Archived" },
        ]),
      ).map((team) => team.id),
    ).toEqual(["t1"]);
  });
});

describe("Library filter by Space (OT-5)", () => {
  const entries = buildLibraryEntries({
    boards: [
      { id: "b1", name: "Roadmap", spaceId: "sp-it" },
      { id: "b2", name: "Infra board", spaceId: "sp-it-infra" },
      { id: "b3", name: "Unplaced" },
    ],
    docs: [
      { id: "d1", name: "Payroll guide", spaceId: "sp-accounting-payroll" },
    ],
    pins: [],
  });

  it("filters to a Space and the Spaces inside it, or to unplaced work", () => {
    const scope = new Set(["sp-it", ...spaceDescendantIds(index(), "sp-it")]);
    expect(
      queryLibrary(
        entries,
        { ...DEFAULT_LIBRARY_QUERY, space: "sp-it" },
        scope,
      ).map((entry) => entry.id),
    ).toEqual(expect.arrayContaining(["b1", "b2"]));
    expect(
      queryLibrary(
        entries,
        { ...DEFAULT_LIBRARY_QUERY, space: "sp-it" },
        scope,
      ),
    ).toHaveLength(2);
    expect(
      queryLibrary(entries, { ...DEFAULT_LIBRARY_QUERY, space: "none" }).map(
        (e) => e.id,
      ),
    ).toEqual(["b3"]);
    expect(queryLibrary(entries, DEFAULT_LIBRARY_QUERY)).toHaveLength(4);
  });
});
