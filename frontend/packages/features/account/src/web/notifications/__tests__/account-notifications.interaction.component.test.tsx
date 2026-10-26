import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  notificationsCopyFixture,
  notificationsDefaultScenario,
  notificationsEdgeDataScenario,
  notificationsErrorScenario,
  notificationsLoadingScenario,
  notificationsSaveErrorScenario,
  notificationsSavedScenario,
  notificationsSavingScenario,
  notificationValuesFixture,
  type NotificationsScenario,
} from "../../../verification/account-notifications-fixtures";
import { en } from "../account-notifications.copy";
import {
  AccountNotificationsSurface,
  type AccountNotificationsSurfaceProps,
} from "../account-notifications-surface";

const LOCALES = ["vi", "en"] as const;

function renderNotifications(scenario: NotificationsScenario) {
  const props: AccountNotificationsSurfaceProps = {
    ...scenario,
    onSave: vi.fn(),
    onRetry: vi.fn(),
  };
  const view = renderPureUi(<AccountNotificationsSurface {...props} />);
  return { props, view };
}

const cell = (
  copy: ReturnType<typeof notificationsCopyFixture>,
  event: keyof typeof en.events,
  channel: keyof typeof en.channels,
) =>
  screen.getByRole("checkbox", {
    name: copy.cellLabel
      .replace("{{event}}", copy.events[event])
      .replace("{{channel}}", copy.channels[channel]),
  });

describe("AccountNotificationsSurface", () => {
  it("FUI[account.notifications:render] shows the matrix with each cell named by event and channel", () => {
    for (const locale of LOCALES) {
      const copy = notificationsCopyFixture(locale);
      const { view } = renderNotifications(
        notificationsDefaultScenario(locale),
      );
      expect(
        screen.getByRole("heading", { level: 2, name: copy.title }),
      ).toBeTruthy();
      const table = screen.getByRole("table", { name: copy.matrixLabel });
      expect(within(table).getAllByRole("checkbox")).toHaveLength(14);
      expect(cell(copy, "assigned", "inApp").getAttribute("aria-checked")).toBe(
        "true",
      );
      expect(cell(copy, "overdue", "email").getAttribute("aria-checked")).toBe(
        "false",
      );
      // Mobile and quiet hours have no producer yet: neither is offered.
      expect(screen.queryByText("Di động")).toBeNull();
      expect(screen.queryByText("Mobile")).toBeNull();
      expect(screen.queryByRole("switch")).toBeNull();
      expect(
        screen.getByText(
          copy.eventNotes.mention.replace("{{name}}", "Ada Lovelace"),
        ),
      ).toBeTruthy();
      view.unmount();
    }
  });

  it("FUI[account.notifications:toggle] flips a cell with the click and the Space key", () => {
    const copy = notificationsCopyFixture("en");
    const { view } = renderNotifications(notificationsDefaultScenario("en"));
    const digestApp = cell(copy, "digest", "inApp");
    fireEvent.click(digestApp);
    expect(digestApp.getAttribute("aria-checked")).toBe("true");
    fireEvent.click(digestApp);
    expect(digestApp.getAttribute("aria-checked")).toBe("false");
    view.unmount();
  });

  it("FUI[account.notifications:scope] says the choices belong to one workspace and quotes the version", () => {
    const copy = notificationsCopyFixture("en");
    const { props, view } = renderNotifications(
      notificationsDefaultScenario("en"),
    );
    expect(screen.getByText(copy.workspaceScope)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: copy.save }));
    expect(props.onSave).toHaveBeenCalledWith(notificationValuesFixture());
    view.unmount();
  });

  it("FUI[account.notifications:save] sends every cell of the matrix", () => {
    for (const locale of LOCALES) {
      const copy = notificationsCopyFixture(locale);
      const { props, view } = renderNotifications(
        notificationsDefaultScenario(locale),
      );
      fireEvent.click(cell(copy, "watching", "email"));
      fireEvent.click(screen.getByRole("button", { name: copy.save }));
      const saved = vi.mocked(props.onSave).mock.calls[0]?.[0];
      expect(saved?.version).toBe(3);
      expect(saved?.rows.watching).toEqual({ inApp: true, email: true });
      expect(saved?.rows.assigned).toEqual({ inApp: true, email: true });
      view.unmount();
    }
  });

  it("FUI[account.notifications:saving] locks the matrix and names the pending save", () => {
    const copy = notificationsCopyFixture("en");
    const { view } = renderNotifications(notificationsSavingScenario("en"));
    expect(
      (screen.getByRole("button", { name: copy.saving }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    expect(
      (cell(copy, "assigned", "inApp") as HTMLButtonElement).disabled,
    ).toBe(true);
    view.unmount();
  });

  it("FUI[account.notifications:saved] confirms the save and announces a failure", () => {
    for (const locale of LOCALES) {
      const copy = notificationsCopyFixture(locale);
      const saved = renderNotifications(notificationsSavedScenario(locale));
      expect(screen.getByRole("status").textContent).toContain(copy.saved);
      saved.view.unmount();
      const failed = renderNotifications(
        notificationsSaveErrorScenario(locale),
      );
      expect(screen.getByRole("alert").textContent).toContain(copy.saveError);
      failed.view.unmount();
    }
  });

  it("FUI[account.notifications:loading] shows a named loading state", () => {
    const copy = notificationsCopyFixture("en");
    const { view } = renderNotifications(notificationsLoadingScenario("en"));
    expect(screen.getByRole("status", { name: copy.loading })).toBeTruthy();
    expect(screen.queryByRole("table")).toBeNull();
    view.unmount();
  });

  it("FUI[account.notifications:error] offers a retry when the preferences cannot load", () => {
    const copy = notificationsCopyFixture("en");
    const { props, view } = renderNotifications(
      notificationsErrorScenario("en"),
    );
    const alert = screen.getByRole("alert");
    expect(alert.textContent).toContain(copy.loadError);
    fireEvent.click(within(alert).getByRole("button", { name: copy.retry }));
    expect(props.onRetry).toHaveBeenCalledTimes(1);
    view.unmount();
  });

  it("FUI[account.notifications:edge] keeps an all-off row and a long name readable", () => {
    const copy = notificationsCopyFixture("vi");
    const { view } = renderNotifications(notificationsEdgeDataScenario());
    expect(cell(copy, "assigned", "email").getAttribute("aria-checked")).toBe(
      "false",
    );
    expect(document.body.textContent).toContain("Nguyễn Thị Phương Anh");
    view.unmount();
  });

  it("FUI[account.notifications:copy] leaves no English literal in the Vietnamese page", () => {
    const { view } = renderNotifications(notificationsSavedScenario("vi"));
    const text = document.body.textContent ?? "";
    for (const literal of [
      en.title,
      en.when,
      en.channels.inApp,
      en.events.assigned,
      en.save,
      en.saved,
    ])
      expect(text).not.toContain(literal);
    view.unmount();
  });
});
