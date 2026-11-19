import { beforeAll, describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";

import {
  settingsAdminScenario,
  settingsCopyFixture,
  settingsDefaultScenario,
  settingsNoTransferCandidateScenario,
} from "../../../verification/workspace-settings-fixtures";
import {
  WorkspaceSettingsSurface,
  type WorkspaceSettingsSurfaceProps,
} from "../workspace-settings-surface";
import type { TransferFailure } from "../transfer-ownership-flow";
import { workspaceSettingsCopy } from "../workspace-settings.copy";

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const copy = settingsCopyFixture();
const d = copy.danger;

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
    onInviteForTransfer: vi.fn(),
    ...overrides,
  };
  renderPureUi(<WorkspaceSettingsSurface {...props} />);
  return props;
}

function open() {
  fireEvent.click(screen.getByRole("button", { name: d.transferAction }));
  return screen.getByRole("alertdialog", {
    name: "Transfer ownership of Product",
  });
}

describe("Transfer ownership (hand the workspace over)", () => {
  it("FUI[workspace.settings:transfer-ownership] is offered to the Owner only", () => {
    renderSettings(settingsAdminScenario());
    expect(screen.queryByRole("button", { name: d.transferAction })).toBeNull();
  });

  it("FUI[workspace.settings:transfer-ownership] lists active non-Guest people, names the effect and confirms the chosen person", async () => {
    const props = renderSettings();
    const dialog = open();

    const group = within(dialog).getByRole("radiogroup", {
      name: d.transferPick,
    });
    const radios = within(group).getAllByRole("radio");
    expect(radios).toHaveLength(3);
    expect(within(group).getByText("Admin · Phòng Nhân sự")).toBeTruthy();
    expect(within(group).getByText("Member")).toBeTruthy();
    // The first person is chosen and the effect names them.
    expect(radios[0]?.getAttribute("aria-checked")).toBe("true");
    expect(
      within(dialog).getByText(
        "Nguyễn Thu Hà becomes the owner and can manage the plan and delete the workspace. You become an admin and can't take ownership back yourself.",
      ),
    ).toBeTruthy();

    fireEvent.click(radios[2]!);
    const confirm = within(dialog).getByRole("button", {
      name: "Transfer to Lê Minh Khoa",
    });
    await act(async () => {
      fireEvent.click(confirm);
    });
    expect(props.onTransfer).toHaveBeenCalledWith("u-khoa");
    await waitFor(() => expect(screen.queryByRole("alertdialog")).toBeNull());
  });

  it("FUI[workspace.settings:transfer-ownership] shows pending and keeps the dialog until the producer answers", async () => {
    let finish: (value: TransferFailure | null) => void = () => undefined;
    const props = renderSettings(settingsDefaultScenario(), {
      onTransfer: vi.fn(
        () =>
          new Promise<TransferFailure | null>((resolve) => {
            finish = resolve;
          }),
      ),
    });
    const dialog = open();
    await act(async () => {
      fireEvent.click(
        within(dialog).getByRole("button", {
          name: "Transfer to Nguyễn Thu Hà",
        }),
      );
    });
    const pending = within(dialog).getByRole("button", {
      name: d.transferPending,
    }) as HTMLButtonElement;
    expect(pending.disabled).toBe(true);
    // A second press while pending sends nothing more.
    fireEvent.click(pending);
    expect(props.onTransfer).toHaveBeenCalledTimes(1);
    await act(async () => finish(null));
    await waitFor(() => expect(screen.queryByRole("alertdialog")).toBeNull());
  });

  it("FUI[workspace.settings:transfer-ownership] with nobody eligible says so and offers the invite path", () => {
    const props = renderSettings(settingsNoTransferCandidateScenario());
    const dialog = open();
    expect(within(dialog).getByText(d.transferNoneTitle)).toBeTruthy();
    expect(within(dialog).getByText(d.transferNoneBody)).toBeTruthy();
    expect(within(dialog).queryByRole("radio")).toBeNull();
    expect(
      within(dialog).queryByRole("button", { name: /^Transfer to/ }),
    ).toBeNull();
    fireEvent.click(
      within(dialog).getByRole("button", { name: d.transferInvite }),
    );
    expect(props.onInviteForTransfer).toHaveBeenCalledTimes(1);
    expect(props.onTransfer).not.toHaveBeenCalled();
  });

  it("FUI[workspace.settings:transfer-ownership] a stale list is refused in place and can be reloaded", async () => {
    const props = renderSettings(settingsDefaultScenario(), {
      onTransfer: vi.fn(async () => ({ kind: "conflict" }) as const),
    });
    const dialog = open();
    await act(async () => {
      fireEvent.click(
        within(dialog).getByRole("button", {
          name: "Transfer to Nguyễn Thu Hà",
        }),
      );
    });
    const alert = within(dialog).getByRole("alert");
    expect(alert.textContent).toContain(d.transferConflictTitle);
    expect(alert.textContent).toContain(d.transferConflictBody);
    // The destructive button is gone, so the same stale choice cannot be sent again.
    expect(
      within(dialog).queryByRole("button", { name: /^Transfer to/ }),
    ).toBeNull();
    fireEvent.click(
      within(dialog).getByRole("button", { name: d.transferReload }),
    );
    expect(props.onReload).toHaveBeenCalledTimes(1);
    expect(props.onTransfer).toHaveBeenCalledTimes(1);
  });

  it.each<[string, TransferFailure, string]>([
    [
      "a rule refusal (a Guest, say)",
      { kind: "rejected", message: "A guest cannot become the owner." },
      "A guest cannot become the owner.",
    ],
    [
      "a rule refusal without a message",
      { kind: "rejected", message: null },
      d.transferRejected,
    ],
    ["no permission", { kind: "forbidden" }, copy.toasts.forbidden],
    ["offline", { kind: "network" }, copy.toasts.network],
    ["a server error", { kind: "unknown" }, copy.toasts.unknown],
  ])(
    "FUI[workspace.settings:transfer-ownership] %s is said in the dialog and can be tried again",
    async (_name, failure, text) => {
      const props = renderSettings(settingsDefaultScenario(), {
        onTransfer: vi.fn(async () => failure),
      });
      const dialog = open();
      await act(async () => {
        fireEvent.click(
          within(dialog).getByRole("button", {
            name: "Transfer to Nguyễn Thu Hà",
          }),
        );
      });
      expect(within(dialog).getByRole("alert").textContent).toContain(text);
      const retry = within(dialog).getByRole("button", {
        name: "Transfer to Nguyễn Thu Hà",
      }) as HTMLButtonElement;
      expect(retry.disabled).toBe(false);
      await act(async () => {
        fireEvent.click(retry);
      });
      expect(props.onTransfer).toHaveBeenCalledTimes(2);
    },
  );

  it("FUI[workspace.settings:transfer-ownership] stays off while the workspace version is unknown", () => {
    renderSettings({ ...settingsDefaultScenario(), versionAvailable: false });
    expect(
      (
        screen.getByRole("button", {
          name: d.transferAction,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });

  it("FUI[workspace.settings:transfer-ownership] its buttons are touch size on a phone", () => {
    renderSettings();
    expect(
      screen.getByRole("button", { name: d.transferAction }).className,
    ).toContain("max-md:h-touch");
    const dialog = open();
    expect(
      within(dialog).getByRole("button", { name: d.cancel }).className,
    ).toContain("max-md:h-touch");
    expect(
      within(dialog).getByRole("button", { name: "Transfer to Nguyễn Thu Hà" })
        .className,
    ).toContain("max-md:h-touch");
  });

  it("FUI[workspace.settings:transfer-ownership] has a Vietnamese copy for every string", () => {
    const vi_ = workspaceSettingsCopy("vi").danger;
    for (const key of Object.keys(d) as (keyof typeof d)[]) {
      expect(vi_[key], key).toBeTruthy();
    }
    expect(vi_.transferConfirm).toContain("{{name}}");
  });
});
