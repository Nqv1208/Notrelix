import { createNotrelixClient } from "@notrelix/contracts";

// Violation: calling createNotrelixClient in feature module
export const client = createNotrelixClient({ baseUrl: "/api/v1" });
