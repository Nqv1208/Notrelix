import { z } from "zod";

/**
 * `view` is the page of Plans and billing (none = the overview), `invoice` the
 * invoice open on the invoices page and `buy` the number of seats in the
 * buy-seats dialog (it opens the dialog).
 */
export const billingSearchSchema = z.object({
  view: z.enum(["compare", "invoices", "downgrade"]).optional(),
  invoice: z.string().optional(),
  buy: z.coerce.number().int().min(1).max(500).optional(),
});

export type BillingSearch = z.infer<typeof billingSearchSchema>;
