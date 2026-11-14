import { describe, expect, it } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import {
  countCardsByLane,
  parseCompoundFilter,
  type Board,
  type BoardGroup,
  type BoardPaging,
  type Card,
  type FieldDefinition,
} from "@notrelix/work-management-core";
import { createBoardApi } from "../api/board.api";
import { groupCardsBySelectField } from "../hooks/use-board-kanban";
import { searchMembersOf, serverFilterOf } from "../hooks/server-view-criteria";
import {
  filterKanbanLanes,
  type KanbanLaneFilter,
} from "../hooks/use-kanban-filters";

const option = (id: string, label: string, position: string) => ({
  id,
  label,
  color: "",
  position,
});

const status: FieldDefinition = {
  id: "field-status",
  boardId: "board-1",
  name: "Status",
  fieldType: "select",
  semanticRole: "status",
  position: 1,
  isHidden: false,
  isSystemField: true,
  options: [option("s-todo", "Todo", "a"), option("s-done", "Done", "b")],
};
const priority: FieldDefinition = {
  id: "field-priority",
  boardId: "board-1",
  name: "Priority",
  fieldType: "select",
  semanticRole: "priority",
  position: 2,
  isHidden: false,
  isSystemField: false,
  options: [
    option("p-high", "Cao", "a"),
    option("p-low", "Thấp", "b"),
    option("p-other", "Sometime", "c"),
  ],
};
const team: FieldDefinition = {
  id: "field-team",
  boardId: "board-1",
  name: "Team",
  fieldType: "select",
  position: 3,
  isHidden: false,
  isSystemField: false,
  options: [option("t-web", "Web", "b"), option("t-api", "API", "a")],
};

const noFacets: KanbanLaneFilter = {
  statusOptionIds: [],
  priorities: [],
  assigneeIds: [],
  labelIds: [],
  overdue: false,
};

function card(id: string, extra: Partial<Card> = {}): Card {
  return {
    id,
    listId: "group-1",
    boardId: "board-1",
    workspaceId: "ws-1",
    title: `Item ${id}`,
    position: 1,
    status: "s-todo",
    statusOptionId: "s-todo",
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues: {},
    _count: { comments: 0, attachments: 0, checklistItems: 0 },
    createdAt: "2026-10-01T00:00:00.000Z",
    ...extra,
  };
}

const group = (cards: Card[]): BoardGroup =>
  ({
    id: "group-1",
    title: "G",
    position: 1,
    isCollapsed: false,
    cards,
  }) as BoardGroup;

describe("the filter a paged read sends", () => {
  const fields = [status, priority, team];

  it("sends only the member's own filter when no facet is set", () => {
    const own = parseCompoundFilter("c2:or~field-team.has_any.t-web")!;
    expect(serverFilterOf(noFacets, own, fields)).toBe(
      "c2:or~field-team.has_any.t-web",
    );
    expect(serverFilterOf(noFacets, undefined, fields)).toBe("");
  });

  it("says the facets as all-of conditions beside the member's filter", () => {
    const own = parseCompoundFilter("c2:and~field-team.has_any.t-web")!;
    const filter = serverFilterOf(
      {
        statusOptionIds: ["s-todo"],
        priorities: ["high"],
        assigneeIds: ["u-1", "u-2"],
        labelIds: ["l-1"],
        overdue: false,
      },
      own,
      fields,
    );
    expect(filter).toBe(
      "c2:and~field-status.has_any.s-todo~field-priority.has_any.p-high~%24assignee.has_any.u-1.u-2~%24label.has_any.l-1~field-team.has_any.t-web",
    );
  });

  it("cannot say overdue, a priority without its field, or facets beside an any-of filter", () => {
    expect(
      serverFilterOf({ ...noFacets, overdue: true }, undefined, fields),
    ).toBeNull();
    expect(
      serverFilterOf({ ...noFacets, priorities: ["high"] }, undefined, [
        status,
      ]),
    ).toBeNull();
    const anyOf = parseCompoundFilter(
      "c2:or~field-team.has_any.t-web~field-team.is_empty",
    )!;
    expect(
      serverFilterOf({ ...noFacets, labelIds: ["l-1"] }, anyOf, fields),
    ).toBeNull();
  });
});

describe("the people a search names", () => {
  it("are those whose name holds the text, as the search box reads names", () => {
    const people: [string, string][] = [
      ["u-linh", "Đỗ Khánh Linh"],
      ["u-an", "Trần An"],
      ["u-linh", "Đỗ Khánh Linh"],
    ];
    expect(searchMembersOf("khanh", people)).toBe("u-linh");
    expect(searchMembersOf("  ", people)).toBe("");
    expect(searchMembersOf("zzz", people)).toBe("");
  });

  it("match the same cards the client search matches by person", () => {
    const cards = [
      card("1", {
        members: [
          {
            id: "m",
            userId: "u-an",
            name: "Trần An",
            initials: "TA",
            color: "",
          },
        ],
      }),
      card("2"),
    ];
    const lanes = [
      {
        optionId: "s-todo",
        name: "Todo",
        category: null,
        color: null,
        cards,
        total: 2,
      },
    ];
    const client = filterKanbanLanes(lanes, noFacets, {
      query: "tran",
      isOverdue: () => false,
    });
    expect(client.lanes[0]!.cards.map((c) => c.id)).toEqual(["1"]);
    expect(searchMembersOf("tran", [["u-an", "Trần An"]])).toBe("u-an");
  });
});

describe("lanes of a Select field on a paged read", () => {
  const cards = [
    card("1", { fieldValues: { "field-team": "t-web" } }),
    card("2", { fieldValues: { "field-team": ["t-api"] } }),
    card("3", { fieldValues: { "field-team": "gone" } }),
    card("4", { fieldValues: { "field-team": "t-web" }, parentItemId: "1" }),
  ];

  it("counts the loaded cards per option, a lost option in the empty lane", () => {
    const counts = countCardsByLane(
      { fieldDefinitions: [status, team], groups: [group(cards)] },
      "field-team",
    );
    expect(Object.fromEntries(counts)).toEqual({
      "t-web": 1,
      "t-api": 1,
      none: 1,
    });
  });

  it("take the server's sizes, moved by what the cache gained since", () => {
    const paging: BoardPaging = {
      lanes: {
        "t-api": {
          total: 40,
          filteredTotal: 12,
          nextCursor: "c1",
          baseLoaded: 1,
        },
        "t-web": {
          total: 1,
          filteredTotal: 1,
          nextCursor: null,
          baseLoaded: 0,
        },
        none: { total: 5, filteredTotal: 1, nextCursor: null, baseLoaded: 1 },
      },
      totalItems: 46,
      filteredTotal: 14,
      criteria: { filter: "", sort: "", groupBy: "field-team" },
    };
    const lanes = groupCardsBySelectField(team, [group(cards)], paging);
    expect(
      lanes.map((l) => [
        l.optionId,
        l.cards.length,
        l.total,
        l.filteredTotal,
        l.hasMore,
      ]),
    ).toEqual([
      ["t-web", 1, 2, 2, false],
      ["t-api", 1, 40, 12, true],
      [null, 1, 5, 1, false],
    ]);
  });

  it("keep a whole read's result as before (sizes are the cards)", () => {
    const lanes = groupCardsBySelectField(team, [group(cards)]);
    expect(lanes.map((l) => [l.optionId, l.total, l.filteredTotal])).toEqual([
      ["t-web", 1, undefined],
      ["t-api", 1, undefined],
      [null, 1, undefined],
    ]);
  });
});

describe("the board read on the wire", () => {
  function recordingClient(response: unknown) {
    const urls: string[] = [];
    const client = {
      api: {
        get: async (url: string) => {
          urls.push(url);
          return response;
        },
      },
    } as unknown as NotrelixClient;
    return { client, urls };
  }

  it("sends groupBy and the people of the search, and keeps them for the next pages", async () => {
    const { client, urls } = recordingClient({
      id: "board-1",
      workspaceId: "ws-1",
      title: "B",
      columns: [],
      lists: [],
      members: [],
      lanes: [
        { optionId: null, itemCount: 0, filteredCount: 0, nextCursor: null },
      ],
    });
    const read = await createBoardApi(client).getFullBoard("board-1", {
      workspaceId: "ws-1",
      page: {
        size: 50,
        filter: "",
        sort: "",
        search: "an",
        searchMembers: "u-1,u-2",
        groupBy: "field-team",
      },
    });
    const query = new URLSearchParams(urls[0]!.split("?")[1]);
    expect(query.get("groupBy")).toBe("field-team");
    expect(query.get("q")).toBe("an");
    expect(query.get("qMembers")).toBe("u-1,u-2");
    expect(read.paging?.criteria).toEqual({
      filter: "",
      sort: "",
      search: "an",
      searchMembers: "u-1,u-2",
      groupBy: "field-team",
    });
  });

  it("leaves the people out without a search, and groupBy out for the Status lanes", async () => {
    const { client, urls } = recordingClient({
      items: [],
      itemCount: 0,
      filteredCount: 0,
    });
    await createBoardApi(client).getLanePage("board-1", "none", {
      cursor: "c",
      limit: 50,
      filter: "",
      sort: "",
      searchMembers: "u-1",
      board: {} as Board,
      fieldDefinitions: [],
    });
    const query = new URLSearchParams(urls[0]!.split("?")[1]);
    expect(query.has("qMembers")).toBe(false);
    expect(query.has("groupBy")).toBe(false);
  });
});
