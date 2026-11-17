import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import { securityCopyFixture } from "../../../verification/account-security-fixtures";
import {
  AccountProvidersSurface,
  type AccountProvidersSurfaceProps,
} from "../account-providers-surface";

function render(overrides: Partial<AccountProvidersSurfaceProps> = {}) {
  const props: AccountProvidersSurfaceProps = {
    copy: securityCopyFixture("en"),
    status: "ready",
    linkedProviders: ["Google"],
    hasPassword: true,
    busy: false,
    notice: null,
    problem: null,
    flow: null,
    onLink: vi.fn(),
    onUnlink: vi.fn(),
    ...overrides,
  };
  renderPureUi(<AccountProvidersSurface {...props} />);
  return props;
}

describe("AccountProvidersSurface", () => {
  it("FUI[account.providers] offers Connect for a provider not linked and Disconnect for one that is", () => {
    const props = render();
    fireEvent.click(screen.getByRole("button", { name: "Disconnect Google" }));
    expect(props.onUnlink).toHaveBeenCalledWith("Google");
    fireEvent.click(screen.getByRole("button", { name: "Connect Microsoft" }));
    expect(props.onLink).toHaveBeenCalledWith("Microsoft");
    expect(screen.queryByRole("button", { name: "Connect Google" })).toBeNull();
  });

  it("FUI[account.providers] never lets the only way to sign in go, and says why", () => {
    const props = render({ hasPassword: false });
    const button = screen.getByRole("button", { name: "Disconnect Google" });
    expect((button as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(button);
    expect(props.onUnlink).not.toHaveBeenCalled();
    expect(screen.getByText(/is your only way to sign in/)).toBeTruthy();
  });

  it("FUI[account.providers] lets a second provider go when another way remains", () => {
    render({ hasPassword: false, linkedProviders: ["Google", "Microsoft"] });
    for (const name of ["Disconnect Google", "Disconnect Microsoft"])
      expect(
        (screen.getByRole("button", { name }) as HTMLButtonElement).disabled,
      ).toBe(false);
  });

  it("FUI[account.providers] names the provider in the proof, and warns before a disconnect", () => {
    render({
      flow: {
        kind: "stepUp",
        purpose: "unlink",
        provider: "Google",
        factor: "password",
        error: null,
      },
    });
    expect(
      screen.getByRole("region", { name: "Confirm to disconnect Google" }),
    ).toBeTruthy();
    expect(
      screen.getByText(/You will no longer be able to sign in/),
    ).toBeTruthy();
  });
});
