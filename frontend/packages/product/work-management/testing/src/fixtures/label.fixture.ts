import type { CardLabel } from "@notrelix/work-management-core";

export function labelFixture(overrides: Partial<CardLabel> = {}): CardLabel {
  return {
    id: "label-test",
    name: "Customer",
    color: "#00c875",
    ...overrides,
  };
}
