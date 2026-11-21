import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  settingsCopyFixture,
  settingsDefaultScenario,
  settingsLoadingScenario,
} from "../../../verification/workspace-settings-fixtures";
import {
  WorkspaceSettingsSurface,
  type WorkspaceSettingsSurfaceProps,
} from "../workspace-settings-surface";

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const copy = settingsCopyFixture();

function render(scenario: ReturnType<typeof settingsDefaultScenario>) {
  const props: WorkspaceSettingsSurfaceProps = {
    ...scenario,
    onSaveProfile: vi.fn(),
    onSaveDefaults: vi.fn(),
    onArchive: vi.fn(),
    onDelete: vi.fn(),
    onReload: vi.fn(),
    onRetryDefaults: vi.fn(),
    onTransfer: vi.fn(async () => null),
    ownershipStepUp: {
      request: vi.fn(async () => ({
        factor: "code" as const,
        challengeToken: "challenge",
      })),
      withCode: vi.fn(async () => "proof"),
      withPassword: vi.fn(async () => "proof"),
    },
    onInviteForTransfer: vi.fn(),
  };
  renderPureUi(<WorkspaceSettingsSurface {...props} />);
  return props;
}

describe("WorkspaceSettingsSurface read states", () => {
  it("FUI[workspace.settings:loading] shows a labelled status for each section and no form while the reads are in flight", () => {
    render(settingsLoadingScenario());
    expect(
      screen.getByRole("status", { name: copy.profile.title }),
    ).toBeTruthy();
    expect(
      screen.getByRole("status", { name: copy.defaults.title }),
    ).toBeTruthy();
    expect(screen.queryByRole("form", { name: copy.profile.title })).toBeNull();
    expect(
      screen.queryByRole("form", { name: copy.defaults.title }),
    ).toBeNull();
    expect(screen.queryByRole("button", { name: copy.save })).toBeNull();
  });

  it("FUI[workspace.settings:profile-error] says the workspace profile could not load, without a form that could overwrite it", () => {
    render({ ...settingsDefaultScenario(), profile: { status: "error" } });
    const section = screen
      .getByRole("heading", { name: copy.profile.title })
      .closest("section") as HTMLElement;
    expect(within(section).getByRole("alert").textContent).toContain(
      copy.loadError,
    );
    expect(screen.queryByRole("form", { name: copy.profile.title })).toBeNull();
    // The defaults section is its own read and stays usable.
    expect(
      screen.getByRole("form", { name: copy.defaults.title }),
    ).toBeTruthy();
  });

  it("FUI[workspace.settings:defaults-error] says the defaults could not load and reads them again from its own button", () => {
    const props = render({
      ...settingsDefaultScenario(),
      defaults: { status: "error" },
    });
    const section = screen
      .getByRole("heading", { name: copy.defaults.title })
      .closest("section") as HTMLElement;
    expect(within(section).getByRole("alert").textContent).toContain(
      copy.loadError,
    );
    expect(
      screen.queryByRole("form", { name: copy.defaults.title }),
    ).toBeNull();
    fireEvent.click(within(section).getByRole("button", { name: copy.retry }));
    expect(props.onRetryDefaults).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.settings:defaults-values] shows the stored defaults, so a member reads exactly what the producer holds", () => {
    render(settingsDefaultScenario());
    const defaults = screen.getByRole("form", { name: copy.defaults.title });
    expect(
      (
        within(defaults).getByLabelText(
          copy.defaults.expiryLabel,
        ) as HTMLInputElement
      ).value,
    ).toBe("7");
  });
});
