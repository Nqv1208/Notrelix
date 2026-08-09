import type {
  Board,
  BoardGroup,
  BoardMember,
  Card,
  FieldDefinition,
  FieldOption,
  FullBoardResponse,
} from "../types";

const statusOptions: FieldOption[] = [
  {
    id: "status-not-started",
    label: "Not Started",
    color: "var(--muted-foreground)",
  },
  { id: "status-working", label: "Working on it", color: "var(--primary)" },
  { id: "status-stuck", label: "Stuck", color: "var(--destructive)" },
  { id: "status-done", label: "Done", color: "var(--accent)" },
  { id: "status-completed", label: "Completed", color: "var(--primary)" },
];

const priorityOptions: FieldOption[] = [
  { id: "urgent", label: "Urgent", color: "var(--destructive)" },
  { id: "high", label: "High", color: "var(--primary)" },
  { id: "medium", label: "Medium", color: "var(--accent)" },
  { id: "low", label: "Low", color: "var(--muted-foreground)" },
];

const members: BoardMember[] = [
  {
    id: "bm-ana",
    userId: "user-ana",
    name: "Ana Moreno",
    initials: "AM",
    role: "owner",
    color: "var(--primary)",
  },
  {
    id: "bm-minh",
    userId: "user-minh",
    name: "Minh Tran",
    initials: "MT",
    role: "editor",
    color: "var(--color-surface-teal)",
  },
  {
    id: "bm-sam",
    userId: "user-sam",
    name: "Sam Carter",
    initials: "SC",
    role: "editor",
    color: "var(--color-surface-sunset)",
  },
  {
    id: "bm-ivy",
    userId: "user-ivy",
    name: "Ivy Chen",
    initials: "IC",
    role: "viewer",
    color: "var(--color-brand-ocean)",
  },
];

function fieldDefinitions(boardId: string): FieldDefinition[] {
  return [
    {
      id: `${boardId}-field-title`,
      boardId,
      name: "Task",
      fieldType: "text",
      options: [],
      position: 1,
      isHidden: false,
      isSystemField: true,
    },
    {
      id: `${boardId}-field-person`,
      boardId,
      name: "Owner",
      fieldType: "person",
      options: [],
      position: 2,
      isHidden: false,
      isSystemField: true,
    },
    {
      id: `${boardId}-field-status`,
      boardId,
      name: "Status",
      fieldType: "select",
      options: statusOptions,
      position: 3,
      isHidden: false,
      isSystemField: true,
    },
    {
      id: `${boardId}-field-priority`,
      boardId,
      name: "Priority",
      fieldType: "select",
      options: priorityOptions,
      position: 4,
      isHidden: false,
      isSystemField: true,
    },
    {
      id: `${boardId}-field-due-date`,
      boardId,
      name: "Due date",
      fieldType: "date",
      options: [],
      position: 5,
      isHidden: false,
      isSystemField: true,
    },
    {
      id: `${boardId}-field-linked-page`,
      boardId,
      name: "Doc",
      fieldType: "linked_page",
      options: [],
      position: 6,
      isHidden: false,
      isSystemField: false,
    },
    {
      id: `${boardId}-field-progress`,
      boardId,
      name: "Progress",
      fieldType: "progress",
      options: [],
      position: 7,
      isHidden: false,
      isSystemField: false,
    },
  ];
}

function makeCard(
  boardId: string,
  workspaceId: string,
  listId: string,
  index: number,
  title: string,
  status: string,
  priority: Card["priority"],
  dueDate: string,
  assigneeIndex: number,
  linkedPageId?: string,
): Card {
  const assignee = members[assigneeIndex % members.length]!;
  const doneItems =
    status === "status-done" || status === "status-completed"
      ? 4
      : status === "status-working"
        ? 2
        : status === "status-stuck"
          ? 1
          : 0;
  return {
    id: `${boardId}-card-${index}`,
    listId,
    boardId,
    workspaceId,
    title,
    descriptionMd: `Execution notes for ${title}.`,
    linkedPageId,
    position: index + 1,
    priority,
    status,
    dueDate,
    startDate: "2026-05-13T00:00:00.000Z",
    isArchived: false,
    isDeleted: false,
    members: [
      {
        id: `cm-${boardId}-${index}`,
        userId: assignee.userId,
        name: assignee.name,
        initials: assignee.initials,
        color: assignee.color,
      },
    ],
    labels: [
      {
        id: `${boardId}-label-product`,
        name: "Product",
        color: "var(--primary)",
      },
    ],
    checklists: [
      {
        id: `${boardId}-checklist-${index}`,
        title: "Delivery",
        position: 1,
        items: Array.from({ length: 4 }).map((_, itemIndex) => ({
          id: `${boardId}-checklist-${index}-${itemIndex}`,
          title: `Step ${itemIndex + 1}`,
          isDone: itemIndex < doneItems,
          position: itemIndex + 1,
        })),
      },
    ],
    fieldValues: {
      [`${boardId}-field-title`]: title,
      [`${boardId}-field-person`]: [assignee.userId],
      [`${boardId}-field-status`]: status,
      [`${boardId}-field-priority`]: priority,
      [`${boardId}-field-due-date`]: dueDate,
      [`${boardId}-field-linked-page`]: linkedPageId,
      [`${boardId}-field-progress`]: doneItems / 4,
    },
    _count: { comments: index % 3, attachments: index % 2, checklistItems: 4 },
    createdAt: "2026-05-01T08:00:00.000Z",
    updatedAt: `2026-05-${String(10 + index).padStart(2, "0")}T09:30:00.000Z`,
  };
}

function makeDueDate(cardIndex: number) {
  return new Date(Date.UTC(2026, 4, 14 + cardIndex, 9, 0, 0)).toISOString();
}

function makeBoard(
  boardId: string,
  title: string,
  description: string,
  linkedPageId: string,
): FullBoardResponse {
  const workspaceId = "workspace-notrelix-os";
  const fields = fieldDefinitions(boardId);
  const board: Board = {
    id: boardId,
    workspaceId,
    title,
    description,
    background: { type: "color", value: "var(--primary)" },
    visibility: "workspace",
    isArchived: false,
    linkedPageId,
    fieldDefinitions: fields,
    members,
    createdAt: "2026-05-01T08:00:00.000Z",
    updatedAt: "2026-05-13T08:00:00.000Z",
  };

  const listSeeds = [
    {
      id: `${boardId}-list-backlog`,
      title: "Backlog",
      color: "var(--muted-foreground)",
    },
    {
      id: `${boardId}-list-working`,
      title: "Working on it",
      color: "var(--primary)",
    },
    {
      id: `${boardId}-list-stuck`,
      title: "Stuck",
      color: "var(--destructive)",
    },
    { id: `${boardId}-list-done`, title: "Done", color: "var(--accent)" },
    {
      id: `${boardId}-list-completed`,
      title: "Completed",
      color: "var(--primary)",
    },
  ];

  const cardTitles = [
    "Define board field architecture",
    "Connect card detail to linked docs",
    "Review customer onboarding flow",
    "Prepare sprint planning agenda",
    "Audit calendar sync edge cases",
    "Prototype table inline editing",
    "Write QA notes for release",
    "Summarize research interviews",
    "Polish workspace sidebar states",
    "Create launch checklist",
    "Map automation webhook payloads",
    "Refine mobile board navigation",
    "Document API migration plan",
    "Triage overdue tasks",
    "Validate permissions matrix",
    "Package workspace table defaults",
    "Confirm launch analytics events",
    "Close follow-up research items",
    "Archive completed QA notes",
    "Publish customer success brief",
  ];

  let cardIndex = 0;
  const groups: BoardGroup[] = listSeeds.map((list, listIndex) => {
    const cards = cardTitles
      .slice(listIndex * 4, listIndex * 4 + 4)
      .map((cardTitle) => {
        const statuses = [
          "status-not-started",
          "status-working",
          "status-stuck",
          "status-done",
          "status-completed",
        ];
        const priorities: Card["priority"][] = [
          "low",
          "medium",
          "high",
          "urgent",
        ];
        cardIndex += 1;
        return makeCard(
          boardId,
          workspaceId,
          list.id,
          cardIndex,
          cardTitle,
          statuses[listIndex]!,
          priorities[cardIndex % priorities.length],
          makeDueDate(cardIndex),
          cardIndex,
          cardIndex % 2 === 0 ? "docs-mvp-spec" : undefined,
        );
      });

    return {
      id: list.id,
      title: list.title,
      color: list.color,
      position: listIndex + 1,
      isCollapsed: false,
      cards,
    };
  });

  return { board, groups, fieldDefinitions: fields };
}

export const mockBoards = [
  makeBoard(
    "board-product",
    "Product delivery",
    "Track delivery work with docs-linked tasks, owners, status, due dates, and progress.",
    "docs-mvp-spec",
  ),
  makeBoard(
    "board-roadmap",
    "Roadmap planning",
    "Prioritize roadmap bets across product, design, engineering, and GTM.",
    "q3-operating-plan",
  ),
  makeBoard(
    "board-design",
    "Design QA",
    "Coordinate design review, implementation QA, and release polish.",
    "design-system-v2",
  ),
];

export const mockCardComments = [
  {
    id: "comment-1",
    cardId: "board-product-card-2",
    author: "Ana Moreno",
    body: "Please keep the linked doc updated before review.",
    createdAt: "2026-05-13T09:12:00.000Z",
  },
  {
    id: "comment-2",
    cardId: "board-product-card-4",
    author: "Minh Tran",
    body: "I will sync this with the workspace chat summary.",
    createdAt: "2026-05-13T09:30:00.000Z",
  },
  {
    id: "comment-3",
    cardId: "board-roadmap-card-3",
    author: "Sam Carter",
    body: "Design dependencies are ready for handoff.",
    createdAt: "2026-05-13T10:00:00.000Z",
  },
];

export const mockCardActivity = [
  {
    id: "activity-1",
    cardId: "board-product-card-2",
    actor: "Ana Moreno",
    action: "changed status to Working on it",
    createdAt: "2026-05-13T09:18:00.000Z",
  },
  {
    id: "activity-2",
    cardId: "board-product-card-4",
    actor: "Minh Tran",
    action: "linked Docs MVP specification",
    createdAt: "2026-05-13T09:34:00.000Z",
  },
  {
    id: "activity-3",
    cardId: "board-roadmap-card-3",
    actor: "Sam Carter",
    action: "added checklist items",
    createdAt: "2026-05-13T10:08:00.000Z",
  },
];
