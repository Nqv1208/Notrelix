import type { IntegrationConnection, Webhook } from '../types/integrations';

export interface IntegrationsApiClient {
  get<TResponse>(url: string, options?: unknown): Promise<TResponse>;
  post<TResponse, TBody = unknown>(url: string, body?: TBody, options?: unknown): Promise<TResponse>;
  delete<TResponse>(url: string, options?: unknown): Promise<TResponse>;
}

export interface IntegrationsEndpoints {
  connections: {
    list: (workspaceId: string) => string;
    disconnect: (connectionId: string) => string;
  };
  webhooks: {
    list: (workspaceId: string) => string;
    create: (workspaceId: string) => string;
    delete: (webhookId: string) => string;
  };
}

export function createIntegrationsService(api: IntegrationsApiClient, endpoints: IntegrationsEndpoints) {
  return {
    async listConnections(workspaceId: string): Promise<IntegrationConnection[]> {
      return api.get<IntegrationConnection[]>(endpoints.connections.list(workspaceId));
    },

    async disconnect(connectionId: string): Promise<void> {
      await api.delete(endpoints.connections.disconnect(connectionId));
    },

    async listWebhooks(workspaceId: string): Promise<Webhook[]> {
      return api.get<Webhook[]>(endpoints.webhooks.list(workspaceId));
    },

    async createWebhook(workspaceId: string, url: string, events: string[]): Promise<Webhook> {
      return api.post<Webhook>(endpoints.webhooks.create(workspaceId), { url, events });
    },

    async deleteWebhook(webhookId: string): Promise<void> {
      await api.delete(endpoints.webhooks.delete(webhookId));
    },
  };
}
