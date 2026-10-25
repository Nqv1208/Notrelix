import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  settingsCopyFixture,
  settingsDefaultScenario,
  settingsErrorScenario,
  settingsReadOnlyScenario,
  settingsSuccessScenario,
  settingsVersionUnavailableScenario,
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
    onReload: vi.fn(),
    onRetryDefaults: vi.fn(),
    ...overrides,
  };
  renderPureUi(<WorkspaceSettingsSurface {...props} />);
  return props;
}

function form(name: string) {
  return screen.getByRole("form", { name });
}

describe("WorkspaceSettingsSurface", () => {
  it("FUI[workspace.settings:render] groups Profile, Defaults and Danger zone with bound labels", () => {
    renderSettings();
    expect(
      screen.getByRole("heading", { level: 2, name: copy.profile.title }),
    ).toBeTruthy();
    expect(
      screen.getByRole("heading", { level: 2, name: copy.defaults.title }),
    ).toBeTruthy();
    expect(
      screen.getByRole("heading", { level: 2, name: copy.danger.title }),
    ).toBeTruthy();
    expect(
      (screen.getByLabelText(copy.profile.nameLabel) as HTMLInputElement).value,
    ).toBe("Product");
    expect(
      (
        screen.getByLabelText(
          copy.profile.descriptionLabel,
        ) as HTMLTextAreaElement
      ).value,
    ).toBe("Planning and delivery for the product team.");
    expect(
      screen.getByRole("switch", { name: copy.defaults.guestsLabel }),
    ).toBeTruthy();
  });

  it("FUI[workspace.settings:profile] saves name and description from its own section", () => {
    const props = renderSettings();
    const profile = form(copy.profile.title);
    const save = within(profile).getByRole("button", {
      name: copy.save,
    }) as HTMLButtonElement;
    expect(save.disabled).toBe(true);
    fireEvent.change(screen.getByLabelText(copy.profile.descriptionLabel), {
      target: { value: "  Roadmap, launches and research.  " },
    });
    expect(save.disabled).toBe(false);
    fireEvent.click(save);
    expect(props.onSaveProfile).toHaveBeenCalledWith({
      name: "Product",
      description: "Roadmap, launches and research.",
    });
    expect(props.onSaveDefaults).not.toHaveBeenCalled();
  });

  it("FUI[workspace.settings:validation] validates the name and invitation expiry inline", () => {
    const props = renderSettings();
    fireEvent.change(screen.getByLabelText(copy.profile.nameLabel), {
      target: { value: "   " },
    });
    expect(screen.getByText(copy.profile.nameRequired)).toBeTruthy();
    expect(
      (
        within(form(copy.profile.title)).getByRole("button", {
          name: copy.save,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);

    fireEvent.change(screen.getByLabelText(copy.defaults.expiryLabel), {
      target: { value: "45" },
    });
    expect(screen.getByText(copy.defaults.expiryInvalid)).toBeTruthy();
    fireEvent.click(
      within(form(copy.defaults.title)).getByRole("button", {
        name: copy.save,
      }),
    );
    expect(props.onSaveDefaults).not.toHaveBeenCalled();
  });

  it("FUI[workspace.settings:defaults] saves defaults from their own section", () => {
    const props = renderSettings();
    fireEvent.click(
      screen.getByRole("switch", { name: copy.defaults.mfaLabel }),
    );
    fireEvent.change(screen.getByLabelText(copy.defaults.expiryLabel), {
      target: { value: "14" },
    });
    fireEvent.click(
      within(form(copy.defaults.title)).getByRole("button", {
        name: copy.save,
      }),
    );
    expect(props.onSaveDefaults).toHaveBeenCalledWith(
      expect.objectContaining({ enforceMfa: true, invitationExpiryDays: 14 }),
    );
  });

  it("FUI[workspace.settings:defaults] takes any IANA time zone and refuses an unknown one", () => {
    const props = renderSettings();
    const zone = screen.getByLabelText(copy.defaults.timeZoneLabel);
    const save = () =>
      fireEvent.click(
        within(form(copy.defaults.title)).getByRole("button", {
          name: copy.save,
        }),
      );

    fireEvent.change(zone, { target: { value: "Mars/Olympus" } });
    expect(screen.getByText(copy.defaults.timeZoneInvalid)).toBeTruthy();
    save();
    expect(props.onSaveDefaults).not.toHaveBeenCalled();

    fireEvent.change(zone, { target: { value: "europe/paris" } });
    save();
    expect(props.onSaveDefaults).toHaveBeenCalledWith(
      expect.objectContaining({ timeZone: "Europe/Paris" }),
    );
  });

  it("FUI[workspace.settings:conflict] explains a version conflict and reloads the latest", () => {
    const props = renderSettings(settingsErrorScenario());
    const alert = within(form(copy.profile.title)).getByRole("alert");
    expect(alert.textContent).toContain(copy.conflict);
    fireEvent.click(within(alert).getByRole("button", { name: copy.reload }));
    expect(props.onReload).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole("button", { name: copy.retry }));
    expect(props.onRetryDefaults).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.settings:saved] confirms a completed save next to the section", () => {
    renderSettings(settingsSuccessScenario());
    expect(within(form(copy.profile.title)).getByText(copy.saved)).toBeTruthy();
  });

  it("FUI[workspace.settings:archive] archives only after typing the workspace name", () => {
    const props = renderSettings();
    fireEvent.click(
      screen.getByRole("button", { name: copy.danger.archiveAction }),
    );
    const dialog = screen.getByRole("alertdialog", {
      name: "Archive Product?",
    });
    const confirm = within(dialog).getByRole("button", {
      name: copy.danger.confirmAction,
    }) as HTMLButtonElement;
    expect(confirm.disabled).toBe(true);
    fireEvent.change(within(dialog).getByLabelText(copy.danger.confirmLabel), {
      target: { value: "Prod" },
    });
    expect(
      within(dialog).getByText("Type Product exactly to continue."),
    ).toBeTruthy();
    fireEvent.change(within(dialog).getByLabelText(copy.danger.confirmLabel), {
      target: { value: "Product" },
    });
    fireEvent.click(confirm);
    expect(props.onArchive).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.settings:permission] is read-only without the manage permission and hides the danger zone", () => {
    renderSettings(settingsReadOnlyScenario());
    expect(screen.getByText(copy.readOnly)).toBeTruthy();
    expect(
      (screen.getByLabelText(copy.profile.nameLabel) as HTMLInputElement)
        .disabled,
    ).toBe(true);
    expect(screen.queryByRole("button", { name: copy.save })).toBeNull();
    expect(
      screen.queryByRole("heading", { name: copy.danger.title }),
    ).toBeNull();
  });

  it("FUI[workspace.settings:unavailable] explains why saving is off when the producer reports no version", () => {
    renderSettings(settingsVersionUnavailableScenario());
    expect(screen.getByText(copy.versionUnavailable)).toBeTruthy();
    expect(screen.queryByRole("button", { name: copy.save })).toBeNull();
    expect(
      (
        screen.getByRole("button", {
          name: copy.danger.archiveAction,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });
});
