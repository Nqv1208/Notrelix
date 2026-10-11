/**
 * Mock Space org tree (WSP-021..025, GOV-025..029) for the mock backend.
 *
 * Mirrors the producer's observable behavior: visible-tree filtering with
 * hidden parents masked, effective access level (workspace Owner/Admin =
 * Manager; explicit User/Team entries inherited downward, a Private Space
 * stopping Member/Viewer inheritance; open Spaces give workspace Members the
 * Member level), sibling name uniqueness, no-cascade archive, cycle-free
 * moves, versioned membership writes and the last-Admin rule.
 */

export type MockSpaceType =
  "Folder" | "Portfolio" | "Department" | "ProjectArea";
export type MockSpaceVisibility = "Private" | "Workspace";
export type MockSpaceRole = "Admin" | "Member" | "Viewer";
export type MockSpaceSubjectType = "User" | "Team";
export type MockSpaceAccess = "Manager" | "Editor" | "Viewer";

export interface MockSpaceRecord {
  id: string;
  workspaceId: string;
  parentId: string | null;
  name: string;
  description: string | null;
  type: MockSpaceType;
  visibility: MockSpaceVisibility;
  archived: boolean;
  position: string;
  version: number;
  createdAt: string;
}

export interface MockSpaceMemberRecord {
  spaceId: string;
  subjectType: MockSpaceSubjectType;
  subjectId: string;
  role: MockSpaceRole;
  addedAt: string;
}

export interface MockTeamRecord {
  id: string;
  workspaceId: string;
  name: string;
  description: string | null;
  archived: boolean;
  memberIds: string[];
  createdAt: string;
}

/** A rule failure, surfaced by handlers as the producer's 400 ProblemDetails. */
export class MockSpaceRuleError extends Error {}

export type MockSpaceWrite =
  | { kind: "ok" }
  | { kind: "not-found" }
  | { kind: "forbidden" }
  | { kind: "stale" }
  | { kind: "rule"; message: string };

export interface MockSpaceViewer {
  readonly userId: string;
  /** The viewer's workspace role, `null` when not a member. */
  readonly workspaceRole: "owner" | "admin" | "member" | "guest" | null;
}

const ROLE_RANK: Record<MockSpaceRole, number> = {
  Viewer: 1,
  Member: 2,
  Admin: 3,
};
const ACCESS_OF: Record<MockSpaceRole, MockSpaceAccess> = {
  Admin: "Manager",
  Member: "Editor",
  Viewer: "Viewer",
};
const ACCESS_RANK: Record<MockSpaceAccess, number> = {
  Viewer: 1,
  Editor: 2,
  Manager: 3,
};

export function accessAtLeast(
  access: MockSpaceAccess | null,
  needed: MockSpaceAccess,
): boolean {
  return access !== null && ACCESS_RANK[access] >= ACCESS_RANK[needed];
}

export class MockSpaceState {
  private spaces = new Map<string, MockSpaceRecord>();
  private members = new Map<string, MockSpaceMemberRecord[]>();
  private teams = new Map<string, MockTeamRecord>();

  clear(): void {
    this.spaces.clear();
    this.members.clear();
    this.teams.clear();
  }

  // ── Records ───────────────────────────────────────────────────────

  insertSpace(space: MockSpaceRecord): void {
    this.spaces.set(space.id, { ...space });
  }

  insertMember(member: MockSpaceMemberRecord): void {
    const list = this.members.get(member.spaceId) ?? [];
    list.push({ ...member });
    this.members.set(member.spaceId, list);
  }

  insertTeam(team: MockTeamRecord): void {
    this.teams.set(team.id, { ...team, memberIds: [...team.memberIds] });
  }

  getSpace(id: string): MockSpaceRecord | undefined {
    return this.spaces.get(id);
  }

  spacesOf(workspaceId: string): MockSpaceRecord[] {
    return [...this.spaces.values()].filter(
      (s) => s.workspaceId === workspaceId,
    );
  }

  membersOf(spaceId: string): MockSpaceMemberRecord[] {
    return [...(this.members.get(spaceId) ?? [])];
  }

  teamsOf(workspaceId: string): MockTeamRecord[] {
    return [...this.teams.values()].filter(
      (t) => t.workspaceId === workspaceId,
    );
  }

  getTeam(id: string): MockTeamRecord | undefined {
    return this.teams.get(id);
  }

  snapshot(): { spaces: unknown[]; spaceMembers: unknown[]; teams: unknown[] } {
    return {
      spaces: [...this.spaces.values()],
      spaceMembers: [...this.members.values()].flat(),
      teams: [...this.teams.values()],
    };
  }

  // ── Access (GOV-025, GOV-027) ─────────────────────────────────────

  private chain(spaceId: string): MockSpaceRecord[] {
    const out: MockSpaceRecord[] = [];
    const seen = new Set<string>();
    let current = this.spaces.get(spaceId);
    while (current && !seen.has(current.id)) {
      seen.add(current.id);
      out.unshift(current);
      current = current.parentId
        ? this.spaces.get(current.parentId)
        : undefined;
    }
    return out;
  }

  private entryRole(
    spaceId: string,
    viewer: MockSpaceViewer,
  ): MockSpaceRole | null {
    let best: MockSpaceRole | null = null;
    for (const entry of this.members.get(spaceId) ?? []) {
      const applies =
        entry.subjectType === "User"
          ? entry.subjectId === viewer.userId
          : (() => {
              const team = this.teams.get(entry.subjectId);
              return (
                !!team &&
                !team.archived &&
                team.memberIds.includes(viewer.userId)
              );
            })();
      if (
        applies &&
        (best === null || ROLE_RANK[entry.role] > ROLE_RANK[best])
      ) {
        best = entry.role;
      }
    }
    return best;
  }

  /** The viewer's effective level on a Space, `null` = no access (NotFound). */
  accessFor(spaceId: string, viewer: MockSpaceViewer): MockSpaceAccess | null {
    const space = this.spaces.get(spaceId);
    if (!space || viewer.workspaceRole === null) return null;
    if (viewer.workspaceRole === "owner" || viewer.workspaceRole === "admin") {
      return "Manager";
    }
    const chain = this.chain(spaceId);
    let best: MockSpaceAccess | null = null;
    chain.forEach((ancestor, index) => {
      const role = this.entryRole(ancestor.id, viewer);
      if (!role) return;
      // A Private Space between the ancestor (exclusive) and S (inclusive)
      // stops Member/Viewer inheritance; Admin always passes.
      const stopped =
        role !== "Admin" &&
        chain.slice(index + 1).some((node) => node.visibility === "Private");
      if (stopped) return;
      const access = ACCESS_OF[role];
      if (best === null || ACCESS_RANK[access] > ACCESS_RANK[best])
        best = access;
    });
    const open = chain.every((node) => node.visibility === "Workspace");
    if (open && viewer.workspaceRole === "member") {
      if (best === null || ACCESS_RANK[best] < ACCESS_RANK.Editor)
        best = "Editor";
    }
    return best;
  }

  /** The visible tree, hidden parents masked as roots (GOV-029). */
  visibleTree(workspaceId: string, viewer: MockSpaceViewer) {
    const visible = this.spacesOf(workspaceId)
      .map((space) => ({ space, access: this.accessFor(space.id, viewer) }))
      .filter(
        (row): row is { space: MockSpaceRecord; access: MockSpaceAccess } =>
          row.access !== null,
      );
    const ids = new Set(visible.map((row) => row.space.id));
    return visible
      .sort(
        (a, b) =>
          (a.space.parentId ?? "").localeCompare(b.space.parentId ?? "") ||
          (a.space.position < b.space.position
            ? -1
            : a.space.position > b.space.position
              ? 1
              : 0) ||
          a.space.id.localeCompare(b.space.id),
      )
      .map(({ space, access }) => ({
        id: space.id,
        parentSpaceId:
          space.parentId && ids.has(space.parentId) ? space.parentId : null,
        name: space.name,
        description: space.description,
        spaceType: space.type,
        visibility: space.visibility,
        isArchived: space.archived,
        position: space.position,
        version: space.version,
        createdAt: space.createdAt,
        accessLevel: access,
      }));
  }

  // ── Placement helpers ─────────────────────────────────────────────

  private siblings(
    workspaceId: string,
    parentId: string | null,
    exceptId?: string,
  ): MockSpaceRecord[] {
    return this.spacesOf(workspaceId)
      .filter((s) => s.parentId === parentId && s.id !== exceptId)
      .sort((a, b) =>
        a.position < b.position ? -1 : a.position > b.position ? 1 : 0,
      );
  }

  /**
   * Neighbor-relative placement; a stale neighbor intent is rejected, not
   * guessed (WSP-021). Returns the sibling order with the placed id in it;
   * `renumber` then writes ordinal keys that keep that order.
   */
  private placeAmong(
    workspaceId: string,
    parentId: string | null,
    placedId: string,
    previousId: string | null,
    nextId: string | null,
  ): string[] {
    const order = this.siblings(workspaceId, parentId, placedId).map(
      (s) => s.id,
    );
    const previousIndex = previousId ? order.indexOf(previousId) : -1;
    const nextIndex = nextId ? order.indexOf(nextId) : -1;
    if ((previousId && previousIndex < 0) || (nextId && nextIndex < 0)) {
      throw new MockSpaceRuleError(
        "The requested position is stale. Reload the space tree and try again.",
      );
    }
    if (previousId && nextId && nextIndex !== previousIndex + 1) {
      throw new MockSpaceRuleError(
        "The requested neighbors are not adjacent. Reload the space tree and try again.",
      );
    }
    const at = previousId
      ? previousIndex + 1
      : nextId
        ? nextIndex
        : order.length;
    order.splice(at, 0, placedId);
    return order;
  }

  private renumber(order: readonly string[]): void {
    order.forEach((id, index) => {
      const space = this.spaces.get(id);
      if (space) space.position = `a${String(index).padStart(4, "0")}`;
    });
  }

  private ensureUniqueName(
    workspaceId: string,
    parentId: string | null,
    name: string,
    exceptId?: string,
  ): void {
    const lower = name.toLowerCase();
    if (
      this.siblings(workspaceId, parentId, exceptId).some(
        (s) => s.name.toLowerCase() === lower,
      )
    ) {
      throw new MockSpaceRuleError(
        "A space with this name already exists under the same parent.",
      );
    }
  }

  // ── Writes ────────────────────────────────────────────────────────

  create(
    workspaceId: string,
    viewer: MockSpaceViewer,
    input: {
      id: string;
      name: string;
      type: MockSpaceType;
      visibility: MockSpaceVisibility | null;
      description: string | null;
      parentId: string | null;
      previousSiblingId: string | null;
      nextSiblingId: string | null;
      now: string;
    },
  ): MockSpaceWrite {
    try {
      if (input.parentId) {
        const parent = this.spaces.get(input.parentId);
        const access = parent ? this.accessFor(parent.id, viewer) : null;
        if (!parent || parent.workspaceId !== workspaceId || access === null)
          return { kind: "not-found" };
        if (access !== "Manager") return { kind: "forbidden" };
        if (parent.archived)
          return {
            kind: "rule",
            message: "Cannot create a space under an archived parent.",
          };
      } else if (
        viewer.workspaceRole !== "owner" &&
        viewer.workspaceRole !== "admin"
      ) {
        return { kind: "forbidden" };
      }
      this.ensureUniqueName(workspaceId, input.parentId, input.name);
      const order = this.placeAmong(
        workspaceId,
        input.parentId,
        input.id,
        input.previousSiblingId,
        input.nextSiblingId,
      );
      this.insertSpace({
        id: input.id,
        workspaceId,
        parentId: input.parentId,
        name: input.name,
        description: input.description,
        type: input.type,
        visibility:
          input.visibility ??
          (input.type === "Department" ? "Private" : "Workspace"),
        archived: false,
        position: "",
        version: 1,
        createdAt: input.now,
      });
      this.renumber(order);
      // The creator becomes the first Space Admin (WSP-024).
      this.insertMember({
        spaceId: input.id,
        subjectType: "User",
        subjectId: viewer.userId,
        role: "Admin",
        addedAt: input.now,
      });
      return { kind: "ok" };
    } catch (error) {
      if (error instanceof MockSpaceRuleError)
        return { kind: "rule", message: error.message };
      throw error;
    }
  }

  private manageable(
    workspaceId: string,
    spaceId: string,
    viewer: MockSpaceViewer,
  ): MockSpaceRecord | MockSpaceWrite {
    const space = this.spaces.get(spaceId);
    const access = space ? this.accessFor(spaceId, viewer) : null;
    if (!space || space.workspaceId !== workspaceId || access === null)
      return { kind: "not-found" };
    if (access !== "Manager") return { kind: "forbidden" };
    return space;
  }

  update(
    workspaceId: string,
    spaceId: string,
    viewer: MockSpaceViewer,
    patch: Partial<
      Pick<MockSpaceRecord, "name" | "description" | "type" | "visibility">
    >,
  ): MockSpaceWrite {
    const space = this.manageable(workspaceId, spaceId, viewer);
    if ("kind" in space) return space;
    if (space.archived)
      return { kind: "rule", message: "Cannot change an archived space." };
    try {
      if (patch.name !== undefined) {
        if (patch.name === space.name) return { kind: "ok" };
        this.ensureUniqueName(
          workspaceId,
          space.parentId,
          patch.name,
          space.id,
        );
      }
    } catch (error) {
      if (error instanceof MockSpaceRuleError)
        return { kind: "rule", message: error.message };
      throw error;
    }
    const changed = Object.entries(patch).some(
      ([key, value]) =>
        (space as unknown as Record<string, unknown>)[key] !== value,
    );
    if (!changed) return { kind: "ok" };
    Object.assign(space, patch);
    space.version += 1;
    return { kind: "ok" };
  }

  archive(
    workspaceId: string,
    spaceId: string,
    viewer: MockSpaceViewer,
    placed: { boards: number; pages: number },
  ): MockSpaceWrite {
    const space = this.manageable(workspaceId, spaceId, viewer);
    if ("kind" in space) return space;
    if (space.archived) return { kind: "ok" };
    const activeChildren = this.siblings(workspaceId, space.id).filter(
      (child) => !child.archived,
    ).length;
    if (activeChildren > 0 || placed.boards > 0 || placed.pages > 0) {
      return {
        kind: "rule",
        message:
          "Cannot archive a space that still has active child spaces, boards or pages. Move or archive them first.",
      };
    }
    space.archived = true;
    space.version += 1;
    return { kind: "ok" };
  }

  unarchive(
    workspaceId: string,
    spaceId: string,
    viewer: MockSpaceViewer,
  ): MockSpaceWrite {
    const space = this.manageable(workspaceId, spaceId, viewer);
    if ("kind" in space) return space;
    if (!space.archived) return { kind: "ok" };
    const parent = space.parentId ? this.spaces.get(space.parentId) : null;
    if (parent?.archived)
      return {
        kind: "rule",
        message: "Cannot unarchive a space whose parent is archived.",
      };
    space.archived = false;
    space.version += 1;
    return { kind: "ok" };
  }

  move(
    workspaceId: string,
    spaceId: string,
    viewer: MockSpaceViewer,
    input: {
      parentId: string | null;
      previousSiblingId: string | null;
      nextSiblingId: string | null;
      expectedVersion: unknown;
    },
  ): MockSpaceWrite {
    const space = this.manageable(workspaceId, spaceId, viewer);
    if ("kind" in space) return space;
    if (input.expectedVersion !== space.version) return { kind: "stale" };
    if (space.archived)
      return { kind: "rule", message: "Cannot move an archived space." };
    if (input.parentId) {
      const target = this.spaces.get(input.parentId);
      const access = target ? this.accessFor(target.id, viewer) : null;
      if (!target || target.workspaceId !== workspaceId || access === null)
        return { kind: "not-found" };
      if (access !== "Manager") return { kind: "forbidden" };
      if (this.chain(target.id).some((node) => node.id === space.id))
        return {
          kind: "rule",
          message:
            "A space cannot be moved under itself or one of its descendants.",
        };
      if (target.archived)
        return {
          kind: "rule",
          message: "Cannot move a space under an archived parent.",
        };
    } else if (
      viewer.workspaceRole !== "owner" &&
      viewer.workspaceRole !== "admin"
    ) {
      return { kind: "forbidden" };
    }
    try {
      this.ensureUniqueName(workspaceId, input.parentId, space.name, space.id);
      const order = this.placeAmong(
        workspaceId,
        input.parentId,
        space.id,
        input.previousSiblingId,
        input.nextSiblingId,
      );
      const before = this.siblings(workspaceId, space.parentId).map(
        (s) => s.id,
      );
      if (space.parentId === input.parentId && before.join() === order.join()) {
        return { kind: "ok" };
      }
      const oldParent = space.parentId;
      space.parentId = input.parentId;
      this.renumber(order);
      if (oldParent !== input.parentId) {
        this.renumber(this.siblings(workspaceId, oldParent).map((s) => s.id));
      }
      space.version += 1;
      return { kind: "ok" };
    } catch (error) {
      if (error instanceof MockSpaceRuleError)
        return { kind: "rule", message: error.message };
      throw error;
    }
  }

  private memberWrite(
    workspaceId: string,
    spaceId: string,
    viewer: MockSpaceViewer,
    expectedVersion: unknown,
    apply: (
      space: MockSpaceRecord,
      entries: MockSpaceMemberRecord[],
    ) => MockSpaceWrite | "noop" | "changed",
  ): MockSpaceWrite {
    const space = this.manageable(workspaceId, spaceId, viewer);
    if ("kind" in space) return space;
    if (expectedVersion !== space.version) return { kind: "stale" };
    if (space.archived)
      return {
        kind: "rule",
        message: "Cannot change the members of an archived space.",
      };
    const entries = this.members.get(spaceId) ?? [];
    const outcome = apply(space, entries);
    if (outcome === "noop") return { kind: "ok" };
    if (outcome === "changed") {
      this.members.set(spaceId, entries);
      space.version += 1;
      return { kind: "ok" };
    }
    return outcome;
  }

  addMember(
    workspaceId: string,
    spaceId: string,
    viewer: MockSpaceViewer,
    input: {
      subjectType: MockSpaceSubjectType;
      subjectId: string;
      role: MockSpaceRole;
      expectedVersion: unknown;
      now: string;
      subjectExists: boolean;
    },
  ): MockSpaceWrite {
    return this.memberWrite(
      workspaceId,
      spaceId,
      viewer,
      input.expectedVersion,
      (_space, entries) => {
        if (!input.subjectExists) return { kind: "not-found" };
        const existing = entries.find(
          (e) =>
            e.subjectType === input.subjectType &&
            e.subjectId === input.subjectId,
        );
        if (existing) {
          if (existing.role === input.role) return "noop";
          return {
            kind: "rule",
            message:
              "This member already belongs to the space with another role. Change the role instead.",
          };
        }
        entries.push({
          spaceId,
          subjectType: input.subjectType,
          subjectId: input.subjectId,
          role: input.role,
          addedAt: input.now,
        });
        return "changed";
      },
    );
  }

  changeMemberRole(
    workspaceId: string,
    spaceId: string,
    viewer: MockSpaceViewer,
    input: {
      subjectType: MockSpaceSubjectType;
      subjectId: string;
      role: MockSpaceRole;
      expectedVersion: unknown;
    },
  ): MockSpaceWrite {
    return this.memberWrite(
      workspaceId,
      spaceId,
      viewer,
      input.expectedVersion,
      (_space, entries) => {
        const entry = entries.find(
          (e) =>
            e.subjectType === input.subjectType &&
            e.subjectId === input.subjectId,
        );
        if (!entry)
          return {
            kind: "rule",
            message: "This subject is not a member of the space.",
          };
        if (entry.role === input.role) return "noop";
        const admins = entries.filter((e) => e.role === "Admin").length;
        if (entry.role === "Admin" && admins <= 1)
          return {
            kind: "rule",
            message: "Cannot downgrade the last admin of the space.",
          };
        entry.role = input.role;
        return "changed";
      },
    );
  }

  removeMember(
    workspaceId: string,
    spaceId: string,
    viewer: MockSpaceViewer,
    input: {
      subjectType: MockSpaceSubjectType;
      subjectId: string;
      expectedVersion: unknown;
    },
  ): MockSpaceWrite {
    return this.memberWrite(
      workspaceId,
      spaceId,
      viewer,
      input.expectedVersion,
      (_space, entries) => {
        const index = entries.findIndex(
          (e) =>
            e.subjectType === input.subjectType &&
            e.subjectId === input.subjectId,
        );
        if (index < 0) return "noop";
        const admins = entries.filter((e) => e.role === "Admin").length;
        if (entries[index]!.role === "Admin" && admins <= 1)
          return {
            kind: "rule",
            message: "Cannot remove the last admin of the space.",
          };
        entries.splice(index, 1);
        return "changed";
      },
    );
  }
}
