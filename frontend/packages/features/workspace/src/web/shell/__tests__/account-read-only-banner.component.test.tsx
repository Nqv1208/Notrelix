import { describe, expect, it } from "vitest";
import { renderPureUi, screen } from "@notrelix/testing";

import { AccountReadOnlyBanner } from "../account-read-only-banner";
import { workspaceShellCopy } from "../workspace-shell.copy";

const copy = workspaceShellCopy("en");
const navigation = {
  getHref: (destination: { section: string }) => `/w/1/${destination.section}`,
  onNavigate: () => {},
};

function renderBanner(
  props: Partial<Parameters<typeof AccountReadOnlyBanner>[0]> = {},
) {
  renderPureUi(
    <AccountReadOnlyBanner
      copy={copy}
      status="Suspended"
      canManageBilling={false}
      navigation={navigation}
      {...props}
    />,
  );
}

describe("AccountReadOnlyBanner (D06)", () => {
  it("FUI[account.suspended:member] tells a plain member the Account is suspended, reads stay open, and offers no billing link", () => {
    renderBanner();
    const banner = screen.getByRole("status");
    expect(banner.textContent).toContain(copy.accountReadOnly.suspendedTitle);
    expect(banner.textContent).toContain(copy.accountReadOnly.suspendedText);
    expect(screen.queryByRole("link")).toBeNull();
  });

  it("FUI[account.suspended:closed] says Closed in its own words", () => {
    renderBanner({ status: "Closed" });
    const banner = screen.getByRole("status");
    expect(banner.textContent).toContain(copy.accountReadOnly.closedTitle);
    expect(banner.textContent).not.toContain(
      copy.accountReadOnly.suspendedTitle,
    );
  });

  it("FUI[account.suspended:owner] gives an Account Owner or Admin the way to billing", () => {
    renderBanner({ canManageBilling: true });
    const link = screen.getByRole("link", {
      name: copy.accountReadOnly.billing,
    });
    expect(link.getAttribute("href")).toBe("/w/1/billing");
  });
});
