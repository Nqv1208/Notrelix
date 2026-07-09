export type IntegrationProvider = 'slack' | 'github' | 'jira' | 'linear' | 'figma' | 'custom';

export type ConnectionStatus = 'connected' | 'disconnected' | 'error' | 'pending';

export interface IntegrationConnection {
  id: string;
  workspaceId: string;
  provider: IntegrationProvider;
  name: string;
  status: ConnectionStatus;
  config?: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
}

export interface Webhook {
  id: string;
  workspaceId: string;
  url: string;
  events: string[];
  isActive: boolean;
  secret?: string;
  createdAt: string;
}
