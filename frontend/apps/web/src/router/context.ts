import type { WebApplicationServices } from '../composition/application-services';

export interface AuthSnapshot {
  readonly isAuthenticated: boolean;
  readonly isLoading: boolean;
  readonly userId?: string;
}

export interface WorkspaceSnapshot {
  readonly workspaceId: string;
  readonly role?: string;
}

export interface AppRouterContext {
  readonly services: WebApplicationServices;
  readonly auth?: AuthSnapshot;
  readonly workspace?: WorkspaceSnapshot | null;
}
