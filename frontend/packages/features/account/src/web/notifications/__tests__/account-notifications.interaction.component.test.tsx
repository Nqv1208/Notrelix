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
      expect(within(table).getAllByRole("checkbox")).toHaveLength(21);
      expect(cell(copy, "assigned", "app").getAttribute("aria-checked")).toBe(
        "true",
      );
      expect(cell(copy, "overdue", "mobile").getAttribute("aria-checked")).toBe(
        "false",
      );
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
    const digestApp = cell(copy, "digest", "app");
    fireEvent.click(digestApp);
    expect(digestApp.getAttribute("aria-checked")).toBe("true");
    fireEvent.click(digestApp);
    expect(digestApp.getAttribute("aria-checked")).toBe("false");
    view.unmount();
  });

  it("FUI[account.notifications:quiet] switches quiet hours and edits their window", () => {
    const copy = notificationsCopyFixture("en");
    const { props, view } = renderNotifications(
      notificationsDefaultScenario("en"),
    );
    const from = screen.getByLabelText(copy.quiet.from) as HTMLSelectElement;
    expect(from.value).toBe("21:00");
    fireEvent.change(from, { target: { value: "22:00" } });
    fireEvent.change(screen.getByLabelText(copy.quiet.days), {
      target: { value: "everyday" },
    });
    fireEvent.click(screen.getByRole("switch", { name: copy.quiet.title }));
    expect(from.disabled).toBe(true);
    fireEvent.click(screen.getByRole("button", { name: copy.save }));
    expect(props.onSave).toHaveBeenCalledWith({
      ...notificationValuesFixture(),
      quietHours: {
        enabled: false,
        from: "22:00",
        to: "07:00",
        days: "everyday",
      },
    });
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
      expect(saved?.rows.watching).toEqual({
        app: true,
        email: true,
        mobile: false,
      });
      expect(saved?.rows.assigned).toEqual({
        app: true,
        email: true,
        mobile: true,
      });
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
    expect((cell(copy, "assigned", "app") as HTMLButtonElement).disabled).toBe(
      true,
    );
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

  it("FUI[account.notifications:edge] keeps an all-off row, quiet hours off and a long name readable", () => {
    const copy = notificationsCopyFixture("vi");
    const { view } = renderNotifications(notificationsEdgeDataScenario());
    expect(cell(copy, "assigned", "email").getAttribute("aria-checked")).toBe(
      "false",
    );
    expect(
      screen
        .getByRole("switch", { name: copy.quiet.title })
        .getAttribute("aria-checked"),
    ).toBe("false");
    expect(document.body.textContent).toContain("Nguyễn Thị Phương Anh");
    view.unmount();
  });

  it("FUI[account.notifications:copy] leaves no English literal in the Vietnamese page", () => {
    const { view } = renderNotifications(notificationsSavedScenario("vi"));
    const text = document.body.textContent ?? "";
    for (const literal of [
      en.title,
      en.when,
      en.channels.app,
      en.events.assigned,
      en.quiet.title,
      en.save,
      en.saved,
    ])
      expect(text).not.toContain(literal);
    view.unmount();
  });
});
