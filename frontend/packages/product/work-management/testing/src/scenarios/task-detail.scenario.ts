import { boardFixture } from "../fixtures/board.fixture";
import { cardDetailFixture } from "../fixtures/card-detail.fixture";
import type { TaskDetailScenarioData, WorkManagementScenario } from "./types";
import {
  ownerCapabilities,
  viewerCapabilities,
} from "../fixtures/capabilities.fixture";

export const taskDetailDefaultScenario = (): TaskDetailScenarioData => ({
  board: boardFixture(),
  card: cardDetailFixture(),
  isLoading: false,
  error: null,
});

export const taskDetailLoadingScenario = (): TaskDetailScenarioData => ({
  board: boardFixture(),
  card: null,
  isLoading: true,
  error: null,
});

export const taskDetailUnavailableScenario = (): TaskDetailScenarioData => ({
  board: boardFixture(),
  card: null,
  isLoading: false,
  error: "Task unavailable",
});

export const taskDetailEdgeScenario = (): TaskDetailScenarioData => ({
  board: boardFixture({ title: "Đa ngôn ngữ — 日本語 — 🚀" }),
  card: cardDetailFixture({
    title: "A very long task detail title used for edge rendering verification",
  }),
  isLoading: false,
  error: null,
});

export const taskDetailReadOnlyScenario =
  (): WorkManagementScenario<TaskDetailScenarioData> => ({
    id: "task-detail-read-only",
    state: "ReadOnly",
    capabilities: viewerCapabilities,
    data: taskDetailDefaultScenario(),
  });

export const taskDetailDefaultUiScenario =
  (): WorkManagementScenario<TaskDetailScenarioData> => ({
    id: "task-detail-default",
    state: "Default",
    capabilities: ownerCapabilities,
    data: taskDetailDefaultScenario(),
  });
