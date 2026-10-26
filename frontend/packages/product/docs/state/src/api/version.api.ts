import type { Block, PageVersion } from "@notrelix/docs-core";
import type {
  PageVersionDetailDtoApi,
  PageVersionDtoApi,
  RestorePageVersionResultDtoApi,
} from "../dto";
import { mapBlock } from "../model/block.mapper";
import {
  mapVersion,
  mapVersionContent,
  type PageVersionContent,
} from "../model/version.mapper";
import type { DocsApiClient, PageApiEndpoints } from "./page.api";

/** The version of a block as the caller last read it (what a restore is checked against). */
export interface SeenBlockVersion {
  readonly blockId: string;
  readonly version: number;
}

export interface RestoredPageVersion {
  readonly version: PageVersion;
  readonly blocks: Block[];
}

/** `Documents.Versions.*`: a page's history, entry by entry. */
export function createVersionApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    /** The history entries, newest first. */
    async list(pageId: string): Promise<PageVersion[]> {
      const dtos = await api.get<PageVersionDtoApi[]>(
        endpoints.pages.versions(pageId),
      );
      return dtos.map(mapVersion);
    },

    /** The blocks one entry captured, read-only. */
    async get(pageId: string, versionId: string): Promise<PageVersionContent> {
      return mapVersionContent(
        await api.get<PageVersionDetailDtoApi>(
          endpoints.pages.version(pageId, versionId),
        ),
      );
    },

    /** Saves the page as it is now under a name. */
    async create(pageId: string, name: string): Promise<PageVersion> {
      return mapVersion(
        await api.post<PageVersionDtoApi>(
          endpoints.pages.versions(pageId),
          { name },
          { skipGlobalErrorToast: true },
        ),
      );
    },

    /**
     * Puts the page's blocks back to an entry in one transaction and adds a
     * restore entry. `seen` is every live block with the version the caller
     * last read; if any changed, appeared or vanished since, the producer
     * answers 412 and restores nothing.
     */
    async restore(
      pageId: string,
      versionId: string,
      seen: readonly SeenBlockVersion[],
    ): Promise<RestoredPageVersion> {
      const dto = await api.post<RestorePageVersionResultDtoApi>(
        endpoints.pages.restoreVersion(pageId, versionId),
        { currentBlocks: seen },
        { skipGlobalErrorToast: true },
      );
      return {
        version: mapVersion(dto.version),
        blocks: dto.blocks.map((block, index) =>
          mapBlock(block, (index + 1) * 1000),
        ),
      };
    },
  };
}
