import { z } from "zod";

/**
 * `item` is the connection open (none = the list), `connect` the catalog entry
 * whose connect dialog (or plan sheet) is open and `disconnect` the
 * disconnect confirmation over an open connection. `connected` (the provider)
 * and `error` are where the provider's redirect lands the member back.
 */
export const integrationsSearchSchema = z.object({
  item: z.string().optional(),
  connect: z.string().optional(),
  disconnect: z.boolean().optional(),
  connected: z.string().optional(),
  error: z.string().optional(),
});

export type IntegrationsSearch = z.infer<typeof integrationsSearchSchema>;
