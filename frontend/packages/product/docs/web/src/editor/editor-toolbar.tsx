import {
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import { useEditorState, type Editor } from "@tiptap/react";
import {
  AlignCenter,
  AlignJustify,
  AlignLeft,
  AlignRight,
  Bold,
  CheckSquare,
  Code,
  Eraser,
  Highlighter,
  ImageIcon,
  IndentDecrease,
  IndentIncrease,
  Italic,
  Link2,
  List,
  ListOrdered,
  MessageCircle,
  MoreHorizontal,
  Minus,
  MoveVertical,
  Palette,
  Paperclip,
  Printer,
  Redo2,
  Strikethrough,
  Table2,
  Underline,
  Undo2,
} from "lucide-react";
import {
  COLOR_TOKENS,
  HIGHLIGHT_TOKENS,
  type BlockType,
} from "@notrelix/docs-core";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  cn,
} from "@notrelix/ui-web";
import type { DocPageCopy } from "../components/doc-page.copy";
import type { DocPageCallbacks } from "../components/doc-page-models";
import {
  applyLink,
  clearFormatting,
  indentBlocks,
  insertDivider,
  removeLink,
  setAlign,
  setBlockType,
  setHighlight,
  setLineSpacing,
  setTextColor,
} from "./editor-commands";
import { selectToolbarState } from "./editor-state";
import { COLOR_SWATCH, HIGHLIGHT_CLASS } from "./mark-classes";
import { mediaOf, pickMedia } from "./media-commands";
import { insertTableBlock } from "./table-commands";

const TOOL =
  "box-border inline-flex h-7.5 min-w-7.5 items-center justify-center gap-1 rounded-[6px] border-0 bg-transparent px-1.5 text-compact leading-[1.45] text-muted-foreground outline-none hover:bg-state-hover-strong hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default aria-disabled:opacity-50 aria-disabled:hover:bg-transparent aria-pressed:bg-state-hover-strong aria-pressed:text-foreground";

const ICON = "size-4 stroke-[1.8]";

export const STYLE_TYPES: readonly BlockType[] = [
  "paragraph",
  "heading_1",
  "heading_2",
  "heading_3",
  "bulleted_list",
  "numbered_list",
  "todo",
  "quote",
  "callout",
  "code",
];

export function styleLabel(copy: DocPageCopy, type: BlockType): string {
  return (
    copy.slash.items[type as keyof typeof copy.slash.items] ??
    copy.slash.items.paragraph
  );
}

interface ToolProps {
  readonly label: string;
  readonly children: ReactNode;
  readonly pressed?: boolean;
  readonly disabled?: boolean;
  readonly onClick?: () => void;
}

/** A toolbar button: keeps the editor's selection and states its toggle. */
export function ToolButton({
  label,
  children,
  pressed,
  disabled,
  onClick,
}: ToolProps) {
  return (
    <button
      type="button"
      data-tool=""
      aria-label={label}
      title={label}
      aria-pressed={pressed}
      aria-disabled={disabled || undefined}
      onMouseDown={(event) => event.preventDefault()}
      onClick={disabled ? undefined : onClick}
      className={TOOL}
    >
      {children}
    </button>
  );
}

function Separator() {
  return <span aria-hidden="true" className="mx-1 h-5 w-px bg-border" />;
}

/** One tab stop for the whole toolbar; arrows, Home and End move inside it. */
function useRovingToolbar() {
  const ref = useRef<HTMLDivElement | null>(null);
  const items = () =>
    Array.from(ref.current?.querySelectorAll<HTMLElement>("[data-tool]") ?? []);

  useEffect(() => {
    const element = ref.current;
    if (!element) return;
    const sync = () => {
      const all = items();
      const current = all.find((node) => node.tabIndex === 0) ?? all[0];
      for (const node of all) node.tabIndex = node === current ? 0 : -1;
    };
    sync();
    const observer = new MutationObserver(sync);
    observer.observe(element, { childList: true, subtree: true });
    return () => observer.disconnect();
  });

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const all = items();
    const at = all.indexOf(document.activeElement as HTMLElement);
    if (at < 0) return;
    let next = at;
    if (event.key === "ArrowRight") next = (at + 1) % all.length;
    else if (event.key === "ArrowLeft")
      next = (at - 1 + all.length) % all.length;
    else if (event.key === "Home") next = 0;
    else if (event.key === "End") next = all.length - 1;
    else return;
    event.preventDefault();
    for (const node of all) node.tabIndex = -1;
    const target = all[next];
    if (target) {
      target.tabIndex = 0;
      target.focus();
    }
  };
  return { ref, onKeyDown };
}

export function SwatchGrid<T extends string>({
  label,
  tokens,
  names,
  swatch,
  current,
  onPick,
}: {
  label: string;
  tokens: readonly T[];
  names: Readonly<Record<T, string>>;
  swatch: (token: T) => string;
  current: string | null;
  onPick: (token: T) => void;
}) {
  return (
    <div role="group" aria-label={label} className="grid grid-cols-7 gap-1.5">
      {tokens.map((token) => (
        <button
          key={token}
          type="button"
          aria-label={names[token]}
          title={names[token]}
          aria-pressed={
            current === token ||
            (current === null && (token === "default" || token === "none"))
          }
          onMouseDown={(event) => event.preventDefault()}
          onClick={() => onPick(token)}
          className="grid size-7 place-items-center rounded-[6px] border border-line-strong bg-transparent p-0 outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-pressed:ring-2 aria-pressed:ring-primary"
        >
          <span
            aria-hidden="true"
            className={cn(
              "size-4 rounded-full border border-border",
              swatch(token),
            )}
          />
        </button>
      ))}
    </div>
  );
}

/** The link address field (toolbar popover and bubble menu share it). */
export function LinkForm({
  editor,
  copy,
  initial,
  onDone,
}: {
  editor: Editor;
  copy: DocPageCopy;
  initial: string | null;
  onDone: () => void;
}) {
  const [value, setValue] = useState(initial ?? "");
  const [invalid, setInvalid] = useState(false);
  const submit = () => {
    if (!value.trim()) {
      removeLink(editor);
      onDone();
      return;
    }
    if (applyLink(editor, value)) onDone();
    else setInvalid(true);
  };
  const t = copy.toolbar;
  return (
    <form
      className="flex flex-col gap-2"
      onSubmit={(event) => {
        event.preventDefault();
        submit();
      }}
    >
      <label className="text-crumb font-medium text-foreground">
        {t.linkUrl}
        <input
          autoFocus
          type="text"
          inputMode="url"
          value={value}
          aria-invalid={invalid || undefined}
          onChange={(event) => {
            setValue(event.target.value);
            setInvalid(false);
          }}
          placeholder="https://"
          className="mt-1 h-8 w-full rounded-[7px] border border-line-strong bg-doc-panel px-2 text-compact font-normal text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
        />
      </label>
      {invalid ? (
        <p role="alert" className="text-crumb text-danger-text">
          {t.linkInvalid}
        </p>
      ) : null}
      <div className="flex justify-end gap-2">
        {initial ? (
          <button
            type="button"
            onClick={() => {
              removeLink(editor);
              onDone();
            }}
            className="h-7 rounded-[6px] border border-line-strong bg-transparent px-2.5 text-crumb font-medium text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            {t.linkRemove}
          </button>
        ) : null}
        <button
          type="submit"
          className="h-7 rounded-[6px] border border-primary bg-brand-solid px-2.5 text-crumb font-semibold text-brand-solid-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
        >
          {t.linkApply}
        </button>
      </div>
    </form>
  );
}

export interface EditorToolbarProps {
  readonly editor: Editor;
  readonly copy: DocPageCopy;
  readonly disabled?: boolean;
  readonly callbacks?: DocPageCallbacks;
}

interface ToolGroup {
  readonly id: string;
  readonly node: ReactNode;
}

/** Width kept for the "more tools" button when groups do not fit. */
const MORE_WIDTH = 44;
const ROW_PADDING = 24;

/**
 * The formatting toolbar wired to the editor: every state comes from the
 * editor's selection, every control runs an editor command. Only controls
 * that work are shown. Groups that do not fit move, whole, into a "more
 * tools" popover, so a narrow window never wraps or clips the bar.
 */
export function EditorToolbar({
  editor,
  copy,
  disabled = false,
  callbacks,
}: EditorToolbarProps) {
  const t = copy.toolbar;
  const state = useEditorState({
    editor,
    selector: ({ editor: current }) => selectToolbarState(current),
  });
  const { ref, onKeyDown } = useRovingToolbar();
  const [linkOpen, setLinkOpen] = useState(false);
  const marks = disabled || state.inCodeBlock;

  // "Insert link" from the menu or the keyboard opens the link form here.
  useEffect(() => {
    const dom = editor.view.dom;
    const open = () => setLinkOpen(true);
    dom.addEventListener(OPEN_LINK_EVENT, open);
    return () => dom.removeEventListener(OPEN_LINK_EVENT, open);
  }, [editor]);

  const groups: ToolGroup[] = [
    {
      id: "history",
      node: (
        <>
          <ToolButton
            label={t.undo}
            disabled={disabled || !state.canUndo}
            onClick={() => editor.chain().focus().undo().run()}
          >
            <Undo2 aria-hidden="true" className={ICON} />
          </ToolButton>
          <ToolButton
            label={t.redo}
            disabled={disabled || !state.canRedo}
            onClick={() => editor.chain().focus().redo().run()}
          >
            <Redo2 aria-hidden="true" className={ICON} />
          </ToolButton>
          <ToolButton label={t.print} onClick={callbacks?.onPrint}>
            <Printer aria-hidden="true" className={ICON} />
          </ToolButton>
        </>
      ),
    },
    {
      id: "style",
      node: (
        <Select
          value={state.blockType ?? "paragraph"}
          disabled={disabled}
          onValueChange={(value) => setBlockType(editor, value as BlockType)}
        >
          <SelectTrigger
            data-tool=""
            aria-label={t.paragraphStyle}
            className="h-7.5 w-[150px] rounded-[6px] border-0 bg-transparent px-2 text-compact text-foreground shadow-none"
          >
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {STYLE_TYPES.map((type) => (
              <SelectItem key={type} value={type}>
                {styleLabel(copy, type)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      ),
    },
    {
      id: "marks",
      node: (
        <>
          <ToolButton
            label={t.bold}
            pressed={state.bold}
            disabled={marks}
            onClick={() => editor.chain().focus().toggleBold().run()}
          >
            <Bold aria-hidden="true" className={ICON} />
          </ToolButton>
          <ToolButton
            label={t.italic}
            pressed={state.italic}
            disabled={marks}
            onClick={() => editor.chain().focus().toggleItalic().run()}
          >
            <Italic aria-hidden="true" className={ICON} />
          </ToolButton>
          <ToolButton
            label={t.underline}
            pressed={state.underline}
            disabled={marks}
            onClick={() => editor.chain().focus().toggleUnderline().run()}
          >
            <Underline aria-hidden="true" className={ICON} />
          </ToolButton>
          <ToolButton
            label={t.strike}
            pressed={state.strike}
            disabled={marks}
            onClick={() => editor.chain().focus().toggleStrike().run()}
          >
            <Strikethrough aria-hidden="true" className={ICON} />
          </ToolButton>
          <ToolButton
            label={t.inlineCode}
            pressed={state.code}
            disabled={marks}
            onClick={() => editor.chain().focus().toggleCode().run()}
          >
            <Code aria-hidden="true" className={ICON} />
          </ToolButton>
        </>
      ),
    },
    {
      id: "inline",
      node: (
        <>
          <Popover>
            <PopoverTrigger asChild>
              <button
                type="button"
                data-tool=""
                aria-label={t.textColor}
                title={t.textColor}
                aria-disabled={marks || undefined}
                onMouseDown={(event) => event.preventDefault()}
                className={TOOL}
              >
                <Palette aria-hidden="true" className={ICON} />
              </button>
            </PopoverTrigger>
            <PopoverContent
              className="w-auto gap-2 p-2.5"
              onOpenAutoFocus={(e) => e.preventDefault()}
            >
              <SwatchGrid
                label={t.textColor}
                tokens={COLOR_TOKENS}
                names={t.colorNames}
                swatch={(token) => COLOR_SWATCH[token]}
                current={state.color}
                onPick={(token) => setTextColor(editor, token)}
              />
            </PopoverContent>
          </Popover>
          <Popover>
            <PopoverTrigger asChild>
              <button
                type="button"
                data-tool=""
                aria-label={t.highlight}
                title={t.highlight}
                aria-disabled={marks || undefined}
                onMouseDown={(event) => event.preventDefault()}
                className={TOOL}
              >
                <Highlighter aria-hidden="true" className={ICON} />
              </button>
            </PopoverTrigger>
            <PopoverContent
              className="w-auto gap-2 p-2.5"
              onOpenAutoFocus={(e) => e.preventDefault()}
            >
              <SwatchGrid
                label={t.highlight}
                tokens={HIGHLIGHT_TOKENS}
                names={t.highlightNames}
                swatch={(token) => HIGHLIGHT_CLASS[token] || "bg-transparent"}
                current={state.highlight}
                onPick={(token) => setHighlight(editor, token)}
              />
            </PopoverContent>
          </Popover>
          <Popover open={linkOpen && !marks} onOpenChange={setLinkOpen}>
            <PopoverTrigger asChild>
              <button
                type="button"
                data-tool=""
                aria-label={t.link}
                title={t.link}
                aria-pressed={state.link !== null}
                aria-disabled={marks || undefined}
                onMouseDown={(event) => event.preventDefault()}
                className={TOOL}
              >
                <Link2 aria-hidden="true" className={ICON} />
              </button>
            </PopoverTrigger>
            <PopoverContent className="w-72 p-3">
              <LinkForm
                editor={editor}
                copy={copy}
                initial={state.link}
                onDone={() => setLinkOpen(false)}
              />
            </PopoverContent>
          </Popover>
          <ToolButton
            label={t.comment}
            onClick={() => {
              // With text selected the comment is anchored to it; otherwise the panel just opens.
              if (!editor.state.selection.empty && callbacks?.onCommentStart) {
                callbacks.onCommentStart();
              } else {
                callbacks?.onOpenComments?.();
              }
            }}
          >
            <MessageCircle aria-hidden="true" className={ICON} />
          </ToolButton>
        </>
      ),
    },
    {
      id: "align",
      node: (
        <>
          {(
            [
              ["left", t.alignLeft, AlignLeft],
              ["center", t.alignCenter, AlignCenter],
              ["right", t.alignRight, AlignRight],
              ["justify", t.alignJustify, AlignJustify],
            ] as const
          ).map(([align, label, Icon]) => (
            <ToolButton
              key={align}
              label={label}
              pressed={state.align === align}
              disabled={disabled || state.inCodeBlock}
              onClick={() => setAlign(editor, align)}
            >
              <Icon aria-hidden="true" className={ICON} />
            </ToolButton>
          ))}
          <Popover>
            <PopoverTrigger asChild>
              <button
                type="button"
                data-tool=""
                aria-label={t.lineSpacing}
                title={t.lineSpacing}
                aria-disabled={marks || undefined}
                onMouseDown={(event) => event.preventDefault()}
                className={TOOL}
              >
                <MoveVertical aria-hidden="true" className={ICON} />
              </button>
            </PopoverTrigger>
            <PopoverContent
              className="w-40 gap-0.5 p-1.5"
              onOpenAutoFocus={(e) => e.preventDefault()}
            >
              <div role="group" aria-label={t.lineSpacing}>
                {(["tight", "normal", "relaxed", "loose"] as const).map(
                  (value) => (
                    <button
                      key={value}
                      type="button"
                      aria-pressed={state.lineSpacing === value}
                      onMouseDown={(event) => event.preventDefault()}
                      onClick={() => setLineSpacing(editor, value)}
                      className="flex h-8 w-full items-center rounded-[7px] border-0 bg-transparent px-2.5 text-left text-nav text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring aria-pressed:bg-state-selected aria-pressed:text-state-selected-foreground"
                    >
                      {t.lineSpacingNames[value]}
                    </button>
                  ),
                )}
              </div>
            </PopoverContent>
          </Popover>
        </>
      ),
    },
    {
      id: "lists",
      node: (
        <>
          <ToolButton
            label={t.bulleted}
            pressed={state.blockType === "bulleted_list"}
            disabled={disabled}
            onClick={() =>
              setBlockType(
                editor,
                state.blockType === "bulleted_list"
                  ? "paragraph"
                  : "bulleted_list",
              )
            }
          >
            <List aria-hidden="true" className={ICON} />
          </ToolButton>
          <ToolButton
            label={t.numbered}
            pressed={state.blockType === "numbered_list"}
            disabled={disabled}
            onClick={() =>
              setBlockType(
                editor,
                state.blockType === "numbered_list"
                  ? "paragraph"
                  : "numbered_list",
              )
            }
          >
            <ListOrdered aria-hidden="true" className={ICON} />
          </ToolButton>
          <ToolButton
            label={t.checklist}
            pressed={state.blockType === "todo"}
            disabled={disabled}
            onClick={() =>
              setBlockType(
                editor,
                state.blockType === "todo" ? "paragraph" : "todo",
              )
            }
          >
            <CheckSquare aria-hidden="true" className={ICON} />
          </ToolButton>
          <ToolButton
            label={t.outdent}
            disabled={disabled || !state.canOutdent}
            onClick={() => indentBlocks(editor, -1)}
          >
            <IndentDecrease aria-hidden="true" className={ICON} />
          </ToolButton>
          <ToolButton
            label={t.indent}
            disabled={disabled || !state.canIndent}
            onClick={() => indentBlocks(editor, 1)}
          >
            <IndentIncrease aria-hidden="true" className={ICON} />
          </ToolButton>
        </>
      ),
    },
    {
      id: "insert",
      node: (
        <>
          <ToolButton
            label={t.insertDivider}
            disabled={disabled}
            onClick={() => insertDivider(editor)}
          >
            <Minus aria-hidden="true" className={ICON} />
          </ToolButton>
          <ToolButton
            label={t.table}
            disabled={disabled}
            onClick={() => insertTableBlock(editor)}
          >
            <Table2 aria-hidden="true" className={ICON} />
          </ToolButton>
          {mediaOf(editor) ? (
            <>
              <ToolButton
                label={t.image}
                disabled={disabled}
                onClick={() => void pickMedia(editor, "image")}
              >
                <ImageIcon aria-hidden="true" className={ICON} />
              </ToolButton>
              <ToolButton
                label={t.file}
                disabled={disabled}
                onClick={() => void pickMedia(editor, "file")}
              >
                <Paperclip aria-hidden="true" className={ICON} />
              </ToolButton>
            </>
          ) : null}
        </>
      ),
    },
    {
      id: "clear",
      node: (
        <ToolButton
          label={t.clearFormat}
          disabled={disabled}
          onClick={() => clearFormatting(editor)}
        >
          <Eraser aria-hidden="true" className={ICON} />
        </ToolButton>
      ),
    },
  ];

  // How many groups fit; the rest go to the "more tools" popover.
  const widths = useRef(new Map<string, number>());
  const [rowWidth, setRowWidth] = useState(0);
  const [count, setCount] = useState(groups.length);
  const groupIds = groups.map((group) => group.id).join("|");

  useLayoutEffect(() => {
    const row = ref.current;
    if (!row) return;
    for (const node of row.querySelectorAll<HTMLElement>("[data-group]")) {
      const id = node.dataset.group as string;
      widths.current.set(id, node.offsetWidth);
    }
    const ids = groupIds.split("|");
    if (!ids.every((id) => widths.current.has(id))) {
      // A group has never been on screen: show all once to measure it.
      setCount(ids.length);
      return;
    }
    const available = rowWidth - ROW_PADDING;
    const sizes = ids.map((id) => widths.current.get(id) as number);
    const total = sizes.reduce((sum, size) => sum + size, 0);
    if (rowWidth === 0 || total <= available) {
      setCount(ids.length);
      return;
    }
    let used = 0;
    let fit = 0;
    for (const size of sizes) {
      if (used + size > available - MORE_WIDTH) break;
      used += size;
      fit += 1;
    }
    setCount(Math.max(1, fit));
  });

  useEffect(() => {
    const row = ref.current;
    if (!row) return;
    const observer = new ResizeObserver(() => setRowWidth(row.clientWidth));
    observer.observe(row);
    setRowWidth(row.clientWidth);
    return () => observer.disconnect();
  }, [ref]);

  const shown = groups.slice(0, count);
  const hidden = groups.slice(count);

  return (
    <div
      ref={ref}
      role="toolbar"
      aria-label={t.label}
      aria-orientation="horizontal"
      onKeyDown={onKeyDown}
      className="flex flex-nowrap items-center gap-0.5 overflow-hidden px-3 pt-1 pb-1.5"
    >
      {shown.map((group, index) => (
        <div
          key={group.id}
          data-group={group.id}
          className="flex shrink-0 items-center gap-0.5"
        >
          {index > 0 ? <Separator /> : null}
          {group.node}
        </div>
      ))}
      {hidden.length > 0 ? (
        <Popover>
          <PopoverTrigger asChild>
            <button
              type="button"
              data-tool=""
              aria-label={t.more}
              title={t.more}
              onMouseDown={(event) => event.preventDefault()}
              className={cn(TOOL, "ml-auto shrink-0")}
            >
              <MoreHorizontal aria-hidden="true" className={ICON} />
            </button>
          </PopoverTrigger>
          <PopoverContent
            align="end"
            className="flex w-auto max-w-[min(92vw,420px)] flex-row flex-wrap items-center gap-1 p-2"
            onOpenAutoFocus={(e) => e.preventDefault()}
          >
            {hidden.map((group) => (
              <div
                key={group.id}
                role="group"
                className="flex items-center gap-0.5"
              >
                {group.node}
              </div>
            ))}
          </PopoverContent>
        </Popover>
      ) : null}
    </div>
  );
}

/** Dispatched on the editor's element to open the toolbar's link form. */
export const OPEN_LINK_EVENT = "doc-open-link";
