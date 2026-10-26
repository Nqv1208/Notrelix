import { env } from "./env";

/**
 * Which pages the connected backend has a producer for. The mock backend serves
 * every page; the real backend does not serve these yet, so their pages say so
 * instead of calling routes that answer 404. Flip a flag when the producer
 * ships, and delete it once the mock is gone.
 */
export interface BackendCapabilities {
  /** The plan, invoices, seats and payment pages of Billing. */
  readonly billingPlans: boolean;
  /** The connections and the catalog of Integrations (the producer serves them on every backend). */
  readonly integrations: boolean;
  /** The reports and their dashboards. */
  readonly reports: boolean;
  /** The department suggestions of the onboarding (a size and a name in, suggested departments out). */
  readonly onboardingCatalog: boolean;
  /** The markers, links and percents the Calendar and the Timeline draw beside the items. */
  readonly planItems: boolean;
}

export function backendCapabilitiesOf(mockApi: boolean): BackendCapabilities {
  return {
    billingPlans: mockApi,
    integrations: true,
    reports: mockApi,
    onboardingCatalog: mockApi,
    planItems: mockApi,
  };
}

export const backendCapabilities: BackendCapabilities = backendCapabilitiesOf(
  env.mockApi,
);
