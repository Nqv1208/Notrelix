import * as React from "react";
import {
  afterEach,
  beforeAll,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from "vitest";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  within,
} from "@notrelix/testing";

import {
  settingsCopyFixture,
  settingsDefaultScenario,
} from "../../../verification/workspace-settings-fixtures";
import {
  WorkspaceSettingsSurface,
  type AddressCheckResult,
  type SaveStatus,
  type WorkspaceSettingsSurfaceProps,
} from "../workspace-settings-surface";

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

beforeEach(() => vi.useFakeTimers());
afterEach(() => vi.useRealTimers());

const copy = settingsCopyFixture();
const FREE: AddressCheckResult = {
  available: true,
  reason: null,
  suggestion: null,
};

function baseProps(
  base = settingsDefaultScenario(),
): WorkspaceSettingsSurfaceProps {
  return {
    ...base,
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
}

function renderAddress({
  canChange = true,
  status = { state: "idle" } as SaveStatus,
  check = vi.fn(async () => FREE),
  base = settingsDefaultScenario(),
}: {
  canChange?: boolean;
  status?: SaveStatus;
  check?: (slug: string) => Promise<AddressCheckResult>;
  base?: ReturnType<typeof settingsDefaultScenario>;
} = {}) {
  const onSave = vi.fn();
  const onCheck = vi.fn(check);
  const props: WorkspaceSettingsSurfaceProps = {
    ...baseProps(base),
    address: { slug: "product", canChange, status, onCheck, onSave },
  };
  renderPureUi(<WorkspaceSettingsSurface {...props} />);
  return { props, onSave, onCheck };
}

const form = () => screen.getByRole("form", { name: copy.address.title });
const input = () =>
  within(form()).getByLabelText(copy.address.label) as HTMLInputElement;
const save = () =>
  within(form()).getByRole("button", { name: copy.save }) as HTMLButtonElement;

async function type(value: string) {
  fireEvent.change(input(), { target: { value } });
  await act(async () => {
    await vi.advanceTimersByTimeAsync(400);
  });
}

describe("workspace address field", () => {
  it("FUI[workspace.settings:address-owner] lets the Owner edit the address and save it once it is free", async () => {
    const { onSave, onCheck } = renderAddress();
    expect(input().value).toBe("product");
    expect(save().disabled).toBe(true);

    await type("  Product-Team ");
    expect(onCheck).toHaveBeenCalledWith("product-team");
    expect(screen.getByText(copy.address.free)).toBeTruthy();
    expect(save().disabled).toBe(false);
    fireEvent.click(save());
    expect(onSave).toHaveBeenCalledWith("product-team");
  });

  it("FUI[workspace.settings:address-readonly] shows the address read-only, with no save, to everyone but the Owner", () => {
    const { onCheck } = renderAddress({ canChange: false });
    expect(input().disabled).toBe(true);
    expect(input().value).toBe("product");
    expect(screen.getByText(copy.address.ownerOnly)).toBeTruthy();
    expect(
      within(form()).queryByRole("button", { name: copy.save }),
    ).toBeNull();
    expect(onCheck).not.toHaveBeenCalled();
  });

  it("FUI[workspace.settings:address-invalid] refuses a malformed address before asking the check", async () => {
    const { onSave, onCheck } = renderAddress();
    await type("Bad Address!");
    expect(screen.getByText(copy.address.invalid)).toBeTruthy();
    expect(save().disabled).toBe(true);
    await type("ab");
    expect(save().disabled).toBe(true);
    expect(onCheck).not.toHaveBeenCalled();
    expect(onSave).not.toHaveBeenCalled();
  });

  it("FUI[workspace.settings:address-taken] says the address is taken, offers the suggestion and blocks the save", async () => {
    const { onSave } = renderAddress({
      check: async () => ({
        available: false,
        reason: "taken",
        suggestion: "product-2",
      }),
    });
    await type("acme");
    expect(
      screen.getByText(
        "The address acme is already used by another workspace.",
      ),
    ).toBeTruthy();
    expect(save().disabled).toBe(true);
    fireEvent.click(within(form()).getByRole("button", { name: "product-2" }));
    expect(input().value).toBe("product-2");
    expect(onSave).not.toHaveBeenCalled();
  });

  it("FUI[workspace.settings:address-taken-on-save] shows the refusal of a save for an address someone took meanwhile", async () => {
    function Harness() {
      const [status, setStatus] = React.useState<SaveStatus>({ state: "idle" });
      return (
        <WorkspaceSettingsSurface
          {...baseProps()}
          address={{
            slug: "product",
            canChange: true,
            status,
            onCheck: async () => FREE,
            onSave: () => setStatus({ state: "taken" }),
          }}
        />
      );
    }
    renderPureUi(<Harness />);
    await type("acme");
    fireEvent.click(save());
    expect(
      screen.getByText(
        "The address acme is already used by another workspace.",
      ),
    ).toBeTruthy();
    expect(save().disabled).toBe(true);
    await type("acme-two");
    expect(screen.queryByText(/already used/)).toBeNull();
    expect(save().disabled).toBe(false);
  });

  it("FUI[workspace.settings:address-pending] disables the field and says saving while the change is pending", () => {
    renderAddress({ status: { state: "saving" } });
    expect(input().disabled).toBe(true);
    expect(
      within(form()).getByRole("button", { name: copy.saving }),
    ).toBeTruthy();
  });

  it("FUI[workspace.settings:address-saved] confirms a saved change", () => {
    renderAddress({ status: { state: "saved" } });
    expect(within(form()).getByText(copy.saved)).toBeTruthy();
  });

  it("FUI[workspace.settings:address-conflict] explains a stale version and reloads the latest", () => {
    const { props } = renderAddress({ status: { state: "conflict" } });
    const alert = within(form()).getByRole("alert");
    expect(alert.textContent).toContain(copy.conflict);
    fireEvent.click(within(alert).getByRole("button", { name: copy.reload }));
    expect(props.onReload).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.settings:address-archived] is read-only in an archived workspace", () => {
    renderAddress({ base: { ...settingsDefaultScenario(), archived: true } });
    expect(input().disabled).toBe(true);
    expect(
      within(form()).queryByRole("button", { name: copy.save }),
    ).toBeNull();
  });
});
