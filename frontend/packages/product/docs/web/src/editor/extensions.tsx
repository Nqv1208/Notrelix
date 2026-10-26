import { useSyncExternalStore } from "react";
import {
  Extension,
  InputRule,
  Mark,
  Node,
  mergeAttributes,
  textblockTypeInputRule,
  type JSONContent,
} from "@tiptap/core";
import {
  NodeViewWrapper,
  ReactNodeViewRenderer,
  type NodeViewProps,
} from "@tiptap/react";
import { Plugin, PluginKey } from "@tiptap/pm/state";
import { Decoration, DecorationSet } from "@tiptap/pm/view";
import StarterKit from "@tiptap/starter-kit";
import { Placeholder } from "@tiptap/extensions";
import { isSafeHref, type BlockDraft } from "@notrelix/docs-core";
import type { DocPageCopy } from "../components/doc-page.copy";
import type {
  DocBlockView,
  DocPageCallbacks,
  DocTaskResolution,
} from "../components/doc-page-models";
import {
  DocLiveTable,
  DocPageReference,
  DocTaskChip,
} from "../components/doc-article-surface";
import { newBlockId, nodeToDraft } from "./block-mapping";
import { createCodeBlock } from "./code-block";
import { DocMedia, FileBlock, ImageBlock, UploadPlaceholder } from "./media";
import type { DocMediaHost } from "./media-host";
import { COLOR_CLASS, HIGHLIGHT_CLASS } from "./mark-classes";
import { BlockMove } from "./block-move";
import { CommentAnchors } from "./comment-anchors";
import { FindReplace } from "./find-replace";
import { TABLE_EXTENSIONS } from "./table";

/** What the node views need from the page; the host keeps it current. */
export interface DocEditorContext {
  copy: DocPageCopy;
  readOnly: boolean;
  callbacks?: DocPageCallbacks;
  resolveTask: (taskId: string) => DocTaskResolution;
  /** The read model of a reference block (live table, page link). */
  viewOf: (draft: BlockDraft) => DocBlockView | undefined;
  /** Image and file blocks; absent, the page offers none. */
  media?: DocMediaHost;
}

export interface ContextHolder {
  current: DocEditorContext;
  /** Bumped when the facts the node views show (tasks, pages) change. */
  version: number;
  listeners: Set<() => void>;
}

/** Re-renders a node view when the page's facts change. */
export function useContextVersion(holder: ContextHolder): number {
  return useSyncExternalStore(
    (listener) => {
      holder.listeners.add(listener);
      return () => holder.listeners.delete(listener);
    },
    () => holder.version,
  );
}

const TEXT_NODES = [
  "paragraph",
  "heading",
  "bulletedItem",
  "numberedItem",
  "todoItem",
  "quote",
  "callout",
  "toggle",
];
const BLOCK_NODES = [
  ...TEXT_NODES,
  "codeBlock",
  "horizontalRule",
  "boardReference",
  "pageReference",
  "table",
  "imageBlock",
  "fileBlock",
  "uploadPlaceholder",
  "unsupportedBlock",
];

const BLOCK_ID_KEY = new PluginKey("docBlockId");

/** Every top-level node carries a unique `blockId`; copies and splits get a new one. */
export const BlockId = Extension.create({
  name: "blockId",
  addGlobalAttributes() {
    return [
      {
        types: BLOCK_NODES,
        attributes: {
          blockId: {
            default: null,
            keepOnSplit: false,
            parseHTML: () => null,
            renderHTML: (attrs) =>
              attrs.blockId ? { "data-block": String(attrs.blockId) } : {},
          },
          rest: { default: null, rendered: false },
        },
      },
      {
        types: TEXT_NODES,
        attributes: {
          align: {
            default: null,
            rendered: true,
            parseHTML: (el) => el.style.textAlign || null,
            renderHTML: (attrs) =>
              attrs.align && attrs.align !== "left"
                ? {
                    "data-align": String(attrs.align),
                    class: `text-${String(attrs.align)}`,
                  }
                : {},
          },
        },
      },
    ];
  },
  addProseMirrorPlugins() {
    return [
      new Plugin({
        key: BLOCK_ID_KEY,
        appendTransaction: (transactions, _old, state) => {
          if (!transactions.some((t) => t.docChanged)) return null;
          const seen = new Set<string>();
          let tr = state.tr;
          let changed = false;
          state.doc.forEach((node, offset) => {
            if (!BLOCK_NODES.includes(node.type.name)) return;
            const id = node.attrs.blockId as string | null;
            if (!id || seen.has(id)) {
              tr = tr.setNodeMarkup(offset, undefined, {
                ...node.attrs,
                blockId: newBlockId(),
              });
              changed = true;
            } else {
              seen.add(id);
            }
          });
          return changed ? tr.setMeta("addToHistory", false) : null;
        },
      }),
    ];
  },
});

/** Selects a block's text colour from the contract's tokens. */
export const ColorToken = Mark.create({
  name: "colorToken",
  excludes: "colorToken",
  addAttributes() {
    return { token: { default: "default" } };
  },
  parseHTML() {
    return [
      {
        tag: "span[data-color]",
        getAttrs: (el) => ({ token: (el as HTMLElement).dataset.color }),
      },
    ];
  },
  renderHTML({ HTMLAttributes }) {
    const token = String(HTMLAttributes.token) as keyof typeof COLOR_CLASS;
    return [
      "span",
      { "data-color": token, class: COLOR_CLASS[token] ?? "" },
      0,
    ];
  },
});

export const HighlightToken = Mark.create({
  name: "highlightToken",
  excludes: "highlightToken",
  addAttributes() {
    return { token: { default: "none" } };
  },
  parseHTML() {
    return [
      {
        tag: "mark[data-highlight]",
        getAttrs: (el) => ({ token: (el as HTMLElement).dataset.highlight }),
      },
    ];
  },
  renderHTML({ HTMLAttributes }) {
    const token = String(HTMLAttributes.token) as keyof typeof HIGHLIGHT_CLASS;
    return [
      "mark",
      {
        "data-highlight": token,
        class: `rounded-[3px] text-inherit ${HIGHLIGHT_CLASS[token] ?? ""}`,
      },
      0,
    ];
  },
});

export const MentionUser = Mark.create({
  name: "mentionUser",
  inclusive: false,
  addAttributes() {
    return { userId: { default: "" } };
  },
  parseHTML() {
    return [
      {
        tag: "span[data-user-id]",
        getAttrs: (el) => ({ userId: (el as HTMLElement).dataset.userId }),
      },
    ];
  },
  renderHTML({ HTMLAttributes }) {
    return [
      "span",
      {
        "data-user-id": String(HTMLAttributes.userId),
        class:
          "cursor-pointer rounded-[4px] bg-brand-subtle px-0.5 text-brand-subtle-foreground",
      },
      0,
    ];
  },
});

export const PageRefMark = Mark.create({
  name: "pageRef",
  inclusive: false,
  addAttributes() {
    return { pageId: { default: "" } };
  },
  parseHTML() {
    return [
      {
        tag: "span[data-page-id]",
        getAttrs: (el) => ({ pageId: (el as HTMLElement).dataset.pageId }),
      },
    ];
  },
  renderHTML({ HTMLAttributes }) {
    return [
      "span",
      {
        "data-page-id": String(HTMLAttributes.pageId),
        class:
          "cursor-pointer text-brand-subtle-foreground underline decoration-dotted",
      },
      0,
    ];
  },
});

function flatTextBlock(
  name: string,
  tag: string,
  className: string,
  extra: Partial<{
    attributes: Record<string, unknown>;
    rules: (type: import("@tiptap/pm/model").NodeType) => InputRule[];
    keys: Record<
      string,
      (props: { editor: import("@tiptap/core").Editor }) => boolean
    >;
  }> = {},
) {
  return Node.create({
    name,
    group: "block",
    content: "inline*",
    defining: true,
    addAttributes() {
      return (extra.attributes ?? {}) as never;
    },
    parseHTML() {
      return [{ tag: `${tag}[data-doc-node="${name}"]` }];
    },
    renderHTML({ HTMLAttributes }) {
      return [
        tag,
        mergeAttributes(HTMLAttributes, {
          "data-doc-node": name,
          class: className,
        }),
        0,
      ];
    },
    addInputRules() {
      return extra.rules ? extra.rules(this.type) : [];
    },
    addKeyboardShortcuts() {
      return extra.keys ?? {};
    },
  });
}

/**
 * Enter at the end of an item starts the next item of the same kind; Enter on an empty item leaves the
 * list; Backspace at its start turns it into text. (Splitting in the middle keeps the kind on its own.)
 */
function exitKeys(name: string) {
  return {
    Enter: ({ editor }: { editor: import("@tiptap/core").Editor }) => {
      const { $from, empty } = editor.state.selection;
      if (!empty || $from.parent.type.name !== name) return false;
      if ($from.parent.content.size === 0) {
        return editor.commands.setNode("paragraph");
      }
      if ($from.parentOffset < $from.parent.content.size) return false;
      return editor.chain().splitBlock().setNode(name).run();
    },
    Backspace: ({ editor }: { editor: import("@tiptap/core").Editor }) => {
      const { $from, empty } = editor.state.selection;
      if (
        !empty ||
        $from.parentOffset !== 0 ||
        $from.parent.type.name !== name
      ) {
        return false;
      }
      return editor.commands.setNode("paragraph");
    },
  };
}

export const BulletedItem = flatTextBlock(
  "bulletedItem",
  "div",
  "doc-bulleted relative mb-1 pl-6 text-doc-list text-doc-body before:absolute before:left-1.5 before:content-['•']",
  {
    rules: (type) => [textblockTypeInputRule({ find: /^\s*([-*])\s$/, type })],
    keys: exitKeys("bulletedItem"),
  },
);

export const NumberedItem = flatTextBlock(
  "numberedItem",
  "div",
  "doc-numbered relative mb-1 pl-7 text-doc-list text-doc-body before:absolute before:left-0 before:w-5 before:text-right before:content-[attr(data-num)]",
  {
    rules: (type) => [textblockTypeInputRule({ find: /^(\d+)\.\s$/, type })],
    keys: exitKeys("numberedItem"),
  },
);

export const Quote = flatTextBlock(
  "quote",
  "blockquote",
  "mb-3.5 max-w-[68ch] border-l-2 border-line-strong pl-4 text-doc-prose text-muted-foreground italic",
  { rules: (type) => [textblockTypeInputRule({ find: /^\s*>\s$/, type })] },
);

export const Toggle = flatTextBlock(
  "toggle",
  "div",
  "doc-toggle relative mb-1 pl-6 text-doc-prose text-doc-body before:absolute before:left-1 before:content-['▸']",
  {
    attributes: { collapsed: { default: false, rendered: false } },
    rules: (type) => [textblockTypeInputRule({ find: /^\s*>>\s$/, type })],
  },
);

export const Callout = Node.create({
  name: "callout",
  group: "block",
  content: "inline*",
  defining: true,
  addAttributes() {
    return {
      icon: { default: null, rendered: false },
      tone: { default: null, rendered: false },
    };
  },
  parseHTML() {
    return [{ tag: 'div[data-doc-node="callout"]' }];
  },
  renderHTML({ HTMLAttributes }) {
    return [
      "div",
      mergeAttributes(HTMLAttributes, {
        "data-doc-node": "callout",
        class:
          "mt-1.5 mb-[18px] rounded-[10px] bg-doc-notice px-4 py-3.5 text-doc-callout text-doc-body",
      }),
      0,
    ];
  },
});

/** A checklist line: a checkbox beside the text. */
export const TodoItem = Node.create({
  name: "todoItem",
  group: "block",
  content: "inline*",
  defining: true,
  addAttributes() {
    return { checked: { default: false, rendered: false } };
  },
  parseHTML() {
    return [{ tag: 'div[data-doc-node="todoItem"]' }];
  },
  renderHTML({ HTMLAttributes }) {
    return [
      "div",
      mergeAttributes(HTMLAttributes, { "data-doc-node": "todoItem" }),
      0,
    ];
  },
  addInputRules() {
    return [
      textblockTypeInputRule({
        find: /^\s*\[( |x)?\]\s$/,
        type: this.type,
        getAttributes: (match) => ({ checked: match[1] === "x" }),
      }),
    ];
  },
  addKeyboardShortcuts() {
    return exitKeys("todoItem");
  },
  addNodeView() {
    return ({ node, editor, getPos }) => {
      const dom = document.createElement("div");
      dom.dataset.docNode = "todoItem";
      dom.dataset.block = String(node.attrs.blockId ?? "");
      const apply = (checked: boolean) => {
        dom.className = `group relative -mx-2 flex min-h-9 items-start gap-2.5 rounded-[8px] px-2 py-1 text-doc-check text-doc-body ${checked ? "[&_.doc-todo-text]:text-meta-foreground [&_.doc-todo-text]:line-through" : ""}`;
        box.setAttribute("aria-checked", String(checked));
        box.dataset.checked = String(checked);
        box.className = `mt-[3px] box-border grid size-[17px] shrink-0 cursor-pointer place-items-center rounded-[5px] border-[1.5px] p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring ${checked ? "border-work-status-done bg-work-status-done text-white" : "border-line-strong bg-transparent"}`;
        box.textContent = checked ? "✓" : "";
      };
      const box = document.createElement("button");
      box.type = "button";
      box.setAttribute("role", "checkbox");
      box.contentEditable = "false";
      const content = document.createElement("div");
      content.className = "doc-todo-text min-w-0 flex-1";
      dom.append(box, content);
      apply(Boolean(node.attrs.checked));
      box.addEventListener("mousedown", (event) => event.preventDefault());
      box.addEventListener("click", () => {
        if (!editor.isEditable) return;
        const pos = typeof getPos === "function" ? getPos() : undefined;
        if (pos === undefined) return;
        const current = editor.state.doc.nodeAt(pos);
        if (!current) return;
        editor.view.dispatch(
          editor.state.tr.setNodeMarkup(pos, undefined, {
            ...current.attrs,
            checked: !current.attrs.checked,
          }),
        );
      });
      return {
        dom,
        contentDOM: content,
        update: (next) => {
          if (next.type.name !== "todoItem") return false;
          dom.dataset.block = String(next.attrs.blockId ?? "");
          apply(Boolean(next.attrs.checked));
          return true;
        },
      };
    };
  },
});

function NodeShell({
  node,
  context,
  children,
}: {
  node: NodeViewProps["node"];
  context: ContextHolder;
  children: (view: DocBlockView, ctx: DocEditorContext) => React.ReactNode;
}) {
  useContextVersion(context);
  const ctx = context.current;
  const id = String(node.attrs.blockId);
  const view = ctx.viewOf(
    nodeToDraft({ type: node.type.name, attrs: node.attrs }),
  );
  return (
    <NodeViewWrapper data-block={id} contentEditable={false}>
      {view ? children(view, ctx) : null}
    </NodeViewWrapper>
  );
}

function referenceNode(
  name: "boardReference" | "pageReference",
  attributes: Record<string, unknown>,
) {
  return Node.create<{ context: ContextHolder }>({
    name,
    group: "block",
    atom: true,
    selectable: true,
    draggable: false,
    addOptions() {
      return { context: undefined as unknown as ContextHolder };
    },
    addAttributes() {
      return Object.fromEntries(
        Object.keys(attributes).map((key) => [
          key,
          { default: null, rendered: false },
        ]),
      );
    },
    parseHTML() {
      return [{ tag: `div[data-doc-node="${name}"]` }];
    },
    renderHTML({ HTMLAttributes }) {
      return [
        "div",
        mergeAttributes(HTMLAttributes, { "data-doc-node": name }),
      ];
    },
    addNodeView() {
      const context = this.options.context;
      return ReactNodeViewRenderer((props: NodeViewProps) => (
        <NodeShell node={props.node} context={context}>
          {(view, ctx) =>
            view.kind === "liveTable" ? (
              <DocLiveTable
                block={view}
                copy={ctx.copy}
                readOnly={ctx.readOnly}
                callbacks={ctx.callbacks}
              />
            ) : view.kind === "pageReference" ? (
              <DocPageReference
                block={view}
                copy={ctx.copy}
                readOnly={ctx.readOnly}
                callbacks={ctx.callbacks}
              />
            ) : null
          }
        </NodeShell>
      ));
    },
  });
}

export const BoardReference = referenceNode("boardReference", {
  linkedBoardId: 1,
  title: 1,
  caption: 1,
  items: 1,
});
export const PageReference = referenceNode("pageReference", {
  linkedPageId: 1,
});

/** A block type the editor cannot edit yet is kept whole and shown as a note. */
export const UnsupportedBlock = Node.create<{ context: ContextHolder }>({
  name: "unsupportedBlock",
  group: "block",
  atom: true,
  addOptions() {
    return { context: undefined as unknown as ContextHolder };
  },
  addAttributes() {
    return { raw: { default: "{}", rendered: false } };
  },
  parseHTML() {
    return [];
  },
  renderHTML({ HTMLAttributes }) {
    return [
      "div",
      mergeAttributes(HTMLAttributes, { "data-doc-node": "unsupportedBlock" }),
    ];
  },
  addNodeView() {
    const context = this.options.context;
    return ReactNodeViewRenderer((props: NodeViewProps) => (
      <NodeViewWrapper
        data-block={String(props.node.attrs.blockId)}
        contentEditable={false}
        className="mb-3.5 rounded-[9px] border border-dashed border-line-strong px-3 py-2 text-compact text-meta-foreground"
      >
        {context.current.copy.blocks.unsupported}
      </NodeViewWrapper>
    ));
  },
});

function TaskChipView({
  node,
  context,
}: {
  node: NodeViewProps["node"];
  context: ContextHolder;
}) {
  useContextVersion(context);
  return (
    <NodeViewWrapper as="span" className="inline">
      <DocTaskChip
        task={context.current.resolveTask(String(node.attrs.taskId))}
        copy={context.current.copy}
      />
    </NodeViewWrapper>
  );
}

/** A task mentioned inside text, shown as the task chip. */
export const TaskRef = Node.create<{ context: ContextHolder }>({
  name: "taskRef",
  group: "inline",
  inline: true,
  atom: true,
  selectable: true,
  addOptions() {
    return { context: undefined as unknown as ContextHolder };
  },
  addAttributes() {
    return { taskId: { default: "", rendered: false } };
  },
  parseHTML() {
    return [
      {
        tag: "span[data-task-ref]",
        getAttrs: (el) => ({ taskId: (el as HTMLElement).dataset.taskRef }),
      },
    ];
  },
  renderHTML({ HTMLAttributes }) {
    return ["span", { "data-task-ref": String(HTMLAttributes.taskId) }];
  },
  renderText({ node }) {
    return `[[task:${String(node.attrs.taskId)}]]`;
  },
  addNodeView() {
    const context = this.options.context;
    return ReactNodeViewRenderer(
      (props: NodeViewProps) => (
        <TaskChipView node={props.node} context={context} />
      ),
      { as: "span" },
    );
  },
});

/** Numbers each numbered item by its place in the run it belongs to. */
export const NumberedRuns = Extension.create({
  name: "numberedRuns",
  addProseMirrorPlugins() {
    return [
      new Plugin({
        props: {
          decorations: (state) => {
            const decorations: Decoration[] = [];
            let count = 0;
            state.doc.forEach((node, offset) => {
              if (node.type.name === "numberedItem") {
                count += 1;
                decorations.push(
                  Decoration.node(offset, offset + node.nodeSize, {
                    "data-num": `${count}.`,
                  }),
                );
              } else {
                count = 0;
              }
            });
            return DecorationSet.create(state.doc, decorations);
          },
        },
      }),
    ];
  },
});

export interface DocExtensionOptions {
  context: ContextHolder;
  /** The hint in the empty block under the caret. */
  placeholder: string;
  /** The hint when the whole page is empty. */
  emptyPlaceholder: string;
}

export function createDocExtensions({
  context,
  placeholder,
  emptyPlaceholder,
}: DocExtensionOptions) {
  return [
    StarterKit.configure({
      blockquote: false,
      bulletList: false,
      orderedList: false,
      listItem: false,
      listKeymap: false,
      trailingNode: false,
      heading: {
        levels: [1, 2, 3],
        HTMLAttributes: {},
      },
      paragraph: {
        HTMLAttributes: {
          class: "mb-3.5 max-w-[68ch] text-doc-prose text-pretty text-doc-body",
        },
      },
      codeBlock: false,
      horizontalRule: { HTMLAttributes: { class: "my-4 border-border" } },
      link: {
        openOnClick: false,
        autolink: true,
        linkOnPaste: true,
        defaultProtocol: "https",
        isAllowedUri: (url) => isSafeHref(url),
        HTMLAttributes: {
          class: "text-brand-subtle-foreground underline underline-offset-2",
          rel: "noopener noreferrer nofollow",
          target: "_blank",
        },
      },
      code: {
        HTMLAttributes: {
          class:
            "rounded-[4px] bg-state-hover px-1 py-0.5 font-mono text-[0.9em]",
        },
      },
    }),
    createCodeBlock(context),
    BlockId,
    BlockMove,
    FindReplace,
    CommentAnchors.configure({
      onOpen: (id) => context.current.callbacks?.onOpenComment?.(id),
    }),
    ColorToken,
    HighlightToken,
    MentionUser,
    PageRefMark,
    BulletedItem,
    NumberedItem,
    Quote,
    Toggle,
    Callout,
    TodoItem,
    ...TABLE_EXTENSIONS,
    BoardReference.configure({ context }),
    PageReference.configure({ context }),
    ImageBlock.configure({ context }),
    FileBlock.configure({ context }),
    UploadPlaceholder.configure({ context }),
    DocMedia.configure({ context }),
    UnsupportedBlock.configure({ context }),
    TaskRef.configure({ context }),
    NumberedRuns,
    Placeholder.configure({
      placeholder: ({ editor }) =>
        editor.isEmpty ? emptyPlaceholder : placeholder,
      showOnlyCurrent: true,
      includeChildren: false,
    }),
  ];
}

export type { JSONContent };
