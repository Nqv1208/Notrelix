import {
  parseRichText,
  plainTextOf,
  upgradeLegacyProperties,
  type BlockDraft,
  type BlockProperties,
  type BlockType,
  type RichTextV1,
} from "@notrelix/docs-core";
import type { BlockDtoApi } from "../dto";

/**
 * How a block's authored text and properties travel to and from the
 * producer. TRANSITION: until Documents sends `content` and `properties` as
 * separate JSON objects, one JSON body carries the rich text beside the
 * block's own properties (`{ v: 1, runs, checked, ... }`). Switching to the
 * two-field shape only changes `encodeBlockBody` and `decodeBlockBody`.
 */
function asObject(value: unknown): Record<string, unknown> | null {
  if (value && typeof value === "object" && !Array.isArray(value)) {
    return value as Record<string, unknown>;
  }
  if (typeof value !== "string") return null;
  try {
    const parsed: unknown = JSON.parse(value);
    return parsed && typeof parsed === "object" && !Array.isArray(parsed)
      ? (parsed as Record<string, unknown>)
      : null;
  } catch {
    return null;
  }
}

export interface DecodedBlockBody {
  content: RichTextV1;
  /** The type's properties plus `text`, the plain text read-only views use. */
  properties: BlockProperties;
}

export function decodeBlockBody(
  type: BlockType,
  dto: Pick<BlockDtoApi, "content" | "properties">,
): DecodedBlockBody {
  const body = asObject(dto.content) ?? {};
  const rich = parseRichText(body);
  if (rich) {
    const { v: _v, runs: _runs, ...rest } = body;
    void _v;
    void _runs;
    const separate = asObject(dto.properties);
    const properties = { ...(separate ?? rest) } as BlockProperties;
    return {
      content: rich,
      properties: { ...properties, text: plainTextOf(rich) },
    };
  }
  const upgraded = upgradeLegacyProperties(type, body as BlockProperties);
  return {
    content: upgraded.content,
    properties: { ...upgraded.properties, text: plainTextOf(upgraded.content) },
  };
}

/** The JSON text the producer stores for a draft. */
export function encodeBlockBody(
  draft: Pick<BlockDraft, "content" | "properties">,
): string {
  return JSON.stringify({
    v: 1,
    runs: draft.content.runs,
    ...draft.properties,
  });
}
