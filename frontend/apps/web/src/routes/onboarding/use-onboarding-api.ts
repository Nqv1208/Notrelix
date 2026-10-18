import { useMemo } from "react";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  createOnboardingApi,
  type OnboardingApiClient,
} from "./onboarding-api";

/** The onboarding calls on the app's runtime client. */
export function useOnboardingApi() {
  const { api: runtimeClient } = useAppRuntime();
  return useMemo(
    () =>
      createOnboardingApi(runtimeClient.api as unknown as OnboardingApiClient),
    [runtimeClient],
  );
}
