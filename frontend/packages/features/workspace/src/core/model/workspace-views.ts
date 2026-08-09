import type { WorkspaceView } from "../types/workspace";

export function buildDefaultViews(workspaceId: string): WorkspaceView[] {
  return [
    {
      id: `${workspaceId}-view-table`,
      workspaceId,
      name: "All Tasks",
      type: "table",
      icon: "table",
      description: "List view of all workspace tasks",
      target: {},
      config: {
        density: "default",
      },
      visibility: "workspace",
      isDefault: true,
      position: 0,
      createdAt: new Date().toISOString(),
    },
    {
      id: `${workspaceId}-view-kanban`,
      workspaceId,
      name: "Kanban Board",
      type: "kanban",
      icon: "kanban",
      description: "Kanban view of workspace tasks",
      target: {},
      config: {
        density: "default",
      },
      visibility: "workspace",
      isDefault: false,
      position: 1,
      createdAt: new Date().toISOString(),
    },
  ];
}
