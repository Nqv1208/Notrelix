import type { Meta, StoryObj } from "@storybook/react";
import {
  cardFixture,
  checklistFixture,
  checklistItemFixture,
  fixedClock,
  fixedIso,
  kanbanDefaultScenario,
  memberFixture,
} from "@notrelix/work-management-testing";
import { KanbanCard } from "./kanban-card";

const noOp = () => undefined;
const normal = kanbanDefaultScenario();
const meta = {
  title: "Work Management/Kanban/Card",
  component: KanbanCard,
} satisfies Meta<typeof KanbanCard>;
export default meta;
type Story = StoryObj<typeof meta>;
const callbacks = {
  onOpenDetails: noOp,
  onCopyLink: noOp,
  onDuplicate: noOp,
  onDelete: noOp,
};
const now = fixedClock();

function people(count: number) {
  return Array.from({ length: count }, (_, index) =>
    memberFixture({
      id: `member-${index + 1}`,
      userId: `user-${index + 1}`,
      name: index === 0 ? "Avery Stone" : `Member ${index + 1}`,
      initials: index === 0 ? "AS" : `M${index + 1}`,
    }),
  );
}

export const Default: Story = {
  args: {
    ...callbacks,
    board: normal.board,
    now,
    card: cardFixture({
      id: "card-story-default",
      title: "Draft the Q3 launch brief",
      status: "status-working",
      priority: "high",
      dueDate: fixedIso(5),
      members: people(2),
      checklists: [
        checklistFixture({
          items: [
            checklistItemFixture({ id: "item-1", isDone: true }),
            checklistItemFixture({ id: "item-2", isDone: false }),
          ],
        }),
      ],
      _count: { comments: 3, attachments: 1, checklistItems: 2 },
    }),
  },
  tags: ["fui-surface--wm.kanban.card", "fui-state--Default"],
};

export const EdgeData: Story = {
  args: {
    ...callbacks,
    board: normal.board,
    now,
    card: cardFixture({
      id: "card-story-edge",
      title:
        "A deliberately long card title that verifies wrapping and the two-line clamp without changing the deterministic fixture contract",
      status: "waiting-on-legal-review-from-another-team",
      priority: "urgent",
      dueDate: fixedIso(-30),
      linkedPageId: "page-story",
      members: [
        memberFixture({
          name: "Bartholomew Maximilian Featherstonehaugh-Cholmondeley",
          initials: "BM",
        }),
        ...people(10).slice(1),
      ],
      _count: { comments: 120, attachments: 45, checklistItems: 0 },
    }),
  },
  tags: ["fui-surface--wm.kanban.card", "fui-state--EdgeData"],
};

export const Overdue: Story = {
  args: {
    ...callbacks,
    board: normal.board,
    now,
    card: cardFixture({
      id: "card-story-overdue",
      title: "Send the contract to finance",
      status: "status-stuck",
      priority: "urgent",
      dueDate: fixedIso(-4),
      members: people(1),
      _count: { comments: 1, attachments: 0, checklistItems: 0 },
    }),
  },
  tags: ["fui-surface--wm.kanban.card", "fui-state--EdgeData"],
};

export const NoDueNoStatus: Story = {
  args: {
    ...callbacks,
    board: normal.board,
    now,
    card: cardFixture({
      id: "card-story-bare",
      title: "Untriaged idea",
      status: "",
      dueDate: undefined,
      members: [],
    }),
  },
  tags: ["fui-surface--wm.kanban.card", "fui-state--EdgeData"],
};
