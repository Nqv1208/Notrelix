import type {
  WorkspaceView,
  CreateWorkspaceViewInput,
  UpdateWorkspaceViewInput,
} from "../types/workspace";
import type { WorkspaceApiClient } from "./workspace.service";

export function createViewsService(
  _api: WorkspaceApiClient,
  options?: {
    mockMode?: boolean;
  },
) {
  const mockMode = options?.mockMode === true;

  return {
    // PENDING BACKEND: GET /workspaces/{workspaceId}/views is missing
    // See: docs/client/audits/backend-contract-gaps.md#2-workspace-custom-views-persisted
    async getList(_workspaceId: string): Promise<WorkspaceView[]> {
      if (mockMode) {
        console.warn(
          "GET /workspaces/{workspaceId}/views is pending backend validation.",
        );
        return [];
      }
      throw new Error("Backend contract missing for workspaces.views");
    },

    // PENDING BACKEND: POST /workspaces/{workspaceId}/views is missing
    async create(_input: CreateWorkspaceViewInput): Promise<WorkspaceView> {
      if (mockMode) {
        console.warn(
          "POST /workspaces/{workspaceId}/views is pending backend validation.",
        );
        return {
          id: "dummy-view-id",
          workspaceId: _input.workspaceId,
          name: _input.name,
          type: _input.type,
          icon: "Layout",
          description: "A workspace view",
          target: _input.target || {},
          config: {},
          visibility: "workspace",
          isDefault: false,
          position: 0,
          createdAt: new Date().toISOString(),
        };
      }
      throw new Error("Endpoint not implemented by backend.");
    },

    // PENDING BACKEND: PATCH /workspaces/{workspaceId}/views/{viewId} is missing
    async update(
      _workspaceId: string,
      _viewId: string,
      _input: UpdateWorkspaceViewInput,
    ): Promise<WorkspaceView> {
      if (mockMode) {
        console.warn(
          "PATCH /workspaces/{workspaceId}/views/{viewId} is pending backend validation.",
        );
        return {
          id: _viewId,
          workspaceId: _workspaceId,
          name: _input.name || "Updated View",
          type: "kanban",
          icon: _input.icon || "Layout",
          description: "A workspace view",
          target: {},
          config: _input.config || {},
          visibility: "workspace",
          isDefault: false,
          position: _input.position || 0,
          createdAt: new Date().toISOString(),
        };
      }
      throw new Error("Endpoint not implemented by backend.");
    },

    // PENDING BACKEND: DELETE /workspaces/{workspaceId}/views/{viewId} is missing
    async delete(_workspaceId: string, _viewId: string): Promise<void> {
      if (mockMode) {
        console.warn(
          "DELETE /workspaces/{workspaceId}/views/{viewId} is pending backend validation.",
        );
        return;
      }
      throw new Error("Endpoint not implemented by backend.");
    },

    // PENDING BACKEND: POST /workspaces/{workspaceId}/views/reorder is missing
    async reorder(_workspaceId: string, _viewIds: string[]): Promise<void> {
      if (mockMode) {
        console.warn(
          "POST /workspaces/{workspaceId}/views/reorder is pending backend validation.",
        );
        return;
      }
      throw new Error("Endpoint not implemented by backend.");
    },
  };
}
