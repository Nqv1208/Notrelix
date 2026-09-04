import type { Card } from "@notrelix/work-management-core";
import {
  ownerCapabilities,
  viewerCapabilities,
} from "../fixtures/capabilities.fixture";
import { boardFixture, boardGroupFixture } from "../fixtures/board.fixture";
import { cardFixture } from "../fixtures/card.fixture";
import type { KanbanScenarioData, WorkManagementScenario } from "./types";

export type KanbanScenario = KanbanScenarioData;

export type KanbanScenarioOptions = {
  seed: string | number;
  columnCount: number;
  cardsPerColumn: number;
  edgeProfile:
    "none" | "long-text" | "unicode" | "missing-optional" | "combined";
};

const DEFAULT_OPTIONS: KanbanScenarioOptions = {
  seed: "default",
  columnCount: 3,
  cardsPerColumn: 4,
  edgeProfile: "none",
};

function stableSegment(value: string | number): string {
  return String(value)
    .replace(/[^a-zA-Z0-9_-]/g, "-")
    .toLowerCase();
}

function edgeCard(
  profile: KanbanScenarioOptions["edgeProfile"],
  index: number,
): Partial<Card> {
  if (profile === "none") return {};
  if (profile === "long-text" || (profile === "combined" && index === 0)) {
    return {
      title:
        "A deliberately long card title that verifies wrapping without changing the deterministic fixture contract",
    };
  }
  if (profile === "unicode" || (profile === "combined" && index === 1)) {
    return { title: "Đa ngôn ngữ — 日本語 — 🚀" };
  }
  if (profile === "missing-optional" || profile === "combined") {
    return {
      descriptionMd: undefined,
      dueDate: undefined,
      priority: undefined,
      updatedAt: undefined,
    };
  }
  return {};
}

export function createKanbanScenario(
  options: Partial<KanbanScenarioOptions> = {},
): KanbanScenario {
  const config = { ...DEFAULT_OPTIONS, ...options };
  const seed = stableSegment(config.seed);
  const workspaceId = `workspace-${seed}`;
  const boardId = `board-${seed}`;
  const columns = Array.from(
    { length: config.columnCount },
    (_, columnIndex) => {
      const groupId = `group-${seed}-${columnIndex + 1}`;
      const cards = Array.from(
        { length: config.cardsPerColumn },
        (_, cardIndex) => {
          const ordinal = columnIndex * config.cardsPerColumn + cardIndex;
          return cardFixture({
            id: `card-${seed}-${columnIndex + 1}-${cardIndex + 1}`,
            listId: groupId,
            boardId,
            workspaceId,
            title: `Card ${columnIndex + 1}.${cardIndex + 1}`,
            position: cardIndex + 1,
            ...edgeCard(config.edgeProfile, ordinal),
          });
        },
      );
      return boardGroupFixture({
        id: groupId,
        title: `Column ${columnIndex + 1}`,
        position: columnIndex + 1,
        cards,
      });
    },
  );

  return {
    workspaceId,
    board: boardFixture({ id: boardId, workspaceId, title: `Kanban ${seed}` }),
    columns,
  };
}

export const kanbanDefaultScenario = (): KanbanScenario =>
  createKanbanScenario();
export const kanbanEmptyScenario = (): KanbanScenario =>
  createKanbanScenario({ seed: "empty", columnCount: 0, cardsPerColumn: 0 });
export const kanbanEdgeScenario = (): KanbanScenario =>
  createKanbanScenario({ seed: "edge", edgeProfile: "combined" });
export const kanbanDenseScenario = (): KanbanScenario =>
  createKanbanScenario({ seed: "dense", columnCount: 8, cardsPerColumn: 40 });
export const kanbanReadOnlyScenario =
  (): WorkManagementScenario<KanbanScenarioData> => ({
    id: "kanban-read-only",
    state: "ReadOnly",
    capabilities: viewerCapabilities,
    data: createKanbanScenario({ seed: "read-only" }),
  });
export const kanbanDefaultUiScenario =
  (): WorkManagementScenario<KanbanScenarioData> => ({
    id: "kanban-default",
    state: "Default",
    capabilities: ownerCapabilities,
    data: kanbanDefaultScenario(),
  });
