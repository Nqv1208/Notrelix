import type { BoardTableColumn } from "@notrelix/work-management-core";
import { boardFixture, boardGroupFixture } from "../fixtures/board.fixture";
import { cardFixture } from "../fixtures/card.fixture";
import { fieldFixture } from "../fixtures/field.fixture";
import {
  ownerCapabilities,
  viewerCapabilities,
} from "../fixtures/capabilities.fixture";
import type { TableScenarioData, WorkManagementScenario } from "./types";

function tableColumns(): BoardTableColumn[] {
  return [
    {
      id: "col-title",
      field: fieldFixture({ id: "field-title", name: "Task" }),
      width: 280,
      minWidth: 220,
      isVisible: true,
    },
    {
      id: "col-status",
      field: fieldFixture({
        id: "field-status",
        name: "Status",
        fieldType: "select",
      }),
      width: 160,
      minWidth: 140,
      isVisible: true,
    },
    {
      id: "col-due",
      field: fieldFixture({
        id: "field-due-date",
        name: "Due date",
        fieldType: "date",
      }),
      width: 160,
      minWidth: 140,
      isVisible: true,
    },
  ];
}

export function createTableScenario(
  seed = "default",
  groupCount = 3,
  cardsPerGroup = 4,
): TableScenarioData {
  const board = boardFixture({
    id: `board-table-${seed}`,
    title: `Table ${seed}`,
  });
  return {
    board,
    columns: tableColumns(),
    groups: Array.from({ length: groupCount }, (_, groupIndex) =>
      boardGroupFixture({
        id: `table-group-${seed}-${groupIndex + 1}`,
        title: `Group ${groupIndex + 1}`,
        cards: Array.from({ length: cardsPerGroup }, (_, cardIndex) =>
          cardFixture({
            id: `table-card-${seed}-${groupIndex + 1}-${cardIndex + 1}`,
            boardId: board.id,
            listId: `table-group-${seed}-${groupIndex + 1}`,
            title: `Table card ${groupIndex + 1}.${cardIndex + 1}`,
          }),
        ),
      }),
    ),
  };
}

export const tableDefaultScenario = () => createTableScenario();
export const tableEmptyScenario = () => createTableScenario("empty", 0, 0);
export const tableEdgeScenario = () => createTableScenario("edge", 2, 2);
export const tableDenseScenario = () => createTableScenario("dense", 10, 30);
export const tableReadOnlyScenario =
  (): WorkManagementScenario<TableScenarioData> => ({
    id: "table-read-only",
    state: "ReadOnly",
    capabilities: viewerCapabilities,
    data: createTableScenario("read-only"),
  });
export const tableDefaultUiScenario =
  (): WorkManagementScenario<TableScenarioData> => ({
    id: "table-default",
    state: "Default",
    capabilities: ownerCapabilities,
    data: tableDefaultScenario(),
  });
