import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  NOTIFICATIONS_REFERENCE_DATE,
  notificationsCopyFixture,
  notificationsDefaultScenario,
  notificationsEdgeDataScenario,
  notificationsEmptyScenario,
  notificationsFormattersFixture,
} from "../../../verification/notifications-ui-fixtures";
import { fillCopy, pluralCopy } from "../notification-bell.copy";
import {
  NotificationBellSurface,
  type NotificationBellSurfaceProps,
} from "../notification-bell-surface";

const LOCALES = ["vi", "en"] as const;

function renderBell(
  locale: (typeof LOCALES)[number],
  props: Partial<NotificationBellSurfaceProps> = {},
) {
  const copy = notificationsCopyFixture(locale);
  const view = renderPureUi(
    <NotificationBellSurface
      notifications={notificationsDefaultScenario()}
      unreadCount={2}
      referenceDate={NOTIFICATIONS_REFERENCE_DATE}
      copy={copy}
      formatters={notificationsFormattersFixture(locale)}
      {...props}
    />,
  );
  return { copy, view };
}

function bellButton(copy: ReturnType<typeof notificationsCopyFixture>, n = 2) {
  return screen.getByRole("button", {
    name: pluralCopy(
      { one: copy.bellUnread_one, other: copy.bellUnread_other },
      n,
    ),
  });
}

describe("notifications web pure surface", () => {
  it("FUI[notifications.bell:render] renders notifications from deterministic fixtures", () => {
    for (const locale of LOCALES) {
      const { copy, view } = renderBell(locale);

      fireEvent.click(bellButton(copy));

      expect(screen.getByText("Ada mentioned you")).toBeTruthy();
      expect(screen.getByText("New comment on Operating plan")).toBeTruthy();
      expect(screen.getByText(copy.panelTitle)).toBeTruthy();
      // Relative time is localized by the injected formatters: two hours
      // after the fixtures' createdAt.
      const expected = notificationsFormattersFixture(locale).relativeTime(
        "2026-01-15T10:30:00.000Z",
        NOTIFICATIONS_REFERENCE_DATE,
      );
      expect(screen.getAllByText(expected).length).toBeGreaterThan(0);
      expect(expected).toMatch(locale === "vi" ? /giờ trước/ : /hours ago/);
      view.unmount();
    }
  });

  it("FUI[notifications.bell:actions] routes mark-read and archive through injected callbacks", () => {
    for (const locale of LOCALES) {
      const onMarkRead = vi.fn();
      const onArchive = vi.fn();
      const { copy, view } = renderBell(locale, { onMarkRead, onArchive });

      fireEvent.click(bellButton(copy));
      const markName = fillCopy(copy.markRead, { title: "Ada mentioned you" });
      const archiveName = fillCopy(copy.archive, {
        title: "Ada mentioned you",
      });
      fireEvent.click(screen.getByRole("button", { name: markName }));
      fireEvent.click(screen.getByRole("button", { name: archiveName }));

      expect(onMarkRead).toHaveBeenCalledWith("notif-mention");
      expect(onArchive).toHaveBeenCalledWith("notif-mention");

      // Actions are named after the notification (never its id) and revealed
      // by keyboard focus and touch, not by hover alone.
      expect(
        screen.queryByRole("button", { name: /notif-mention/ }),
      ).toBeNull();
      const panel =
        screen.getByRole("button", { name: archiveName }).parentElement
          ?.className ?? "";
      expect(panel).toContain("group-focus-within:opacity-100");
      expect(panel).toContain("pointer-coarse:opacity-100");
      view.unmount();
    }
  });

  it("FUI[notifications.bell:mark-all] routes mark-all-read through the injected callback", () => {
    for (const locale of LOCALES) {
      const onMarkAllRead = vi.fn();
      const { copy, view } = renderBell(locale, { onMarkAllRead });

      fireEvent.click(bellButton(copy));
      fireEvent.click(screen.getByRole("button", { name: copy.markAllRead }));

      expect(onMarkAllRead).toHaveBeenCalledTimes(1);
      view.unmount();
    }
  });

  it("FUI[notifications.bell:empty] renders the empty notifications state without query providers", () => {
    for (const locale of LOCALES) {
      const copy = notificationsCopyFixture(locale);
      const view = renderPureUi(
        <NotificationBellSurface
          notifications={notificationsEmptyScenario()}
          copy={copy}
          formatters={notificationsFormattersFixture(locale)}
        />,
      );

      fireEvent.click(screen.getByRole("button", { name: copy.bell }));

      expect(screen.getByText(copy.empty)).toBeTruthy();
      expect(
        screen.queryByRole("button", { name: copy.markAllRead }),
      ).toBeNull();
      view.unmount();
    }
  });
});

describe("notification bell unread badge", () => {
  const cases = [
    { count: 0, badge: null },
    { count: 1, badge: "1" },
    { count: 99, badge: "99" },
    { count: 100, badge: "99+" },
    { count: 1234, badge: "99+" },
  ] as const;

  for (const locale of LOCALES) {
    for (const { count, badge } of cases) {
      it(`shows ${badge ?? "no badge"} for ${count} unread in ${locale}`, () => {
        const copy = notificationsCopyFixture(locale);
        renderBell(locale, { unreadCount: count });

        const name =
          count === 0
            ? copy.bell
            : pluralCopy(
                { one: copy.bellUnread_one, other: copy.bellUnread_other },
                count,
              );
        const button = screen.getByRole("button", { name });
        if (badge === null) {
          expect(button.textContent).toBe("");
        } else {
          expect(button.textContent).toBe(badge);
        }
        // The accessible name keeps the exact count even when the badge caps.
        expect(button.getAttribute("aria-label")).toBe(name);
      });
    }
  }
});

describe("notification bell localization", () => {
  it("names every icon-only control after its notification in vi and en", () => {
    for (const locale of LOCALES) {
      const { copy, view } = renderBell(locale);
      fireEvent.click(bellButton(copy));

      const list = screen.getAllByRole("button");
      for (const button of list) {
        const name =
          button.getAttribute("aria-label") ?? button.textContent ?? "";
        expect(name.trim()).not.toBe("");
      }
      for (const notification of notificationsDefaultScenario()) {
        expect(
          screen.getByRole("button", {
            name: fillCopy(copy.archive, { title: notification.title }),
          }),
        ).toBeTruthy();
      }
      view.unmount();
    }
  });

  it("leaves no English literal in the Vietnamese surface", () => {
    const en = notificationsCopyFixture("en");
    const { copy, view } = renderBell("vi");
    fireEvent.click(bellButton(copy));

    const dialogText = document.body.textContent ?? "";
    const labels = Array.from(
      document.body.querySelectorAll("[aria-label],[title]"),
    ).map(
      (node) =>
        `${node.getAttribute("aria-label")} ${node.getAttribute("title")}`,
    );
    const haystack = `${dialogText} ${labels.join(" ")}`;
    for (const literal of [
      en.panelTitle,
      en.markAllRead,
      en.markReadHint,
      en.archiveHint,
    ]) {
      expect(haystack).not.toContain(literal);
    }
    view.unmount();
  });

  it("localizes the loading state", () => {
    for (const locale of LOCALES) {
      const { copy, view } = renderBell(locale, {
        notifications: [],
        status: "loading",
      });
      fireEvent.click(bellButton(copy));
      expect(screen.getByText(copy.loading)).toBeTruthy();
      view.unmount();
    }
  });

  it("keeps long Vietnamese titles whole in the accessible name and lets the header wrap", () => {
    const { copy } = renderBell("vi", {
      notifications: notificationsEdgeDataScenario(),
    });
    fireEvent.click(bellButton(copy));

    const longTitle = notificationsEdgeDataScenario().find(
      (item) => item.id === "notif-long-title-vi",
    )!.title;
    // Visual truncation never shortens the DOM text or the action's name.
    expect(screen.getByText(longTitle).className).toContain("truncate");
    expect(
      screen.getByRole("button", {
        name: fillCopy(copy.archive, { title: longTitle }),
      }),
    ).toBeTruthy();

    const header = screen.getByText(copy.panelTitle).parentElement!;
    expect(header.className).toContain("flex-wrap");
    expect(
      within(header).getByRole("button", { name: copy.markAllRead }).className,
    ).toContain("shrink-0");
  });

  it("closes the panel on Escape", () => {
    const { copy } = renderBell("en");
    fireEvent.click(bellButton(copy));
    expect(screen.getByText(copy.panelTitle)).toBeTruthy();

    fireEvent.keyDown(screen.getByText(copy.panelTitle), { key: "Escape" });
    expect(screen.queryByText(copy.panelTitle)).toBeNull();
  });
});
