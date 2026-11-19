import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  settingsCopyFixture,
  settingsDefaultScenario,
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

function render(overrides: Partial<WorkspaceSettingsSurfaceProps> = {}) {
  const props: WorkspaceSettingsSurfaceProps = {
    ...settingsDefaultScenario(),
    archived: true,
    canUnarchive: true,
    onUnarchive: vi.fn(),
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

describe("WorkspaceSettingsSurface of an archived workspace (D02)", () => {
  it("FUI[workspace.settings:archived-owner] the Owner sees Unarchive instead of Archive and confirms it", () => {
    const props = render();
    expect(
      screen.queryByRole("button", { name: copy.danger.archiveAction }),
    ).toBeNull();
    fireEvent.click(
      screen.getByRole("button", { name: copy.danger.unarchiveAction }),
    );
    const dialog = screen.getByRole("alertdialog");
    expect(props.onUnarchive).not.toHaveBeenCalled();
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.danger.unarchiveAction }),
    );
    expect(props.onUnarchive).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.settings:archived-admin] an Admin reads why Unarchive is not theirs and gets no button", () => {
    render({ canUnarchive: false });
    expect(screen.getByText(copy.danger.unarchiveOwnerOnly)).toBeTruthy();
    expect(
      screen.queryByRole("button", { name: copy.danger.unarchiveAction }),
    ).toBeNull();
  });

  it("FUI[workspace.settings:archived-pending] shows Unarchive as running and cannot be opened again", () => {
    render({ unarchiving: true });
    const button = screen.getByRole("button", {
      name: copy.danger.unarchiving,
    }) as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });

  it("FUI[workspace.settings:archived-inert] the profile and defaults are read-only while archived, with the reason", () => {
    render();
    expect(screen.getByText(copy.archivedReadOnly)).toBeTruthy();
    expect(screen.queryByRole("button", { name: copy.save })).toBeNull();
  });

  it("FUI[workspace.settings:active-unchanged] an active workspace keeps its Archive card and no Unarchive", () => {
    render({ archived: false });
    expect(
      screen.getByRole("button", { name: copy.danger.archiveAction }),
    ).toBeTruthy();
    expect(
      screen.queryByRole("button", { name: copy.danger.unarchiveAction }),
    ).toBeNull();
  });
});
