import { describe, expect, it } from "vitest";
import {
  boardFixture,
  boardGroupFixture,
  boardMemberFixture,
  calendarDefaultScenario,
  calendarDenseScenario,
  calendarEdgeScenario,
  calendarEmptyScenario,
  cardDetailFixture,
  checklistFixture,
  commentFixture,
  createKanbanScenario,
  createKanbanUiController,
  createTableUiController,
  createTaskDetailUiController,
  createTableScenario,
  editorCapabilities,
  fieldFixture,
  fixedClock,
  fixedIso,
  FIXED_NOW,
  itemFixture,
  kanbanDefaultUiScenario,
  kanbanReadOnlyScenario,
  labelFixture,
  memberFixture,
  ownerCapabilities,
  tableDefaultScenario,
  tableDefaultUiScenario,
  tableDenseScenario,
  tableEdgeScenario,
  tableEmptyScenario,
  tableReadOnlyScenario,
  taskDetailDefaultScenario,
  taskDetailDefaultUiScenario,
  taskDetailEdgeScenario,
  taskDetailLoadingScenario,
  taskDetailReadOnlyScenario,
  taskDetailUnavailableScenario,
  timelineDefaultScenario,
  timelineDenseScenario,
  timelineEdgeScenario,
  timelineEmptyScenario,
  viewerCapabilities,
} from "../index";

function serialize(value: unknown): string {
  return JSON.stringify(value);
}

describe("Work Management verification fixtures and scenarios", () => {
  it("provides complete deterministic entity fixtures", () => {
    expect(boardFixture()).toEqual(boardFixture());
    expect(boardGroupFixture()).toEqual(boardGroupFixture());
    expect(boardMemberFixture()).toEqual(boardMemberFixture());
    expect(memberFixture()).toEqual(memberFixture());
    expect(labelFixture()).toEqual(labelFixture());
    expect(checklistFixture()).toEqual(checklistFixture());
    expect(commentFixture()).toEqual(commentFixture());
    expect(cardDetailFixture()).toEqual(cardDetailFixture());
    expect(itemFixture()).toEqual(itemFixture());
    expect(fieldFixture()).toEqual(fieldFixture());
  });

  it("uses a fixed verification clock with fresh Date instances", () => {
    const first = fixedClock();
    const second = fixedClock();

    expect(first.toISOString()).toBe(FIXED_NOW);
    expect(second.toISOString()).toBe(FIXED_NOW);
    expect(first).not.toBe(second);
    expect(fixedIso(1)).toBe("2026-01-16T12:00:00.000Z");
  });

  it("defines framework-neutral scenario/capability contracts", () => {
    expect(ownerCapabilities.canDelete).toBe(true);
    expect(editorCapabilities.canDelete).toBe(false);
    expect(viewerCapabilities.canCreateCard).toBe(false);
    expect(kanbanDefaultUiScenario().capabilities).toBe(ownerCapabilities);
    expect(kanbanReadOnlyScenario().capabilities).toBe(viewerCapabilities);
    expect(tableDefaultUiScenario().data.groups.length).toBeGreaterThan(0);
    expect(tableReadOnlyScenario().state).toBe("ReadOnly");
    expect(taskDetailDefaultUiScenario().data.card?.title).toBe("Test card");
    expect(taskDetailReadOnlyScenario().capabilities).toBe(viewerCapabilities);
  });

  it("provides deterministic Kanban, Table, Calendar, Timeline and Task Detail scenarios", () => {
    expect(createKanbanScenario({ seed: "stable" })).toEqual(
      createKanbanScenario({ seed: "stable" }),
    );
    expect(createTableScenario("stable")).toEqual(
      createTableScenario("stable"),
    );
    expect(calendarDefaultScenario()).toEqual(calendarDefaultScenario());
    expect(timelineDefaultScenario()).toEqual(timelineDefaultScenario());
    expect(taskDetailDefaultScenario()).toEqual(taskDetailDefaultScenario());

    expect(tableDefaultScenario().groups).toHaveLength(3);
    expect(tableEmptyScenario().groups).toHaveLength(0);
    expect(
      tableEdgeScenario().groups.flatMap((group) => group.cards),
    ).toHaveLength(4);
    expect(
      tableDenseScenario().groups.flatMap((group) => group.cards),
    ).toHaveLength(300);
    expect(calendarEmptyScenario().groups).toEqual([]);
    expect(
      calendarEdgeScenario().groups.flatMap((group) => group.cards),
    ).toHaveLength(4);
    expect(
      calendarDenseScenario().groups.flatMap((group) => group.cards).length,
    ).toBeGreaterThan(50);
    expect(timelineEmptyScenario().groups).toEqual([]);
    expect(
      timelineEdgeScenario().groups.flatMap((group) => group.cards),
    ).toHaveLength(4);
    expect(
      timelineDenseScenario().groups.flatMap((group) => group.cards).length,
    ).toBeGreaterThan(50);
    expect(taskDetailLoadingScenario().isLoading).toBe(true);
    expect(taskDetailUnavailableScenario().error).toBe("Task unavailable");
    expect(taskDetailEdgeScenario().card?.title).toContain("very long");
  });
});

describe("Work Management local UI controllers", () => {
  it("implements deterministic isolated Kanban callbacks", () => {
    const scenario = createKanbanScenario({ seed: "controller" });
    const first = createKanbanUiController(scenario);
    const second = createKanbanUiController(scenario);

    const created = first.createCard(
      first.state.columns[0]!.id,
      "New local card",
    );
    first.openDetail(created.id);
    first.renameGroup(first.state.columns[0]!.id, "Renamed");
    first.moveCard(created.id, first.state.columns[1]!.id, 1.5);
    first.deleteCard(created.id);

    expect(second.state).toEqual({
      ...scenario,
      selectedCardId: null,
    });
    expect(first.state.selectedCardId).toBeNull();
    expect(first.state.columns[0]!.title).toBe("Renamed");
    expect(serialize(createKanbanUiController(scenario).state)).toBe(
      serialize(createKanbanUiController(scenario).state),
    );
  });

  it("implements deterministic isolated Table callbacks", () => {
    const scenario = createTableScenario("controller");
    const controller = createTableUiController(scenario);
    const fresh = createTableUiController(scenario);
    const groupId = controller.state.groups[0]!.id;
    const created = controller.addTask(groupId, "Local table task");

    controller.toggleRow(created.id, true);
    controller.openRow(created.id);
    controller.renameGroup(groupId, "Renamed table group");
    controller.editCell(created.id, "field-status", "done");

    expect(controller.state.selectedCardIds).toEqual([created.id]);
    expect(controller.state.openCardId).toBe(created.id);
    expect(controller.state.groups[0]!.title).toBe("Renamed table group");
    expect(
      controller.state.groups[0]!.cards.at(-1)?.fieldValues["field-status"],
    ).toBe("done");
    expect(fresh.state).toEqual({
      ...scenario,
      selectedCardIds: [],
      openCardId: null,
    });
  });

  it("implements deterministic isolated Task Detail callbacks", () => {
    const scenario = taskDetailDefaultScenario();
    const controller = createTaskDetailUiController(scenario);
    const fresh = createTaskDetailUiController(scenario);

    controller.renameTitle("Renamed task");
    controller.editField("field-status", "done");
    controller.selectTab("files");
    controller.addUpdate("Local update");

    expect(controller.state.card?.title).toBe("Renamed task");
    expect(controller.state.card?.fieldValues["field-status"]).toBe("done");
    expect(controller.state.activeTab).toBe("files");
    expect(controller.state.card?.updates.at(-1)?.id).toBe("local-update-2");
    expect(fresh.state).toEqual({ ...scenario, activeTab: "updates" });
  });
});
