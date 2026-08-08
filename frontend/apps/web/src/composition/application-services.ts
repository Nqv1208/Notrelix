import type { QueryClient } from '@tanstack/react-query';
import type { AppRuntime } from '@notrelix/runtime-web';
import { createQueryClient } from '@notrelix/query';
import { workspaceQueryKeys } from '@notrelix/features-workspace';
import {
  createWorkManagementServices,
  type WorkManagementServices,
} from '@notrelix/work-management-state';
import {
  createApplicationLifecycle,
  createWorkspaceEventSource,
  type ApplicationLifecycle,
  type WorkspaceEventSource,
} from './application-lifecycle';

export interface WebApplicationServices {
  readonly runtime: AppRuntime;
  readonly queryClient: QueryClient;
  readonly workManagement: WorkManagementServices;
  readonly workspaceEvents: WorkspaceEventSource;
  readonly lifecycle: ApplicationLifecycle;
  dispose(): Promise<void>;
}

export interface WebApplicationServicesOptions {
  readonly navigateToSignedOut: () => void;
}

export function createWebApplicationServices(
  runtime: AppRuntime,
  options: WebApplicationServicesOptions
): WebApplicationServices {
  const queryClient = createQueryClient();
  const workManagement = createWorkManagementServices(runtime.api);
  const workspaceEvents = createWorkspaceEventSource((error, context) => {
    runtime.telemetry.reportError(error, context);
  });

  const lifecycle = createApplicationLifecycle({
    queryClient,
    realtime: runtime.realtime,
    sessionEvents: runtime.sessionEvents,
    workspaceEvents,
    clearSessionState: () => {
      queryClient.removeQueries({ queryKey: ['auth'] });
    },
    clearWorkspaceState: () => {
      queryClient.removeQueries({ queryKey: workspaceQueryKeys.all });
    },
    navigateToSignedOut: options.navigateToSignedOut,
  });

  return {
    runtime,
    queryClient,
    workManagement,
    workspaceEvents,
    lifecycle,
    async dispose() {
      lifecycle.dispose();
      await runtime.dispose();
    },
  };
}
