import type {
  Block,
  PageFromTemplate,
  PageTemplate,
  PageVersion,
  PageVersionKind,
} from "@notrelix/docs-core";
import type {
  PageFromTemplateDtoApi,
  PageTemplateDtoApi,
  PageVersionDetailDtoApi,
  PageVersionDtoApi,
} from "../dto";
import { mapBlock } from "./block.mapper";

const KINDS: readonly PageVersionKind[] = ["automatic", "named", "restore"];

export function mapVersion(dto: PageVersionDtoApi): PageVersion {
  const kind = KINDS.find((candidate) => candidate === dto.kind);
  return {
    id: dto.id,
    pageId: dto.pageId,
    versionNumber: dto.versionNumber,
    kind: kind ?? "automatic",
    name: dto.name?.trim() ? dto.name : null,
    createdBy: dto.createdBy ?? null,
    createdAt: dto.createdAt,
  };
}

/** What a history entry captured: its title and blocks as read-only `Block`s (version 1, never written). */
export interface PageVersionContent {
  readonly version: PageVersion;
  readonly title: string;
  readonly blocks: Block[];
}

export function mapVersionContent(
  dto: PageVersionDetailDtoApi,
): PageVersionContent {
  const version = mapVersion(dto.version);
  return {
    version,
    title: dto.title,
    blocks: dto.blocks.map((block, index) =>
      mapBlock(
        {
          ...block,
          pageId: version.pageId,
          version: 1,
          createdAt: version.createdAt,
        },
        (index + 1) * 1000,
      ),
    ),
  };
}

export function mapTemplate(dto: PageTemplateDtoApi): PageTemplate {
  return {
    id: dto.id,
    name: dto.name,
    description: dto.description?.trim() ? dto.description : null,
    title: dto.title,
    icon: dto.icon,
    blockCount: dto.blockCount,
    createdBy: dto.createdBy ?? null,
    createdAt: dto.createdAt,
  };
}

export function mapPageFromTemplate(
  dto: PageFromTemplateDtoApi,
): PageFromTemplate {
  return { pageId: dto.pageId, droppedBlockCount: dto.droppedBlockCount };
}
