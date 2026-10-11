import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  profileCopyFixture,
  profileDefaultScenario,
  profileEdgeDataScenario,
  profileErrorScenario,
  profileFixture,
  profileFormattersFixture,
  profileLoadingScenario,
  profileSaveErrorScenario,
  profileSavedScenario,
  profileSavingScenario,
  type ProfileScenario,
} from "../../../verification/account-profile-fixtures";
import { en } from "../account-profile.copy";
import {
  AccountProfileSurface,
  type AccountProfileSurfaceProps,
} from "../account-profile-surface";

beforeAll(() => {
  // Radix Select measures and scrolls items; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const LOCALES = ["vi", "en"] as const;

function renderProfile(
  scenario: ProfileScenario,
  overrides: Partial<AccountProfileSurfaceProps> = {},
) {
  const props: AccountProfileSurfaceProps = {
    ...scenario,
    onSave: vi.fn(),
    onRetry: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<AccountProfileSurface {...props} />);
  return { props, view };
}

function saveButton(copy: ReturnType<typeof profileCopyFixture>) {
  return screen.getByRole("button", {
    name: new RegExp(`^(${copy.save}|${copy.saving})$`),
  }) as HTMLButtonElement;
}

describe("AccountProfileSurface", () => {
  it("FUI[account.profile:render] shows the profile with bound labels in vi and en", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { view } = renderProfile(profileDefaultScenario(locale));

      expect(
        screen.getByRole("heading", { level: 1, name: copy.title }),
      ).toBeTruthy();
      expect(
        (screen.getByLabelText(copy.nameLabel) as HTMLInputElement).value,
      ).toBe("Ada Lovelace");
      const email = screen.getByLabelText(copy.emailLabel) as HTMLInputElement;
      expect(email.value).toBe("ada@example.com");
      expect(email.disabled).toBe(true);
      expect(screen.getByText(copy.emailHelp)).toBeTruthy();
      expect(
        screen.getByRole("combobox", { name: copy.timezoneLabel }).textContent,
      ).toContain(copy.timezones["Asia/Ho_Chi_Minh"]);
      expect(
        screen.getByRole("combobox", { name: copy.localeLabel }).textContent,
      ).toContain(copy.locales.en);
      // The join date goes through the injected locale formatters.
      const since = copy.memberSince.replace(
        "{{date}}",
        profileFormattersFixture(locale).date(
          profileFixture().createdAt as string,
        ),
      );
      expect(screen.getByText(since)).toBeTruthy();
      view.unmount();
    }
  });

  it("FUI[account.profile:dirty] keeps Save disabled until a field changes", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { view } = renderProfile(profileDefaultScenario(locale));
      const save = saveButton(copy);
      expect(save.disabled).toBe(true);

      const name = screen.getByLabelText(copy.nameLabel);
      fireEvent.change(name, { target: { value: "Ada King" } });
      expect(save.disabled).toBe(false);

      // Back to the saved value (trailing space ignored): nothing to save.
      fireEvent.change(name, { target: { value: "Ada Lovelace " } });
      expect(save.disabled).toBe(true);
      view.unmount();
    }
  });

  it("FUI[account.profile:save] sends the trimmed name with the chosen time zone and language", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { props, view } = renderProfile(profileDefaultScenario(locale));

      fireEvent.change(screen.getByLabelText(copy.nameLabel), {
        target: { value: "  Ada King  " },
      });
      fireEvent.keyDown(
        screen.getByRole("combobox", { name: copy.timezoneLabel }),
        { key: "Enter" },
      );
      fireEvent.click(
        screen.getByRole("option", { name: copy.timezones["Asia/Tokyo"] }),
      );
      fireEvent.keyDown(
        screen.getByRole("combobox", { name: copy.localeLabel }),
        { key: "Enter" },
      );
      fireEvent.click(screen.getByRole("option", { name: copy.locales.vi }));
      fireEvent.click(saveButton(copy));

      expect(props.onSave).toHaveBeenCalledTimes(1);
      expect(props.onSave).toHaveBeenCalledWith({
        name: "Ada King",
        timezone: "Asia/Tokyo",
        locale: "vi",
      });
      view.unmount();
    }
  });

  it("FUI[account.profile:discard] restores the saved values", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { view } = renderProfile(profileDefaultScenario(locale));
      fireEvent.change(screen.getByLabelText(copy.nameLabel), {
        target: { value: "Someone else" },
      });
      fireEvent.click(screen.getByRole("button", { name: copy.discard }));
      expect(
        (screen.getByLabelText(copy.nameLabel) as HTMLInputElement).value,
      ).toBe("Ada Lovelace");
      expect(saveButton(copy).disabled).toBe(true);
      expect(screen.queryByRole("button", { name: copy.discard })).toBeNull();
      view.unmount();
    }
  });

  it("FUI[account.profile:validation] blocks an empty name and explains why", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { props, view } = renderProfile(profileDefaultScenario(locale));
      const name = screen.getByLabelText(copy.nameLabel);

      fireEvent.change(name, { target: { value: "   " } });
      expect(screen.getByText(copy.nameRequired)).toBeTruthy();
      expect(name.getAttribute("aria-invalid")).toBe("true");
      expect(saveButton(copy).disabled).toBe(true);

      fireEvent.submit(name.closest("form") as HTMLFormElement);
      expect(props.onSave).not.toHaveBeenCalled();
      view.unmount();
    }
  });

  it("FUI[account.profile:saving] disables the form and names the pending save", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { view } = renderProfile(profileSavingScenario(locale));
      const save = screen.getByRole("button", {
        name: copy.saving,
      }) as HTMLButtonElement;
      expect(save.disabled).toBe(true);
      expect(
        (screen.getByLabelText(copy.nameLabel) as HTMLInputElement).disabled,
      ).toBe(true);
      view.unmount();
    }
  });

  it("FUI[account.profile:saved] confirms the save and the failure is announced", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const saved = renderProfile(profileSavedScenario(locale));
      expect(screen.getByRole("status").textContent).toContain(copy.saved);
      saved.view.unmount();

      const failed = renderProfile(profileSaveErrorScenario(locale));
      expect(screen.getByRole("alert").textContent).toContain(copy.saveError);
      failed.view.unmount();
    }
  });

  it("FUI[account.profile:loading] shows a named loading state without the form", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { view } = renderProfile(profileLoadingScenario(locale));
      expect(screen.getByRole("status", { name: copy.loading })).toBeTruthy();
      expect(screen.queryByLabelText(copy.nameLabel)).toBeNull();
      view.unmount();
    }
  });

  it("FUI[account.profile:error] offers a retry when the profile cannot load", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { props, view } = renderProfile(profileErrorScenario(locale));
      const alert = screen.getByRole("alert");
      expect(alert.textContent).toContain(copy.loadError);
      fireEvent.click(within(alert).getByRole("button", { name: copy.retry }));
      expect(props.onRetry).toHaveBeenCalledTimes(1);
      view.unmount();
    }
  });

  it("FUI[account.profile:edge] keeps long Vietnamese data and unknown choices readable", () => {
    const copy = profileCopyFixture("vi");
    const { view } = renderProfile(profileEdgeDataScenario());
    const name = screen.getByLabelText(copy.nameLabel) as HTMLInputElement;
    expect(name.value).toContain("Nguyễn Thị Phương Anh");
    // A long address stays in its read-only field (it scrolls inside it).
    const email = screen.getByLabelText(copy.emailLabel) as HTMLInputElement;
    expect(email.value.length).toBeGreaterThan(60);
    expect(email.readOnly).toBe(true);
    // A zone/language outside the lists stays selectable by its raw id.
    expect(
      screen.getByRole("combobox", { name: copy.timezoneLabel }).textContent,
    ).toContain("Pacific/Auckland");
    expect(
      screen.getByRole("combobox", { name: copy.localeLabel }).textContent,
    ).toContain("fr");
    expect(screen.getByRole("alert").textContent).toContain(copy.saveError);
    view.unmount();
  });

  it("FUI[account.profile:copy] leaves no English literal in the Vietnamese page", () => {
    const { view } = renderProfile(profileSaveErrorScenario("vi"));
    const text = document.body.textContent ?? "";
    for (const literal of [
      en.title,
      en.description,
      en.nameLabel,
      en.emailHelp,
      en.timezoneLabel,
      en.localeLabel,
      en.saveError,
    ]) {
      expect(text).not.toContain(literal);
    }
    expect((screen.getByLabelText("Tên") as HTMLInputElement).placeholder).toBe(
      "Tên của bạn",
    );
    view.unmount();
  });
});
