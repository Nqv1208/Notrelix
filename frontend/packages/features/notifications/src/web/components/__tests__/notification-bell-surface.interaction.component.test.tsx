import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  notificationsDefaultScenario,
  notificationsEmptyScenario,
} from "../../../verification/notifications-ui-fixtures";
import { NotificationBellSurface } from "../notification-bell-surface";

describe("notifications web pure surface", () => {
  it("renders notifications from deterministic fixtures", () => {
    renderPureUi(
      <NotificationBellSurface
        notifications={notificationsDefaultScenario()}
        unreadCount={2}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Notifications" }));

    expect(screen.getByText("Ada mentioned you")).toBeTruthy();
    expect(screen.getByText("New comment on Operating plan")).toBeTruthy();
  });

  it("routes mark-read and archive through injected callbacks", () => {
    const onMarkRead = vi.fn();
    const onArchive = vi.fn();

    renderPureUi(
      <NotificationBellSurface
        notifications={notificationsDefaultScenario()}
        unreadCount={2}
        onMarkRead={onMarkRead}
        onArchive={onArchive}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Notifications" }));
    fireEvent.click(
      screen.getByRole("button", { name: "Mark notif-mention as read" }),
    );
    fireEvent.click(
      screen.getByRole("button", { name: "Archive notif-mention" }),
    );

    expect(onMarkRead).toHaveBeenCalledWith("notif-mention");
    expect(onArchive).toHaveBeenCalledWith("notif-mention");
  });

  it("routes mark-all-read through the injected callback", () => {
    const onMarkAllRead = vi.fn();

    renderPureUi(
      <NotificationBellSurface
        notifications={notificationsDefaultScenario()}
        unreadCount={2}
        onMarkAllRead={onMarkAllRead}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Notifications" }));
    fireEvent.click(screen.getByRole("button", { name: /Mark all read/ }));

    expect(onMarkAllRead).toHaveBeenCalledTimes(1);
  });

  it("renders the empty notifications state without query providers", () => {
    renderPureUi(
      <NotificationBellSurface notifications={notificationsEmptyScenario()} />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Notifications" }));

    expect(screen.getByText("No notifications found")).toBeTruthy();
  });
});
