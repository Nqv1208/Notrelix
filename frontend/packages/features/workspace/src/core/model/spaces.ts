/**
 * Space org tree model (WSP-021..025, GOV-025..029, OT-5).
 *
 * Pure and framework-neutral. The producer returns the caller's visible
 * Spaces as a flat, parent-linked list (`GET …/spaces/tree`); a Space whose
 * parent the caller cannot see arrives as a root, so the client never learns
 * about hidden Spaces. `access` is the caller's effective level on the Space
 * (GOV-027); it only shapes what the UI offers — every write is authorized by
 * the server (FE-AGENTS §40).
 */

import { normalizeSearchText } from "./navigation";
import type { WorkspaceRole } from "../types/workspace";

export const SPACE_TYPES = [
  "Department",
  "Folder",
  "Portfolio",
  "ProjectArea",
] as const;
export type SpaceType = (typeof SPACE_TYPES)[number];

export const SPACE_VISIBILITIES = ["Private", "Workspace"] as const;
export type SpaceVisibility = (typeof SPACE_VISIBILITIES)[number];

export type SpaceAccessLevel = "Viewer" | "Editor" | "Manager";

export const SPACE_ROLES = ["Admin", "Member", "Viewer"] as const;
export type SpaceRole = (typeof SPACE_ROLES)[number];

export type SpaceSubjectType = "User" | "Team";

export interface SpaceNode {
  readonly id: string;
  /** `null` at the root, and when the parent is not visible to the caller. */
  readonly parentId: string | null;
  readonly name: string;
  readonly description: string | null;
  readonly type: SpaceType;
  readonly visibility: SpaceVisibility;
  readonly isArchived: boolean;
  /** Fractional sibling key; siblings sort by ordinal comparison. */
  readonly position: string;
  /** Aggregate version, sent back as `expectedVersion`. */
  readonly version: number;
  readonly createdAt: string;
  readonly access: SpaceAccessLevel;
}

export interface SpaceMemberEntry {
  readonly subjectType: SpaceSubjectType;
  readonly subjectId: string;
  readonly role: SpaceRole;
  readonly displayName: string;
  readonly avatarUrl: string | null;
  readonly addedAt: string;
}

export interface SpaceTeam {
  readonly id: string;
  readonly name: string;
  readonly memberCount: number;
}

function lastOf<T>(items: readonly T[]): T | undefined {
  return items[items.length - 1];
}

function pick<T extends string>(
  value: unknown,
  options: readonly T[],
): T | null {
  if (typeof value !== "string") return null;
  const lower = value.toLowerCase();
  return options.find((option) => option.toLowerCase() === lower) ?? null;
}

export function parseSpaceType(value: unknown): SpaceType | null {
  return pick(value, SPACE_TYPES);
}

export function parseSpaceVisibility(value: unknown): SpaceVisibility | null {
  return pick(value, SPACE_VISIBILITIES);
}

/** Unknown levels read as the least privilege. */
export function parseSpaceAccess(value: unknown): SpaceAccessLevel {
  return pick(value, ["Viewer", "Editor", "Manager"] as const) ?? "Viewer";
}

export function parseSpaceRole(value: unknown): SpaceRole | null {
  return pick(value, SPACE_ROLES);
}

export function parseSpaceSubjectType(value: unknown): SpaceSubjectType | null {
  return pick(value, ["User", "Team"] as const);
}

/**
 * WSP-023: a Department Space is Private when the creator does not choose;
 * other types are open to the parent's audience.
 */
export function defaultVisibilityFor(type: SpaceType): SpaceVisibility {
  return type === "Department" ? "Private" : "Workspace";
}

// ── Tree index ─────────────────────────────────────────────────────

export interface SpaceIndex {
  readonly byId: ReadonlyMap<string, SpaceNode>;
  /** Children per parent id (`null` = root), in sibling order. */
  readonly children: ReadonlyMap<string | null, readonly SpaceNode[]>;
}

function compareSiblings(a: SpaceNode, b: SpaceNode): number {
  if (a.position !== b.position) return a.position < b.position ? -1 : 1;
  return a.id < b.id ? -1 : a.id > b.id ? 1 : 0;
}

/**
 * Indexes the flat list. A parent id that is not in the list is treated as a
 * root (the producer already masks hidden parents; this keeps the client safe
 * if a stale cache holds a removed parent).
 */
export function buildSpaceIndex(nodes: readonly SpaceNode[]): SpaceIndex {
  const byId = new Map(nodes.map((node) => [node.id, node] as const));
  const children = new Map<string | null, SpaceNode[]>();
  for (const node of nodes) {
    const parent =
      node.parentId !== null && byId.has(node.parentId) ? node.parentId : null;
    const list = children.get(parent) ?? [];
    list.push(node);
    children.set(parent, list);
  }
  for (const list of children.values()) list.sort(compareSiblings);
  return { byId, children };
}

export function childrenOf(
  index: SpaceIndex,
  parentId: string | null,
): readonly SpaceNode[] {
  return index.children.get(parentId) ?? [];
}

function effectiveParent(index: SpaceIndex, node: SpaceNode): string | null {
  return node.parentId !== null && index.byId.has(node.parentId)
    ? node.parentId
    : null;
}

/** Ancestors from the root down to the parent (the Space itself excluded). */
export function spaceAncestors(
  index: SpaceIndex,
  spaceId: string,
): SpaceNode[] {
  const out: SpaceNode[] = [];
  const seen = new Set<string>([spaceId]);
  let current = index.byId.get(spaceId);
  while (current) {
    const parentId = effectiveParent(index, current);
    if (parentId === null || seen.has(parentId)) break;
    seen.add(parentId);
    const parent = index.byId.get(parentId);
    if (!parent) break;
    out.unshift(parent);
    current = parent;
  }
  return out;
}

/** Every descendant id of a Space (the Space itself excluded). */
export function spaceDescendantIds(
  index: SpaceIndex,
  spaceId: string,
): Set<string> {
  const out = new Set<string>();
  const stack = [...childrenOf(index, spaceId)];
  while (stack.length > 0) {
    const node = stack.pop()!;
    if (out.has(node.id) || node.id === spaceId) continue;
    out.add(node.id);
    stack.push(...childrenOf(index, node.id));
  }
  return out;
}

/** "Engineering / Platform / Infra" — the visible path of a Space. */
export function spacePathLabel(index: SpaceIndex, spaceId: string): string {
  const node = index.byId.get(spaceId);
  if (!node) return "";
  return [...spaceAncestors(index, spaceId), node]
    .map((entry) => entry.name)
    .join(" / ");
}

// ── Visible rows (lazy rendering) ──────────────────────────────────

export interface SpaceTreeRow {
  readonly node: SpaceNode;
  /** 1-based level (`aria-level`). */
  readonly level: number;
  /** 1-based position among siblings (`aria-posinset`). */
  readonly posInSet: number;
  readonly setSize: number;
  readonly hasChildren: boolean;
  readonly expanded: boolean;
}

/**
 * The rows a tree shows: roots, then the children of expanded rows only.
 * Collapsed subtrees are never visited, so a large tree costs only what is
 * open (depth is unlimited, WSP-021).
 */
export function visibleSpaceRows(
  index: SpaceIndex,
  expanded: ReadonlySet<string>,
): SpaceTreeRow[] {
  const rows: SpaceTreeRow[] = [];
  const visit = (parentId: string | null, level: number, path: Set<string>) => {
    const siblings = childrenOf(index, parentId);
    siblings.forEach((node, position) => {
      if (path.has(node.id)) return;
      const hasChildren = childrenOf(index, node.id).length > 0;
      const open = hasChildren && expanded.has(node.id);
      rows.push({
        node,
        level,
        posInSet: position + 1,
        setSize: siblings.length,
        hasChildren,
        expanded: open,
      });
      if (open) {
        path.add(node.id);
        visit(node.id, level + 1, path);
        path.delete(node.id);
      }
    });
  };
  visit(null, 1, new Set());
  return rows;
}

/** Expanded ids plus the ancestors of `activeId`, so the current Space shows. */
export function withActivePath(
  index: SpaceIndex,
  expanded: ReadonlySet<string>,
  activeId: string | null,
): Set<string> {
  const out = new Set(expanded);
  if (activeId) {
    for (const ancestor of spaceAncestors(index, activeId))
      out.add(ancestor.id);
  }
  return out;
}

/**
 * Text filter for pickers: matching Spaces and their ancestors, all expanded.
 * Diacritic-insensitive ("phong ke toan" matches "Phòng Kế toán").
 */
export function filterSpaceIndex(
  index: SpaceIndex,
  text: string,
): { readonly ids: Set<string>; readonly expanded: Set<string> } | null {
  const needle = normalizeSearchText(text.trim());
  if (needle === "") return null;
  const ids = new Set<string>();
  const expanded = new Set<string>();
  for (const node of index.byId.values()) {
    if (!normalizeSearchText(node.name).includes(needle)) continue;
    ids.add(node.id);
    for (const ancestor of spaceAncestors(index, node.id)) {
      ids.add(ancestor.id);
      expanded.add(ancestor.id);
    }
  }
  return { ids, expanded };
}

/** Restricts an index to a set of ids, keeping sibling order. */
export function subsetSpaceIndex(
  index: SpaceIndex,
  ids: ReadonlySet<string>,
): SpaceIndex {
  return buildSpaceIndex(
    [...index.byId.values()].filter((node) => ids.has(node.id)),
  );
}

// ── Capabilities (UX only; the producer authorizes) ────────────────

export function isWorkspaceManager(role: WorkspaceRole | null): boolean {
  return role === "owner" || role === "admin";
}

export interface SpaceCapabilities {
  /** Rename, describe, type, visibility, archive, members, child Spaces. */
  readonly canManage: boolean;
  /** Create Boards and Docs in the Space (Editor level, GOV-028). */
  readonly canCreateResources: boolean;
  readonly canCreateChild: boolean;
  readonly canMove: boolean;
}

export function spaceCapabilities(node: SpaceNode): SpaceCapabilities {
  const manager = node.access === "Manager";
  const editor = manager || node.access === "Editor";
  return {
    canManage: manager,
    canCreateResources: editor && !node.isArchived,
    canCreateChild: manager && !node.isArchived,
    canMove: manager && !node.isArchived,
  };
}

/** A root Space needs ManageSpaces on the Workspace (Owner/Admin, GOV-028). */
export function canCreateRootSpace(role: WorkspaceRole | null): boolean {
  return isWorkspaceManager(role);
}

export type MoveSubject =
  | { readonly kind: "space"; readonly spaceId: string }
  | { readonly kind: "board" };

export interface MoveTargets {
  /** Spaces the subject may move into. */
  readonly spaceIds: ReadonlySet<string>;
  /** Whether the Workspace root is a valid target. */
  readonly root: boolean;
}

/**
 * Valid targets for a move (GOV-028, WSP-021):
 * - a Space moves under a Space the caller manages, never under itself or a
 *   descendant, never under an archived parent; the root needs workspace
 *   Owner/Admin;
 * - a Board moves into a Space where the caller may create Boards (Editor),
 *   or out to the root when the caller may create Boards in the Workspace
 *   (any member but a guest).
 */
export function moveTargets(
  index: SpaceIndex,
  subject: MoveSubject,
  role: WorkspaceRole | null,
): MoveTargets {
  const excluded =
    subject.kind === "space"
      ? new Set([
          subject.spaceId,
          ...spaceDescendantIds(index, subject.spaceId),
        ])
      : new Set<string>();
  const spaceIds = new Set<string>();
  for (const node of index.byId.values()) {
    if (excluded.has(node.id) || node.isArchived) continue;
    const caps = spaceCapabilities(node);
    if (
      subject.kind === "space" ? caps.canCreateChild : caps.canCreateResources
    )
      spaceIds.add(node.id);
  }
  const root =
    subject.kind === "space"
      ? isWorkspaceManager(role)
      : role !== null && role !== "guest";
  return { spaceIds, root };
}

/** Neighbor intent that appends under `parentId` (both neighbors absent appends). */
export function appendPlacement(
  index: SpaceIndex,
  parentId: string | null,
  movingId?: string,
): { previousSiblingId: string | null; nextSiblingId: string | null } {
  const siblings = childrenOf(index, parentId).filter(
    (node) => node.id !== movingId,
  );
  return {
    previousSiblingId: lastOf(siblings)?.id ?? null,
    nextSiblingId: null,
  };
}

/**
 * The optimistic tree after appending `spaceId` under `parentId`. The
 * temporary position sorts after the current last sibling (any suffix makes
 * an ordinal key greater than its prefix); the producer's tree replaces it.
 */
export function applySpaceMove(
  nodes: readonly SpaceNode[],
  spaceId: string,
  parentId: string | null,
): SpaceNode[] {
  const index = buildSpaceIndex(nodes);
  const last = lastOf(
    childrenOf(index, parentId).filter((node) => node.id !== spaceId),
  );
  return nodes.map((node) =>
    node.id === spaceId
      ? { ...node, parentId, position: `${last?.position ?? ""}~` }
      : node,
  );
}

/** Whether a move changes anything (same parent and already last = no-op). */
export function isSpaceMoveNoop(
  index: SpaceIndex,
  spaceId: string,
  parentId: string | null,
): boolean {
  const node = index.byId.get(spaceId);
  if (!node) return true;
  if (effectiveParent(index, node) !== parentId) return false;
  return lastOf(childrenOf(index, parentId))?.id === spaceId;
}

// ── Members (WSP-024) ──────────────────────────────────────────────

export function spaceMemberKey(
  entry: Pick<SpaceMemberEntry, "subjectType" | "subjectId">,
): string {
  return `${entry.subjectType}:${entry.subjectId}`;
}

export type SpaceMemberBlock = "last-admin" | "not-manager";

export interface SpaceMemberCapabilities {
  readonly roles: readonly SpaceRole[];
  readonly canRemove: boolean;
  readonly blockedBy: SpaceMemberBlock | null;
}

/**
 * The last Admin entry cannot be removed or downgraded (WSP-024). The reason
 * is shown instead of offering the action.
 */
export function spaceMemberCapabilities(
  entry: SpaceMemberEntry,
  entries: readonly SpaceMemberEntry[],
  canManage: boolean,
): SpaceMemberCapabilities {
  if (!canManage)
    return { roles: [], canRemove: false, blockedBy: "not-manager" };
  const admins = entries.filter((item) => item.role === "Admin").length;
  if (entry.role === "Admin" && admins <= 1) {
    return { roles: ["Admin"], canRemove: false, blockedBy: "last-admin" };
  }
  return { roles: SPACE_ROLES, canRemove: true, blockedBy: null };
}

// ── Archive (WSP-022: no cascade) ──────────────────────────────────

export interface SpaceContentCounts {
  readonly childSpaces: number;
  readonly boards: number;
  readonly docs: number;
}

/**
 * Archive is rejected while active children or placed resources exist.
 * Counts are what the caller can see; Spaces and resources hidden from the
 * caller can still block the archive, and the producer's answer decides.
 */
export function archiveBlocked(counts: SpaceContentCounts): boolean {
  return counts.childSpaces + counts.boards + counts.docs > 0;
}

export function activeChildSpaceCount(
  index: SpaceIndex,
  spaceId: string,
): number {
  return childrenOf(index, spaceId).filter((node) => !node.isArchived).length;
}

// ── Breadcrumb ─────────────────────────────────────────────────────

export interface SpacePathItem {
  readonly id: string;
  readonly name: string;
}

export function spacePath(index: SpaceIndex, spaceId: string): SpacePathItem[] {
  const node = index.byId.get(spaceId);
  if (!node) return [];
  return [...spaceAncestors(index, spaceId), node].map((entry) => ({
    id: entry.id,
    name: entry.name,
  }));
}
