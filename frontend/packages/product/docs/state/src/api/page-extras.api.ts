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
    async getPermissions(pageId: string): Promise<PagePermissionsDto> {
      const dto = await api.get<Partial<PagePermissionsDto> | null>(
        `${endpoints.pages.detail(pageId)}/permissions`,
      );
      return { members: dto?.members ?? [] };
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
