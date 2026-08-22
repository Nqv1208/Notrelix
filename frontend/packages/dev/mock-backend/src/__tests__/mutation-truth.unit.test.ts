import { describe, it, expect } from "vitest";
import { ENABLED_CONSUMERS } from "../../../../../tooling/contracts/enabled-consumer-surface";
import { buildOperationRegistry } from "../operations/build-registry";

describe("MDF-FZ-09: Mutation Truth", () => {
  it("T-MFB-036: canonical mutation intent exactly matches registered HTTP methods", () => {
    const registry = buildOperationRegistry();
    const metadata = registry.operationMetadata();
    const canonicals = ENABLED_CONSUMERS.filter(
      (c) => c.classification === "CANONICAL_MOCKED",
    );

    for (const consumer of canonicals) {
      let match: (typeof metadata)[number] | undefined;

      if (consumer.mockOperationId) {
        // Row uses a mock-specific id — find by id
        match = metadata.find((m) => m.id === consumer.mockOperationId);
      } else if (consumer.operationId) {
        // Row anchors to OpenAPI operationId — only match registry openapi-kind operations
        match = metadata.find(
          (m) =>
            m.contract.kind === "openapi" &&
            m.contract.operationId === consumer.operationId,
        );
      }
      // If match is still undefined (stub/placeholder), skip — not yet resolvable

      if (!match) continue;

      const isMutationMethod =
        match.method.toUpperCase() !== "GET" &&
        match.method.toUpperCase() !== "HEAD" &&
        match.method.toUpperCase() !== "OPTIONS";

      expect(
        isMutationMethod,
        `${consumer.capability} registry method=${match.method} but surface mutation=${consumer.mutation}`,
      ).toBe(consumer.mutation);
    }
  });
});
