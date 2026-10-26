import {
  draftOfBlock,
  type Block,
  type Mark,
  type Run,
} from "@notrelix/docs-core";
import type { ColorToken, HighlightToken } from "@notrelix/docs-core";

/** One block with the blocks nested under it, in reading order. */
export interface DocNode {
  readonly block: Block;
  readonly runs: readonly Run[];
  readonly children: readonly DocNode[];
  /** 1-based place among the run of numbered list items it belongs to. */
  readonly ordinal?: number;
}

function collect(
  blocks: readonly Block[],
  into: Map<string, Block>,
  nestedIn: Map<string, string>,
  parent: string | null = null,
): void {
  for (const block of blocks) {
    if (!into.has(block.id)) into.set(block.id, block);
    if (parent) nestedIn.set(block.id, parent);
    if (block.children?.length)
      collect(block.children, into, nestedIn, block.id);
  }
}

/**
 * The blocks of a page as a tree. Blocks may arrive flat (linked by
 * `parentId`) or nested (`children`); a block whose parent is not among them
 * is shown at the top level rather than dropped.
 */
export function buildDocTree(blocks: readonly Block[]): DocNode[] {
  const byId = new Map<string, Block>();
  const nestedIn = new Map<string, string>();
  collect(blocks, byId, nestedIn);
  const childrenOf = new Map<string | null, Block[]>();
  for (const block of byId.values()) {
    const declared = nestedIn.get(block.id) ?? block.parentId;
    const parent = declared && byId.has(declared) ? declared : null;
    const list = childrenOf.get(parent);
    if (list) list.push(block);
    else childrenOf.set(parent, [block]);
  }

  const build = (
    parent: string | null,
    seen: ReadonlySet<string>,
  ): DocNode[] => {
    const siblings = [...(childrenOf.get(parent) ?? [])].sort(
      (a, b) => a.position - b.position,
    );
    let ordinal = 0;
    return siblings
      .filter((block) => !seen.has(block.id))
      .map((block) => {
        ordinal = block.type === "numbered_list" ? ordinal + 1 : 0;
        const next = new Set(seen).add(block.id);
        return {
          block,
          runs: draftOfBlock(block).content.runs,
          children: build(block.id, next),
          ...(block.type === "numbered_list" ? { ordinal } : {}),
        };
      });
  };
  return build(null, new Set());
}

/** What the marks of one run mean for how its text is drawn. */
export interface RunPresentation {
  readonly bold: boolean;
  readonly italic: boolean;
  readonly underline: boolean;
  readonly strike: boolean;
  readonly code: boolean;
  readonly color?: ColorToken;
  readonly highlight?: HighlightToken;
  /** Only a safe link (http, https, mailto) is kept. */
  readonly href?: string;
  readonly mentionUserId?: string;
  readonly pageId?: string;
}

const SAFE_HREF = /^(?:https?:\/\/|mailto:)\S+$/i;

export function presentRun(marks: readonly Mark[]): RunPresentation {
  let bold = false;
  let italic = false;
  let underline = false;
  let strike = false;
  let code = false;
  let color: ColorToken | undefined;
  let highlight: HighlightToken | undefined;
  let href: string | undefined;
  let mentionUserId: string | undefined;
  let pageId: string | undefined;
  for (const mark of marks) {
    switch (mark.type) {
      case "bold":
        bold = true;
        break;
      case "italic":
        italic = true;
        break;
      case "underline":
        underline = true;
        break;
      case "strike":
        strike = true;
        break;
      case "code":
        code = true;
        break;
      case "color":
        if (mark.token !== "default") color = mark.token;
        break;
      case "highlight":
        if (mark.token !== "none") highlight = mark.token;
        break;
      case "link":
        if (SAFE_HREF.test(mark.href.trim())) href = mark.href.trim();
        break;
      case "mention":
        mentionUserId = mark.userId;
        break;
      case "pageRef":
        pageId = mark.pageId;
        break;
    }
  }
  return {
    bold,
    italic,
    underline,
    strike,
    code,
    ...(color ? { color } : {}),
    ...(highlight ? { highlight } : {}),
    ...(href ? { href } : {}),
    ...(mentionUserId ? { mentionUserId } : {}),
    ...(pageId ? { pageId } : {}),
  };
}
