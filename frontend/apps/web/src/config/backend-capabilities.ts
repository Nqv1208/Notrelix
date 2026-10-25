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
  /** The connections and the catalog of Integrations. */
  readonly integrations: boolean;
  /** The reports and their dashboards. */
  readonly reports: boolean;
}

export function backendCapabilitiesOf(mockApi: boolean): BackendCapabilities {
  return { billingPlans: mockApi, integrations: mockApi, reports: mockApi };
}

export const backendCapabilities: BackendCapabilities = backendCapabilitiesOf(
  env.mockApi,
);
