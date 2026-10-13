import type { Page } from "@notrelix/docs-core";
import {
  markToneFor,
  spaceAncestors,
  type SpaceIndex,
  type SpaceNode,
} from "@notrelix/features-workspace/core";
import type {
  SidebarDocPage,
  SidebarDocsDepartment,
} from "@notrelix/features-workspace/web";

export interface SidebarDocsModel {
  readonly departments: readonly SidebarDocsDepartment[];
  /** Node ids to open so the active document shows (department, folders). */
  readonly reveal: readonly string[];
}

/**
 * The document tree of the department that holds the open document: every page
 * placed in the department's Spaces, nested by parent page. `null` when no
 * document is open or the document sits at the workspace level.
 */
export function buildSidebarDocs({
  pages,
  spaces,
  spaceIndex,
  activeDocId,
}: {
  pages: readonly Page[];
  spaces: readonly SpaceNode[];
  spaceIndex: SpaceIndex;
  activeDocId: string | null;
}): SidebarDocsModel | null {
  if (!activeDocId) return null;
  const active = pages.find((page) => page.id === activeDocId);
  const spaceId = active?.spaceId ?? null;
  if (!active || !spaceId) return null;
  const home = spaceIndex.byId.get(spaceId);
  if (!home) return null;

  // The department is the top-most visible ancestor of the page's Space.
  const root = spaceAncestors(spaceIndex, spaceId)[0] ?? home;
  const inDepartment = new Set<string>([root.id]);
  let grew = true;
  while (grew) {
    grew = false;
    for (const space of spaces) {
      if (
        !space.isArchived &&
        space.parentId !== null &&
        inDepartment.has(space.parentId) &&
        !inDepartment.has(space.id)
      ) {
        inDepartment.add(space.id);
        grew = true;
      }
    }
  }

  const placed = pages.filter(
    (page) => page.spaceId && inDepartment.has(page.spaceId),
  );
  const placedIds = new Set(placed.map((page) => page.id));
  const children = new Map<string | null, Page[]>();
  for (const page of placed) {
    const parent =
      page.parentId !== null && placedIds.has(page.parentId)
        ? page.parentId
        : null;
    const list = children.get(parent) ?? [];
    list.push(page);
    children.set(parent, list);
  }
  for (const list of children.values()) {
    list.sort(
      (a, b) => a.position - b.position || a.title.localeCompare(b.title),
    );
  }
  const toNodes = (
    parent: string | null,
    seen: Set<string>,
  ): SidebarDocPage[] =>
    (children.get(parent) ?? [])
      .filter((page) => !seen.has(page.id))
      .map((page) => ({
        id: page.id,
        title: page.title,
        children: toNodes(page.id, new Set(seen).add(page.id)),
      }));

  const reveal = [root.id];
  for (
    let page: Page | undefined = active;
    page && page.parentId && placedIds.has(page.parentId);
    page = pages.find((candidate) => candidate.id === page?.parentId)
  ) {
    reveal.push(page.parentId);
  }

  return {
    departments: [
      {
        id: root.id,
        name: root.name,
        markTone: markToneFor(root.id),
        pages: toNodes(null, new Set()),
      },
    ],
    reveal,
  };
}
