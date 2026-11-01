import { useLocale } from "@notrelix/i18n";
import {
  pendingInvitationsCopy,
  type PendingInvitationsCopy,
} from "./pending-invitations.copy";

/** Pending invitations copy for the active locale (requires the host `I18nProvider`). */
export function usePendingInvitationsCopy(): PendingInvitationsCopy {
  return pendingInvitationsCopy(useLocale().locale);
}
