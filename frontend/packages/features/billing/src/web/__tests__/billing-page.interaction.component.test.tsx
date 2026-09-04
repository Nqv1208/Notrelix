import { describe, expect, it } from "vitest";
import { renderPureUi, screen } from "@notrelix/testing";

import {
  billingPageBusinessScenario,
  billingPageDefaultScenario,
  billingPageFreeScenario,
} from "../../verification/billing-ui-fixtures";
import { BillingPage } from "../billing-page";

describe("billing web pure surface", () => {
  it("renders the billing page from deterministic fixtures", () => {
    renderPureUi(<BillingPage {...billingPageDefaultScenario()} />);

    expect(screen.getByRole("heading", { name: "Billing" })).toBeTruthy();
    expect(screen.getByText("Current Plan")).toBeTruthy();
    expect(screen.getByText("Upgrade Plan")).toBeTruthy();
  });

  it("marks the current plan as current across tiers", () => {
    renderPureUi(<BillingPage {...billingPageFreeScenario()} />);

    expect(screen.getByText("Free forever")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Current plan" })).toBeTruthy();
  });

  it("renders the business tier without state/query providers", () => {
    renderPureUi(<BillingPage {...billingPageBusinessScenario()} />);

    expect(screen.getByText("Business")).toBeTruthy();
    expect(screen.getByText("SAML SSO")).toBeTruthy();
  });
});
