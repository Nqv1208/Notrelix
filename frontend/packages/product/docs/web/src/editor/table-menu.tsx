import { useEditorState, type Editor } from "@tiptap/react";
import {
  BetweenHorizontalEnd,
  BetweenHorizontalStart,
  BetweenVerticalEnd,
  BetweenVerticalStart,
  Columns3,
  PaintBucket,
  Rows3,
  TableProperties,
  Trash2,
} from "lucide-react";
import { HIGHLIGHT_TOKENS } from "@notrelix/docs-core";
import { Popover, PopoverContent, PopoverTrigger } from "@notrelix/ui-web";
import type { DocPageCopy } from "../components/doc-page.copy";
import { SwatchGrid, ToolButton } from "./editor-toolbar";
import { HIGHLIGHT_CLASS } from "./mark-classes";
import {
  TABLE_COMMANDS,
  selectTableState,
  setCellBackground,
} from "./table-commands";

const ICON = "size-4 stroke-[1.8]";

/** Row, column, header and cell background tools above the table under the caret. */
export function TableMenu({
  editor,
  copy,
}: {
  readonly editor: Editor;
  readonly copy: DocPageCopy;
}) {
  const state = useEditorState({
    editor,
    selector: ({ editor: current }) => selectTableState(current),
  });
  if (!state.active || state.pos === null) return null;
  const t = copy.tableTools;
  const table = editor.view.nodeDOM(state.pos);
  const host = editor.view.dom.parentElement?.getBoundingClientRect();
  const rect =
    table instanceof HTMLElement ? table.getBoundingClientRect() : null;
  return (
    <div
      role="toolbar"
      aria-label={t.label}
      style={{
        top: (rect?.top ?? 0) - (host?.top ?? 0) - 40,
        left: Math.max(0, (rect?.left ?? 0) - (host?.left ?? 0)),
      }}
      className="absolute z-dropdown flex items-center gap-0.5 rounded-[10px] border border-line-strong bg-doc-panel p-1 shadow-popover"
    >
      <ToolButton
        label={t.rowBefore}
        onClick={() => TABLE_COMMANDS.rowBefore(editor)}
      >
        <BetweenHorizontalStart aria-hidden="true" className={ICON} />
      </ToolButton>
      <ToolButton
        label={t.rowAfter}
        onClick={() => TABLE_COMMANDS.rowAfter(editor)}
      >
        <BetweenHorizontalEnd aria-hidden="true" className={ICON} />
      </ToolButton>
      <ToolButton
        label={t.columnBefore}
        onClick={() => TABLE_COMMANDS.columnBefore(editor)}
      >
        <BetweenVerticalStart aria-hidden="true" className={ICON} />
      </ToolButton>
      <ToolButton
        label={t.columnAfter}
        onClick={() => TABLE_COMMANDS.columnAfter(editor)}
      >
        <BetweenVerticalEnd aria-hidden="true" className={ICON} />
      </ToolButton>
      <span aria-hidden="true" className="mx-1 h-5 w-px bg-border" />
      <ToolButton
        label={t.headerRow}
        pressed={state.headerRow}
        onClick={() => TABLE_COMMANDS.toggleHeaderRow(editor)}
      >
        <TableProperties aria-hidden="true" className={ICON} />
      </ToolButton>
      <Popover>
        <PopoverTrigger asChild>
          <button
            type="button"
            data-tool=""
            aria-label={t.cellBackground}
            title={t.cellBackground}
            onMouseDown={(event) => event.preventDefault()}
            className="box-border inline-flex h-7.5 min-w-7.5 items-center justify-center rounded-[6px] border-0 bg-transparent px-1.5 text-muted-foreground outline-none hover:bg-state-hover-strong hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <PaintBucket aria-hidden="true" className={ICON} />
          </button>
        </PopoverTrigger>
        <PopoverContent
          className="w-auto gap-2 p-2.5"
          onOpenAutoFocus={(e) => e.preventDefault()}
        >
          <SwatchGrid
            label={t.cellBackground}
            tokens={HIGHLIGHT_TOKENS}
            names={copy.toolbar.highlightNames}
            swatch={(token) => HIGHLIGHT_CLASS[token] || "bg-transparent"}
            current={state.bg}
            onPick={(token) => setCellBackground(editor, token)}
          />
        </PopoverContent>
      </Popover>
      <span aria-hidden="true" className="mx-1 h-5 w-px bg-border" />
      <ToolButton
        label={t.deleteRow}
        onClick={() => TABLE_COMMANDS.deleteRow(editor)}
      >
        <Rows3 aria-hidden="true" className={ICON} />
      </ToolButton>
      <ToolButton
        label={t.deleteColumn}
        onClick={() => TABLE_COMMANDS.deleteColumn(editor)}
      >
        <Columns3 aria-hidden="true" className={ICON} />
      </ToolButton>
      <ToolButton
        label={t.deleteTable}
        onClick={() => TABLE_COMMANDS.deleteTable(editor)}
      >
        <Trash2 aria-hidden="true" className={ICON} />
      </ToolButton>
    </div>
  );
}
