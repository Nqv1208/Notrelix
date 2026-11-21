import { ShieldAlert } from "lucide-react";
import { Button } from "@notrelix/ui-web";
import { ShellLink } from "./shell-link";
import type { ShellNavigation } from "./shell-models";
import type { WorkspaceShellCopy } from "./workspace-shell.copy";

export interface AccountReadOnlyBannerProps {
  readonly copy: WorkspaceShellCopy;
  /** Closed reads like Suspended with its own wording. */
  readonly status: "Suspended" | "Closed";
  /** Account Owner or Admin: offered the billing page, the next step to restore the Account. */
  readonly canManageBilling: boolean;
  readonly navigation: ShellNavigation;
}

/**
 * Every member sees that a Suspended or Closed Account is read-only: reads stay open and the producer
 * refuses every write with `account.suspended`. Modeled on the archived-workspace banner
 * (`Wsp-Var-Archived-Banner`); no board draws this state.
 */
export function AccountReadOnlyBanner({
  copy,
  status,
  canManageBilling,
  navigation,
}: AccountReadOnlyBannerProps) {
  const text = copy.accountReadOnly;
  const closed = status === "Closed";
  return (
    <div
      role="status"
      data-f="account-read-only-banner"
      className="mx-4 mt-3 flex flex-wrap items-center gap-3 rounded-lg bg-warning-subtle px-3.5 py-2.5 text-compact text-warning-subtle-foreground"
    >
      <ShieldAlert aria-hidden="true" className="size-4 shrink-0" />
      <span className="min-w-0 flex-1">
        <b className="font-semibold">
          {closed ? text.closedTitle : text.suspendedTitle}
        </b>{" "}
        {closed ? text.closedText : text.suspendedText}
        {canManageBilling ? ` ${text.billingHint}` : null}
      </span>
      {canManageBilling ? (
        <Button size="sm" variant="outline" asChild>
          <ShellLink
            destination={{ section: "billing" }}
            navigation={navigation}
          >
            {text.billing}
          </ShellLink>
        </Button>
      ) : null}
    </div>
  );
}
