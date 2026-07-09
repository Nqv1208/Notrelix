import type { WorkspaceActivityItem } from '../types/workspace';
import type { WorkspaceApiClient } from './workspace.service';

export function createActivityService(
  _api: WorkspaceApiClient,
) {
  return {
    // PENDING BACKEND: GET /workspaces/{workspaceId}/activity is missing
    // See: docs/client/audits/backend-contract-gaps.md#3-workspace-activity-log
    async getList(_workspaceId: string): Promise<WorkspaceActivityItem[]> {
      console.warn('GET /workspaces/{workspaceId}/activity is pending backend validation.');
      return [];
    },
  };
}
