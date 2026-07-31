import type { GovernanceRole, GovernancePermission, AuditLogEntry } from '../types/governance';

export interface GovernanceApiClient {
  get<TResponse>(url: string, options?: unknown): Promise<TResponse>;
  post<TResponse, TBody = unknown>(url: string, body?: TBody, options?: unknown): Promise<TResponse>;
  put<TResponse, TBody = unknown>(url: string, body?: TBody, options?: unknown): Promise<TResponse>;
  delete<TResponse>(url: string, options?: unknown): Promise<TResponse>;
}

export interface GovernanceEndpoints {
  roles: {
    list: (workspaceId: string) => string;
    create: (workspaceId: string) => string;
    update: (roleId: string) => string;
    delete: (roleId: string) => string;
  };
  permissions: {
    list: (workspaceId: string) => string;
  };
  auditLogs: {
    list: (workspaceId: string) => string;
  };
}

export function createGovernanceService(api: GovernanceApiClient, endpoints: GovernanceEndpoints) {
  return {
    async listRoles(workspaceId: string): Promise<GovernanceRole[]> {
      return api.get<GovernanceRole[]>(endpoints.roles.list(workspaceId));
    },

    async createRole(workspaceId: string, name: string, permissions: string[]): Promise<GovernanceRole> {
      return api.post<GovernanceRole>(endpoints.roles.create(workspaceId), { name, permissions });
    },

    async updateRole(roleId: string, name: string, permissions: string[]): Promise<GovernanceRole> {
      return api.put<GovernanceRole>(endpoints.roles.update(roleId), { name, permissions });
    },

    async deleteRole(roleId: string): Promise<void> {
      await api.delete(endpoints.roles.delete(roleId));
    },

    async listPermissions(workspaceId: string): Promise<GovernancePermission[]> {
      return api.get<GovernancePermission[]>(endpoints.permissions.list(workspaceId));
    },

    async listAuditLogs(workspaceId: string): Promise<AuditLogEntry[]> {
      return api.get<AuditLogEntry[]>(endpoints.auditLogs.list(workspaceId));
    },
  };
}
