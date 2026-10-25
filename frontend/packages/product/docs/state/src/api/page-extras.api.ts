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
    /** The subject's id; the host names a person from the workspace's people when `name` is empty. */
    readonly subjectId?: string;
    readonly name: string;
    /** The producer's description of how the person has access. */
    readonly detail: string;
    readonly outside: boolean;
    readonly creator: boolean;
    readonly level: "manage" | "edit" | "comment" | "view";
  }[];
}

export interface ConvertedTaskDto {
  readonly id: string;
  readonly key: string;
  readonly title: string;
}

export type PagePermissionLevel =
  PagePermissionsDto["members"][number]["level"];

/** `Governance.ResourcePermissions.Get` row. */
interface ResourcePermissionDto {
  readonly id: string;
  readonly subjectType: string;
  readonly subjectId: string;
  readonly level: string;
  readonly isRevoked: boolean;
}

const PAGE_KIND = "documents.page";
const QUIET = { skipGlobalErrorToast: true } as const;

const FROM_LEVEL: Readonly<Record<string, PagePermissionLevel>> = {
  Viewer: "view",
  Commenter: "comment",
  Editor: "edit",
  Manager: "manage",
  Owner: "manage",
};

const TO_LEVEL: Readonly<Record<PagePermissionLevel, string>> = {
  view: "Viewer",
  comment: "Commenter",
  edit: "Editor",
  manage: "Manager",
};

export function createPageExtrasApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    async getBacklinks(pageId: string): Promise<PageBacklinksDto> {
      // A producer without the read answers 404: the page then lists none.
      const dto = await api
        .get<Partial<PageBacklinksDto> | null>(
          `${endpoints.pages.detail(pageId)}/backlinks`,
          QUIET,
        )
        .catch(() => null);
      return { docs: dto?.docs ?? [], tasks: dto?.tasks ?? [] };
    },
    /**
     * The page's own grants (`Governance.ResourcePermissions.Get`, readable by
     * an owner or manager of the page). Where the producer has no grants to
     * show, the page-scoped read (gap `CTR-GAP-DOCS-PAGE-EXT`) is tried.
     */
    async getPermissions(pageId: string): Promise<PagePermissionsDto> {
      const rows = await api
        .get<ResourcePermissionDto[] | null>(
          endpoints.resourcePermissions.list(PAGE_KIND, pageId),
          QUIET,
        )
        .catch(() => null);
      const granted = (rows ?? []).filter((row) => !row.isRevoked);
      if (granted.length > 0) {
        return {
          members: granted.map((row) => ({
            id: row.id,
            kind:
              row.subjectType === "Team"
                ? ("team" as const)
                : ("person" as const),
            userId: row.subjectType === "Team" ? null : row.subjectId,
            subjectId: row.subjectId,
            name: "",
            detail: "",
            outside: false,
            creator: false,
            level: FROM_LEVEL[row.level] ?? "view",
          })),
        };
      }
      const dto = await api
        .get<Partial<PagePermissionsDto> | null>(
          `${endpoints.pages.detail(pageId)}/permissions`,
          QUIET,
        )
        .catch(() => null);
      return { members: dto?.members ?? [] };
    },
    /** Gives a workspace member a permission on the page (`Governance.ResourcePermissions.Grant`). */
    async grantPermission(
      pageId: string,
      userId: string,
      level: PagePermissionLevel,
    ): Promise<void> {
      await api.post<unknown>(
        endpoints.resourcePermissions.list(PAGE_KIND, pageId),
        { subjectType: "User", subjectId: userId, level: TO_LEVEL[level] },
        QUIET,
      );
    },
    /** The blocks as they stood after a history entry (for the version compare). */
    async getVersionSnapshot(
      pageId: string,
      entryId: string,
    ): Promise<Block[]> {
      const dtos = await api.get<BlockDtoApi[]>(
        `${endpoints.pages.detail(pageId)}/history/${entryId}/snapshot`,
        QUIET,
      );
      return dtos.map((dto, index) => mapBlock(dto, (index + 1) * 1000));
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
