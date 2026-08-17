import type { QueryClient } from "@tanstack/react-query";
import type { AppRuntime } from "@notrelix/runtime-web";
import { createQueryClient } from "@notrelix/query";
import {
  createWorkManagementServices,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import type { SearchApi, SearchResult } from "@notrelix/features-search";
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
    mockApi: runtime.env?.mockApi,
  });

  const searchApi: SearchApi = {
    async search(input) {
      const params = new URLSearchParams({
        q: input.query,
        workspaceId: input.workspaceId,
      });
      for (const type of input.types) params.append("type", type);
      const response = await runtime.api.api.get<
        readonly SearchResult[] | { readonly results: readonly SearchResult[] }
      >(`/api/v1/search?${params}`);
      return "results" in response ? response.results : response;
    },
  };

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
