export type AutomationTriggerType =
  | 'card_status_changed'
  | 'card_priority_changed'
  | 'card_created'
  | 'card_deleted'
  | 'field_updated';

export type AutomationActionType =
  | 'archive_card'
  | 'remove_members'
  | 'notify_owner'
  | 'send_email'
  | 'move_to_group'
  | 'set_field_value';

export interface AutomationRule {
  id: string;
  workspaceId: string;
  boardId: string;
  name: string;
  description: string;
  triggerType: AutomationTriggerType;
  triggerConfig: Record<string, unknown>;
  actionType: AutomationActionType;
  actionConfig: Record<string, unknown>;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface AutomationRun {
  id: string;
  ruleId: string;
  status: 'queued' | 'running' | 'succeeded' | 'failed' | 'cancelled';
  triggeredBy: string;
  startedAt: string;
  completedAt?: string;
  error?: string;
}
