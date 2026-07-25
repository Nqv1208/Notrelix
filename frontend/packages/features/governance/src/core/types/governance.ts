export interface GovernanceRole {
  id: string;
  workspaceId: string;
  name: string;
  permissions: string[];
  memberCount: number;
}

export interface GovernancePermission {
  id: string;
  resource: string;
  action: string;
  description: string;
}

export interface AuditLogEntry {
  id: string;
  workspaceId: string;
  actorId: string;
  actorName: string;
  action: string;
  resourceType: string;
  resourceId: string;
  resourceName: string;
  createdAt: string;
}
