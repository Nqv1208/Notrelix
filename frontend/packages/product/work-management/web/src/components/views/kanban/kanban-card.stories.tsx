import type { Meta, StoryObj } from "@storybook/react";
import {
  cardFixture,
  labelFixture,
  memberFixture,
} from "@notrelix/work-management-testing";
import { KanbanCard } from "./kanban-card";
import { kanbanCardCopy } from "./kanban-card.copy";

const noOp = () => undefined;
const meta = {
  title: "Work Management/Kanban/Card",
  component: KanbanCard,
} satisfies Meta<typeof KanbanCard>;
export default meta;
type Story = StoryObj<typeof meta>;

/** The ERP board clock: 12/10/2026 10:40. */
const now = new Date(2026, 9, 12, 10, 40);
const copy = kanbanCardCopy("vi");

function dueOn(month: number, day: number): string {
  return new Date(2026, month - 1, day, 17, 0).toISOString();
}

const quan = memberFixture({
  id: "member-quan",
  userId: "user-quan",
  name: "Trần Minh Quân",
  initials: "MQ",
});
const phuc = memberFixture({
  id: "member-phuc",
  userId: "user-phuc",
  name: "Lê Hoàng Phúc",
  initials: "HP",
});

const erp142 = cardFixture({
  id: "card-erp-142",
  itemKey: "ERP-142",
  title: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
  statusCategory: "in_progress",
  labels: [labelFixture({ id: "label-data", name: "Dữ liệu" })],
  childProgress: { done: 3, total: 5 },
  dueAt: dueOn(10, 10),
  priority: "urgent",
  members: [quan],
});

export const Default: Story = {
  args: { card: erp142, now, copy, onOpenDetails: noOp },
  tags: ["fui-surface--wm.kanban.card", "fui-state--Default"],
};

/** Long title (three-line clamp), no key, no label, 0/0 progress. */
export const EdgeData: Story = {
  args: {
    now,
    copy,
    onOpenDetails: noOp,
    card: cardFixture({
      id: "card-story-edge",
      itemKey: null,
      title:
        "Rà soát toàn bộ quy trình đối soát công nợ phải thu giữa phân hệ Kế toán, phân hệ Bán hàng và báo cáo quản trị hằng tháng trước khi chốt số liệu quý bốn",
      labels: [],
      childProgress: { done: 0, total: 0 },
      dueAt: dueOn(10, 18),
      priority: "medium",
      members: [
        memberFixture({
          name: "Bartholomew Maximilian Featherstonehaugh-Cholmondeley",
          initials: "BM",
        }),
        quan,
        phuc,
      ],
    }),
  },
  tags: ["fui-surface--wm.kanban.card", "fui-state--EdgeData"],
};

/** Overdue by one day, high priority, due today on the sibling story. */
export const Overdue: Story = {
  args: {
    now,
    copy,
    onOpenDetails: noOp,
    card: cardFixture({
      id: "card-story-overdue",
      itemKey: "ERP-151",
      title: "Cấu hình quyền truy cập theo phòng ban cho phân hệ Kế toán",
      labels: [labelFixture({ id: "label-sec", name: "Bảo mật" })],
      childProgress: { done: 0, total: 4 },
      dueAt: dueOn(10, 11),
      priority: "high",
      members: [phuc],
    }),
  },
  tags: ["fui-surface--wm.kanban.card", "fui-state--EdgeData"],
};

/** Overdue by twelve days. */
export const OverdueTwelveDays: Story = {
  args: {
    now,
    copy,
    onOpenDetails: noOp,
    card: cardFixture({
      id: "card-story-overdue-12",
      itemKey: "ERP-150",
      title: "Thiết lập cảnh báo khi đồng bộ tồn kho thất bại",
      labels: [labelFixture({ id: "label-test", name: "Kiểm thử" })],
      dueAt: dueOn(9, 30),
      priority: "urgent",
      members: [quan],
    }),
  },
  tags: ["fui-surface--wm.kanban.card", "fui-state--EdgeData"],
};

/** Due today. */
export const DueToday: Story = {
  args: {
    now,
    copy,
    onOpenDetails: noOp,
    card: cardFixture({
      id: "card-story-today",
      itemKey: "ERP-144",
      title: "Kiểm thử phân hệ công nợ với số liệu tháng 9",
      labels: [labelFixture({ id: "label-test", name: "Kiểm thử" })],
      childProgress: { done: 6, total: 9 },
      dueAt: dueOn(10, 12),
      priority: "high",
      members: [phuc],
    }),
  },
  tags: ["fui-surface--wm.kanban.card", "fui-state--EdgeData"],
};

/** No due date, no key, no label, no assignee. */
export const NoDueNoStatus: Story = {
  args: {
    now,
    copy,
    onOpenDetails: noOp,
    card: cardFixture({
      id: "card-story-bare",
      title: "Ý tưởng chưa phân loại",
      itemKey: null,
      statusCategory: null,
      dueAt: null,
      members: [],
    }),
  },
  tags: ["fui-surface--wm.kanban.card", "fui-state--EdgeData"],
};

/** Done: muted title, no due wording. */
export const Done: Story = {
  args: {
    now,
    copy,
    onOpenDetails: noOp,
    card: cardFixture({
      id: "card-story-done",
      itemKey: "ERP-131",
      title: "Sao lưu cơ sở dữ liệu tự động hằng đêm",
      statusCategory: "done",
      labels: [labelFixture({ id: "label-infra", name: "Hạ tầng" })],
      dueAt: dueOn(10, 2),
      members: [
        memberFixture({
          id: "member-thanh",
          userId: "user-thanh",
          name: "Vũ Thanh",
          initials: "VT",
        }),
      ],
    }),
  },
  tags: ["fui-surface--wm.kanban.card", "fui-state--EdgeData"],
};

/** Selected: the item sheet is open for this card. */
export const Selected: Story = {
  args: { card: erp142, now, copy, selected: true, onOpenDetails: noOp },
  tags: ["fui-surface--wm.kanban.card", "fui-state--EdgeData"],
};

/** English copy. */
export const English: Story = {
  args: {
    card: erp142,
    now,
    copy: kanbanCardCopy("en"),
    onOpenDetails: noOp,
  },
  tags: ["fui-surface--wm.kanban.card", "fui-state--EdgeData"],
};
