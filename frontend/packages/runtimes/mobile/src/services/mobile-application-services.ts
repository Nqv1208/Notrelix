import type { QueryClient } from "@tanstack/react-query";
import { createQueryClient } from "@notrelix/query";
import type { MobileRuntime } from "../runtime/mobile-runtime";
import {
  createMobileWorkspaceLifecycle,
  type MobileWorkspaceLifecycle,
} from "../runtime/mobile-workspace-lifecycle";

export interface MobileApplicationServices {
  readonly runtime: MobileRuntime;
  readonly queryClient: QueryClient;
  readonly workspaceLifecycle: MobileWorkspaceLifecycle;
  dispose(): Promise<void>;
}

export function createMobileApplicationServices(
  runtime: MobileRuntime,
): MobileApplicationServices {
  const queryClient = createQueryClient();
  const workspaceLifecycle = createMobileWorkspaceLifecycle({
    queryClient,
    realtime: runtime.realtime,
  });

  return {
    runtime,
    queryClient,
    workspaceLifecycle,
    async dispose() {
      queryClient.clear();
      await runtime.dispose();
    },
  };
}
