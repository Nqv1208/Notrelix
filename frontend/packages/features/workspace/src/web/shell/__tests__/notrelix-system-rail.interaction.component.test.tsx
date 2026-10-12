import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  systemRailCopyFixture,
  systemRailEdgeDataScenario,
  systemRailHiddenScenario,
  systemRailOverlayScenario,
  systemRailPinnedScenario,
  type SystemRailScenario,
} from "../../../verification/workspace-system-rail-fixtures";
import type { NotrelixSystemRailCopy } from "../notrelix-system-rail.copy";
import {
  NotrelixSystemRailSurface,
  type NotrelixSystemRailSurfaceProps,
  type SystemRailWorkspace,
} from "../notrelix-system-rail-surface";

const LOCALES = ["vi", "en"] as const;

function renderRail(
  scenario: SystemRailScenario,
  overrides: Partial<NotrelixSystemRailSurfaceProps> = {},
) {
  const props: NotrelixSystemRailSurfaceProps = {
    ...scenario,
    onSwitchWorkspace: vi.fn(),
    onCreateWorkspace: vi.fn(),
    onOpenAllInbox: vi.fn(),
    onOpenGlobalSearch: vi.fn(),
    onOpenAccount: vi.fn(),
    onTogglePinned: vi.fn(),
    onClose: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<NotrelixSystemRailSurface {...props} />);
  return { props, view };
}

/** The accessible name the rail gives a workspace entry. */
function workspaceName(
  copy: NotrelixSystemRailCopy,
  workspace: SystemRailWorkspace,
): string {
  const guest = workspace.isGuest ? ` ${copy.guest}` : "";
  const unread =
    workspace.unread !== null && workspace.unread > 0
      ? `, ${copy.unread.replace("{{count}}", String(workspace.unread))}`
      : "";
  return `${workspace.name}${guest}${unread}`;
}

function rail(copy: NotrelixSystemRailCopy) {
  return screen.getByRole("navigation", { name: copy.label });
}

function badgeOf(element: HTMLElement): string | null {
  return (
    element.querySelector('[data-slot="system-rail-badge"]')?.textContent ??
    null
  );
}

describe("NotrelixSystemRailSurface", () => {
  it("FUI[workspace.system-rail:hidden] renders nothing focusable when hidden", () => {
    for (const locale of LOCALES) {
      const { view } = renderRail(systemRailHiddenScenario(locale));
      expect(screen.queryByRole("navigation")).toBeNull();
      expect(
        view.container.querySelectorAll("button, a[href], [tabindex]"),
      ).toHaveLength(0);
      view.unmount();
    }
  });

  it("FUI[workspace.system-rail:render] names the rail, marks the current workspace and labels guests", () => {
    for (const locale of LOCALES) {
      const copy = systemRailCopyFixture(locale);
      const scenario = systemRailOverlayScenario(locale);
      const { view } = renderRail(scenario);
      const nav = rail(copy);
      const [current, unread, guest] = scenario.workspaces;

      const currentButton = within(nav).getByRole("button", {
        name: workspaceName(copy, current!),
      });
      expect(currentButton.getAttribute("aria-current")).toBe("page");
      expect(within(currentButton).getByText(current!.shortName)).toBeTruthy();

      const unreadButton = within(nav).getByRole("button", {
        name: workspaceName(copy, unread!),
      });
      expect(unreadButton.getAttribute("aria-current")).toBeNull();
      expect(badgeOf(unreadButton)).toBe("2");

      // Guest membership is part of the accessible name: "(khách)" / "(guest)".
      const guestButton = within(nav).getByRole("button", {
        name: `${guest!.name} ${copy.guest}`,
      });
      // Unread unavailable (null) → no badge, never "0".
      expect(badgeOf(guestButton)).toBeNull();
      expect(badgeOf(currentButton)).toBeNull();

      // Dedicated rail roles: dark in every appearance, no borrowed `.dark` scope.
      expect(nav.classList.contains("bg-rail")).toBe(true);
      expect(nav.classList.contains("dark")).toBe(false);
      expect(within(nav).getByRole("img", { name: copy.logo })).toBeTruthy();
      expect(
        within(nav).getByRole("img", {
          name: copy.user.replace("{{name}}", "Nguyễn Thu Hà"),
        }),
      ).toBeTruthy();
      view.unmount();
    }
  });

  it("FUI[workspace.system-rail:edge-data] caps large counts and hides zero or unknown counts", () => {
    for (const locale of LOCALES) {
      const copy = systemRailCopyFixture(locale);
      const scenario = systemRailEdgeDataScenario(locale);
      const { view } = renderRail(scenario);
      const nav = rail(copy);
      const buttons = scenario.workspaces.map((workspace) =>
        within(nav).getByRole("button", {
          name: workspaceName(copy, workspace),
        }),
      );
      expect(buttons).toHaveLength(8);
      expect(buttons.map(badgeOf)).toEqual([
        null,
        "3",
        "99+",
        null,
        null,
        "7",
        null,
        "99",
      ]);
      const allInbox = within(nav).getByRole("button", {
        name: `${copy.allInbox.label}, ${copy.unread.replace("{{count}}", "120")}`,
      });
      expect(badgeOf(allInbox)).toBe("99+");
      // No signed-in user → no avatar.
      expect(within(nav).getAllByRole("img")).toHaveLength(1);
      view.unmount();
    }
  });

  it("FUI[workspace.system-rail:dismiss] closes the overlay on Escape and on the scrim", () => {
    for (const locale of LOCALES) {
      const copy = systemRailCopyFixture(locale);
      const { props, view } = renderRail(systemRailOverlayScenario(locale));
      fireEvent.keyDown(rail(copy), { key: "Escape" });
      expect(props.onClose).toHaveBeenCalledTimes(1);

      const scrim = view.container.querySelector(
        '[data-slot="system-rail-scrim"]',
      );
      expect(scrim).not.toBeNull();
      fireEvent.click(scrim!);
      expect(props.onClose).toHaveBeenCalledTimes(2);
      view.unmount();
    }
  });

  it("FUI[workspace.system-rail:dismiss] closes the overlay when focus leaves it, not when it moves inside", () => {
    for (const locale of LOCALES) {
      const copy = systemRailCopyFixture(locale);
      const scenario = systemRailOverlayScenario(locale);
      const outside = document.createElement("button");
      document.body.append(outside);
      const { props, view } = renderRail(scenario);
      const nav = rail(copy);
      const [current, other] = scenario.workspaces;
      const currentButton = within(nav).getByRole("button", {
        name: workspaceName(copy, current!),
      });
      const otherButton = within(nav).getByRole("button", {
        name: workspaceName(copy, other!),
      });

      // Focus moving between rail entries keeps it open.
      fireEvent.blur(currentButton, { relatedTarget: otherButton });
      expect(props.onClose).not.toHaveBeenCalled();

      // Tabbing past the last entry closes the overlay.
      fireEvent.blur(otherButton, { relatedTarget: outside });
      expect(props.onClose).toHaveBeenCalledTimes(1);
      view.unmount();
      outside.remove();
    }

    // Pinned: focus leaving the rail is ordinary navigation.
    const copy = systemRailCopyFixture("vi");
    const pinned = renderRail(systemRailPinnedScenario("vi"));
    const first = within(rail(copy)).getAllByRole("button")[0]!;
    fireEvent.blur(first, { relatedTarget: document.body });
    expect(pinned.props.onClose).not.toHaveBeenCalled();
  });

  it("FUI[workspace.system-rail:pinned] stays in flow without a scrim and ignores Escape when pinned", () => {
    const copy = systemRailCopyFixture("vi");
    const { props, view } = renderRail(systemRailPinnedScenario("vi"));
    expect(
      view.container.querySelector('[data-slot="system-rail-scrim"]'),
    ).toBeNull();
    expect(rail(copy).getAttribute("data-presentation")).toBe("pinned");
    fireEvent.keyDown(rail(copy), { key: "Escape" });
    expect(props.onClose).not.toHaveBeenCalled();
  });

  it("FUI[workspace.system-rail:switch-workspace] switches to another workspace; the current one closes the overlay", () => {
    for (const locale of LOCALES) {
      const copy = systemRailCopyFixture(locale);
      const scenario = systemRailOverlayScenario(locale);
      const { props, view } = renderRail(scenario);
      const nav = rail(copy);
      fireEvent.click(
        within(nav).getByRole("button", {
          name: workspaceName(copy, scenario.workspaces[1]!),
        }),
      );
      expect(props.onSwitchWorkspace).toHaveBeenCalledWith(
        "ws-minh-an-da-nang",
      );
      // The current workspace closes the overlay instead of switching.
      fireEvent.click(
        within(nav).getByRole("button", {
          name: workspaceName(copy, scenario.workspaces[0]!),
        }),
      );
      expect(props.onSwitchWorkspace).toHaveBeenCalledTimes(1);
      expect(props.onClose).toHaveBeenCalledTimes(1);
      view.unmount();
    }

    // Pinned: the current workspace does nothing.
    const copy = systemRailCopyFixture("vi");
    const scenario = systemRailPinnedScenario("vi");
    const pinned = renderRail(scenario);
    fireEvent.click(
      within(rail(copy)).getByRole("button", {
        name: workspaceName(copy, scenario.workspaces[0]!),
      }),
    );
    expect(pinned.props.onSwitchWorkspace).not.toHaveBeenCalled();
    expect(pinned.props.onClose).not.toHaveBeenCalled();
  });

  it("FUI[workspace.system-rail:create] opens workspace creation from Add", () => {
    for (const locale of LOCALES) {
      const copy = systemRailCopyFixture(locale);
      const { props, view } = renderRail(systemRailOverlayScenario(locale));
      const add = within(rail(copy)).getByRole("button", {
        name: copy.create.label,
      });
      expect(add.getAttribute("title")).toBe(copy.create.title);
      fireEvent.click(add);
      expect(props.onCreateWorkspace).toHaveBeenCalledTimes(1);
      view.unmount();
    }
  });

  it("FUI[workspace.system-rail:all-inbox] disables the all-workspace inbox with a visible reason until it is available", () => {
    for (const locale of LOCALES) {
      const copy = systemRailCopyFixture(locale);
      const { props, view } = renderRail(systemRailOverlayScenario(locale));
      const nav = rail(copy);
      const allInbox = within(nav).getByRole("button", {
        name: copy.allInbox.label,
      }) as HTMLButtonElement;
      expect(allInbox.disabled).toBe(true);
      const reason = within(nav).getByText(copy.allInbox.unavailable);
      expect(allInbox.getAttribute("aria-describedby")).toBe(reason.id);
      fireEvent.click(allInbox);
      expect(props.onOpenAllInbox).not.toHaveBeenCalled();
      view.unmount();

      const available = renderRail(systemRailEdgeDataScenario(locale));
      const enabled = within(rail(copy)).getByRole("button", {
        name: new RegExp(`^${copy.allInbox.label}`),
      }) as HTMLButtonElement;
      expect(enabled.disabled).toBe(false);
      expect(screen.queryByText(copy.allInbox.unavailable)).toBeNull();
      fireEvent.click(enabled);
      expect(available.props.onOpenAllInbox).toHaveBeenCalledTimes(1);
      available.view.unmount();
    }
  });

  it("FUI[workspace.system-rail:search] opens global search", () => {
    for (const locale of LOCALES) {
      const copy = systemRailCopyFixture(locale);
      const { props, view } = renderRail(systemRailOverlayScenario(locale));
      const search = within(rail(copy)).getByRole("button", {
        name: copy.search.label,
      });
      expect(search.getAttribute("title")).toBe(copy.search.title);
      fireEvent.click(search);
      expect(props.onOpenGlobalSearch).toHaveBeenCalledTimes(1);
      view.unmount();
    }
  });

  it("FUI[workspace.system-rail:account] opens account settings", () => {
    for (const locale of LOCALES) {
      const copy = systemRailCopyFixture(locale);
      const { props, view } = renderRail(systemRailOverlayScenario(locale));
      const settings = within(rail(copy)).getByRole("button", {
        name: copy.settings.label,
      });
      expect(settings.getAttribute("title")).toBe(copy.settings.title);
      fireEvent.click(settings);
      expect(props.onOpenAccount).toHaveBeenCalledTimes(1);
      view.unmount();
    }
  });

  it("FUI[workspace.system-rail:pin] toggles the pinned preference with a pressed state", () => {
    for (const locale of LOCALES) {
      const copy = systemRailCopyFixture(locale);
      const overlay = renderRail(systemRailOverlayScenario(locale));
      const unpinned = within(rail(copy)).getByRole("button", {
        name: copy.pin.label,
      });
      expect(unpinned.getAttribute("aria-pressed")).toBe("false");
      fireEvent.click(unpinned);
      expect(overlay.props.onTogglePinned).toHaveBeenCalledTimes(1);
      overlay.view.unmount();

      const pinned = renderRail(systemRailPinnedScenario(locale));
      const pressed = within(rail(copy)).getByRole("button", {
        name: copy.pin.label,
      });
      expect(pressed.getAttribute("aria-pressed")).toBe("true");
      fireEvent.click(pressed);
      expect(pinned.props.onTogglePinned).toHaveBeenCalledTimes(1);
      pinned.view.unmount();
    }
  });

  it("FUI[workspace.system-rail:keyboard] moves focus into the overlay and keeps a top-to-bottom tab order", () => {
    for (const locale of LOCALES) {
      const copy = systemRailCopyFixture(locale);
      const scenario = systemRailOverlayScenario(locale);
      const { view } = renderRail(scenario);
      const nav = rail(copy);
      const current = within(nav).getByRole("button", {
        name: workspaceName(copy, scenario.workspaces[0]!),
      });
      expect(document.activeElement).toBe(current);

      const focusable = Array.from(
        nav.querySelectorAll<HTMLElement>("button:not([disabled]), a[href]"),
      );
      expect(
        focusable.map(
          (element) =>
            element.getAttribute("aria-label") ?? element.textContent?.trim(),
        ),
      ).toEqual([
        ...scenario.workspaces.map((workspace) =>
          workspaceName(copy, workspace),
        ),
        copy.create.label,
        copy.search.label,
        copy.settings.label,
        copy.pin.label,
      ]);
      for (const element of focusable) {
        expect(element.getAttribute("tabindex")).not.toBe("-1");
        element.focus();
        expect(document.activeElement).toBe(element);
      }
      view.unmount();
    }
  });
});
