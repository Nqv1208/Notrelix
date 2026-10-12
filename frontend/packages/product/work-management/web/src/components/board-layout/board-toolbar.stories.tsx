import type { Meta, StoryObj } from "@storybook/react";
import { BoardToolbar } from "./board-toolbar";
import { en, vi } from "./board-header.copy";

const noOp = () => undefined;
const meta = {
  title: "Work Management/Board Layout/Toolbar",
  component: BoardToolbar,
  parameters: { layout: "fullscreen" },
} satisfies Meta<typeof BoardToolbar>;
export default meta;
type Story = StoryObj<typeof meta>;

const access = {
  myLevel: "manager",
  mySource: { kind: "workspace-admin" },
  peopleCount: 32,
  guestCount: 1,
  sources: [
    { kind: "space", id: "space-cn", name: "Phòng Công nghệ", count: 24 },
    { kind: "team", id: "team-kt", name: "Nhóm Kế toán tổng hợp", count: 6 },
    { kind: "user", id: "user-linh", name: "Đỗ Khánh Linh", count: 1 },
    { kind: "guest", count: 1 },
  ],
} as const;

const header = {
  space: {
    name: "Phòng Công nghệ",
    initials: "CN",
    markTone: "blue",
    href: "#space-cn",
  },
  visibility: "space",
  access,
  people: [
    { id: "user-ha", name: "Nguyễn Thu Hà", initials: "HA", tone: "sage" },
    { id: "user-quan", name: "Trần Minh Quân", initials: "MQ", tone: "blue" },
    { id: "user-phuc", name: "Lê Hoàng Phúc", initials: "HP", tone: "ember" },
  ],
  onOpenSpace: noOp,
  onCopyLink: noOp,
} as const;

export const Default: Story = {
  args: {
    boardTitle: "Triển khai ERP Q4",
    activeView: "kanban",
    onViewChange: noOp,
    header,
    copy: vi,
  },
  tags: ["fui-surface--wm.board-layout.toolbar", "fui-state--Default"],
};

/** Commenter: banner, no Settings or Share. */
export const ReadOnly: Story = {
  args: {
    ...Default.args,
    header: {
      ...header,
      access: {
        ...access,
        myLevel: "commenter",
        mySource: {
          kind: "explicit",
          grantedBy: { userId: "user-phuc", name: "Lê Hoàng Phúc" },
        },
      },
    },
  },
  tags: ["fui-surface--wm.board-layout.toolbar", "fui-state--ReadOnly"],
};

/** No route data yet (or no provider): the title and the view tabs only. */
export const TitleOnly: Story = {
  name: "Title only (English)",
  args: {
    boardTitle: "Operating plan",
    activeView: "table",
    onViewChange: noOp,
    copy: en,
  },
};
