import { useMemo } from "react";
import type { NotrelixClient } from "@notrelix/contracts";
import { useAppRuntime } from "./app-runtime";

export interface FeatureRuntimeDependencies {
  readonly api: NotrelixClient["api"];
  readonly endpoints: NotrelixClient["endpoints"];
}

export function useFeatureRuntimeDependencies(): FeatureRuntimeDependencies {
  const runtime = useAppRuntime();

  return useMemo(
    () => ({
      api: runtime.api.api,
      endpoints: runtime.api.endpoints,
    }),
    [runtime.api],
  );
}
