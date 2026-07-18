import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { createIntegrationsService, type IntegrationsApiClient, type IntegrationsEndpoints } from '../api/integrations.service';
import { integrationsQueryKeys } from './keys';

export function createUseConnections(api: IntegrationsApiClient, endpoints: IntegrationsEndpoints) {
  const service = createIntegrationsService(api, endpoints);
  return function useConnections(workspaceId: string) {
    return useQuery({
      queryKey: integrationsQueryKeys.connections(workspaceId),
      queryFn: () => service.listConnections(workspaceId),
      enabled: !!workspaceId,
    });
  };
}

export function createUseDisconnect(api: IntegrationsApiClient, endpoints: IntegrationsEndpoints) {
  const service = createIntegrationsService(api, endpoints);
  const queryClient = useQueryClient();
  return function useDisconnect(workspaceId: string) {
    return useMutation({
      mutationFn: (connectionId: string) => service.disconnect(connectionId),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: integrationsQueryKeys.connections(workspaceId) });
      },
    });
  };
}

export function createUseWebhooks(api: IntegrationsApiClient, endpoints: IntegrationsEndpoints) {
  const service = createIntegrationsService(api, endpoints);
  return function useWebhooks(workspaceId: string) {
    return useQuery({
      queryKey: integrationsQueryKeys.webhooks(workspaceId),
      queryFn: () => service.listWebhooks(workspaceId),
      enabled: !!workspaceId,
    });
  };
}
