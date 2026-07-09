export interface BoardViewConfig {
  groupBy?: string;
  hiddenFields?: string[];
  columnOrder?: string[];
  density?: 'compact' | 'default' | 'comfortable';
  filters?: Array<{ fieldId: string; operator: string; value: unknown }>;
  sortBy?: Array<{ fieldId: string; direction: 'asc' | 'desc' }>;
}

export function parseBoardViewConfig(configStr?: string): BoardViewConfig {
  if (!configStr) return {};
  try {
    return JSON.parse(configStr) as BoardViewConfig;
  } catch {
    return {};
  }
}

export function serializeBoardViewConfig(config: BoardViewConfig): string {
  return JSON.stringify(config);
}
