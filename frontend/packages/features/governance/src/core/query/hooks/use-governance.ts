import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { createGovernanceService, type GovernanceApiClient, type GovernanceEndpoints } from '../api/governance.service';
import { governanceQueryKeys } from './keys';

export function createUseRoles(api: GovernanceApiClient, endpoints: GovernanceEndpoints) {
  const service = createGovernanceService(api, endpoints);
  return function useRoles(workspaceId: string) {
    return useQuery({
      queryKey: governanceQueryKeys.roles(workspaceId),
      queryFn: () => service.listRoles(workspaceId),
      enabled: !!workspaceId,
    });
  };
}

export function createUseCreateRole(api: GovernanceApiClient, endpoints: GovernanceEndpoints) {
  const service = createGovernanceService(api, endpoints);
  const queryClient = useQueryClient();
  return function useCreateRole(workspaceId: string) {
    return useMutation({
      mutationFn: ({ name, permissions }: { name: string; permissions: string[] }) =>
        service.createRole(workspaceId, name, permissions),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: governanceQueryKeys.roles(workspaceId) });
      },
    });
  };
}

export function createUseAuditLogs(api: GovernanceApiClient, endpoints: GovernanceEndpoints) {
  const service = createGovernanceService(api, endpoints);
  return function useAuditLogs(workspaceId: string) {
    return useQuery({
      queryKey: governanceQueryKeys.auditLogs(workspaceId),
      queryFn: () => service.listAuditLogs(workspaceId),
      enabled: !!workspaceId,
    });
  };
}
