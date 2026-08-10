import { RouteGuardError } from "./errors";

export function requireFeatureFlag(input: {
  readonly flag: string;
  readonly isFeatureEnabled: (flag: string) => boolean;
}): void {
  if (input.isFeatureEnabled(input.flag)) return;
  throw new RouteGuardError(
    "feature-unavailable",
    "Feature flag is disabled.",
    404,
  );
}
