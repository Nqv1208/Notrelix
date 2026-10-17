import { z } from "zod";

/**
 * `item` is the connection open (none = the list), `connect` the catalog entry
 * whose connect dialog (or plan sheet) is open and `disconnect` the
 * disconnect confirmation over an open connection.
 */
export const integrationsSearchSchema = z.object({
  item: z.string().optional(),
  connect: z.string().optional(),
  disconnect: z.boolean().optional(),
});

export type IntegrationsSearch = z.infer<typeof integrationsSearchSchema>;
