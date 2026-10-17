import { act, render, renderHook, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { defineCatalog } from "../catalog";
import { I18nProvider, useFormatters, useLocale, useT } from "../i18n-provider";
import { LOCALE_STORAGE_KEY, type LocaleStorage } from "../locale";

const demo = defineCatalog("demo", {
  vi: {
    greeting: "Xin chào, {{name}}",
    items_other: "{{count}} mục",
  },
  en: {
    greeting: "Hello, {{name}}",
    items_one: "{{count}} item",
    items_other: "{{count}} items",
  },
});

function memoryStorage(initial: Record<string, string> = {}) {
  const data = new Map(Object.entries(initial));
  const storage: LocaleStorage = {
    getItem: (key) => data.get(key) ?? null,
    setItem: (key, value) => void data.set(key, value),
  };
  return { storage, data };
}

function Probe() {
  const { t } = useT(demo);
  const { locale, setLocale } = useLocale();
  const format = useFormatters();
  return (
    <div>
      <p data-testid="greeting">{t("greeting", { name: "Lan" })}</p>
      <p data-testid="one">{t("items", { count: 1 })}</p>
      <p data-testid="many">{t("items", { count: 5 })}</p>
      <p data-testid="number">{format.number(1234.5)}</p>
      <button
        type="button"
        onClick={() => setLocale(locale === "vi" ? "en" : "vi")}
      >
        switch
      </button>
    </div>
  );
}

describe("I18nProvider", () => {
  it("renders Vietnamese by default, with pluralization and Intl formatting", () => {
    render(
      <I18nProvider catalogs={[demo]}>
        <Probe />
      </I18nProvider>,
    );
    expect(screen.getByTestId("greeting").textContent).toContain(
      "Xin chào, Lan",
    );
    expect(screen.getByTestId("one").textContent).toContain("1 mục");
    expect(screen.getByTestId("many").textContent).toContain("5 mục");
    expect(screen.getByTestId("number").textContent).toContain("1.234,5");
  });

  it("switches locale, re-renders, persists the choice and notifies the host", () => {
    const { storage, data } = memoryStorage();
    const onLocaleChange = vi.fn();
    render(
      <I18nProvider
        catalogs={[demo]}
        storage={storage}
        onLocaleChange={onLocaleChange}
      >
        <Probe />
      </I18nProvider>,
    );
    expect(onLocaleChange).toHaveBeenLastCalledWith("vi");

    act(() => screen.getByRole("button", { name: "switch" }).click());

    expect(screen.getByTestId("greeting").textContent).toContain("Hello, Lan");
    expect(screen.getByTestId("one").textContent).toContain("1 item");
    expect(screen.getByTestId("many").textContent).toContain("5 items");
    expect(screen.getByTestId("number").textContent).toContain("1,234.5");
    expect(data.get(LOCALE_STORAGE_KEY)).toBe("en");
    expect(onLocaleChange).toHaveBeenLastCalledWith("en");
  });

  it("detects stored preference before browser languages", () => {
    const { storage } = memoryStorage({ [LOCALE_STORAGE_KEY]: "en" });
    const wrapper = ({ children }: { children: ReactNode }) => (
      <I18nProvider
        catalogs={[demo]}
        storage={storage}
        browserLocales={["vi-VN"]}
      >
        {children}
      </I18nProvider>
    );
    expect(
      renderHook(() => useLocale(), { wrapper }).result.current.locale,
    ).toBe("en");
  });

  it("uses browser languages when nothing is stored", () => {
    const wrapper = ({ children }: { children: ReactNode }) => (
      <I18nProvider catalogs={[demo]} browserLocales={["en-US"]}>
        {children}
      </I18nProvider>
    );
    expect(
      renderHook(() => useLocale(), { wrapper }).result.current.locale,
    ).toBe("en");
  });

  it("isolates instances (no global i18next state between providers)", () => {
    render(
      <>
        <I18nProvider catalogs={[demo]} initialLocale="en">
          <span data-testid="a">
            <Greeting />
          </span>
        </I18nProvider>
        <I18nProvider catalogs={[demo]} initialLocale="vi">
          <span data-testid="b">
            <Greeting />
          </span>
        </I18nProvider>
      </>,
    );
    expect(screen.getByTestId("a").textContent).toContain("Hello");
    expect(screen.getByTestId("b").textContent).toContain("Xin chào");
  });

  it("rejects duplicate namespaces", () => {
    expect(() =>
      render(
        <I18nProvider catalogs={[demo, demo]}>
          <span />
        </I18nProvider>,
      ),
    ).toThrow(/Duplicate i18n namespace/);
  });

  it("fails clearly outside a provider", () => {
    expect(() => renderHook(() => useLocale())).toThrow(/I18nProvider/);
  });
});

function Greeting() {
  const { t } = useT("demo");
  return <>{t("greeting", { name: "Lan" })}</>;
}
