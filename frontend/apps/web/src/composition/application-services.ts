import type { QueryClient } from '@tanstack/react-query';
import type { AppRuntime } from '@notrelix/runtime-web';
import { createQueryClient } from '@notrelix/query';
import {
  createWorkManagementServices,
  type WorkManagementServices,
} from '@notrelix/work-management-state';

export interface WebApplicationServices {
  readonly runtime: AppRuntime;
  readonly queryClient: QueryClient;
  readonly workManagement: WorkManagementServices;
  dispose(): Promise<void>;
}

export function createWebApplicationServices(runtime: AppRuntime): WebApplicationServices {
  const queryClient = createQueryClient();
  const workManagement = createWorkManagementServices(runtime.api);

  return {
    runtime,
    queryClient,
    workManagement,
    async dispose() {
      await runtime.dispose();
    },
  };
}
