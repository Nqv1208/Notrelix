export const mockPersonas = ["owner", "admin", "member", "viewer"] as const;
export type MockPersona = (typeof mockPersonas)[number];

export const mockScenarios = [
  "default",
  "new-user",
  "empty",
  "large",
  "error",
  "permissions",
] as const;
export type MockScenario = (typeof mockScenarios)[number];

export interface MockRuntimeConfig {
  readonly persona: MockPersona;
  readonly scenario: MockScenario;
  readonly latencyMs: number;
}
