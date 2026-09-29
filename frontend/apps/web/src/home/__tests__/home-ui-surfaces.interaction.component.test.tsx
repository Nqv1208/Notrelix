import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { describe, expect, it } from "vitest";
import { HomeSidebarSurface } from "../home-sidebar-surface";
import { HomeSurface } from "../home-surface";
import {
  homeDefaultScenario,
  homeSidebarScenario,
} from "../verification/home-ui-fixtures";
import type { HomeSidebarNavigationRenderer } from "../home-sidebar-surface";

const renderNavigation: HomeSidebarNavigationRenderer = (
  _target,
  children,
  props,
) => (
  <a
    href="#"
    className={props.className}
    title={props.title}
    aria-label={props.ariaLabel}
    aria-current={props.ariaCurrent}
  >
    {children}
  </a>
);

describe("Home UI surfaces", () => {
  it("FUI[home.surface:render] renders the home view model without a route or provider", () => {
    renderPureUi(<HomeSurface data={homeDefaultScenario()} />);
    expect(
      screen.getByRole("heading", { name: /good morning, ada/i }),
    ).toBeTruthy();
    expect(screen.getByRole("heading", { name: "My work" })).toBeTruthy();
  });

  it("FUI[home.surface:search] filters recent work through its public search interaction", () => {
    renderPureUi(<HomeSurface data={homeDefaultScenario()} />);
    fireEvent.change(
      screen.getByRole("textbox", { name: "Search home content" }),
      { target: { value: "roadmap" } },
    );
    expect(
      screen.getByRole("button", { name: /Product roadmap/ }),
    ).toBeTruthy();
    expect(
      screen.queryByRole("button", { name: /Launch planning/ }),
    ).toBeNull();
  });

  it("FUI[home.sidebar:rail] exposes compact navigation and focus preview semantics", () => {
    const onPreviewChange = (open: boolean) => {
      if (open) screen.getByRole("link", { name: "Home" });
    };
    renderPureUi(
      <HomeSidebarSurface
        data={homeSidebarScenario()}
        collapsed
        onPreviewChange={onPreviewChange}
        renderNavigation={renderNavigation}
      />,
    );
    expect(
      screen.getByRole("navigation", { name: "Compact navigation" }),
    ).toBeTruthy();
    expect(screen.getByRole("link", { name: "Home" })).toBeTruthy();
    fireEvent.focus(screen.getByRole("link", { name: "Home" }));
    expect(
      screen.getByRole("link", { name: "Favorite: Product roadmap" }),
    ).toBeTruthy();
  });
});
