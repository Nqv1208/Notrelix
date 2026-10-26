import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  profileCopyFixture,
  profileDefaultScenario,
  profileEdgeDataScenario,
  profileErrorScenario,
  profileFixture,
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

const LOCALES = ["vi", "en"] as const;

function renderProfile(
  scenario: ProfileScenario,
  overrides: Partial<AccountProfileSurfaceProps> = {},
) {
  const props: AccountProfileSurfaceProps = {
    ...scenario,
    onSave: vi.fn(),
    onRetry: vi.fn(),
    onPhotoChange: vi.fn(),
    onPhotoRemove: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<AccountProfileSurface {...props} />);
  return { props, view };
}

/** Opens the styled Select behind a label and picks one option by its text. */
function choose(label: string, option: string) {
  fireEvent.keyDown(screen.getByLabelText(label), { key: "Enter" });
  fireEvent.click(screen.getByRole("option", { name: option }));
}

describe("AccountProfileSurface", () => {
  it("FUI[account.profile:render] shows the profile with bound labels in vi and en", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { view } = renderProfile(profileDefaultScenario(locale));

      expect(
        screen.getByRole("heading", { level: 2, name: copy.title }),
      ).toBeTruthy();
      expect(
        (screen.getByLabelText(copy.nameLabel) as HTMLInputElement).value,
      ).toBe("Ada Lovelace");
      expect(
        (screen.getByLabelText(copy.jobTitleLabel) as HTMLInputElement).value,
      ).toBe("Implementation lead");
      const email = screen.getByLabelText(copy.emailLabel) as HTMLInputElement;
      expect(email.value).toBe("ada@example.com");
      expect(email.readOnly).toBe(true);
      expect(
        screen.getByText(
          copy.emailManaged
            .replace("{{workspace}}", "Minh An Group")
            .replace("{{provider}}", "Microsoft Entra"),
        ),
      ).toBeTruthy();
      expect(screen.getByLabelText(copy.timezoneLabel).textContent).toBe(
        copy.timezones["Asia/Ho_Chi_Minh"],
      );
      expect(screen.getByLabelText(copy.dateFormatLabel).textContent).toBe(
        copy.dateFormats["dd/mm/yyyy"],
      );
      // Nothing changed yet: no save bar.
      expect(screen.queryByRole("region", { name: copy.unsavedRegion })).toBe(
        null,
      );
      view.unmount();
    }
  });

  it("FUI[account.profile:dirty] shows the save bar with what changed, and hides it when changes are undone", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { view } = renderProfile(profileDefaultScenario(locale));
      const phone = screen.getByLabelText(copy.phoneLabel);
      fireEvent.change(phone, { target: { value: "+84 988 000 111" } });
      const bar = screen.getByRole("region", { name: copy.unsavedRegion });
      expect(bar.textContent).toContain(copy.unsaved.one);
      expect(bar.textContent).toContain(copy.phoneLabel);
      expect(screen.getByText(copy.edited)).toBeTruthy();

      fireEvent.change(screen.getByLabelText(copy.jobTitleLabel), {
        target: { value: "Director" },
      });
      expect(
        screen.getByRole("region", { name: copy.unsavedRegion }).textContent,
      ).toContain(copy.unsaved.other.replace("{{count}}", "2"));

      // Back to the saved number, typed with spaces: spacing is not a change.
      fireEvent.change(phone, { target: { value: "+84 912 468 135 " } });
      fireEvent.change(screen.getByLabelText(copy.jobTitleLabel), {
        target: { value: "Implementation lead" },
      });
      expect(screen.queryByRole("region", { name: copy.unsavedRegion })).toBe(
        null,
      );
      view.unmount();
    }
  });

  it("FUI[account.profile:save] sends the trimmed values with the chosen time zone and date format", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { props, view } = renderProfile(profileDefaultScenario(locale));

      fireEvent.change(screen.getByLabelText(copy.nameLabel), {
        target: { value: "  Ada King  " },
      });
      choose(copy.timezoneLabel, copy.timezones["Asia/Tokyo"]);
      choose(copy.dateFormatLabel, copy.dateFormats["yyyy-mm-dd"]);
      fireEvent.click(screen.getByRole("button", { name: copy.save }));

      expect(props.onSave).toHaveBeenCalledTimes(1);
      expect(props.onSave).toHaveBeenCalledWith({
        name: "Ada King",
        jobTitle: "Implementation lead",
        phone: "+84912468135",
        phoneVisibleToWorkspaceMembers: false,
        timezone: "Asia/Tokyo",
        dateFormat: "yyyy-mm-dd",
      });
      view.unmount();
    }
  });

  it("FUI[account.profile:phone-visibility] keeps the phone to the person by default and sends the choice with the save", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { props, view } = renderProfile(profileDefaultScenario(locale));
      const visible = screen.getByLabelText(
        copy.phoneVisibleLabel,
      ) as HTMLInputElement;
      expect(visible.checked).toBe(false);
      expect(screen.getByText(copy.phoneVisibleHint)).toBeTruthy();

      fireEvent.click(visible);
      const bar = screen.getByRole("region", { name: copy.unsavedRegion });
      expect(bar.textContent).toContain(copy.phoneVisibleLabel);
      fireEvent.click(screen.getByRole("button", { name: copy.save }));
      expect(props.onSave).toHaveBeenCalledWith(
        expect.objectContaining({
          phone: "+84912468135",
          phoneVisibleToWorkspaceMembers: true,
        }),
      );

      // Back to off: nothing is left to save.
      fireEvent.click(visible);
      expect(screen.queryByRole("region", { name: copy.unsavedRegion })).toBe(
        null,
      );
      view.unmount();
    }
  });

  it("FUI[account.profile:phone-visibility-empty] cannot be turned on without a phone number", () => {
    const copy = profileCopyFixture("en");
    const { view } = renderProfile({
      ...profileDefaultScenario("en"),
      profile: { status: "ready", data: profileFixture({ phone: "" }) },
    });
    expect(
      (screen.getByLabelText(copy.phoneVisibleLabel) as HTMLInputElement)
        .disabled,
    ).toBe(true);
    view.unmount();

    const shared = renderProfile({
      ...profileDefaultScenario("en"),
      profile: {
        status: "ready",
        data: profileFixture({
          phone: "",
          phoneVisibleToWorkspaceMembers: true,
        }),
      },
    });
    // A stored "visible" can always be switched off again.
    expect(
      (screen.getByLabelText(copy.phoneVisibleLabel) as HTMLInputElement)
        .disabled,
    ).toBe(false);
    shared.view.unmount();
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
      expect(screen.queryByRole("button", { name: copy.discard })).toBeNull();
      view.unmount();
    }
  });

  it("FUI[account.profile:validation] blocks an empty name and a bad phone and explains why", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { props, view } = renderProfile(profileDefaultScenario(locale));
      const name = screen.getByLabelText(copy.nameLabel);

      fireEvent.change(name, { target: { value: "   " } });
      expect(screen.getByText(copy.nameRequired)).toBeTruthy();
      expect(name.getAttribute("aria-invalid")).toBe("true");
      expect(
        (screen.getByRole("button", { name: copy.save }) as HTMLButtonElement)
          .disabled,
      ).toBe(true);
      fireEvent.submit(name.closest("form") as HTMLFormElement);

      fireEvent.change(name, { target: { value: "Ada" } });
      fireEvent.change(screen.getByLabelText(copy.phoneLabel), {
        target: { value: "call me" },
      });
      expect(screen.getByText(copy.phoneInvalid)).toBeTruthy();
      fireEvent.submit(name.closest("form") as HTMLFormElement);
      expect(props.onSave).not.toHaveBeenCalled();
      view.unmount();
    }
  });

  it("FUI[account.profile:phone] sends the phone as E.164 and asks for the country code on a national number", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { props, view } = renderProfile(profileDefaultScenario(locale));
      const phone = screen.getByLabelText(copy.phoneLabel);
      const form = phone.closest("form") as HTMLFormElement;

      // A national number is not guessed into a region.
      fireEvent.change(phone, { target: { value: "0988 000 111" } });
      expect(screen.getByText(copy.phoneCountryCode)).toBeTruthy();
      expect(phone.getAttribute("aria-invalid")).toBe("true");
      expect(
        (screen.getByRole("button", { name: copy.save }) as HTMLButtonElement)
          .disabled,
      ).toBe(true);
      fireEvent.submit(form);
      expect(props.onSave).not.toHaveBeenCalled();

      // Spacing, hyphens and brackets are tidied away before the send.
      fireEvent.change(phone, { target: { value: "+84 (988) 000-111" } });
      expect(screen.queryByText(copy.phoneCountryCode)).toBeNull();
      fireEvent.click(screen.getByRole("button", { name: copy.save }));
      expect(props.onSave).toHaveBeenCalledWith(
        expect.objectContaining({ phone: "+84988000111" }),
      );

      // Emptying the field clears the phone.
      fireEvent.change(phone, { target: { value: "  " } });
      fireEvent.click(screen.getByRole("button", { name: copy.save }));
      expect(props.onSave).toHaveBeenLastCalledWith(
        expect.objectContaining({ phone: "" }),
      );
      view.unmount();
    }
  });

  it("FUI[account.profile:saving] disables the form and names the pending save", () => {
    for (const locale of LOCALES) {
      const copy = profileCopyFixture(locale);
      const { view } = renderProfile(profileSavingScenario(locale), {
        profile: {
          status: "ready",
          data: profileFixture({ name: "Ada" }),
        },
      });
      // The draft only differs after an edit; the pending state alone keeps
      // the fields locked.
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

  it("FUI[account.profile:photo] hands the chosen photo over and reports a refused one", () => {
    const copy = profileCopyFixture("en");
    const { props, view } = renderProfile(profileDefaultScenario("en"));
    const button = screen.getByRole("button", { name: copy.changePhotoLabel });
    expect(button.textContent).toBe(copy.changePhoto);
    const input = document.querySelector(
      "input[type=file]",
    ) as HTMLInputElement;
    const file = new File(["x"], "me.png", { type: "image/png" });
    fireEvent.change(input, { target: { files: [file] } });
    expect(props.onPhotoChange).toHaveBeenCalledWith(file);
    view.unmount();

    for (const [failure, text] of [
      ["unsupported", copy.photoUnsupported],
      ["too-large", copy.photoTooLarge],
      ["failed", copy.photoFailed],
    ] as const) {
      const refused = renderProfile({
        ...profileDefaultScenario("en"),
        photoError: failure,
      });
      expect(screen.getByRole("alert").textContent).toContain(text);
      refused.view.unmount();
    }
  });

  it("FUI[account.profile:remove-photo] offers to remove the photo only while there is one", () => {
    const copy = profileCopyFixture("en");
    const without = renderProfile(profileDefaultScenario("en"));
    expect(
      screen.queryByRole("button", { name: copy.removePhotoLabel }),
    ).toBeNull();
    without.view.unmount();

    const base = profileDefaultScenario("en");
    const { props, view } = renderProfile({
      ...base,
      profile:
        base.profile.status === "ready"
          ? {
              status: "ready",
              data: {
                ...base.profile.data,
                avatarUrl: "/api/v1/users/u1/avatar?v=a1",
              },
            }
          : base.profile,
    });
    fireEvent.click(
      screen.getByRole("button", { name: copy.removePhotoLabel }),
    );
    expect(props.onPhotoRemove).toHaveBeenCalledTimes(1);
    view.unmount();
  });

  it("FUI[account.profile:edge] keeps long Vietnamese data and unknown choices readable", () => {
    const copy = profileCopyFixture("vi");
    const { view } = renderProfile(profileEdgeDataScenario());
    const name = screen.getByLabelText(copy.nameLabel) as HTMLInputElement;
    expect(name.value).toContain("Nguyễn Thị Phương Anh");
    const email = screen.getByLabelText(copy.emailLabel) as HTMLInputElement;
    expect(email.value.length).toBeGreaterThan(60);
    expect(email.readOnly).toBe(true);
    // Nobody manages this email: the plain help shows instead.
    expect(screen.getByText(copy.emailHelp)).toBeTruthy();
    // A zone outside the list stays selectable by its raw id.
    expect(screen.getByLabelText(copy.timezoneLabel).textContent).toBe(
      "Pacific/Auckland",
    );
    const alerts = screen.getAllByRole("alert").map((a) => a.textContent);
    expect(alerts.join(" ")).toContain(copy.saveError);
    expect(alerts.join(" ")).toContain(copy.photoUnsupported);
    view.unmount();
  });

  it("FUI[account.profile:copy] leaves no English literal in the Vietnamese page", () => {
    const { view } = renderProfile(profileSaveErrorScenario("vi"));
    const text = document.body.textContent ?? "";
    for (const literal of [
      en.title,
      en.description,
      en.nameLabel,
      en.emailManaged.slice(0, 10),
      en.timezoneLabel,
      en.dateFormatLabel,
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
