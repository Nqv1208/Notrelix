import type { WorkspaceSummary } from "@notrelix/features-workspace/core";
import type { HomeSidebarData } from "../../shell/home/types";
import type { HomeResourceItem, HomeViewModel } from "../home-model";

function workspace(
  id: string,
  name: string,
  overrides: Partial<WorkspaceSummary> = {},
): WorkspaceSummary {
  return {
    id,
    slug: id,
    name,
    description: `${name} workspace`,
    icon: name.slice(0, 1),
    plan: "pro",
    memberCount: 12,
    isPersonal: false,
    ...overrides,
  };
}

function resource(
  id: string,
  title: string,
  kind: HomeResourceItem["kind"],
  workspaceId = "ws-product",
  workspaceName = "Product",
): HomeResourceItem {
  return {
    id,
    title,
    kind,
    workspaceId,
    workspaceName,
    subtitle: kind === "board" ? "Board" : "Document",
    updatedLabel: "Recently updated",
  };
}

export function homeDefaultScenario(): HomeViewModel {
  const workspaces = [
    workspace("ws-product", "Product"),
    workspace("ws-design", "Design"),
  ];
  return {
    userName: "Ada",
    workspaces,
    favoriteDocs: [resource("doc-roadmap", "Product roadmap", "doc")],
    continueItems: [
      resource("doc-roadmap", "Product roadmap", "doc"),
      resource("board-launch", "Launch planning", "board"),
    ],
    tasks: {
      status: "ready",
      items: [
        {
          id: "task-1",
          title: "Review launch brief",
          workspaceId: "ws-product",
          workspaceName: "Product",
          dueLabel: "Due today",
          priority: "high",
          completed: false,
        },
      ],
    },
    activity: {
      status: "ready",
      items: [
        {
          id: "activity-1",
          actor: "Grace",
          initials: "GH",
          action: "updated",
          target: "Launch planning",
          workspaceName: "Product",
          timeLabel: "10 minutes ago",
        },
      ],
    },
  };
}

export function homeEmptyScenario(): HomeViewModel {
  return {
    userName: "Ada",
    workspaces: [],
    favoriteDocs: [],
    continueItems: [],
    tasks: { status: "unavailable", message: "Tasks are not available yet." },
    activity: {
      status: "unavailable",
      message: "Activity is not available yet.",
    },
  };
}

export function homeUnavailableScenario(): HomeViewModel {
  return {
    ...homeDefaultScenario(),
    tasks: {
      status: "unavailable",
      message: "Tasks will appear when the task feed is available.",
    },
    activity: {
      status: "unavailable",
      message: "Activity will appear when the activity feed is available.",
    },
  };
}

export function homeEdgeDataScenario(): HomeViewModel {
  return {
    ...homeDefaultScenario(),
    workspaces: [
      workspace("ws-global-program", "Global Enterprise Program", {
        memberCount: 128,
      }),
    ],
    continueItems: [
      resource(
        "doc-unicode",
        "Decision log — rollout / запуск / 发布",
        "doc",
        "ws-global-program",
        "Global Enterprise Program",
      ),
    ],
  };
}

export function homeSidebarScenario(): HomeSidebarData {
  return {
    workspaces: [
      workspace("ws-product", "Product"),
      workspace("ws-design", "Design"),
    ],
    favoriteDocs: [
      {
        id: "doc-roadmap",
        title: "Product roadmap",
        workspaceId: "ws-product",
      },
    ],
    recentDocs: [
      { id: "doc-spec", title: "Launch spec", workspaceId: "ws-product" },
    ],
    recentBoards: [
      {
        id: "board-launch",
        title: "Launch planning",
        workspaceId: "ws-product",
      },
    ],
  };
}

export function homeSidebarEdgeDataScenario(): HomeSidebarData {
  return {
    ...homeSidebarScenario(),
    workspaces: [
      workspace("ws-global-program", "Global Enterprise Program", {
        memberCount: 128,
      }),
    ],
    favoriteDocs: [
      {
        id: "doc-unicode",
        title: "Decision log — rollout / запуск / 发布",
        workspaceId: "ws-global-program",
      },
    ],
  };
}
