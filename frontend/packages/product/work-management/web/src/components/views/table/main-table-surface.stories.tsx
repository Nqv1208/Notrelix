import { useMemo, useReducer } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import {
  createTableUiController,
  tableDefaultScenario,
  tableDenseScenario,
  tableEdgeScenario,
  tableEmptyScenario,
  tableReadOnlyScenario,
} from "@notrelix/work-management-testing";
import type { TableScenarioData } from "@notrelix/work-management-testing";
import type { Card, UpdateCardInput } from "@notrelix/work-management-core";
import { MainTableSurface } from "./main-table-surface";

const meta = {
  title: "Work Management/Table/Main Table",
} satisfies Meta;
export default meta;
type Story = StoryObj;

function findCard(scenario: TableScenarioData, cardId: string): Card | null {
  return (
    scenario.groups
      .flatMap((group) => group.cards)
      .find((card) => card.id === cardId) ?? null
  );
}

function TableSurfaceStory({
  scenario,
  readOnly = false,
}: {
  scenario: TableScenarioData;
  readOnly?: boolean;
}) {
  const [, rerender] = useReducer((count: number) => count + 1, 0);
  const controller = useMemo(
    () => createTableUiController(scenario),
    [scenario],
  );
  const state = controller.state;
  const selectedCardIdSet = useMemo(
    () => new Set(state.selectedCardIds),
    [state.selectedCardIds],
  );

  function apply(update: () => void) {
    if (readOnly) return;
    update();
    rerender();
  }

  return (
    <div className="h-[720px] min-h-0 rounded-lg border bg-card">
      <MainTableSurface
        board={state.board}
        columns={state.columns}
        groups={state.groups}
        fieldDefinitions={state.columns.map((column) => column.field)}
        selectedCardIds={state.selectedCardIds}
        selectedCardIdSet={selectedCardIdSet}
        isAllSelected={false}
        activeDetailCardId={state.openCardId}
        searchQuery=""
        hiddenFieldIds={[]}
        onSearchChange={() => undefined}
        onNewTaskIntent={() => {
          const firstGroupId = state.groups[0]?.id;
          if (firstGroupId)
            document.getElementById(`add-card-${firstGroupId}`)?.focus();
        }}
        onCreateGroup={() => undefined}
        onCreateColumn={() => undefined}
        onClearFilters={() => undefined}
        onSetFilters={() => undefined}
        onClearSort={() => undefined}
        onSetSort={() => undefined}
        onSetGroupBy={() => undefined}
        onResetTableView={() => undefined}
        onToggleFieldVisible={() => undefined}
        onDeleteSelectedCards={() => undefined}
        onToggleAll={() => undefined}
        onResizeColumn={() => undefined}
        onHideColumn={() => undefined}
        onRenameColumn={() => undefined}
        onDeleteColumn={() => undefined}
        onSetCardSelected={(cardId, selected) =>
          apply(() => controller.toggleRow(cardId, selected))
        }
        onOpenDetail={(cardId) => apply(() => controller.openRow(cardId))}
        onToggleGroup={(groupId) =>
          apply(() => {
            const group = state.groups.find((item) => item.id === groupId);
            if (group) group.isCollapsed = !group.isCollapsed;
          })
        }
        onCreateTask={(groupId, title) =>
          apply(() => {
            controller.addTask(groupId, title);
          })
        }
        onRenameGroup={(groupId, title) =>
          apply(() => controller.renameGroup(groupId, title))
        }
        onUpdateGroupColor={(groupId, color) =>
          apply(() => {
            const group = state.groups.find((item) => item.id === groupId);
            if (group) group.color = color;
          })
        }
        onDuplicateGroup={() => undefined}
        onDeleteGroup={() => undefined}
        onDuplicateCard={() => undefined}
        onDeleteCard={() => undefined}
        onUpdateCard={(cardId, patch: UpdateCardInput) =>
          apply(() => {
            const card = findCard(state, cardId);
            if (card && patch.title) card.title = patch.title;
          })
        }
        onUpdateFieldValue={({ cardId, fieldDefinitionId, value }) =>
          apply(() => controller.editCell(cardId, fieldDefinitionId, value))
        }
        onMoveRow={() => undefined}
      />
    </div>
  );
}

export const Default: Story = {
  render: () => <TableSurfaceStory scenario={tableDefaultScenario()} />,
  tags: ["fui-surface--wm.table.main", "fui-state--Default"],
};

export const Empty: Story = {
  render: () => <TableSurfaceStory scenario={tableEmptyScenario()} />,
  tags: ["fui-surface--wm.table.main", "fui-state--Empty"],
};

export const EdgeData: Story = {
  render: () => <TableSurfaceStory scenario={tableEdgeScenario()} />,
  tags: ["fui-surface--wm.table.main", "fui-state--EdgeData"],
};

export const HighDensity: Story = {
  render: () => <TableSurfaceStory scenario={tableDenseScenario()} />,
  tags: ["fui-surface--wm.table.main", "fui-state--HighDensity"],
};

export const ReadOnly: Story = {
  render: () => (
    <TableSurfaceStory scenario={tableReadOnlyScenario().data} readOnly />
  ),
  tags: ["fui-surface--wm.table.main", "fui-state--ReadOnly"],
};
