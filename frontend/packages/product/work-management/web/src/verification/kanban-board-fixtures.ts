/**
 * Deterministic status lanes for the Kanban board and lane stories: the ERP
 * board of the mockup (Cần làm 6, Đang làm 3, Chờ duyệt 2, Hoàn thành 5) and
 * its edge and dense variants. The clock is injected (12/10/2026 10:40).
 */
import {
  groupCardsByStatus,
  type Card,
  type KanbanStatusLane,
} from "@notrelix/work-management-core";
import { erpBoardScenario } from "@notrelix/work-management-testing";

export const KANBAN_STORY_NOW = new Date("2026-10-12T10:40:00+07:00");

/** The ERP board's lanes, in option order. */
export function erpKanbanLanes(): KanbanStatusLane[] {
  const { board, groups } = erpBoardScenario();
  return groupCardsByStatus({
    fieldDefinitions: board.fieldDefinitions,
    groups,
  });
}

/** The same lanes with no card (a new board). */
export function erpKanbanEmptyLanes(): KanbanStatusLane[] {
  return erpKanbanLanes().map((lane) => ({ ...lane, cards: [], total: 0 }));
}

/** The "khong-ket-qua" state: a filter that leaves every lane empty. */
export function erpKanbanNoResultLanes(): KanbanStatusLane[] {
  return erpKanbanLanes().map((lane) => ({ ...lane, cards: [] }));
}

/**
 * Edge data: a very long title, an item without key or label, a long lane
 * name and the lane for items without a status.
 */
export function erpKanbanEdgeLanes(): KanbanStatusLane[] {
  const lanes = erpKanbanLanes();
  const [todo, doing, review, done] = lanes as [
    KanbanStatusLane,
    KanbanStatusLane,
    KanbanStatusLane,
    KanbanStatusLane,
  ];
  const first = todo.cards[0]!;
  const longTitle: Card = {
    ...first,
    id: "edge-long-title",
    title:
      "Đối soát toàn bộ số dư công nợ phải thu và phải trả của mười hai chi nhánh trước khi khoá sổ kế toán quý IV và chuyển số liệu sang hệ thống ERP mới",
  };
  const bare: Card = {
    ...first,
    id: "edge-bare",
    title: "Việc chưa có mã, nhãn hay hạn",
    itemKey: null,
    labels: [],
    members: [],
    dueAt: null,
    dueDate: undefined,
    childProgress: null,
    priority: undefined,
  };
  const withoutStatus: KanbanStatusLane = {
    optionId: null,
    name: "",
    category: null,
    color: null,
    cards: [{ ...bare, id: "edge-no-status", statusOptionId: null }],
    total: 1,
  };
  return [
    { ...todo, cards: [longTitle, bare, ...todo.cards.slice(1, 3)], total: 4 },
    {
      ...doing,
      name: "Đang triển khai tại các chi nhánh miền Nam và miền Trung",
    },
    review,
    done,
    withoutStatus,
  ];
}

/** High density: every lane holds many cards (the lane shows four). */
export function erpKanbanDenseLanes(): KanbanStatusLane[] {
  return erpKanbanLanes().map((lane) => {
    const cards = Array.from({ length: 6 }, (_, round) =>
      lane.cards.map((card) => ({
        ...card,
        id: `${card.id}-${round}`,
        itemKey: card.itemKey ? `${card.itemKey}.${round}` : card.itemKey,
      })),
    ).flat();
    return { ...lane, cards, total: cards.length };
  });
}
