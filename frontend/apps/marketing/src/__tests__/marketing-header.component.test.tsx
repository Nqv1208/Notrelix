import { act } from "react";
import { createRoot } from "react-dom/client";
import { NextIntlClientProvider } from "next-intl";
import { describe, expect, it } from "vitest";

import { MarketingHeader } from "../components/marketing-header";
import { messages } from "../messages/en";

describe("MarketingHeader app links", () => {
  it("points sign-in and sign-up at same-origin relative paths", async () => {
    (
      globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }
    ).IS_REACT_ACT_ENVIRONMENT = true;
    const container = document.createElement("div");
    document.body.append(container);
    const root = createRoot(container);

    await act(async () => {
      root.render(
        <NextIntlClientProvider locale="en" messages={messages}>
          <MarketingHeader />
        </NextIntlClientProvider>,
      );
    });

    const hrefsFor = (label: string) =>
      [...container.querySelectorAll("a")]
        .filter((a) => a.textContent?.includes(label))
        .map((a) => a.getAttribute("href"));

    expect(hrefsFor(messages.header.signIn)).toContain("/sign-in");
    expect(hrefsFor(messages.header.tryFree)).toContain("/sign-up");
    expect(
      [...container.querySelectorAll("a")]
        .map((a) => a.getAttribute("href"))
        .filter((h) => h?.includes("sign-")),
    ).toEqual(["/sign-in", "/sign-up", "/sign-in", "/sign-up"]);

    await act(async () => root.unmount());
    container.remove();
  });
});
