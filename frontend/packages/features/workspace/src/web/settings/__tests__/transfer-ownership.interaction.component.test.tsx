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
import type { OwnershipStepUp } from "../ownership-step-up";
import type { TransferFailure } from "../transfer-ownership-flow";
import { workspaceSettingsCopy } from "../workspace-settings.copy";

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const copy = settingsCopyFixture();
const d = copy.danger;

function stepUpPort(over: Partial<OwnershipStepUp> = {}): OwnershipStepUp {
  return {
    request: vi.fn(async () => ({
      factor: "code" as const,
      challengeToken: "challenge-1",
    })),
    withCode: vi.fn(async () => "proof-1"),
    withPassword: vi.fn(async () => "proof-pw"),
    ...over,
  };
}

function renderSettings(
  scenario = settingsDefaultScenario(),
  overrides: Partial<WorkspaceSettingsSurfaceProps> = {},
) {
  const props: WorkspaceSettingsSurfaceProps = {
    ...scenario,
    ownershipStepUp: stepUpPort(),
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

/** Confirms the chosen person; the dialog then asks the host which factor proves presence. */
async function confirmChoice(dialog: HTMLElement, name = "Nguyễn Thu Hà") {
  await act(async () => {
    fireEvent.click(
      within(dialog).getByRole("button", { name: `Transfer to ${name}` }),
    );
  });
}

function typeCode(dialog: HTMLElement, code = "123456") {
  fireEvent.change(within(dialog).getByLabelText(d.stepUpCodeLabel), {
    target: { value: code },
  });
}

/** Answers the proof step with a 6-digit code and sends it. */
async function proveWithCode(dialog: HTMLElement, code = "123456") {
  typeCode(dialog, code);
  await act(async () => {
    fireEvent.click(
      within(dialog).getByRole("button", { name: d.stepUpConfirm }),
    );
  });
}

/** The whole confirm-then-prove path: choose, confirm, answer with a code. */
async function handOver(dialog: HTMLElement, name?: string) {
  await confirmChoice(dialog, name);
  await proveWithCode(dialog);
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
    await confirmChoice(dialog, "Lê Minh Khoa");
    // Confirming the choice sends nothing yet: the proof of presence comes first.
    expect(props.ownershipStepUp.request).toHaveBeenCalledTimes(1);
    expect(props.onTransfer).not.toHaveBeenCalled();
    await proveWithCode(dialog);
    expect(props.ownershipStepUp.withCode).toHaveBeenCalledWith(
      "challenge-1",
      "123456",
    );
    expect(props.onTransfer).toHaveBeenCalledWith("u-khoa", "proof-1");
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
    await handOver(dialog);
    const pending = within(dialog).getByRole("button", {
      name: d.stepUpChecking,
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
    await handOver(dialog);
    const alert = within(dialog).getByRole("alert");
    expect(alert.textContent).toContain(d.transferConflictTitle);
    expect(alert.textContent).toContain(d.transferConflictBody);
    // The destructive button is gone, so the same stale choice cannot be sent again.
    expect(
      within(dialog).queryByRole("button", { name: d.stepUpConfirm }),
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
      await handOver(dialog);
      expect(within(dialog).getByRole("alert").textContent).toContain(text);
      // The proof was spent, so a fresh challenge waits and the field is empty again.
      expect(props.ownershipStepUp.request).toHaveBeenCalledTimes(2);
      await proveWithCode(dialog);
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

  it("FUI[workspace.settings:transfer-ownership] the proof step's buttons are touch size on a phone", async () => {
    renderSettings();
    const dialog = open();
    await confirmChoice(dialog);
    for (const name of [d.cancel, d.stepUpConfirm]) {
      expect(within(dialog).getByRole("button", { name }).className).toContain(
        "max-md:h-touch",
      );
    }
    expect(
      within(dialog).getByRole("button", { name: d.stepUpUseRecovery })
        .className,
    ).toContain("max-md:min-h-touch");
  });

  describe("the step-up proof (confirm it's you)", () => {
    it("FUI[workspace.settings:transfer-ownership-stepup] asks the proof after Confirm, names the person and sends nothing before the code", async () => {
      const props = renderSettings();
      const dialog = open();
      await confirmChoice(dialog);

      expect(within(dialog).getByText(d.stepUpTitle)).toBeTruthy();
      expect(
        within(dialog).getByText(
          "Handing Product to Nguyễn Thu Hà needs you to confirm it's you, even though you're signed in.",
        ),
      ).toBeTruthy();
      // The person cannot send an unfinished code.
      typeCode(dialog, "48");
      expect(
        (
          within(dialog).getByRole("button", {
            name: d.stepUpConfirm,
          }) as HTMLButtonElement
        ).disabled,
      ).toBe(true);
      expect(props.onTransfer).not.toHaveBeenCalled();
    });

    it("FUI[workspace.settings:transfer-ownership-stepup] a wrong code is said in place and the person can try again", async () => {
      const port = stepUpPort({
        withCode: vi
          .fn<OwnershipStepUp["withCode"]>()
          .mockRejectedValueOnce({
            status: 400,
            code: "identity.security.step-up-invalid-code",
          })
          .mockResolvedValueOnce("proof-2"),
      });
      const props = renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: port,
      });
      const dialog = open();
      await handOver(dialog);

      expect(within(dialog).getByRole("alert").textContent).toContain(
        d.stepUpWrongCode,
      );
      expect(props.onTransfer).not.toHaveBeenCalled();
      expect(port.request).toHaveBeenCalledTimes(1);
      await proveWithCode(dialog, "654321");
      expect(props.onTransfer).toHaveBeenCalledWith("u-ha", "proof-2");
    });

    it("FUI[workspace.settings:transfer-ownership-stepup] a person without two-factor confirms with the password", async () => {
      const port = stepUpPort({
        request: vi.fn(async () => ({
          factor: "password" as const,
          challengeToken: null,
        })),
        withPassword: vi
          .fn<OwnershipStepUp["withPassword"]>()
          .mockRejectedValueOnce({
            status: 400,
            code: "identity.security.step-up-invalid-password",
          })
          .mockResolvedValueOnce("proof-pw"),
      });
      const props = renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: port,
      });
      const dialog = open();
      await confirmChoice(dialog);

      const field = within(dialog).getByLabelText(d.stepUpPasswordLabel);
      expect(field.getAttribute("type")).toBe("password");
      fireEvent.change(field, { target: { value: "wrong-password" } });
      await act(async () => {
        fireEvent.click(
          within(dialog).getByRole("button", { name: d.stepUpConfirm }),
        );
      });
      expect(within(dialog).getByRole("alert").textContent).toContain(
        d.stepUpWrongPassword,
      );
      fireEvent.change(within(dialog).getByLabelText(d.stepUpPasswordLabel), {
        target: { value: "right-password" },
      });
      await act(async () => {
        fireEvent.click(
          within(dialog).getByRole("button", { name: d.stepUpConfirm }),
        );
      });
      expect(port.withPassword).toHaveBeenLastCalledWith("right-password");
      expect(props.onTransfer).toHaveBeenCalledWith("u-ha", "proof-pw");
      expect(port.withCode).not.toHaveBeenCalled();
    });

    it("FUI[workspace.settings:transfer-ownership-stepup] a recovery code can stand in for the authenticator code", async () => {
      const port = stepUpPort();
      const props = renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: port,
      });
      const dialog = open();
      await confirmChoice(dialog);
      fireEvent.click(
        within(dialog).getByRole("button", { name: d.stepUpUseRecovery }),
      );
      fireEvent.change(within(dialog).getByLabelText(d.stepUpRecoveryLabel), {
        target: { value: " RC01-MOCK-1000 " },
      });
      await act(async () => {
        fireEvent.click(
          within(dialog).getByRole("button", { name: d.stepUpConfirm }),
        );
      });
      expect(port.withCode).toHaveBeenCalledWith(
        "challenge-1",
        "RC01-MOCK-1000",
      );
      expect(props.onTransfer).toHaveBeenCalledWith("u-ha", "proof-1");
    });

    it("FUI[workspace.settings:transfer-ownership-stepup] a person who signs in with a provider only is told to add a way to confirm first", async () => {
      const props = renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: stepUpPort({
          request: vi.fn(async () => ({
            factor: "provider" as const,
            challengeToken: null,
          })),
        }),
      });
      const dialog = open();
      await confirmChoice(dialog);
      expect(within(dialog).getByText(d.stepUpProviderTitle)).toBeTruthy();
      expect(within(dialog).getByText(d.stepUpProviderBody)).toBeTruthy();
      expect(
        within(dialog).queryByRole("button", { name: d.stepUpConfirm }),
      ).toBeNull();
      expect(props.onTransfer).not.toHaveBeenCalled();
    });

    it("FUI[workspace.settings:transfer-ownership-stepup] an expired confirmation locks the field and starts again on request", async () => {
      const port = stepUpPort({
        withCode: vi.fn(async () => {
          throw { status: 400, code: "identity.security.step-up-invalid" };
        }),
      });
      const props = renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: port,
      });
      const dialog = open();
      await handOver(dialog);

      expect(within(dialog).getByText(d.stepUpExpiredTitle)).toBeTruthy();
      expect(within(dialog).getByText(d.stepUpExpiredBody)).toBeTruthy();
      expect(
        (within(dialog).getByLabelText(d.stepUpCodeLabel) as HTMLInputElement)
          .disabled,
      ).toBe(true);
      expect(
        within(dialog).queryByRole("button", { name: d.stepUpConfirm }),
      ).toBeNull();
      expect(props.onTransfer).not.toHaveBeenCalled();
      await act(async () => {
        fireEvent.click(
          within(dialog).getByRole("button", { name: d.stepUpRestart }),
        );
      });
      expect(port.request).toHaveBeenCalledTimes(2);
      expect(within(dialog).queryByText(d.stepUpExpiredTitle)).toBeNull();
      expect(
        (within(dialog).getByLabelText(d.stepUpCodeLabel) as HTMLInputElement)
          .disabled,
      ).toBe(false);
    });

    it.each([
      ["step-up-required", "no proof reached the producer"],
      ["step-up-invalid", "a stale, spent or foreign proof"],
    ] as const)(
      "FUI[workspace.settings:transfer-ownership-stepup] when the producer refuses the handover for %s (%s), nothing changes and a fresh confirmation is asked",
      async (kind, _why) => {
        const props = renderSettings(settingsDefaultScenario(), {
          onTransfer: vi
            .fn<WorkspaceSettingsSurfaceProps["onTransfer"]>()
            .mockResolvedValueOnce({ kind })
            .mockResolvedValueOnce(null),
        });
        const dialog = open();
        await handOver(dialog);

        const alert = within(dialog).getByRole("alert");
        expect(alert.textContent).toContain(d.stepUpRefusedTitle);
        expect(alert.textContent).toContain(d.stepUpRefusedBody);
        // A new challenge, an empty field, and the dialog is still open.
        expect(props.ownershipStepUp.request).toHaveBeenCalledTimes(2);
        expect(
          (within(dialog).getByLabelText(d.stepUpCodeLabel) as HTMLInputElement)
            .value,
        ).toBe("");
        await proveWithCode(dialog, "111222");
        expect(props.onTransfer).toHaveBeenCalledTimes(2);
        await waitFor(() =>
          expect(screen.queryByRole("alertdialog")).toBeNull(),
        );
      },
    );

    it("FUI[workspace.settings:transfer-ownership-stepup] too many tries says how long to wait and holds the button", async () => {
      renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: stepUpPort({
          withCode: vi.fn(async () => {
            throw { status: 429, details: { retryAfterSeconds: 60 } };
          }),
        }),
      });
      const dialog = open();
      await handOver(dialog);
      expect(within(dialog).getByRole("alert").textContent).toContain(
        "Wait 60 seconds",
      );
      typeCode(dialog, "222333");
      expect(
        (
          within(dialog).getByRole("button", {
            name: d.stepUpConfirm,
          }) as HTMLButtonElement
        ).disabled,
      ).toBe(true);
    });

    it("FUI[workspace.settings:transfer-ownership-stepup] offline while asking for the proof stays on the choice and says so", async () => {
      const props = renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: stepUpPort({
          request: vi.fn(async () => {
            throw { kind: "network" };
          }),
        }),
      });
      const dialog = open();
      await confirmChoice(dialog);
      expect(within(dialog).getByRole("alert").textContent).toContain(
        copy.toasts.network,
      );
      expect(
        within(dialog).getByRole("button", {
          name: "Transfer to Nguyễn Thu Hà",
        }),
      ).toBeTruthy();
      expect(props.onTransfer).not.toHaveBeenCalled();
    });

    it("FUI[workspace.settings:transfer-ownership-stepup] offline while answering says so and keeps the dialog", async () => {
      const props = renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: stepUpPort({
          withCode: vi.fn(async () => {
            throw { kind: "network" };
          }),
        }),
      });
      const dialog = open();
      await handOver(dialog);
      expect(within(dialog).getByRole("alert").textContent).toContain(
        copy.toasts.network,
      );
      expect(props.onTransfer).not.toHaveBeenCalled();
    });

    it("FUI[workspace.settings:transfer-ownership-stepup] cancel closes the dialog and sends nothing", async () => {
      const props = renderSettings();
      const dialog = open();
      await confirmChoice(dialog);
      fireEvent.click(within(dialog).getByRole("button", { name: d.cancel }));
      await waitFor(() => expect(screen.queryByRole("alertdialog")).toBeNull());
      expect(props.onTransfer).not.toHaveBeenCalled();
    });

    it("FUI[workspace.settings:transfer-ownership-stepup] reopening starts from the choice again", async () => {
      renderSettings();
      let dialog = open();
      await confirmChoice(dialog);
      fireEvent.click(within(dialog).getByRole("button", { name: d.cancel }));
      await waitFor(() => expect(screen.queryByRole("alertdialog")).toBeNull());
      dialog = open();
      expect(within(dialog).getByRole("radiogroup")).toBeTruthy();
      expect(within(dialog).queryByText(d.stepUpTitle)).toBeNull();
    });
  });

  describe("the provider round trip (a person who signs in only through a provider)", () => {
    function providerPort(
      over: Partial<NonNullable<OwnershipStepUp["provider"]>> = {},
    ): OwnershipStepUp {
      return stepUpPort({
        request: vi.fn(async () => ({
          factor: "provider" as const,
          challengeToken: null,
        })),
        provider: {
          providers: vi.fn(async () => ["Google", "Microsoft"]),
          start: vi.fn(async () => undefined),
          resume: vi.fn(() => null),
          claim: vi.fn(async () => "proof-provider"),
          ...over,
        },
      });
    }

    it("FUI[workspace.settings:transfer-ownership-stepup-provider] offers one button per linked provider and leaves with the chosen person", async () => {
      const port = providerPort();
      const props = renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: port,
      });
      const dialog = open();
      await confirmChoice(dialog);

      expect(within(dialog).getByText(d.stepUpProviderText)).toBeTruthy();
      expect(
        within(dialog).getByRole("button", { name: "Confirm with Microsoft" }),
      ).toBeTruthy();
      await act(async () => {
        fireEvent.click(
          within(dialog).getByRole("button", { name: "Confirm with Google" }),
        );
      });

      expect(port.provider?.start).toHaveBeenCalledWith("Google", "u-ha");
      expect(props.onTransfer).not.toHaveBeenCalled();
      expect(
        within(dialog).queryByRole("button", { name: d.stepUpConfirm }),
      ).toBeNull();
    });

    it("FUI[workspace.settings:transfer-ownership-stepup-provider] a return reopens the dialog, takes the proof once and hands over", async () => {
      const port = providerPort({
        resume: vi.fn(() => ({ newOwnerId: "u-lan", failed: false })),
      });
      const props = renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: port,
      });

      await waitFor(() =>
        expect(props.onTransfer).toHaveBeenCalledWith(
          "u-lan",
          "proof-provider",
        ),
      );
      expect(props.onTransfer).toHaveBeenCalledTimes(1);
      expect(port.provider?.claim).toHaveBeenCalledTimes(1);
      await waitFor(() => expect(screen.queryByRole("alertdialog")).toBeNull());
    });

    it("FUI[workspace.settings:transfer-ownership-stepup-provider] a provider that refuses hands nothing over and offers the buttons again", async () => {
      const port = providerPort({
        resume: vi.fn(() => ({ newOwnerId: "u-ha", failed: true })),
      });
      const props = renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: port,
      });

      const dialog = await screen.findByRole("alertdialog");
      expect(
        await within(dialog).findByText(d.stepUpProviderFailed),
      ).toBeTruthy();
      expect(
        within(dialog).getByRole("button", { name: "Confirm with Google" }),
      ).toBeTruthy();
      expect(port.provider?.claim).not.toHaveBeenCalled();
      expect(props.onTransfer).not.toHaveBeenCalled();
    });

    it("FUI[workspace.settings:transfer-ownership-stepup-provider] a return with no proof left says the confirmation expired and hands nothing over", async () => {
      const port = providerPort({
        resume: vi.fn(() => ({ newOwnerId: "u-ha", failed: false })),
        claim: vi.fn(async () => {
          throw { status: 400, code: "identity.security.step-up-invalid" };
        }),
      });
      const props = renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: port,
      });

      const dialog = await screen.findByRole("alertdialog");
      expect(
        await within(dialog).findByText(d.stepUpExpiredTitle, { exact: false }),
      ).toBeTruthy();
      expect(props.onTransfer).not.toHaveBeenCalled();
    });

    it("FUI[workspace.settings:transfer-ownership-stepup-provider] a return naming someone who is not eligible is ignored and spends nothing", async () => {
      const port = providerPort({
        resume: vi.fn(() => ({ newOwnerId: "u-nobody", failed: false })),
      });
      const props = renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: port,
      });

      await waitFor(() => expect(port.provider?.resume).toHaveBeenCalled());
      expect(screen.queryByRole("alertdialog")).toBeNull();
      expect(port.provider?.claim).not.toHaveBeenCalled();
      expect(props.onTransfer).not.toHaveBeenCalled();
    });

    it("FUI[workspace.settings:transfer-ownership-stepup-provider] a proof the producer refuses is said in place and the buttons come back", async () => {
      const failure: TransferFailure = { kind: "step-up-invalid" };
      const port = providerPort({
        resume: vi.fn(() => ({ newOwnerId: "u-ha", failed: false })),
      });
      renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: port,
        onTransfer: vi.fn(async () => failure),
      });

      const dialog = await screen.findByRole("alertdialog");
      expect(
        await within(dialog).findByText(d.stepUpRefusedTitle, { exact: false }),
      ).toBeTruthy();
      expect(
        within(dialog).getByRole("button", { name: "Confirm with Google" }),
      ).toBeTruthy();
    });

    it("FUI[workspace.settings:transfer-ownership-stepup-provider] a provider that cannot be reached says so and nothing leaves", async () => {
      const port = providerPort({
        start: vi.fn(async () => {
          throw { kind: "network" };
        }),
      });
      renderSettings(settingsDefaultScenario(), { ownershipStepUp: port });
      const dialog = open();
      await confirmChoice(dialog);
      await act(async () => {
        fireEvent.click(
          within(dialog).getByRole("button", { name: "Confirm with Google" }),
        );
      });
      expect(await within(dialog).findByText(copy.toasts.network)).toBeTruthy();
    });

    it("FUI[workspace.settings:transfer-ownership-stepup-provider] with no linked provider the dialog still says to add a way to confirm", async () => {
      renderSettings(settingsDefaultScenario(), {
        ownershipStepUp: providerPort({ providers: vi.fn(async () => []) }),
      });
      const dialog = open();
      await confirmChoice(dialog);
      expect(within(dialog).getByText(d.stepUpProviderTitle)).toBeTruthy();
    });
  });

  it("FUI[workspace.settings:transfer-ownership] has a Vietnamese copy for every string", () => {
    const vi_ = workspaceSettingsCopy("vi").danger;
    for (const key of Object.keys(d) as (keyof typeof d)[]) {
      expect(vi_[key], key).toBeTruthy();
    }
    expect(vi_.transferConfirm).toContain("{{name}}");
  });
});
