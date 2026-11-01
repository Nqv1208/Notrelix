import { useRef, type ReactNode } from "react";
import { cn } from "@notrelix/ui-web";
import type { DocPageCopy } from "./doc-page.copy";
import type { DocPageCallbacks } from "./doc-page-models";
import { useMenuKeys } from "./use-menu-keys";
import { shortcutKeys, shortcutLabel } from "../editor/shortcut-catalog";

export interface DocFileMenuSurfaceProps {
  readonly copy: DocPageCopy;
  /** The Download submenu is open (the design shows it beside the menu). */
  readonly downloadOpen: boolean;
  readonly canShare: boolean;
  readonly callbacks?: DocPageCallbacks;
  /** ArrowLeft / ArrowRight: hand over to the neighbouring menu of the bar. */
  readonly onSide?: (direction: -1 | 1) => void;
  /** Escape: close the menu and give focus back to the File button. */
  readonly onEscape?: () => void;
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
      data-menu-item=""
      tabIndex={-1}
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
 * Move to trash and the trash list are enabled for editors; entries with no producer yet are disabled with the stated reason.
 * Download offers HTML, PDF (the browser's print) and Markdown; Save version, Save as template and Import
 * open their dialogs and are offered to people who may edit.
 */
export function DocFileMenuSurface({
  copy,
  downloadOpen,
  canShare,
  callbacks,
  onSide,
  onEscape,
}: DocFileMenuSurfaceProps) {
  const f = copy.fileMenu;
  const panel = useRef<HTMLDivElement | null>(null);
  const keys = useMenuKeys(panel, {
    onClose: (reason) =>
      reason === "escape" ? onEscape?.() : callbacks?.onCloseOverlay?.(),
    onSide,
  });
  return (
    <>
      <div
        ref={panel}
        role="menu"
        aria-label={f.label}
        data-menu-panel=""
        onKeyDown={keys}
        className={cn(PANEL, "top-[30px] left-4 w-[280px]")}
      >
        <Item
          label={f.newDoc}
          hint={shortcutLabel("Alt-n")}
          onClick={callbacks?.onNewDoc}
          unavailable={f.unavailable}
        />
        <Item
          label={f.duplicate}
          onClick={callbacks?.onDuplicatePage}
          unavailable={f.unavailable}
        />
        <Item
          label={f.rename}
          onClick={callbacks?.onRenamePage}
          unavailable={f.unavailable}
        />
        <Item
          label={f.move}
          popup="dialog"
          onClick={callbacks?.onMovePage}
          unavailable={f.unavailable}
        />
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
          hint={shortcutLabel(shortcutKeys("print"))}
          onClick={callbacks?.onPrint}
          unavailable={f.unavailable}
        />
        <Item
          label={f.pageSetup}
          popup="dialog"
          onClick={callbacks?.onPageSetupOpen}
          unavailable={f.unavailable}
        />
        <Separator />
        <Item
          label={f.versionHistory}
          hint={shortcutLabel("Mod-Alt-Shift-h")}
          onClick={callbacks?.onOpenVersions}
          unavailable={f.unavailable}
        />
        <Item
          label={f.saveVersion}
          popup="dialog"
          onClick={callbacks?.onSaveVersionOpen}
          unavailable={f.unavailable}
        />
        <Item
          label={f.saveTemplate}
          popup="dialog"
          onClick={callbacks?.onSaveTemplateOpen}
          unavailable={f.unavailable}
        />
        <Item
          label={f.importPage}
          popup="dialog"
          onClick={callbacks?.onImportOpen}
          unavailable={f.unavailable}
        />
        <Item
          label={f.info}
          popup="dialog"
          onClick={callbacks?.onShowPageInfo}
          unavailable={f.unavailable}
        />
        <Separator />
        <Item
          label={f.openTrash}
          onClick={callbacks?.onOpenTrash}
          unavailable={f.unavailable}
        />
        <Item
          label={f.trash}
          danger
          onClick={callbacks?.onTrashPage}
          unavailable={f.unavailable}
        />
      </div>
      {downloadOpen ? <DownloadMenu copy={copy} callbacks={callbacks} /> : null}
    </>
  );
}

/** The Download submenu: HTML, PDF (the browser's print) and Markdown. */
function DownloadMenu({
  copy,
  callbacks,
}: {
  copy: DocPageCopy;
  callbacks?: DocPageCallbacks;
}) {
  const f = copy.fileMenu;
  const panel = useRef<HTMLDivElement | null>(null);
  const keys = useMenuKeys(panel, {
    onClose: (reason) =>
      reason === "escape"
        ? callbacks?.onToggleDownload?.()
        : callbacks?.onCloseOverlay?.(),
    onBack: () => callbacks?.onToggleDownload?.(),
  });
  return (
    <div
      ref={panel}
      role="menu"
      aria-label={f.downloadLabel}
      data-menu-panel=""
      onKeyDown={keys}
      className={cn(PANEL, "top-[150px] left-[282px] w-[250px]")}
    >
      <Item
        label={f.html}
        onClick={() => callbacks?.onDownload?.("html")}
        unavailable={f.unavailable}
        leading={<FileType label="HTM" className="bg-doc-file-word" />}
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
  );
}
