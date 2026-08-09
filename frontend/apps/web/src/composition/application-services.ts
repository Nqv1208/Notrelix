import type { QueryClient } from "@tanstack/react-query";
import type { AppRuntime } from "@notrelix/runtime-web";
import { createQueryClient } from "@notrelix/query";
import {
  createWorkManagementServices,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import {
  createUnavailableSearchApi,
  type SearchApi,
} from "@notrelix/features-search";
import {
  createApplicationLifecycle,
  type ApplicationLifecycle,
} from "./application-lifecycle";

export interface WebApplicationServices {
  readonly runtime: AppRuntime;
  readonly queryClient: QueryClient;
  readonly workManagement: WorkManagementServices;
  readonly lifecycle: ApplicationLifecycle;
  readonly searchApi: SearchApi;
  dispose(): Promise<void>;
}

export interface CreateWebApplicationServicesOptions {
  readonly navigateToSignedOut: () => void;
}

export type WebApplicationServicesOptions = CreateWebApplicationServicesOptions;

export function createWebApplicationServices(
  runtime: AppRuntime,
  options: CreateWebApplicationServicesOptions,
): WebApplicationServices {
  const queryClient = createQueryClient();
  const workManagement = createWorkManagementServices(runtime.api);

  const lifecycle = createApplicationLifecycle({
    queryClient,
    realtime: runtime.realtime,
    sessionEvents: runtime.sessionEvents,
    navigateToSignedOut: options.navigateToSignedOut,
  });

  const searchApi = createUnavailableSearchApi();

  return {
    runtime,
    queryClient,
    workManagement,
    lifecycle,
    searchApi,
    async dispose() {
      lifecycle.dispose();
      queryClient.clear();
      await runtime.dispose();
    },
  };
}
