import { useId, useState, type ReactNode } from "react";
import {
  AlignCenter,
  AlignJustify,
  AlignLeft,
  AlignRight,
  AtSign,
  ChevronDown,
  Highlighter,
  IndentDecrease,
  IndentIncrease,
  Image as ImageIcon,
  Link2,
  List,
  ListChecks,
  ListOrdered,
  MessageCircle,
  Pencil,
  Printer,
  Redo2,
  Table2,
  Undo2,
  type LucideIcon,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { BlockType } from "@notrelix/docs-core";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import type { DocPageCallbacks } from "./doc-page-models";
import { DocFileMenuSurface } from "./doc-file-menu-surface";
import { ICON_CLASS } from "./doc-page-ui";

/** What the toolbar reflects: the block holding the caret. */
export interface DocToolbarCurrent {
  readonly blockType: BlockType | null;
  /** Font size in px shown in the size box. */
  readonly fontSize: number;
  readonly bold: boolean;
  readonly italic: boolean;
  readonly underline: boolean;
  readonly strike: boolean;
  readonly align: "left" | "center" | "right";
}

export const DEFAULT_TOOLBAR_CURRENT: DocToolbarCurrent = {
  blockType: null,
  fontSize: 16,
  bold: false,
  italic: false,
  underline: false,
  strike: false,
  align: "left",
};

export interface DocToolbarSurfaceProps {
  readonly copy: DocPageCopy;
  readonly current?: DocToolbarCurrent;
  readonly fileMenuOpen: boolean;
  readonly downloadOpen?: boolean;
  readonly canShare?: boolean;
  /** Block-level formatting needs a block to act on. */
  readonly canFormat?: boolean;
  readonly callbacks?: DocPageCallbacks;
}

const MENU_BUTTON =
  "h-7 rounded-[6px] border-0 bg-transparent px-[9px] text-compact leading-[1.45] text-muted-foreground outline-none hover:bg-state-hover-strong hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default";

const TOOL =
  "box-border inline-flex h-7.5 min-w-7.5 items-center justify-center gap-1 rounded-[6px] border-0 bg-transparent px-1.5 text-compact leading-[1.45] text-muted-foreground outline-none hover:bg-state-hover-strong hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default aria-disabled:hover:bg-transparent aria-disabled:hover:text-muted-foreground";

const TOOL_ON = "bg-state-selected text-state-selected-foreground";

const SELECT =
  "box-border inline-flex h-7.5 items-center justify-center gap-1.5 rounded-[6px] border border-line-strong bg-doc-panel px-2 text-compact leading-[1.45] text-foreground outline-none hover:bg-doc-panel focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default";

function Separator() {
  return (
    <span
      aria-hidden="true"
      className="mx-[5px] h-5 w-px shrink-0 bg-line-strong"
    />
  );
}

const STYLE_TYPES: readonly BlockType[] = [
  "paragraph",
  "heading_1",
  "heading_2",
  "heading_3",
  "quote",
  "code",
];

function styleLabel(copy: DocPageCopy, type: BlockType | null): string {
  const styles = copy.toolbar.paragraphStyles as Record<string, string>;
  return (type && styles[type]) ?? copy.toolbar.paragraphStyles.paragraph;
}

/**
 * The document menu bar and formatting toolbar (Final-Docs `.tbar`): six menu
 * buttons over a wrapping tool row, and the File menu when open. Tools with no
 * producer yet render disabled with the stated reason.
 */
export function DocToolbarSurface({
  copy,
  current = DEFAULT_TOOLBAR_CURRENT,
  fileMenuOpen,
  downloadOpen = false,
  canShare = false,
  canFormat = false,
  callbacks,
}: DocToolbarSurfaceProps) {
  const t = copy.toolbar;
  const styleMenuId = useId();
  const [styleOpen, setStyleOpen] = useState(false);

  const unavailable = (name: string) => fillDocCopy(t.unavailable, { name });

  function tool(
    key: string,
    label: string,
    icon: ReactNode,
    options: {
      readonly on?: boolean;
      readonly pressed?: boolean;
      readonly onClick?: () => void;
      readonly className?: string;
    } = {},
  ) {
    const enabled = Boolean(options.onClick);
    return (
      <button
        key={key}
        type="button"
        aria-label={label}
        aria-pressed={options.pressed}
        aria-disabled={enabled ? undefined : true}
        title={enabled ? label : unavailable(label)}
        onClick={options.onClick}
        className={cn(TOOL, options.on && TOOL_ON, options.className)}
      >
        {icon}
      </button>
    );
  }

  const icon = (Icon: LucideIcon) => (
    <Icon aria-hidden="true" className={ICON_CLASS} />
  );

  const glyph = (paths: readonly string[]) => (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className={ICON_CLASS}
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      {paths.map((d) => (
        <path key={d} d={d} />
      ))}
    </svg>
  );
  const onFormat = canFormat ? callbacks?.onFormat : undefined;
  const isHeading =
    current.blockType === "heading_1" ||
    current.blockType === "heading_2" ||
    current.blockType === "heading_3";

  return (
    <div className="relative z-20 shrink-0 border-b border-border bg-doc-page">
      <nav aria-label={copy.menus.label} className="flex px-4 pt-0.5">
        {(
          [
            ["file", copy.menus.file],
            ["edit", copy.menus.edit],
            ["view", copy.menus.view],
            ["insert", copy.menus.insert],
            ["format", copy.menus.format],
            ["tools", copy.menus.tools],
          ] as const
        ).map(([key, label]) =>
          key === "file" ? (
            <button
              key={key}
              type="button"
              aria-haspopup="menu"
              aria-expanded={fileMenuOpen}
              onClick={
                fileMenuOpen
                  ? callbacks?.onCloseOverlay
                  : callbacks?.onOpenFileMenu
              }
              className={cn(
                MENU_BUTTON,
                fileMenuOpen && "bg-state-hover-strong text-foreground",
              )}
            >
              {label}
            </button>
          ) : (
            <button
              key={key}
              type="button"
              aria-haspopup="menu"
              aria-disabled="true"
              title={fillDocCopy(copy.menus.unavailable, { name: label })}
              className={MENU_BUTTON}
            >
              {label}
            </button>
          ),
        )}
      </nav>
      <div
        role="toolbar"
        aria-label={t.label}
        className="flex flex-wrap items-center gap-0.5 px-3 pt-1 pb-1.5"
      >
        {tool("undo", t.undo, icon(Undo2))}
        {tool("redo", t.redo, icon(Redo2))}
        {tool("print", t.print, icon(Printer), { onClick: callbacks?.onPrint })}
        <Separator />
        <div className="relative inline-flex">
          <button
            type="button"
            aria-haspopup="listbox"
            aria-expanded={styleOpen}
            aria-controls={styleOpen ? styleMenuId : undefined}
            aria-label={t.paragraphStyle}
            aria-disabled={
              onFormat && callbacks?.onParagraphStyle ? undefined : true
            }
            onClick={() =>
              canFormat && callbacks?.onParagraphStyle
                ? setStyleOpen((open) => !open)
                : undefined
            }
            className={SELECT}
          >
            {styleLabel(copy, current.blockType)}
            <ChevronDown
              aria-hidden="true"
              className="size-3 stroke-[1.8] text-meta-foreground"
            />
          </button>
          {styleOpen ? (
            <div
              id={styleMenuId}
              role="listbox"
              aria-label={t.paragraphStyle}
              className="absolute top-8 left-0 z-dropdown w-[180px] rounded-[10px] border border-line-strong bg-doc-panel p-1.5 shadow-popover"
            >
              {STYLE_TYPES.map((type) => (
                <button
                  key={type}
                  type="button"
                  role="option"
                  aria-selected={current.blockType === type}
                  onClick={() => {
                    callbacks?.onParagraphStyle?.(type);
                    setStyleOpen(false);
                  }}
                  className="flex h-8.5 w-full items-center rounded-[7px] border-0 bg-transparent px-2.5 text-left text-nav font-normal text-foreground outline-none hover:bg-state-hover-strong focus-visible:bg-state-hover-strong aria-selected:bg-state-hover-strong leading-[1.45]"
                >
                  {styleLabel(copy, type)}
                </button>
              ))}
            </div>
          ) : null}
        </div>
        <button
          type="button"
          aria-haspopup="listbox"
          aria-label={t.font}
          aria-disabled="true"
          title={unavailable(t.font)}
          className={SELECT}
        >
          {t.fontFamily}
          <ChevronDown
            aria-hidden="true"
            className="size-3 stroke-[1.8] text-meta-foreground"
          />
        </button>
        <span
          role="group"
          aria-label={t.fontSize}
          className="box-content inline-flex h-[30px] items-center overflow-hidden rounded-[6px] border border-line-strong"
        >
          <button
            type="button"
            aria-label={t.fontSizeDown}
            aria-disabled="true"
            title={unavailable(t.fontSizeDown)}
            className="h-7 w-6 border-0 bg-transparent text-muted-foreground outline-none"
          >
            −
          </button>
          <span className="min-w-[26px] text-center text-compact leading-[1.45] tabular-nums">
            {current.fontSize}
          </span>
          <button
            type="button"
            aria-label={t.fontSizeUp}
            aria-disabled="true"
            title={unavailable(t.fontSizeUp)}
            className="h-7 w-6 border-0 bg-transparent text-muted-foreground outline-none"
          >
            +
          </button>
        </span>
        <Separator />
        {tool("bold", t.bold, <span className="font-bold">B</span>, {
          on: current.bold || isHeading,
          pressed: current.bold || isHeading,
          onClick: onFormat ? () => onFormat("bold") : undefined,
          className: "font-bold",
        })}
        {tool("italic", t.italic, <span className="italic">I</span>, {
          on: current.italic,
          pressed: current.italic,
          onClick: onFormat ? () => onFormat("italic") : undefined,
        })}
        {tool("underline", t.underline, <span className="underline">U</span>, {
          on: current.underline,
          pressed: current.underline,
          onClick: onFormat ? () => onFormat("underline") : undefined,
        })}
        {tool("strike", t.strike, <span className="line-through">S</span>, {
          on: current.strike,
          pressed: current.strike,
          onClick: onFormat ? () => onFormat("strike") : undefined,
        })}
        {tool(
          "textColor",
          t.textColor,
          <span className="flex flex-col items-center leading-none font-bold">
            A
            <i className="mt-0.5 block h-[3px] w-3.5 rounded-[1px] bg-danger-text" />
          </span>,
        )}
        {tool("highlight", t.highlight, icon(Highlighter))}
        <Separator />
        {tool("link", t.link, icon(Link2))}
        {tool("image", t.image, icon(ImageIcon))}
        {tool("table", t.table, icon(Table2))}
        {tool("mention", t.mention, icon(AtSign), {
          onClick: callbacks?.onMentionInsert,
        })}
        {tool("comment", t.comment, icon(MessageCircle), {
          onClick: callbacks?.onOpenComments,
        })}
        <Separator />
        {tool("left", t.alignLeft, icon(AlignLeft), {
          on: current.align === "left",
          pressed: current.align === "left",
          onClick: onFormat ? () => onFormat("left") : undefined,
        })}
        {tool("center", t.alignCenter, icon(AlignCenter), {
          on: current.align === "center",
          pressed: current.align === "center",
          onClick: onFormat ? () => onFormat("center") : undefined,
        })}
        {tool("right", t.alignRight, icon(AlignRight), {
          on: current.align === "right",
          pressed: current.align === "right",
          onClick: onFormat ? () => onFormat("right") : undefined,
        })}
        {tool("justify", t.alignJustify, icon(AlignJustify))}
        {tool(
          "spacing",
          t.lineSpacing,
          glyph(["M11 6h10M11 12h10M11 18h10M5 4v16M2 7l3-3 3 3M2 17l3 3 3-3"]),
        )}
        <Separator />
        {tool("bulleted", t.bulleted, icon(List), {
          onClick: canFormat
            ? () => callbacks?.onParagraphStyle?.("bulleted_list")
            : undefined,
        })}
        {tool("numbered", t.numbered, icon(ListOrdered), {
          onClick: canFormat
            ? () => callbacks?.onParagraphStyle?.("numbered_list")
            : undefined,
        })}
        {tool("checklist", t.checklist, icon(ListChecks), {
          onClick: canFormat
            ? () => callbacks?.onParagraphStyle?.("todo")
            : undefined,
        })}
        {tool("outdent", t.outdent, icon(IndentDecrease))}
        {tool("indent", t.indent, icon(IndentIncrease))}
        {tool(
          "clear",
          t.clearFormat,
          glyph(["M4 7V4h16v3M9 20h6M12 4v16M3 3l18 18"]),
        )}
        <button
          type="button"
          aria-haspopup="listbox"
          aria-label={t.mode}
          aria-disabled="true"
          title={unavailable(t.mode)}
          className={cn(SELECT, "ml-auto")}
        >
          <Pencil
            aria-hidden="true"
            className="size-3.5 shrink-0 stroke-[1.8] text-foreground"
          />
          {t.modeEdit}
          <ChevronDown
            aria-hidden="true"
            className="size-3 stroke-[1.8] text-meta-foreground"
          />
        </button>
      </div>
      {fileMenuOpen ? (
        <DocFileMenuSurface
          copy={copy}
          downloadOpen={downloadOpen}
          canShare={canShare}
          callbacks={callbacks}
        />
      ) : null}
    </div>
  );
}
