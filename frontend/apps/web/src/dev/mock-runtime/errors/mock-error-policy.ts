import { AppError, type AppErrorKind } from "@notrelix/kernel";
import type { MockScenario } from "../config/mock-runtime-config";
import type { MockRequest } from "../transport/mock-request";

export interface MockErrorSpec {
  readonly kind: AppErrorKind;
  readonly status?: number;
  readonly code?: string;
  readonly message: string;
  readonly validationErrors?: Record<string, string[]>;
}

const errorScenarioOperations: ReadonlyArray<{
  matches(request: MockRequest): boolean;
  error: MockErrorSpec;
}> = [
  {
    matches: (request) => request.method === "GET" && request.url === "/workspaces",
    error: { kind: "network", message: "Deterministic mock workspace failure." },
  },
];

export function throwConfiguredMockError(scenario: MockScenario, request: MockRequest): void {
  if (scenario !== "error") return;
  const configured = errorScenarioOperations.find((entry) => entry.matches(request));
  if (configured) throw new AppError(configured.error);
}
