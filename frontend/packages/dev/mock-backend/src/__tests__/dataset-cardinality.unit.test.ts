import { describe, it, expect } from "vitest";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { MOCK_DATASET_CARDINALITIES } from "../state/mock-dataset.manifest";

describe("MDF-FZ-07: Dataset Manifest Closure", () => {
  it("T-MFB-031: stress density generates massive scale without violating relationships", () => {
    try {
      const store = new MockStore({
        seed: 1001,
        persona: "owner",
        state: "default",
        density: "stress",
        overlays: [],
        faultProfile: {},
        latency: "instant",
      });
      store.assertInvariants();
    } catch (e) {
      console.error(e);
      throw e;
    }
  });
});
