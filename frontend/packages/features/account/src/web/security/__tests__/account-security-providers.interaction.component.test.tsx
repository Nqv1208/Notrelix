import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  securityDefaultScenario,
  securityFixture,
  type SecurityScenario,
} from "../../../verification/account-security-fixtures";
import {
  AccountSecuritySurface,
  type AccountSecurityData,
  type AccountSecurityFlow,
  type AccountSecuritySurfaceProps,
} from "../account-security-surface";

function render(
  data: Partial<AccountSecurityData>,
  overrides: Partial<AccountSecuritySurfaceProps> = {},
  flow: AccountSecurityFlow | null = null,
) {
  const scenario: SecurityScenario = {
    ...securityDefaultScenario("en"),
    security: {
      status: "ready",
      data: securityFixture({ ssoProvider: null, ...data }),
    },
    flow,
  };
  const props: AccountSecuritySurfaceProps = {
    ...scenario,
    onSetTwoFactor: vi.fn(),
    onRenewBackupCodes: vi.fn(),
    onSignOut: vi.fn(),
    onSignOutOthers: vi.fn(),
    onRetry: vi.fn(),
    onLinkProvider: vi.fn(),
    onUnlinkProvider: vi.fn(),
    ...overrides,
  };
  renderPureUi(<AccountSecuritySurface {...props} />);
  return props;
}

describe("AccountSecuritySurface connected accounts", () => {
  it("FUI[account.security:providers] offers Connect for a provider not linked and Disconnect for one that is", () => {
    const props = render({ hasPassword: true, linkedProviders: ["Google"] });
    fireEvent.click(screen.getByRole("button", { name: "Disconnect Google" }));
    expect(props.onUnlinkProvider).toHaveBeenCalledWith("Google");
    fireEvent.click(screen.getByRole("button", { name: "Connect Microsoft" }));
    expect(props.onLinkProvider).toHaveBeenCalledWith("Microsoft");
    expect(screen.queryByRole("button", { name: "Connect Google" })).toBeNull();
  });

  it("FUI[account.security:providers] never offers to disconnect the only way to sign in, and says why", () => {
    render({ hasPassword: false, linkedProviders: ["Google"] });
    expect(
      screen.queryByRole("button", { name: "Disconnect Google" }),
    ).toBeNull();
    expect(screen.getByText(/This is your only way to sign in/)).toBeTruthy();
  });

  it("FUI[account.security:providers] lets a second provider go when another way remains", () => {
    render({ hasPassword: false, linkedProviders: ["Google", "Microsoft"] });
    expect(
      screen.getByRole("button", { name: "Disconnect Google" }),
    ).toBeTruthy();
    expect(
      screen.getByRole("button", { name: "Disconnect Microsoft" }),
    ).toBeTruthy();
  });

  it("FUI[account.security:providers] names the provider in the link and unlink proofs, and warns before a disconnect", () => {
    render(
      { hasPassword: true, linkedProviders: ["Google"] },
      {},
      {
        kind: "stepUp",
        purpose: "unlink",
        provider: "Google",
        factor: "password",
        error: null,
      },
    );
    expect(
      screen.getByRole("region", { name: "Confirm to disconnect Google" }),
    ).toBeTruthy();
    expect(
      screen.getByText(/You will no longer be able to sign in/),
    ).toBeTruthy();
  });

  it("FUI[account.security:providers] hides the block when the page cannot link and nothing is linked", () => {
    render({ linkedProviders: [] }, { onLinkProvider: undefined });
    expect(screen.queryByText("Connected accounts")).toBeNull();
  });
});
