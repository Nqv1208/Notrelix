import type { WorkspaceView, CreateWorkspaceViewInput, UpdateWorkspaceViewInput } from '../types/workspace';
import type { WorkspaceApiClient } from './workspace.service';

export function createViewsService(
  _api: WorkspaceApiClient,
) {
  return {
    // PENDING BACKEND: GET /workspaces/{workspaceId}/views is missing
    // See: docs/client/audits/backend-contract-gaps.md#2-workspace-custom-views-persisted
    async getList(_workspaceId: string): Promise<WorkspaceView[]> {
      console.warn('GET /workspaces/{workspaceId}/views is pending backend validation.');
      return [];
    },

    // PENDING BACKEND: POST /workspaces/{workspaceId}/views is missing
    async create(_input: CreateWorkspaceViewInput): Promise<WorkspaceView> {
      console.warn('POST /workspaces/{workspaceId}/views is pending backend validation.');
      throw new Error('Endpoint not implemented by backend.');
    },

    // PENDING BACKEND: PATCH /workspaces/{workspaceId}/views/{viewId} is missing
    async update(
      _workspaceId: string,
      _viewId: string,
      _input: UpdateWorkspaceViewInput,
    ): Promise<WorkspaceView> {
      console.warn('PATCH /workspaces/{workspaceId}/views/{viewId} is pending backend validation.');
      throw new Error('Endpoint not implemented by backend.');
    },

    // PENDING BACKEND: DELETE /workspaces/{workspaceId}/views/{viewId} is missing
    async delete(_workspaceId: string, _viewId: string): Promise<void> {
      console.warn('DELETE /workspaces/{workspaceId}/views/{viewId} is pending backend validation.');
      throw new Error('Endpoint not implemented by backend.');
    },

    // PENDING BACKEND: POST /workspaces/{workspaceId}/views/reorder is missing
    async reorder(_workspaceId: string, _viewIds: string[]): Promise<void> {
      console.warn('POST /workspaces/{workspaceId}/views/reorder is pending backend validation.');
      throw new Error('Endpoint not implemented by backend.');
    },
  };
}
