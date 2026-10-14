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

const WORKSPACE_ID = "mock-workspace-primary";
const ERP_ITEM = `/workspaces/${WORKSPACE_ID}/boards/mock-board-erp/items`;
const UNREAD_NOTIFICATIONS = 3;

/**
 * The member's notifications in the 2026-10 mockups' world: the first three
 * are the Inbox mockup's unread ones (12 min, 1 h and 2 h 40 min before the
 * mockup clock, Monday 12/10 10:40); the rest are older and read.
 */
const NOTIFICATION_TEMPLATES: readonly {
  type: MockNotificationRecord["type"];
  title: string;
  body: string;
  link: string;
  minutesAgo: number;
}[] = [
  {
    type: "assignment",
    title:
      "Trần Minh Quân gửi bạn duyệt Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
    body: "ERP-138 · Triển khai ERP Q4 · cần duyệt trước thứ Tư 14/10",
    link: `${ERP_ITEM}/mock-erp-item-138`,
    minutesAgo: 12,
  },
  {
    type: "mention",
    title: "Lý Thu Trang nhắc tên bạn trong Quy trình nhập hoá đơn đầu vào",
    body: "“chị xem giúp đề xuất 3 ngày đối chiếu cuối tháng trong quy trình nhập hoá đơn.”",
    link: `/workspaces/${WORKSPACE_ID}/docs/mock-doc-invoice-process`,
    minutesAgo: 60,
  },
  {
    type: "system",
    title:
      "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP đã quá hạn 2 ngày",
    body: "Bạn đang theo dõi việc này. ERP-142 · Trần Minh Quân phụ trách.",
    link: `${ERP_ITEM}/mock-erp-item-142`,
    minutesAgo: 160,
  },
  {
    type: "comment",
    title:
      "Đỗ Khánh Linh bình luận ở Kiểm thử phân hệ công nợ với số liệu tháng 9",
    body: "Đã đối chiếu xong 6/9 phân hệ, còn công nợ phải trả.",
    link: `${ERP_ITEM}/mock-erp-item-144`,
    minutesAgo: 1440 + 20,
  },
  {
    type: "status_change",
    title:
      "Lê Hoàng Phúc chuyển Chốt danh sách người dùng đợt 1 sang Hoàn thành",
    body: "ERP-136 · Triển khai ERP Q4",
    link: `${ERP_ITEM}/mock-erp-item-136`,
    minutesAgo: 1440 + 95,
  },
  {
    type: "assignment",
    title:
      "Vũ Thanh giao cho bạn Thiết lập cảnh báo khi đồng bộ tồn kho thất bại",
    body: "ERP-150 · Triển khai ERP Q4 · hạn 28/10",
    link: `${ERP_ITEM}/mock-erp-item-150`,
    minutesAgo: 2 * 1440 + 30,
  },
  {
    type: "mention",
    title:
      "Phan Ngọc Anh nhắc tên bạn trong Cấu hình quyền truy cập theo phòng ban",
    body: "“@Hà cho mình xin danh sách phòng ban cần phân quyền.”",
    link: `${ERP_ITEM}/mock-erp-item-151`,
    minutesAgo: 3 * 1440 + 15,
  },
  {
    type: "status_change",
    title:
      "Vũ Thanh chuyển Sao lưu cơ sở dữ liệu tự động hằng đêm sang Hoàn thành",
    body: "ERP-131 · Triển khai ERP Q4",
    link: `${ERP_ITEM}/mock-erp-item-131`,
    minutesAgo: 6 * 1440 + 45,
  },
  {
    type: "comment",
    title:
      "Vũ Thanh bình luận ở Cài đặt máy chủ ứng dụng cho môi trường kiểm thử",
    body: "Đã cài xong, đang chờ cấp quyền truy cập.",
    link: `${ERP_ITEM}/mock-erp-item-133`,
    minutesAgo: 8 * 1440 + 50,
  },
  {
    type: "invitation",
    title: "Lê Hoàng Phúc mời bạn vào bảng Triển khai ERP Q4",
    body: "Bạn tham gia với vai trò Quản trị.",
    link: `/workspaces/${WORKSPACE_ID}/boards/mock-board-erp`,
    minutesAgo: 9 * 1440 + 10,
  },
  {
    type: "assignment",
    title: "Lê Hoàng Phúc giao cho bạn Chốt danh sách người dùng đợt 1",
    body: "ERP-136 · Triển khai ERP Q4 · hạn 09/10",
    link: `${ERP_ITEM}/mock-erp-item-136`,
    minutesAgo: 10 * 1440 + 35,
  },
  {
    type: "status_change",
    title:
      "Phan Ngọc Anh chuyển Khảo sát quy trình nghiệp vụ hiện tại của phòng Kế toán sang Hoàn thành",
    body: "ERP-135 · Triển khai ERP Q4",
    link: `${ERP_ITEM}/mock-erp-item-135`,
    minutesAgo: 17 * 1440 + 20,
  },
];

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
      const template =
        NOTIFICATION_TEMPLATES[index % NOTIFICATION_TEMPLATES.length]!;
      const week = Math.floor(index / NOTIFICATION_TEMPLATES.length);
      return {
        id: padId("notif", index),
        workspaceId: WORKSPACE_ID,
        userId,
        type: template.type,
        title: template.title,
        body: template.body,
        link: template.link,
        isRead: index >= UNREAD_NOTIFICATIONS || week > 0,
        isArchived: false,
        createdAt: clock.offsetMinutes(
          template.minutesAgo * -1 - week * 21 * 1440,
        ),
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
