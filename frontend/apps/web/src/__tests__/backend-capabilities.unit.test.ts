import { describe, expect, it } from "vitest";
import { backendCapabilitiesOf } from "../config/backend-capabilities";

describe("backendCapabilitiesOf", () => {
  it("serves every page on the mock backend", () => {
    expect(backendCapabilitiesOf(true)).toEqual({
      billingPlans: true,
      integrations: true,
      reports: true,
      planItems: true,
    });
  });

  it("serves only the pages with a producer on the real backend", () => {
    expect(backendCapabilitiesOf(false)).toEqual({
      billingPlans: false,
      integrations: true,
      reports: false,
      planItems: false,
    });
  });
});
