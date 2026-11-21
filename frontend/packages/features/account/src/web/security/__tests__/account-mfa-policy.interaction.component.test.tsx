import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  securityCopyFixture,
  securityDefaultScenario,
  securityFixture,
} from "../../../verification/account-security-fixtures";
import {
  AccountSecuritySurface,
  type AccountSecurityFlow,
  type AccountSecuritySurfaceProps,
} from "../account-security-surface";

const t = securityCopyFixture("en");

function render(
  policy: { required: boolean; canChange: boolean } | undefined,
  flow: AccountSecurityFlow | null = null,
) {
  const props: AccountSecuritySurfaceProps = {
    ...securityDefaultScenario("en"),
    security: {
      status: "ready",
      data: securityFixture({ accountMfaPolicy: policy }),
    },
    flow,
    onSetTwoFactor: vi.fn(),
    onSetAccountMfaPolicy: vi.fn(),
    onRenewBackupCodes: vi.fn(),
    onSignOut: vi.fn(),
    onSignOutOthers: vi.fn(),
    onRetry: vi.fn(),
  };
  renderPureUi(<AccountSecuritySurface {...props} />);
  return props;
}

describe("AccountSecuritySurface account MFA policy", () => {
  it("lets the Owner require it for everyone, then stop requiring it", () => {
    const on = render({ required: false, canChange: true });
    fireEvent.click(screen.getByRole("button", { name: t.policy.require }));
    expect(on.onSetAccountMfaPolicy).toHaveBeenCalledWith(true);
  });

  it("offers to stop requiring it when it is on", () => {
    const props = render({ required: true, canChange: true });
    expect(screen.getByText(t.policy.required)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: t.policy.stop }));
    expect(props.onSetAccountMfaPolicy).toHaveBeenCalledWith(false);
  });

  it("shows a member the fact read-only, with no control", () => {
    render({ required: true, canChange: false });
    expect(screen.getByText(t.policy.required)).toBeTruthy();
    expect(screen.getByText(t.policy.memberText)).toBeTruthy();
    expect(screen.queryByRole("button", { name: t.policy.require })).toBeNull();
    expect(screen.queryByRole("button", { name: t.policy.stop })).toBeNull();
  });

  it("draws no block when the policy could not be read", () => {
    render(undefined);
    expect(screen.queryByText(t.policy.title)).toBeNull();
  });

  it("says why turning two-factor off is refused when the Account requires it", () => {
    render(
      { required: true, canChange: false },
      {
        kind: "stepUp",
        purpose: "disable",
        factor: "code",
        error: "requiredByAccount",
      },
    );
    expect(screen.getByRole("alert").textContent).toContain(
      t.flow.requiredByAccount,
    );
  });
});
