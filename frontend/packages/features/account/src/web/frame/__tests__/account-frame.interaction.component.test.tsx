import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  frameCopyFixture,
  frameDefaultScenario,
  frameEdgeDataScenario,
  frameLinksFixture,
} from "../../../verification/account-frame-fixtures";
import { AccountFrame } from "../account-frame";

describe("AccountFrame", () => {
  it("FUI[account.frame:render] shows the member in the head and the five pages in the menu", () => {
    for (const locale of ["vi", "en"] as const) {
      const copy = frameCopyFixture(locale);
      const view = renderPureUi(
        <AccountFrame {...frameDefaultScenario(locale)}>
          <p>page</p>
        </AccountFrame>,
      );
      expect(
        screen.getByRole("heading", { level: 1, name: copy.title }),
      ).toBeTruthy();
      expect(
        screen.getByText("Nguyễn Thu Hà · thuha.nguyen@example.com"),
      ).toBeTruthy();
      const nav = screen.getByRole("navigation", { name: copy.label });
      expect(nav.querySelectorAll("a")).toHaveLength(5);
      const current = nav.querySelector("[aria-current=page]");
      expect(current?.textContent).toBe(copy.appearance);
      expect(screen.getByText("page")).toBeTruthy();
      view.unmount();
    }
  });

  it("FUI[account.frame:navigate] hands a plain click to the host and leaves modified clicks to the browser", () => {
    const onNavigate = vi.fn();
    const copy = frameCopyFixture("en");
    const view = renderPureUi(
      <AccountFrame
        {...frameDefaultScenario("en")}
        links={frameLinksFixture(onNavigate)}
      >
        <p>page</p>
      </AccountFrame>,
    );
    const link = screen.getByRole("link", { name: copy.security });
    expect(link.getAttribute("href")).toBe("/account/security");
    // jsdom cannot navigate: a native handler cancels what the host leaves.
    link.addEventListener("click", (event) => event.preventDefault());
    fireEvent.click(link);
    expect(onNavigate).toHaveBeenCalledTimes(1);
    fireEvent.click(link, { ctrlKey: true });
    expect(onNavigate).toHaveBeenCalledTimes(1);
    view.unmount();
  });

  it("FUI[account.frame:edge] keeps a long name and address in the head", () => {
    const view = renderPureUi(
      <AccountFrame {...frameEdgeDataScenario()}>
        <p>page</p>
      </AccountFrame>,
    );
    expect(document.body.textContent).toContain("Nguyễn Thị Phương Anh");
    view.unmount();
  });
});
