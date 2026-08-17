/**
 * Deterministic record factories.
 *
 * Creates internal MockRecords (not DTOs) from seed + index.
 * No uncontrolled Math.random() or new Date() calls.
 *
 * Plan: 03-MOCK-DATA-MODEL.md §Factories, §Determinism
 */

import { defaultClock, type MockClock } from "./clock";
import type {
  MockUserRecord,
  MockWorkspaceRecord,
  MockMembershipRecord,
  MockWorkspaceViewRecord,
  MockBoardRecord,
  MockListRecord,
  MockCardRecord,
  MockNotificationRecord,
  MockPageRecord,
} from "./records";

function padId(prefix: string, index: number): string {
  return `${prefix}-${String(index).padStart(5, "0")}`;
}

const PLANS = ["free", "pro", "business", "enterprise"] as const;
const ICONS = [
  "Layout",
  "Layers",
  "BarChart",
  "Kanban",
  "FileText",
  "Globe",
] as const;
const BG_COLORS = [
  "#1E90FF",
  "#FC744C",
  "#22C55E",
  "#A855F7",
  "#F59E0B",
  "#EF4444",
] as const;
const LIST_COLORS = [
  "#1E90FF",
  "#FC744C",
  "#22C55E",
  "#A855F7",
  "#F59E0B",
] as const;
const VIEW_TYPES = ["kanban", "table", "doc", "calendar", "timeline"] as const;
const MEMBER_COLORS = [
  "#1E90FF",
  "#FC744C",
  "#22C55E",
  "#A855F7",
  "#F59E0B",
  "#EF4444",
  "#06B6D4",
  "#84CC16",
] as const;

export interface MockFactories {
  readonly clock: MockClock;
  user(index: number, overrides?: Partial<MockUserRecord>): MockUserRecord;
  workspace(
    index: number,
    overrides?: Partial<MockWorkspaceRecord>,
  ): MockWorkspaceRecord;
  membership(
    index: number,
    workspaceId: string,
    userId: string,
    overrides?: Partial<MockMembershipRecord>,
  ): MockMembershipRecord;
  view(
    index: number,
    workspaceId: string,
    overrides?: Partial<MockWorkspaceViewRecord>,
  ): MockWorkspaceViewRecord;
  board(
    index: number,
    workspaceId: string,
    overrides?: Partial<MockBoardRecord>,
  ): MockBoardRecord;
  list(
    index: number,
    boardId: string,
    overrides?: Partial<MockListRecord>,
  ): MockListRecord;
  card(
    index: number,
    boardId: string,
    listId: string,
    overrides?: Partial<MockCardRecord>,
  ): MockCardRecord;
  notification(
    index: number,
    userId: string,
    overrides?: Partial<MockNotificationRecord>,
  ): MockNotificationRecord;
  page(
    index: number,
    workspaceId: string,
    overrides?: Partial<MockPageRecord>,
  ): MockPageRecord;
}

export function createFactories(
  clock: MockClock = defaultClock,
): MockFactories {
  return {
    clock,

    user(index, overrides = {}) {
      return {
        id: padId("user", index),
        email: `user-${index}@notrelix.local`,
        name: `User ${index}`,
        avatarUrl: null,
        ...overrides,
      };
    },

    workspace(index, overrides = {}) {
      return {
        id: padId("ws", index),
        name: `Workspace ${index}`,
        slug: `workspace-${index}`,
        plan: PLANS[index % PLANS.length]!,
        icon: ICONS[index % ICONS.length]!,
        isPersonal: false,
        ...overrides,
      };
    },

    membership(index, workspaceId, userId, overrides = {}) {
      return {
        id: padId("mem", index),
        workspaceId,
        userId,
        role: "member",
        status: "active",
        workload: index % 10,
        color: MEMBER_COLORS[index % MEMBER_COLORS.length]!,
        joinedAt: clock.offsetDays(-(index * 3)),
        ...overrides,
      };
    },

    view(index, workspaceId, overrides = {}) {
      return {
        id: padId("view", index),
        workspaceId,
        name: `View ${index}`,
        type: VIEW_TYPES[index % VIEW_TYPES.length]!,
        icon: ICONS[index % ICONS.length]!,
        description: `View ${index} description`,
        visibility: "workspace",
        isDefault: index === 0,
        position: index,
        createdAt: clock.offsetDays(-index),
        ...overrides,
      };
    },

    board(index, workspaceId, overrides = {}) {
      return {
        id: padId("board", index),
        workspaceId,
        title: `Board ${index}`,
        description: `Board ${index} description`,
        background: {
          type: "color",
          value: BG_COLORS[index % BG_COLORS.length]!,
        },
        visibility: "workspace",
        isArchived: false,
        createdAt: clock.offsetDays(-(index * 2)),
        updatedAt: clock.offsetDays(-index),
        ...overrides,
      };
    },

    list(index, boardId, overrides = {}) {
      return {
        id: padId("list", index),
        boardId,
        title: `List ${index}`,
        color: LIST_COLORS[index % LIST_COLORS.length],
        position: index,
        isCollapsed: false,
        ...overrides,
      };
    },

    card(index, boardId, listId, overrides = {}) {
      return {
        id: padId("card", index),
        boardId,
        listId,
        title: `Task ${index}: Sample work item`,
        description: `Description for task ${index}. This is sample content.`,
        position: index,
        createdAt: clock.offsetSeconds(-(index * 300)),
        updatedAt: clock.offsetSeconds(-(index * 60)),
        ...overrides,
      };
    },

    notification(index, userId, overrides = {}) {
      return {
        id: padId("notif", index),
        userId,
        title: `Notification ${index}`,
        message: `You were mentioned in a card (notification ${index}).`,
        isRead: index % 3 === 0,
        createdAt: clock.offsetSeconds(-(index * 600)),
        ...overrides,
      };
    },

    page(index, workspaceId, overrides = {}) {
      return {
        id: padId("page", index),
        workspaceId,
        title: `Page ${index}`,
        icon: "FileText",
        parentId: undefined,
        createdAt: clock.offsetDays(-index),
        updatedAt: clock.offsetDays(-index),
        ...overrides,
      };
    },
  };
}

export const defaultFactories: MockFactories = createFactories(defaultClock);
