import type { Meta, StoryObj } from "@storybook/react";
import { vi } from "./board-header.copy";
import {
  BoardHeaderSurface,
  type BoardHeaderAccess,
  type BoardHeaderPerson,
} from "./board-header-surface";

const noOp = () => undefined;

const meta = {
  title: "Work Management/Board Layout/Header",
  component: BoardHeaderSurface,
  parameters: { layout: "fullscreen" },
} satisfies Meta<typeof BoardHeaderSurface>;
export default meta;
type Story = StoryObj<typeof meta>;

/** Board "Triển khai ERP Q4" as Hà (workspace admin) sees it. */
const erpAccess: BoardHeaderAccess = {
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
};

const erpPeople: readonly BoardHeaderPerson[] = [
  { id: "user-ha", name: "Nguyễn Thu Hà", initials: "HA", tone: "sage" },
  { id: "user-quan", name: "Trần Minh Quân", initials: "MQ", tone: "blue" },
  { id: "user-phuc", name: "Lê Hoàng Phúc", initials: "HP", tone: "ember" },
];

const erpSpace = {
  name: "Phòng Công nghệ",
  initials: "CN",
  markTone: "blue",
  href: "#space-cn",
} as const;

export const Default: Story = {
  args: {
    title: "Triển khai ERP Q4",
    copy: vi,
    space: erpSpace,
    visibility: "space",
    access: erpAccess,
    people: erpPeople,
    onOpenSpace: noOp,
    onCopyLink: noOp,
  },
  tags: ["fui-surface--wm.board-layout.header", "fui-state--Default"],
};

/** Đỗ Khánh Linh: explicit commenter entry added by Lê Hoàng Phúc. */
export const ReadOnly: Story = {
  args: {
    ...Default.args,
    access: {
      ...erpAccess,
      myLevel: "commenter",
      mySource: {
        kind: "explicit",
        grantedBy: { userId: "user-phuc", name: "Lê Hoàng Phúc" },
      },
    },
  },
  tags: ["fui-surface--wm.board-layout.header", "fui-state--ReadOnly"],
};

/** A private board outside any Space, a long title, sources the caller cannot name. */
export const EdgeData: Story = {
  args: {
    title:
      "Kế hoạch chuyển đổi toàn bộ quy trình kế toán, kho vận và mua hàng sang hệ thống ERP mới trước kỳ quyết toán cuối năm",
    copy: vi,
    space: null,
    visibility: "private",
    access: {
      myLevel: "editor",
      mySource: { kind: "team" },
      peopleCount: 1204,
      guestCount: 12,
      sources: [
        { kind: "team", count: 1180 },
        { kind: "user", count: 12 },
        { kind: "guest", count: 12 },
      ],
    },
    people: [],
    onCopyLink: noOp,
  },
  tags: ["fui-surface--wm.board-layout.header", "fui-state--EdgeData"],
};
