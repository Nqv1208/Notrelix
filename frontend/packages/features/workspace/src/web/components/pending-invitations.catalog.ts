import { defineCatalog } from "@notrelix/i18n";
import { en, vi } from "./pending-invitations.copy";

/** The `workspace-pending-invitations` catalog (completeness checked by `pnpm check:i18n`). */
export const pendingInvitationsCatalog = defineCatalog(
  "workspace-pending-invitations",
  { vi, en },
);
