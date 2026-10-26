import type { Block } from "@notrelix/docs-core";
import type { BlockDtoApi } from "../dto";
import { mapBlock } from "../model/block.mapper";
import type { DocsApiClient, PageApiEndpoints } from "./page.api";

/**
 * Reads and writes beyond the generated contract (gap
 * `CTR-GAP-DOCS-PAGE-EXT`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Docs"): what mentions a page, and turning a
 * checklist line into a work item.
 */
export interface PageBacklinksDto {
  /** Documents that link the page. */
  readonly docs: readonly {
    readonly id: string;
    readonly title: string;
    readonly spaceName: string | null;
  }[];
  /** Work items that have the page attached. */
  readonly tasks: readonly {
    readonly id: string;
    readonly key: string;
    readonly title: string;
  }[];
}

/** Permissions given on the page itself (gap `CTR-GAP-DOCS-PAGE-EXT`). */
export interface PagePermissionsDto {
  readonly members: readonly {
    readonly id: string;
    readonly kind: "person" | "team";
    readonly userId: string | null;
    readonly name: string;
    /** The producer's description of how the person has access. */
    readonly detail: string;
    readonly outside: boolean;
    readonly creator: boolean;
    readonly level: "manage" | "edit" | "comment" | "view";
  }[];
}

/** One row of Governance's resource-permission read. */
export interface PageGrantRead {
  readonly id: string;
  readonly subjectType: string;
  readonly subjectId: string;
  readonly level: string;
  readonly createdBy: string | null;
  readonly isDeleted?: boolean;
}

export interface PermissionMemberRead {
  readonly userId: string;
  readonly name: string | null;
  readonly role: string | null;
  readonly jobTitle?: string | null;
}

export interface PermissionTeamRead {
  readonly id: string;
  readonly name: string | null;
  readonly memberCount: number;
}

const LEVELS: Record<string, PagePermissionsDto["members"][number]["level"]> = {
  owner: "manage",
  manager: "manage",
  editor: "edit",
  commenter: "comment",
  viewer: "view",
};

/**
 * Turns the grants into the share dialog's rows. A grant whose person or Team
 * the caller cannot name is left out rather than shown nameless; a Guest of the
 * workspace is shown as outside; the person who made their own grant is the
 * page's creator; manage comes first.
 */
export function mapPagePermissions(
  grants: readonly PageGrantRead[],
  members: readonly PermissionMemberRead[],
  teams: readonly PermissionTeamRead[],
): PagePermissionsDto {
  const rows: PagePermissionsDto["members"][number][] = [];
  for (const grant of grants) {
    if (grant.isDeleted) continue;
    const level = LEVELS[grant.level.toLowerCase()];
    if (!level) continue;
    if (grant.subjectType.toLowerCase() === "team") {
      const team = teams.find((candidate) => candidate.id === grant.subjectId);
      if (!team?.name) continue;
      rows.push({
        id: grant.id,
        kind: "team",
        userId: null,
        name: team.name,
        detail: "",
        outside: false,
        creator: false,
        level,
      });
      continue;
    }
    const member = members.find(
      (candidate) => candidate.userId === grant.subjectId,
    );
    if (!member?.name) continue;
    rows.push({
      id: grant.id,
      kind: "person",
      userId: member.userId,
      name: member.name,
      detail: member.jobTitle ?? "",
      outside: (member.role ?? "").toLowerCase() === "guest",
      creator: grant.createdBy === grant.subjectId,
      level,
    });
  }
  const rank = { manage: 0, edit: 1, comment: 2, view: 3 } as const;
  return {
    members: rows.sort(
      (a, b) =>
        Number(b.creator) - Number(a.creator) || rank[a.level] - rank[b.level],
    ),
  };
}

export interface ConvertedTaskDto {
  readonly id: string;
  readonly key: string;
  readonly title: string;
}

export function createPageExtrasApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    async getBacklinks(pageId: string): Promise<PageBacklinksDto> {
      const dto = await api.get<Partial<PageBacklinksDto> | null>(
        `${endpoints.pages.detail(pageId)}/backlinks`,
      );
      return { docs: dto?.docs ?? [], tasks: dto?.tasks ?? [] };
    },
    /**
     * The grants given on the page, named from the workspace's members and
     * Teams: Governance's resource-permission read carries subject ids only.
     */
    async getPermissions(
      pageId: string,
      workspaceId: string,
    ): Promise<PagePermissionsDto> {
      const [grants, members, teams] = await Promise.all([
        api.get<PageGrantRead[] | null>(
          `/resources/documents.page/${pageId}/permissions`,
        ),
        api
          .get<PermissionMemberRead[] | null>(
            `/workspaces/${workspaceId}/members`,
          )
          .catch(() => null),
        api
          .get<PermissionTeamRead[] | null>(`/workspaces/${workspaceId}/teams`)
          .catch(() => null),
      ]);
      return mapPagePermissions(grants ?? [], members ?? [], teams ?? []);
    },
    /** The blocks as they stood after a history entry (for the version compare). */
    async getVersionSnapshot(
      pageId: string,
      entryId: string,
    ): Promise<Block[]> {
      const dtos = await api.get<BlockDtoApi[]>(
        `${endpoints.pages.detail(pageId)}/history/${entryId}/snapshot`,
      );
      return dtos.map(mapBlock);
    },
    /** Creates a work item from a checklist line and links the line to it. */
    convertTodo(pageId: string, blockId: string): Promise<ConvertedTaskDto> {
      return api.post<ConvertedTaskDto>(
        `${endpoints.pages.detail(pageId)}/blocks/${blockId}/convert-to-task`,
        {},
        { skipGlobalErrorToast: true },
      );
    },
  };
}
