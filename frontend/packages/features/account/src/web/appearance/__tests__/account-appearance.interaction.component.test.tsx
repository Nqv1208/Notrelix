import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  appearanceCopyFixture,
  appearanceDefaultScenario,
  appearanceEdgeDataScenario,
  appearanceErrorScenario,
  appearanceLoadingScenario,
  appearanceSaveErrorScenario,
  appearanceSavedScenario,
  appearanceSavingScenario,
  type AppearanceScenario,
} from "../../../verification/account-appearance-fixtures";
import { en } from "../account-appearance.copy";
import {
  AccountAppearanceSurface,
  type AccountAppearanceSurfaceProps,
} from "../account-appearance-surface";

const LOCALES = ["vi", "en"] as const;

function renderAppearance(
  scenario: AppearanceScenario,
  overrides: Partial<AccountAppearanceSurfaceProps> = {},
) {
  const props: AccountAppearanceSurfaceProps = {
    ...scenario,
    onModeChange: vi.fn(),
    onAccentChange: vi.fn(),
    onLocaleChange: vi.fn(),
    onSidebarCollapsedChange: vi.fn(),
    onRetryPreferences: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<AccountAppearanceSurface {...props} />);
  return { props, view };
}

describe("AccountAppearanceSurface", () => {
  it("FUI[account.appearance:render] shows the four groups with the current choices checked", () => {
    for (const locale of LOCALES) {
      const copy = appearanceCopyFixture(locale);
      const { view } = renderAppearance(appearanceDefaultScenario(locale));
      expect(
        screen.getByRole("heading", { level: 2, name: copy.title }),
      ).toBeTruthy();
      const mode = screen.getByRole("radiogroup", { name: copy.mode.legend });
      expect(
        (
          within(mode).getByRole("radio", {
            name: new RegExp(copy.mode.light.label),
          }) as HTMLInputElement
        ).checked,
      ).toBe(true);
      expect(within(mode).getAllByRole("radio")).toHaveLength(4);
      expect(
        within(
          screen.getByRole("radiogroup", { name: copy.accent.legend }),
        ).getAllByRole("radio"),
      ).toHaveLength(5);
      expect(
        (
          screen.getByRole("radio", {
            name: copy.language[locale],
          }) as HTMLInputElement
        ).checked,
      ).toBe(true);
      view.unmount();
    }
  });

  it("FUI[account.appearance:mode] reports the chosen mode", () => {
    const copy = appearanceCopyFixture("en");
    const { props, view } = renderAppearance(appearanceDefaultScenario("en"));
    fireEvent.click(screen.getByRole("radio", { name: copy.mode.dark.label }));
    expect(props.onModeChange).toHaveBeenCalledWith("dark");
    fireEvent.click(
      screen.getByRole("radio", { name: copy.mode.highContrast.label }),
    );
    expect(props.onModeChange).toHaveBeenCalledWith("high-contrast");
    view.unmount();
  });

  it("FUI[account.appearance:accent] reports the chosen accent and the preview follows its name", () => {
    const copy = appearanceCopyFixture("en");
    const { props, view } = renderAppearance(appearanceDefaultScenario("en"));
    fireEvent.click(
      screen.getByRole("radio", { name: copy.accent.sage.label }),
    );
    expect(props.onAccentChange).toHaveBeenCalledWith("sage");
    expect(
      screen.getByText(`${copy.accent.blue.label} · ${copy.mode.light.label}`),
    ).toBeTruthy();
    view.unmount();
  });

  it("FUI[account.appearance:language] reports the chosen language", () => {
    const { props, view } = renderAppearance(appearanceDefaultScenario("vi"));
    fireEvent.click(screen.getByRole("radio", { name: "English" }));
    expect(props.onLocaleChange).toHaveBeenCalledWith("en");
    view.unmount();
  });

  it("FUI[account.appearance:sidebar] toggles the sidebar default with the switch", () => {
    for (const locale of LOCALES) {
      const copy = appearanceCopyFixture(locale);
      const { props, view } = renderAppearance(
        appearanceDefaultScenario(locale),
      );
      const toggle = screen.getByRole("switch", { name: copy.sidebar.label });
      expect(toggle.getAttribute("aria-checked")).toBe("false");
      fireEvent.click(toggle);
      expect(props.onSidebarCollapsedChange).toHaveBeenCalledWith(true);
      view.unmount();
    }
  });

  it("FUI[account.appearance:preview] draws the preview and the company default note", () => {
    const copy = appearanceCopyFixture("vi");
    const { view } = renderAppearance(appearanceDefaultScenario("vi"));
    expect(
      screen.getByRole("complementary", { name: copy.preview.title }),
    ).toBeTruthy();
    expect(screen.getByText(/Minh An Group:/)).toBeTruthy();
    view.unmount();
  });

  it("FUI[account.appearance:loading] shows a named loading state for the sidebar choice only", () => {
    const copy = appearanceCopyFixture("en");
    const { view } = renderAppearance(appearanceLoadingScenario("en"));
    expect(
      screen.getByRole("status", { name: copy.sidebar.loading }),
    ).toBeTruthy();
    expect(screen.getAllByRole("radio").length).toBeGreaterThan(0);
    view.unmount();
  });

  it("FUI[account.appearance:error] offers a retry when the preference cannot load", () => {
    const copy = appearanceCopyFixture("en");
    const { props, view } = renderAppearance(appearanceErrorScenario("en"));
    const alert = screen.getByRole("alert");
    expect(alert.textContent).toContain(copy.sidebar.loadError);
    fireEvent.click(
      within(alert).getByRole("button", { name: copy.sidebar.retry }),
    );
    expect(props.onRetryPreferences).toHaveBeenCalledTimes(1);
    view.unmount();
  });

  it("FUI[account.appearance:saving] locks the switch while the preference is written", () => {
    const copy = appearanceCopyFixture("en");
    const { view } = renderAppearance(appearanceSavingScenario("en"));
    expect(
      (
        screen.getByRole("switch", {
          name: copy.sidebar.label,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    view.unmount();
  });

  it("FUI[account.appearance:saved] shows the toast and announces a failed save", () => {
    for (const locale of LOCALES) {
      const copy = appearanceCopyFixture(locale);
      const saved = renderAppearance(appearanceSavedScenario(locale));
      expect(screen.getByRole("status").textContent).toContain(
        copy.sidebar.saved,
      );
      saved.view.unmount();
      const failed = renderAppearance(appearanceSaveErrorScenario(locale));
      expect(screen.getByRole("alert").textContent).toContain(
        copy.sidebar.saveError,
      );
      failed.view.unmount();
    }
  });

  it("FUI[account.appearance:edge] keeps high contrast, amber and a long workspace name readable", () => {
    const copy = appearanceCopyFixture("vi");
    const { view } = renderAppearance(appearanceEdgeDataScenario());
    expect(
      (
        screen.getByRole("radio", {
          name: new RegExp(copy.mode.highContrast.label),
        }) as HTMLInputElement
      ).checked,
    ).toBe(true);
    expect(
      (
        screen.getByRole("radio", {
          name: new RegExp(copy.accent.amber.label),
        }) as HTMLInputElement
      ).checked,
    ).toBe(true);
    expect(document.body.textContent).toContain("Minh An Việt Nam");
    view.unmount();
  });

  it("FUI[account.appearance:copy] leaves no English literal in the Vietnamese page", () => {
    const { view } = renderAppearance(appearanceSavedScenario("vi"));
    const text = document.body.textContent ?? "";
    for (const literal of [
      en.title,
      en.mode.legend,
      en.accent.legend,
      en.sidebar.label,
      en.preview.title,
      en.preview.addItem,
      en.sidebar.saved,
    ]) {
      expect(text).not.toContain(literal);
    }
    view.unmount();
  });
});
