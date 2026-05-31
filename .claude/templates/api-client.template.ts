// Template: API Client Functions
// Replace placeholders: {{EntityName}}, {{FeatureName}}, {{EndpointPath}}

import { apiClient } from '@/lib/api/api-client';
import { ENDPOINTS } from '@/lib/api/endpoints';
import type { {{EntityName}}, Create{{EntityName}}Dto, Update{{EntityName}}Dto } from '../types';

export const {{FeatureName}}Api = {
  /**
   * Get all {{EntityName}}s
   */
  get{{EntityName}}s: async (parentId: string): Promise<{{EntityName}}[]> => {
    const response = await apiClient.get<{ data: {{EntityName}}[] }>(
      ENDPOINTS.{{FeatureName}}.list(parentId)
    );
    return response.data.data;
  },

  /**
   * Get a single {{EntityName}} by ID
   */
  get{{EntityName}}: async (id: string): Promise<{{EntityName}}> => {
    const response = await apiClient.get<{ data: {{EntityName}} }>(
      ENDPOINTS.{{FeatureName}}.detail(id)
    );
    return response.data.data;
  },

  /**
   * Create a new {{EntityName}}
   */
  create{{EntityName}}: async (parentId: string, data: Create{{EntityName}}Dto): Promise<{{EntityName}}> => {
    const response = await apiClient.post<{ data: {{EntityName}} }>(
      ENDPOINTS.{{FeatureName}}.list(parentId),
      data
    );
    return response.data.data;
  },

  /**
   * Update a {{EntityName}}
   */
  update{{EntityName}}: async (id: string, data: Update{{EntityName}}Dto): Promise<{{EntityName}}> => {
    const response = await apiClient.patch<{ data: {{EntityName}} }>(
      ENDPOINTS.{{FeatureName}}.detail(id),
      data
    );
    return response.data.data;
  },

  /**
   * Delete a {{EntityName}} (soft delete)
   */
  delete{{EntityName}}: async (id: string): Promise<void> => {
    await apiClient.delete(ENDPOINTS.{{FeatureName}}.detail(id));
  },
};
