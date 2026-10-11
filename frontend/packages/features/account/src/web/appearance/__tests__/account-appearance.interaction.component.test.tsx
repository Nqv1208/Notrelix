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
  it("FUI[account.appearance:render] offers labelled radio groups for mode, accent and language", () => {
    for (const locale of LOCALES) {
      const copy = appearanceCopyFixture(locale);
      const { view } = renderAppearance(appearanceDefaultScenario(locale));

      expect(
        screen.getByRole("heading", { level: 1, name: copy.title }),
      ).toBeTruthy();
      const mode = screen.getByRole("radiogroup", { name: copy.mode.legend });
      expect(within(mode).getAllByRole("radio")).toHaveLength(4);
      const accent = screen.getByRole("radiogroup", {
        name: copy.accent.legend,
      });
      expect(within(accent).getAllByRole("radio")).toHaveLength(5);
      const language = screen.getByRole("radiogroup", {
        name: copy.language.legend,
      });
      expect(within(language).getAllByRole("radio")).toHaveLength(2);

      // Each radio is named by its label and described by its explanation.
      const light = within(mode).getByRole("radio", {
        name: copy.mode.light.label,
      });
      expect(light.getAttribute("aria-checked")).toBe("true");
      expect(
        within(mode)
          .getByRole("radio", { name: copy.mode.dark.label })
          .getAttribute("aria-checked"),
      ).toBe("false");
      expect(light.getAttribute("aria-describedby")).toBeTruthy();
      view.unmount();
    }
  });

  it("FUI[account.appearance:mode] reports the chosen mode through the callback", () => {
    for (const locale of LOCALES) {
      const copy = appearanceCopyFixture(locale);
      const { props, view } = renderAppearance(
        appearanceDefaultScenario(locale),
      );
      fireEvent.click(
        screen.getByRole("radio", { name: copy.mode.highContrast.label }),
      );
      expect(props.onModeChange).toHaveBeenCalledWith("high-contrast");
      fireEvent.click(
        screen.getByRole("radio", { name: copy.mode.dark.label }),
      );
      expect(props.onModeChange).toHaveBeenLastCalledWith("dark");
      view.unmount();
    }
  });

  it("FUI[account.appearance:accent] shows each accent seed colour through its token variable", () => {
    for (const locale of LOCALES) {
      const copy = appearanceCopyFixture(locale);
      const { props, view } = renderAppearance(
        appearanceDefaultScenario(locale),
      );
      const accent = screen.getByRole("radiogroup", {
        name: copy.accent.legend,
      });
      for (const id of ["blue", "coral", "ember", "sage", "amber"] as const) {
        const swatch = accent.querySelector(`[data-accent-swatch="${id}"]`);
        const style = (swatch as HTMLElement | null)?.getAttribute("style");
        expect(style).toContain(`var(--accent-swatch-${id})`);
        expect(style).not.toMatch(/#|rgb|hsl|oklch/);
      }
      fireEvent.click(
        within(accent).getByRole("radio", { name: copy.accent.coral.label }),
      );
      expect(props.onAccentChange).toHaveBeenCalledWith("coral");
      view.unmount();
    }
  });

  it("FUI[account.appearance:language] switches the language and marks each option's own language", () => {
    for (const locale of LOCALES) {
      const copy = appearanceCopyFixture(locale);
      const { props, view } = renderAppearance(
        appearanceDefaultScenario(locale),
      );
      const language = screen.getByRole("radiogroup", {
        name: copy.language.legend,
      });
      const current = locale;
      const other = locale === "vi" ? "en" : "vi";
      expect(
        within(language)
          .getByRole("radio", { name: copy.language[current] })
          .getAttribute("aria-checked"),
      ).toBe("true");
      const option = within(language).getByRole("radio", {
        name: copy.language[other],
      });
      expect(option.closest("[lang]")?.getAttribute("lang")).toBe(other);
      fireEvent.click(option);
      expect(props.onLocaleChange).toHaveBeenCalledWith(other);
      view.unmount();
    }
  });

  it("FUI[account.appearance:sidebar] is a real switch named by its label that saves the preference", () => {
    for (const locale of LOCALES) {
      const copy = appearanceCopyFixture(locale);
      const { props, view } = renderAppearance(
        appearanceDefaultScenario(locale),
      );
      const toggle = screen.getByRole("switch", { name: copy.sidebar.label });
      // A native button: Space and Enter activate it (proved in the mock e2e).
      expect(toggle.tagName).toBe("BUTTON");
      expect(toggle.getAttribute("aria-checked")).toBe("false");
      expect(screen.getByText(copy.sidebar.help)).toBeTruthy();
      fireEvent.click(toggle);
      expect(props.onSidebarCollapsedChange).toHaveBeenCalledWith(true);
      view.unmount();
    }
  });

  it("FUI[account.appearance:loading] shows a named loading state for the sidebar preference only", () => {
    for (const locale of LOCALES) {
      const copy = appearanceCopyFixture(locale);
      const { view } = renderAppearance(appearanceLoadingScenario(locale));
      expect(
        screen.getByRole("status", { name: copy.sidebar.loading }),
      ).toBeTruthy();
      expect(screen.queryByRole("switch")).toBeNull();
      // The device appearance does not wait for the account.
      expect(
        screen.getByRole("radiogroup", { name: copy.mode.legend }),
      ).toBeTruthy();
      view.unmount();
    }
  });

  it("FUI[account.appearance:error] offers a retry when the sidebar preference cannot load", () => {
    for (const locale of LOCALES) {
      const copy = appearanceCopyFixture(locale);
      const { props, view } = renderAppearance(appearanceErrorScenario(locale));
      const alert = screen.getByRole("alert");
      expect(alert.textContent).toContain(copy.sidebar.loadError);
      fireEvent.click(
        within(alert).getByRole("button", { name: copy.sidebar.retry }),
      );
      expect(props.onRetryPreferences).toHaveBeenCalledTimes(1);
      view.unmount();
    }
  });

  it("FUI[account.appearance:saving] disables the switch while the preference is saved", () => {
    for (const locale of LOCALES) {
      const copy = appearanceCopyFixture(locale);
      const { view } = renderAppearance(appearanceSavingScenario(locale));
      const toggle = screen.getByRole("switch", {
        name: copy.sidebar.label,
      }) as HTMLButtonElement;
      expect(toggle.disabled).toBe(true);
      expect(screen.getByRole("status").textContent).toContain(
        copy.sidebar.saving,
      );
      view.unmount();
    }
  });

  it("FUI[account.appearance:saved] confirms the saved preference and announces a failed one", () => {
    for (const locale of LOCALES) {
      const copy = appearanceCopyFixture(locale);
      const saved = renderAppearance(appearanceSavedScenario(locale));
      expect(
        screen
          .getByRole("switch", { name: copy.sidebar.label })
          .getAttribute("aria-checked"),
      ).toBe("true");
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

  it("FUI[account.appearance:edge] keeps the high-contrast mode and amber accent selected in Vietnamese", () => {
    const copy = appearanceCopyFixture("vi");
    const { view } = renderAppearance(appearanceEdgeDataScenario());
    expect(
      screen
        .getByRole("radio", { name: copy.mode.highContrast.label })
        .getAttribute("aria-checked"),
    ).toBe("true");
    expect(
      screen
        .getByRole("radio", { name: copy.accent.amber.label })
        .getAttribute("aria-checked"),
    ).toBe("true");
    view.unmount();
  });

  it("FUI[account.appearance:copy] leaves no English literal in the Vietnamese page", () => {
    const { view } = renderAppearance(appearanceSaveErrorScenario("vi"));
    const text = document.body.textContent ?? "";
    for (const literal of [
      en.title,
      en.description,
      en.mode.legend,
      en.mode.light.label,
      en.mode.highContrast.description,
      en.accent.legend,
      en.accent.coral.label,
      en.sidebar.legend,
      en.sidebar.label,
      en.sidebar.saveError,
    ]) {
      expect(text).not.toContain(literal);
    }
    view.unmount();
  });
});
