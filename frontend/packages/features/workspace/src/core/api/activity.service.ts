import type { WorkspaceActivityItem } from "../types/workspace";
import type { WorkspaceApiClient } from "./workspace.service";

export function createActivityService(_api: WorkspaceApiClient) {
  return {
    async getList(_workspaceId: string): Promise<WorkspaceActivityItem[]> {
      throw new Error("Backend contract missing for workspaces.activity");
    },
  };
}
