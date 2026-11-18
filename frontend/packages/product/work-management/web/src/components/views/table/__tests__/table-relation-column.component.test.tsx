import { describe, expect, it, vi } from "vitest";
import { renderPureUi, screen } from "@notrelix/testing";
import type { FieldDefinition } from "@notrelix/work-management-core";
import { mainTableCopy } from "../main-table.copy";
import { fieldColumn } from "../table-columns";
import { TableFieldCell } from "../table-field-cell";
import { TableHeaderRow } from "../table-header-row";

const copy = mainTableCopy("en");
const relation = {
  id: "field-rel",
  boardId: "board-erp",
  name: "Việc liên quan",
  fieldType: "relation",
  options: [],
  position: 1,
  isHidden: false,
  isSystemField: false,
} as FieldDefinition;

describe("a Relation column of the table", () => {
  it("carries the type lock in its header, even for a member who can edit fields", () => {
    renderPureUi(
      <TableHeaderRow
        copy={copy}
        columns={[fieldColumn(relation)]}
        widths={{}}
        sort={null}
        allChecked={false}
        canSelect={false}
        canEditFields
        onToggleAll={vi.fn()}
        onWidthPreview={vi.fn()}
        onWidthCommit={vi.fn()}
        onWidthReset={vi.fn()}
      />,
    );
    expect(
      screen.getByLabelText(`Việc liên quan: ${copy.fields.lockType}`),
    ).toBeTruthy();
  });

  it.each([true, false])(
    "shows the link count and no viewer lock in a cell (editable %s)",
    (editable) => {
      renderPureUi(
        <TableFieldCell
          field={relation}
          cell={{ value: ["a", "b"] }}
          rowKey="ERP-142"
          copy={copy}
          members={[]}
          editable={editable}
          onWrite={vi.fn()}
          onRetry={vi.fn()}
          onKeepMine={vi.fn()}
          onUseTheirs={vi.fn()}
          onReloadSchema={vi.fn()}
        />,
      );
      expect(screen.getByText("2")).toBeTruthy();
      expect(screen.queryByRole("img", { name: /Việc liên quan/ })).toBeNull();
    },
  );
});
