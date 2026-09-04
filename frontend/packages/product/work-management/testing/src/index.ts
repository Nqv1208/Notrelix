/**
 * @notrelix/wm-testing — Work Management verification fixtures.
 *
 * Usable by core/state/web/mobile tests.
 * No production dependency on this package.
 */

export { boardFixture, boardGroupFixture } from "./fixtures/board.fixture";
export { cardFixture } from "./fixtures/card.fixture";
export {
  createKanbanScenario,
  kanbanDefaultScenario,
  kanbanDenseScenario,
  kanbanEdgeScenario,
  kanbanEmptyScenario,
  type KanbanScenario,
  type KanbanScenarioOptions,
} from "./scenarios/kanban.scenario";
export { itemFixture } from "./fixtures/item.fixture";
export { fieldFixture } from "./fixtures/field.fixture";
export { memberFixture, boardMemberFixture } from "./fixtures/member.fixture";
export { labelFixture } from "./fixtures/label.fixture";
export {
  checklistFixture,
  checklistItemFixture,
} from "./fixtures/checklist.fixture";
export {
  activityFixture,
  cardDetailFixture,
  commentFixture,
  fileFixture,
  updateFixture,
} from "./fixtures/card-detail.fixture";
export {
  editorCapabilities,
  ownerCapabilities,
  viewerCapabilities,
  type WorkManagementUiCapabilities,
} from "./fixtures/capabilities.fixture";
export { createBoardSnapshot } from "./factories/create-board-snapshot";
export { createBoardPatch } from "./factories/create-board-patch";
export { mockCommandBus } from "./mocks/mock-command-bus";
export { fixedIso, FIXED_NOW, fixedClock } from "./support/fixed-clock";
export type {
  CalendarScenarioData,
  KanbanScenarioData,
  TableScenarioData,
  TaskDetailScenarioData,
  TimelineScenarioData,
  UiScenarioState,
  WorkManagementScenario,
} from "./scenarios/types";
export {
  kanbanDefaultUiScenario,
  kanbanReadOnlyScenario,
} from "./scenarios/kanban.scenario";
export {
  createTableScenario,
  tableDefaultScenario,
  tableDefaultUiScenario,
  tableDenseScenario,
  tableEdgeScenario,
  tableEmptyScenario,
  tableReadOnlyScenario,
} from "./scenarios/table.scenario";
export {
  calendarDefaultScenario,
  calendarDenseScenario,
  calendarEdgeScenario,
  calendarEmptyScenario,
} from "./scenarios/calendar.scenario";
export {
  timelineDefaultScenario,
  timelineDenseScenario,
  timelineEdgeScenario,
  timelineEmptyScenario,
} from "./scenarios/timeline.scenario";
export {
  taskDetailDefaultScenario,
  taskDetailDefaultUiScenario,
  taskDetailEdgeScenario,
  taskDetailLoadingScenario,
  taskDetailReadOnlyScenario,
  taskDetailUnavailableScenario,
} from "./scenarios/task-detail.scenario";
export { createKanbanUiController } from "./controllers/kanban-ui-controller";
export { createTableUiController } from "./controllers/table-ui-controller";
export { createTaskDetailUiController } from "./controllers/task-detail-ui-controller";
