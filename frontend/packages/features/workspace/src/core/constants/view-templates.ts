import type { WorkspaceViewType } from '~/core/types/workspace';

export const workspaceViewTemplates: Array<{
  type: WorkspaceViewType;
  label: string;
  description: string;
  icon: string;
  badge?: string;
}> = [
  { type: 'table', label: 'Table', description: 'Rows, groups, fields, and inline editing.', icon: '▦' },
  { type: 'kanban', label: 'Kanban', description: 'Cards grouped by list or status.', icon: '▥' },
  { type: 'calendar', label: 'Calendar', description: 'Tasks and docs by due date.', icon: '◇' },
  { type: 'timeline', label: 'Timeline', description: 'Plan work across dates.', icon: '═' },
  { type: 'doc', label: 'Doc', description: 'Write a workspace document.', icon: '□' },
  { type: 'dashboard', label: 'Dashboard', description: 'Track workspace signals.', icon: '◌' },
  { type: 'form', label: 'Form', description: 'Capture structured requests.', icon: '▤', badge: 'Soon' },
  { type: 'gallery', label: 'File gallery', description: 'Browse files and attachments.', icon: '▧', badge: 'Soon' },
  { type: 'chart', label: 'Chart', description: 'Visualize board metrics.', icon: '◍', badge: 'Soon' },
  { type: 'gantt', label: 'Gantt', description: 'Advanced timeline planning.', icon: '≡', badge: 'Soon' },
];
