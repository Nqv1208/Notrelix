import { useState } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
} from "@notrelix/testing";
import type {
  FormPublicAccess,
  FormPublicAccessResult,
} from "@notrelix/work-management-core";
import { formsCopyFixture } from "../../../verification/forms-fixtures";
import { copyText } from "../copy-text";
import {
  FormPublicAccessSection,
  type FormPublicAccessSectionProps,
} from "../form-public-access-section";

const ORIGIN = "https://app.notrelix.test";
const PRIVATE: FormPublicAccess = { isPublic: false, publicKey: null };

/** The host owns the state: the section only asks, the answer comes back as the new access. */
function Host({
  start = PRIVATE,
  answer,
  locale,
  status = "ready",
  onRetry = () => undefined,
}: {
  start?: FormPublicAccess;
  answer: (enabled: boolean) => Promise<FormPublicAccessResult>;
  locale: "vi" | "en";
  status?: FormPublicAccessSectionProps["status"];
  onRetry?: () => void;
}) {
  const [access, setAccess] = useState(start);
  return (
    <FormPublicAccessSection
      copy={formsCopyFixture(locale)}
      status={status}
      access={access}
      origin={ORIGIN}
      onCopyLink={copyText}
      onRetry={onRetry}
      onSwitch={async (enabled) => {
        const result = await answer(enabled);
        if (result.ok) setAccess(result.access);
        return result;
      }}
    />
  );
}

const enable = async (): Promise<FormPublicAccessResult> => ({
  ok: true,
  access: { isPublic: true, publicKey: "key-1" },
});
const disable = async (): Promise<FormPublicAccessResult> => ({
  ok: true,
  access: PRIVATE,
});

afterEach(() => {
  vi.restoreAllMocks();
  Reflect.deleteProperty(navigator, "clipboard");
});

describe.each(["vi", "en"] as const)("form public link (%s)", (locale) => {
  const t = formsCopyFixture(locale).publicAccess;
  const switchOf = () => screen.getByRole("switch", { name: t.switchLabel });

  it("is private by default: no link, an off switch and the way to make it public", () => {
    renderPureUi(<Host locale={locale} answer={enable} />);
    expect(switchOf().getAttribute("aria-checked")).toBe("false");
    expect(screen.queryByLabelText(t.linkLabel)).toBeNull();
    expect(screen.getByText(t.offNote)).toBeTruthy();
  });

  it("shows the link and copies exactly it when switched public, then hides it when switched private", async () => {
    const writeText = vi.fn(async () => undefined);
    Object.defineProperty(navigator, "clipboard", {
      value: { writeText },
      configurable: true,
    });
    renderPureUi(
      <Host
        locale={locale}
        answer={(enabled) => (enabled ? enable() : disable())}
      />,
    );
    fireEvent.click(switchOf());
    const link = (await screen.findByLabelText(
      t.linkLabel,
    )) as HTMLInputElement;
    expect(link.value).toBe(`${ORIGIN}/f/key-1`);
    expect(link.readOnly).toBe(true);
    expect(screen.getByText(t.onNote)).toBeTruthy();

    fireEvent.click(screen.getByRole("button", { name: t.copy }));
    await waitFor(() =>
      expect(writeText).toHaveBeenCalledWith(`${ORIGIN}/f/key-1`),
    );
    expect(await screen.findByRole("button", { name: t.copied })).toBeTruthy();

    fireEvent.click(switchOf());
    await waitFor(() =>
      expect(screen.queryByLabelText(t.linkLabel)).toBeNull(),
    );
    expect(switchOf().getAttribute("aria-checked")).toBe("false");
  });

  it("says so when the browser refuses to copy, and leaves the link selected for a hand copy", async () => {
    Object.defineProperty(navigator, "clipboard", {
      value: {
        writeText: vi.fn(async () => {
          throw new Error("denied");
        }),
      },
      configurable: true,
    });
    renderPureUi(
      <Host
        locale={locale}
        start={{ isPublic: true, publicKey: "key-2" }}
        answer={disable}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: t.copy }));
    expect(await screen.findByText(t.copyFailed)).toBeTruthy();
  });

  it("shows the saving state while the switch is being written and blocks a second tap", async () => {
    let finish: (value: FormPublicAccessResult) => void = () => undefined;
    const answer = vi.fn(
      () =>
        new Promise<FormPublicAccessResult>((resolve) => (finish = resolve)),
    );
    renderPureUi(<Host locale={locale} answer={answer} />);
    fireEvent.click(switchOf());
    await waitFor(() => expect(screen.getByText(t.saving)).toBeTruthy());
    expect((switchOf() as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(switchOf());
    expect(answer).toHaveBeenCalledTimes(1);
    await act(async () =>
      finish({ ok: true, access: { isPublic: true, publicKey: "key-3" } }),
    );
    expect(await screen.findByLabelText(t.linkLabel)).toBeTruthy();
    expect((switchOf() as HTMLButtonElement).disabled).toBe(false);
  });

  it("is denied for someone who is not a workspace Owner or Admin: the switch is off and disabled with the reason", () => {
    renderPureUi(<Host locale={locale} status="denied" answer={enable} />);
    expect((switchOf() as HTMLButtonElement).disabled).toBe(true);
    expect(screen.getByText(t.denied)).toBeTruthy();
    expect(switchOf().getAttribute("aria-describedby")).toBeTruthy();
  });

  it("explains that public links are off in the workspace and stays private", async () => {
    renderPureUi(
      <Host
        locale={locale}
        answer={async () => ({ ok: false, reason: "linksOff" })}
      />,
    );
    fireEvent.click(switchOf());
    expect((await screen.findByRole("alert")).textContent).toBe(t.linksOff);
    expect(switchOf().getAttribute("aria-checked")).toBe("false");
    expect(screen.queryByLabelText(t.linkLabel)).toBeNull();
  });

  it("keeps the state and says it did not change when the write fails", async () => {
    renderPureUi(
      <Host
        locale={locale}
        answer={async () => ({ ok: false, reason: "failed" })}
      />,
    );
    fireEvent.click(switchOf());
    expect((await screen.findByRole("alert")).textContent).toContain(
      t.saveError,
    );
    expect(switchOf().getAttribute("aria-checked")).toBe("false");
  });

  it("shows a skeleton while loading and a retry when the read failed", () => {
    const onRetry = vi.fn();
    const { rerender } = renderPureUi(
      <Host locale={locale} status="loading" answer={enable} />,
    );
    expect(screen.getByRole("status", { name: t.loading })).toBeTruthy();
    expect(screen.queryByRole("switch")).toBeNull();
    rerender(
      <Host locale={locale} status="error" answer={enable} onRetry={onRetry} />,
    );
    expect(screen.getByText(t.loadError)).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", {
        name: formsCopyFixture(locale).common.retry,
      }),
    );
    expect(onRetry).toHaveBeenCalled();
  });
});
