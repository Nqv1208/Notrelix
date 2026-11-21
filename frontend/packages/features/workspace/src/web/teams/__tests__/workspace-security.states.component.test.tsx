import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  securityDefaultScenario,
  securityErrorScenario,
  securityLinksOnScenario,
  securityLoadingScenario,
  type SecurityScenario,
} from "../../../verification/workspace-teams-fixtures";
import { WorkspaceSecuritySurface } from "../workspace-security-surface";

function render(scenario: SecurityScenario) {
  const props = {
    ...scenario,
    onToggleLinks: vi.fn(),
    onRetry: vi.fn(),
  };
  renderPureUi(<WorkspaceSecuritySurface {...props} />);
  return props;
}

describe("WorkspaceSecuritySurface states", () => {
  it("FUI[workspace.security:loading] shows a status that names this page while the extras read", () => {
    const props = render(securityLoadingScenario());
    expect(
      screen.getByRole("status", { name: props.copy.security.loading }),
    ).toBeTruthy();
    expect(screen.queryByRole("switch")).toBeNull();
  });

  it("FUI[workspace.security:error] says this page could not load and offers to read again", () => {
    const props = render(securityErrorScenario());
    expect(screen.getByRole("alert").textContent).toContain(
      props.copy.security.error,
    );
    expect(screen.queryByRole("switch")).toBeNull();
    fireEvent.click(
      screen.getByRole("button", { name: props.copy.security.retry }),
    );
    expect(props.onRetry).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.security:domain] shows the company domain row when the Account has a verified domain", () => {
    const props = render(securityDefaultScenario());
    expect(
      (
        screen.getByLabelText(
          props.copy.security.domain.title,
        ) as HTMLInputElement
      ).value,
    ).toBe("minhan.vn");
  });

  it("FUI[workspace.security:domain-hidden] hides the company domain row when the Account has no verified domain", () => {
    const base = securityDefaultScenario();
    if (base.view.status !== "ready") throw new Error("fixture is ready");
    const props = render({
      ...base,
      view: {
        status: "ready",
        data: { ...base.view.data, companyDomain: null },
      },
    });
    expect(
      screen.queryByLabelText(props.copy.security.domain.title),
    ).toBeNull();
    expect(screen.queryByText(props.copy.security.domain.title)).toBeNull();
    // The rest of the page stays.
    expect(screen.getByRole("switch")).toBeTruthy();
  });

  it("FUI[workspace.security:saving] holds the switch while the write is in flight and shows the failure beside it", () => {
    const props = render({
      ...securityDefaultScenario(),
      saving: true,
      error: "Không lưu được.",
    });
    expect(
      (
        screen.getByRole("switch", {
          name: props.copy.security.link.switchLabel,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    expect(screen.getByText("Không lưu được.")).toBeTruthy();
  });

  it("FUI[workspace.security:guests] counts one Guest in the singular and a default with no end as unlimited", () => {
    const props = render(securityLinksOnScenario());
    expect(
      screen.getByText(
        props.copy.security.guests.count_one.replace("{{count}}", "1"),
      ),
    ).toBeTruthy();
    expect(
      screen.getByLabelText(props.copy.security.expiry.title).textContent,
    ).toBe(props.copy.security.expiry.options.never);
  });
});
