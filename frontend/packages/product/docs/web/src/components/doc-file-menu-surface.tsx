import type { ReactNode } from "react";
import { cn } from "@notrelix/ui-web";
import type { DocPageCopy } from "./doc-page.copy";
import type { DocPageCallbacks } from "./doc-page-models";

export interface DocFileMenuSurfaceProps {
  readonly copy: DocPageCopy;
  /** The Download submenu is open (the design shows it beside the menu). */
  readonly downloadOpen: boolean;
  readonly canShare: boolean;
  readonly callbacks?: DocPageCallbacks;
}

const PANEL =
  "absolute box-content z-dropdown rounded-[10px] border border-line-strong bg-doc-panel p-1.5 shadow-popover";

const ITEM =
  "flex h-8.5 w-full items-center gap-2.5 rounded-[7px] border-0 bg-transparent px-2.5 text-left text-nav leading-[1.45] font-normal text-foreground outline-none hover:bg-state-hover-strong focus-visible:bg-state-hover-strong aria-disabled:cursor-default aria-disabled:hover:bg-transparent";

function Separator() {
  return <div role="separator" className="mx-1 my-[5px] h-px bg-border" />;
}

function Item({
  label,
  hint,
  onClick,
  unavailable,
  active,
  danger,
  popup,
  expanded,
  leading,
  trailing,
}: {
  label: string;
  hint?: string;
  onClick?: () => void;
  unavailable: string;
  active?: boolean;
  danger?: boolean;
  popup?: "menu" | "dialog";
  expanded?: boolean;
  leading?: ReactNode;
  trailing?: ReactNode;
}) {
  const enabled = Boolean(onClick);
  return (
    <button
      type="button"
      role="menuitem"
      aria-haspopup={popup}
      aria-expanded={expanded}
      aria-disabled={enabled ? undefined : true}
      title={enabled ? undefined : unavailable}
      onClick={onClick}
      className={cn(
        ITEM,
        active && "bg-state-hover-strong",
        danger && "text-danger-text aria-disabled:text-danger-text",
      )}
    >
      {leading}
      {label}
      {hint ? (
        <span className="ml-auto text-metadata text-meta-foreground leading-[1.45] tracking-normal">
          {hint}
        </span>
      ) : null}
      {trailing}
    </button>
  );
}

function FileType({ label, className }: { label: string; className: string }) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "grid size-6.5 shrink-0 place-items-center rounded-[6px] text-avatar-md leading-[1.45] font-bold text-white",
        className,
      )}
    >
      {label}
    </span>
  );
}

/**
 * The File menu and its Download submenu (Final-Docs `.menu` and `.sub`).
 * Entries with no producer yet are disabled with the stated reason; Word
 * export carries the design's "Sắp có" tag.
 */
export function DocFileMenuSurface({
  copy,
  downloadOpen,
  canShare,
  callbacks,
}: DocFileMenuSurfaceProps) {
  const f = copy.fileMenu;
  return (
    <>
      <div
        role="menu"
        aria-label={f.label}
        className={cn(PANEL, "top-[30px] left-4 w-[280px]")}
      >
        <Item
          label={f.newDoc}
          hint="⌥N"
          onClick={callbacks?.onNewDoc}
          unavailable={f.unavailable}
        />
        <Item label={f.duplicate} unavailable={f.unavailable} />
        <Item
          label={f.share}
          popup="dialog"
          onClick={canShare ? callbacks?.onOpenShare : undefined}
          unavailable={f.unavailable}
        />
        <Separator />
        <Item
          label={f.download}
          hint="›"
          popup="menu"
          expanded={downloadOpen}
          active={downloadOpen}
          onClick={callbacks?.onToggleDownload}
          unavailable={f.unavailable}
        />
        <Item
          label={f.print}
          hint="⌘P"
          onClick={callbacks?.onPrint}
          unavailable={f.unavailable}
        />
        <Item label={f.pageSetup} unavailable={f.unavailable} />
        <Separator />
        <Item
          label={f.versionHistory}
          hint="⌘⌥⇧H"
          onClick={callbacks?.onOpenVersions}
          unavailable={f.unavailable}
        />
        <Item label={f.info} unavailable={f.unavailable} />
        <Separator />
        <Item label={f.trash} danger unavailable={f.unavailable} />
      </div>
      {downloadOpen ? (
        <div
          role="menu"
          aria-label={f.downloadLabel}
          className={cn(PANEL, "top-[150px] left-[282px] w-[250px]")}
        >
          <Item
            label={f.word}
            active
            unavailable={f.unavailable}
            leading={<FileType label="DOC" className="bg-doc-file-word" />}
            trailing={
              <span className="ml-auto rounded-full bg-doc-chip-review px-[7px] py-px text-mark-md leading-[1.45] font-semibold text-warning-text">
                {f.soon}
              </span>
            }
          />
          <Item
            label={f.pdf}
            onClick={() => callbacks?.onDownload?.("pdf")}
            unavailable={f.unavailable}
            leading={<FileType label="PDF" className="bg-doc-file-pdf" />}
          />
          <Item
            label={f.markdown}
            onClick={() => callbacks?.onDownload?.("markdown")}
            unavailable={f.unavailable}
            leading={<FileType label="MD" className="bg-doc-file-text" />}
          />
        </div>
      ) : null}
    </>
  );
}
