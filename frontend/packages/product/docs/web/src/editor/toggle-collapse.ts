import type { Node as PmNode } from "@tiptap/pm/model";
import {
  Plugin,
  PluginKey,
  type EditorState,
  type Transaction,
} from "@tiptap/pm/state";
import { Decoration, DecorationSet, type EditorView } from "@tiptap/pm/view";

/**
 * A toggle's children are the blocks right after it that are indented deeper
 * than the toggle (a block that cannot be indented ends the run). Collapsing is
 * the toggle's own `collapsed` property; the children stay in the document and
 * keep their position, so a save never moves or drops them.
 */

/** Width of the chevron area at the toggle's left edge. */
const CHEVRON_WIDTH = 24;

function indentOf(node: PmNode): number {
  return Number(node.attrs.indent) || 0;
}

function canIndent(node: PmNode): boolean {
  return "indent" in (node.type.spec.attrs ?? {});
}

export interface CollapsedRun {
  /** Document position of the toggle node. */
  readonly toggle: number;
  /** Document positions of the hidden child blocks. */
  readonly children: readonly number[];
}

/** The collapsed toggles of a document and the blocks each one hides. */
export function collapsedRuns(doc: PmNode): CollapsedRun[] {
  const blocks: { node: PmNode; pos: number }[] = [];
  doc.forEach((node, offset) => blocks.push({ node, pos: offset }));
  const runs: CollapsedRun[] = [];
  const hidden = new Set<number>();
  for (let i = 0; i < blocks.length; i += 1) {
    const { node, pos } = blocks[i]!;
    if (node.type.name !== "toggle" || !node.attrs.collapsed) continue;
    if (hidden.has(pos)) continue;
    const level = indentOf(node);
    const children: number[] = [];
    for (let j = i + 1; j < blocks.length; j += 1) {
      const next = blocks[j]!;
      if (!canIndent(next.node) || indentOf(next.node) <= level) break;
      children.push(next.pos);
    }
    for (const child of children) hidden.add(child);
    runs.push({ toggle: pos, children });
  }
  return runs;
}

/** Flips the collapsed state of the toggle at `pos`; null when nothing changes. */
export function setToggleCollapsed(
  state: EditorState,
  pos: number,
  collapsed: boolean,
): Transaction | null {
  const node = state.doc.nodeAt(pos);
  if (!node || node.type.name !== "toggle") return null;
  if (Boolean(node.attrs.collapsed) === collapsed) return null;
  return state.tr.setNodeMarkup(pos, undefined, {
    ...node.attrs,
    collapsed,
  });
}

/** Collapses or expands the toggle holding the selection. */
export function toggleCollapseAtSelection(
  view: Pick<EditorView, "state" | "dispatch">,
): boolean {
  const { $from } = view.state.selection;
  if ($from.depth < 1) return false;
  const node = $from.node(1);
  if (node.type.name !== "toggle") return false;
  const tr = setToggleCollapsed(
    view.state,
    $from.before(1),
    !node.attrs.collapsed,
  );
  if (!tr) return false;
  view.dispatch(tr);
  return true;
}

const KEY = new PluginKey("docToggleCollapse");

/**
 * Marks collapsed toggles and hides the blocks they cover; a click on the
 * chevron flips the toggle.
 */
export function toggleCollapsePlugin(): Plugin {
  return new Plugin({
    key: KEY,
    props: {
      decorations(state) {
        const decorations: Decoration[] = [];
        for (const run of collapsedRuns(state.doc)) {
          const node = state.doc.nodeAt(run.toggle)!;
          decorations.push(
            Decoration.node(run.toggle, run.toggle + node.nodeSize, {
              "data-collapsed": "true",
              "aria-expanded": "false",
            }),
          );
          for (const child of run.children) {
            const childNode = state.doc.nodeAt(child)!;
            decorations.push(
              Decoration.node(child, child + childNode.nodeSize, {
                class: "hidden",
                "data-collapsed-child": "true",
              }),
            );
          }
        }
        return DecorationSet.create(state.doc, decorations);
      },
      handleClick(view, _pos, event) {
        if (!view.editable) return false;
        const target = event.target;
        if (!(target instanceof HTMLElement)) return false;
        const element = target.closest<HTMLElement>('[data-doc-node="toggle"]');
        if (!element) return false;
        const left = element.getBoundingClientRect().left;
        if (event.clientX - left > CHEVRON_WIDTH) return false;
        const pos = view.posAtDOM(element, 0) - 1;
        const node = view.state.doc.nodeAt(pos);
        if (!node || node.type.name !== "toggle") return false;
        const tr = setToggleCollapsed(view.state, pos, !node.attrs.collapsed);
        if (!tr) return false;
        view.dispatch(tr);
        return true;
      },
    },
  });
}
