import CodeBlock from "@tiptap/extension-code-block";
import {
  NodeViewContent,
  NodeViewWrapper,
  ReactNodeViewRenderer,
  type NodeViewProps,
} from "@tiptap/react";
import { Plugin, PluginKey } from "@tiptap/pm/state";
import { Decoration, DecorationSet } from "@tiptap/pm/view";
import type { Node as PmNode } from "@tiptap/pm/model";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@notrelix/ui-web";
import {
  CODE_LANGUAGES,
  highlightRanges,
  loadHighlighter,
  type CodeHighlighter,
} from "./code-highlight";
import { useContextVersion, type ContextHolder } from "./extensions";

const PLAIN = "plain";
const HIGHLIGHT_KEY = new PluginKey<DecorationSet>("docCodeHighlight");
const LOADED = "docCodeHighlighterLoaded";

function CodeBlockView({
  node,
  editor,
  updateAttributes,
  context,
}: NodeViewProps & { context: ContextHolder }) {
  useContextVersion(context);
  const { copy } = context.current;
  const language = (node.attrs.language as string | null) || PLAIN;
  return (
    <NodeViewWrapper
      data-block={String(node.attrs.blockId)}
      className="group/code relative mb-3.5"
    >
      <div
        contentEditable={false}
        className="absolute right-2 top-1.5 z-10 opacity-0 transition-opacity focus-within:opacity-100 group-hover/code:opacity-100"
      >
        <Select
          value={language}
          disabled={!editor.isEditable}
          onValueChange={(value) =>
            updateAttributes({ language: value === PLAIN ? null : value })
          }
        >
          <SelectTrigger
            size="sm"
            aria-label={copy.blocks.codeLanguage}
            className="h-6 gap-1 border-0 bg-transparent px-1.5 text-caption text-meta-foreground shadow-none"
          >
            <SelectValue />
          </SelectTrigger>
          <SelectContent align="end">
            <SelectItem value={PLAIN}>{copy.blocks.codePlain}</SelectItem>
            {CODE_LANGUAGES.map((item) => (
              <SelectItem key={item.value} value={item.value}>
                {item.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <pre className="overflow-x-auto rounded-[8px] bg-state-hover px-3 py-2.5 font-mono text-compact leading-[1.45] text-foreground">
        <NodeViewContent className="whitespace-pre" />
      </pre>
    </NodeViewWrapper>
  );
}

function decorate(
  doc: PmNode,
  highlighter: CodeHighlighter | null,
): DecorationSet {
  if (!highlighter) return DecorationSet.empty;
  const decorations: Decoration[] = [];
  doc.descendants((node, pos) => {
    if (node.type.name !== "codeBlock") return;
    const ranges = highlightRanges(
      highlighter,
      node.attrs.language as string | null,
      node.textContent,
    );
    for (const range of ranges) {
      decorations.push(
        Decoration.inline(pos + 1 + range.from, pos + 1 + range.to, {
          class: range.class,
        }),
      );
    }
    return false;
  });
  return DecorationSet.create(doc, decorations);
}

function hasCodeBlock(doc: PmNode): boolean {
  let found = false;
  doc.descendants((node) => {
    if (node.type.name === "codeBlock") found = true;
    return !found && node.type.name !== "codeBlock";
  });
  return found;
}

/**
 * Colours code blocks by their `language` property. The highlighter loads (as
 * its own chunk) only once a block exists on the page; until then, and for a
 * block without a known language, the text stays plain.
 */
function highlightPlugin(): Plugin<DecorationSet> {
  let highlighter: CodeHighlighter | null = null;
  let requested = false;
  return new Plugin<DecorationSet>({
    key: HIGHLIGHT_KEY,
    state: {
      init: (_config, state) => decorate(state.doc, highlighter),
      apply: (tr, previous, _old, state) =>
        tr.docChanged || tr.getMeta(LOADED)
          ? decorate(state.doc, highlighter)
          : previous,
    },
    props: {
      decorations: (state) => HIGHLIGHT_KEY.getState(state),
    },
    view(view) {
      const ensure = () => {
        if (highlighter || requested || !hasCodeBlock(view.state.doc)) return;
        requested = true;
        void loadHighlighter()
          .then((loaded) => {
            highlighter = loaded;
            if (!view.isDestroyed) {
              view.dispatch(view.state.tr.setMeta(LOADED, true));
            }
          })
          .catch(() => {
            // The chunk is missing: code stays plain.
            requested = false;
          });
      };
      ensure();
      return { update: ensure };
    },
  });
}

/** The code block: a language picker over the block, coloured text inside. */
export function createCodeBlock(context: ContextHolder) {
  return CodeBlock.extend({
    addNodeView() {
      return ReactNodeViewRenderer((props: NodeViewProps) => (
        <CodeBlockView {...props} context={context} />
      ));
    },
    addProseMirrorPlugins() {
      return [...(this.parent?.() ?? []), highlightPlugin()];
    },
  }).configure({ defaultLanguage: null });
}
