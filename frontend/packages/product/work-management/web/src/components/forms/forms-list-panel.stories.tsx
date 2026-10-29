import type { Meta, StoryObj } from "@storybook/react";
import { formsCopyFixture } from "../../verification/forms-fixtures";
import {
  FormsListPanel,
  type BoardFormsPanelProps,
  type FormListRow,
} from "./forms-list-panel";

const ok = async () => ({ ok: true }) as const;
const rows: FormListRow[] = [
  {
    id: "f-1",
    boardId: "b",
    title: "Yêu cầu hỗ trợ IT",
    status: "published",
    createdAt: "2026-10-05T02:00:00Z",
    questionCount: 5,
    newSubmissionCount: 3,
  },
  {
    id: "f-2",
    boardId: "b",
    title:
      "Đề xuất mua sắm thiết bị và phần mềm cho chi nhánh Đà Nẵng và Hải Phòng quý 4",
    status: "draft",
    createdAt: "2026-10-09T02:00:00Z",
    questionCount: 4,
    newSubmissionCount: 0,
  },
  {
    id: "f-3",
    boardId: "b",
    title: "Báo lỗi ERP",
    status: "closed",
    createdAt: "2026-09-20T02:00:00Z",
    questionCount: 6,
    newSubmissionCount: 0,
  },
];

const props = (
  overrides: Partial<BoardFormsPanelProps> = {},
): BoardFormsPanelProps => ({
  copy: formsCopyFixture("vi"),
  locale: "vi",
  status: "ready",
  forms: rows,
  onReload: () => undefined,
  onCreate: ok,
  onPublish: ok,
  onClose: ok,
  onDelete: ok,
  onOpenBuilder: () => undefined,
  onOpenResponses: () => undefined,
  onCopyLink: async () => true,
  ...overrides,
});

const meta: Meta = {
  title: "Work Management/Forms/List",
  parameters: { layout: "fullscreen" },
};
export default meta;
type Story = StoryObj;

/** The board's forms: a published one with new responses, a draft, a closed one. */
export const Default: Story = {
  tags: ["fui-surface--wm.forms.list", "fui-state--Default"],
  render: () => (
    <div className="flex h-[640px]">
      <FormsListPanel {...props()} />
    </div>
  ),
};

const framed = (overrides: Partial<BoardFormsPanelProps>, h = 480) => (
  <div className={`flex`} style={{ height: h }}>
    <FormsListPanel {...props(overrides)} />
  </div>
);

/** The forms are on their way. */
export const Loading: Story = {
  tags: ["fui-surface--wm.forms.list", "fui-state--Loading"],
  render: () => framed({ status: "loading", forms: [] }),
};

/** The list could not load: the message offers Retry. */
export const Error: Story = {
  tags: ["fui-surface--wm.forms.list", "fui-state--Error"],
  render: () => framed({ status: "error", forms: [] }),
};

/** Nothing yet: the empty state offers the first form. */
export const Empty: Story = {
  tags: ["fui-surface--wm.forms.list", "fui-state--Empty"],
  render: () => framed({ forms: [] }),
};

/** Forty forms: the list scrolls inside its panel. */
export const HighDensity: Story = {
  tags: ["fui-surface--wm.forms.list", "fui-state--HighDensity"],
  render: () =>
    framed(
      {
        forms: Array.from({ length: 40 }, (_, n) => ({
          ...rows[n % rows.length]!,
          id: `f-${n}`,
          title: `${rows[n % rows.length]!.title} (${n + 1})`,
        })),
      },
      640,
    ),
};

/** One very long title wraps and does not push the row actions out. */
export const EdgeData: Story = {
  tags: ["fui-surface--wm.forms.list", "fui-state--EdgeData"],
  render: () =>
    framed({
      forms: [
        {
          ...rows[1]!,
          title: `${rows[1]!.title}, kèm đầy đủ biên bản nghiệm thu, báo giá của ba nhà cung cấp và ý kiến của kế toán trưởng`,
        },
      ],
    }),
};
