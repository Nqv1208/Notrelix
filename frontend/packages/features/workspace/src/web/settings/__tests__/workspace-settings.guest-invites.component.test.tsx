import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  settingsCopyFixture,
  settingsDefaultScenario,
  settingsReadOnlyScenario,
  workspaceSettingsFixture,
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

function renderSettings(
  scenario = settingsDefaultScenario(),
  overrides: Partial<WorkspaceSettingsSurfaceProps> = {},
) {
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
    ...overrides,
  };
  renderPureUi(<WorkspaceSettingsSurface {...props} />);
  return props;
}

function form(name: string) {
  return screen.getByRole("form", { name });
}

describe("WorkspaceSettingsSurface guest invites switch", () => {
  const offScenario = () => ({
    ...settingsDefaultScenario(),
    defaults: {
      status: "ready" as const,
      data: workspaceSettingsFixture({ allowGuestInvites: false }),
    },
  });
  const guestSwitch = () =>
    screen.getByRole("switch", { name: copy.defaults.guestsLabel });

  it("FUI[workspace.settings:guest-invites] shows the stored value, and the Owner turns it on and saves it", () => {
    const props = renderSettings(offScenario());
    expect(guestSwitch().getAttribute("aria-checked")).toBe("false");

    fireEvent.click(guestSwitch());
    fireEvent.click(
      within(form(copy.defaults.title)).getByRole("button", {
        name: copy.save,
      }),
    );

    expect(props.onSaveDefaults).toHaveBeenCalledWith(
      expect.objectContaining({ allowGuestInvites: true }),
    );
  });

  it("FUI[workspace.settings:guest-invites-readonly] is shown but locked for someone who cannot manage the workspace", () => {
    const props = renderSettings({
      ...settingsReadOnlyScenario(),
      defaults: offScenario().defaults,
    });

    expect(guestSwitch().getAttribute("aria-checked")).toBe("false");
    expect((guestSwitch() as HTMLButtonElement).disabled).toBe(true);
    expect(screen.queryByRole("button", { name: copy.save })).toBeNull();
    expect(props.onSaveDefaults).not.toHaveBeenCalled();
  });
});
