import { describe, expect, it } from "vitest";
import { backendCapabilitiesOf } from "../config/backend-capabilities";

describe("backendCapabilitiesOf", () => {
  it("serves every page on the mock backend", () => {
    expect(backendCapabilitiesOf(true)).toEqual({
      billingPlans: true,
      integrations: true,
      reports: true,
    });
  });

  it("serves none of the pages without a producer on the real backend", () => {
    expect(backendCapabilitiesOf(false)).toEqual({
      billingPlans: false,
      integrations: false,
      reports: false,
    });
  });
});
