// Template: TanStack Query Hook
// Replace placeholders: {{EntityName}}, {{FeatureName}}, {{HookName}}

import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/lib/query/query-keys';
import { {{FeatureName}}Api } from '../api/{{FeatureName}}-api';

/**
 * Hook to fetch {{EntityName}} by ID
 */
export function {{HookName}}(id: string) {
  return useQuery({
    queryKey: queryKeys.{{FeatureName}}.detail(id),
    queryFn: () => {{FeatureName}}Api.get{{EntityName}}(id),
    staleTime: 30 * 1000, // 30 seconds
    enabled: !!id,
  });
}

/**
 * Hook to fetch all {{EntityName}}s
 */
export function {{HookName}}List(parentId: string) {
  return useQuery({
    queryKey: queryKeys.{{FeatureName}}.list(parentId),
    queryFn: () => {{FeatureName}}Api.get{{EntityName}}s(parentId),
    staleTime: 30 * 1000, // 30 seconds
    enabled: !!parentId,
  });
}
