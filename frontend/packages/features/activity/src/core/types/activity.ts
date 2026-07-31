export type ActivityAction =
  | 'created'
  | 'updated'
  | 'deleted'
  | 'commented'
  | 'assigned'
  | 'moved'
  | 'archived'
  | 'restored';

export interface ActivityEntry {
  id: string;
  workspaceId: string;
  actorId: string;
  actorName: string;
  action: ActivityAction;
  resourceType: string;
  resourceId: string;
  resourceName: string;
  metadata?: Record<string, unknown>;
  createdAt: string;
}
