import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  NOTIFICATION_DEFAULTS,
  notificationsCopyFixture,
  notificationsDefaultScenario,
  notificationsEdgeDataScenario,
  notificationsErrorScenario,
  notificationsLoadingScenario,
  notificationsSaveErrorScenario,
  notificationsSavedScenario,
  notificationsSavingScenario,
  type NotificationsScenario,
} from "../../../verification/account-notifications-fixtures";
import { en } from "../account-notifications.copy";
import {
  AccountNotificationsSurface,
  type AccountNotificationsSurfaceProps,
} from "../account-notifications-surface";

const LOCALES = ["vi", "en"] as const;

function renderNotifications(
  scenario: NotificationsScenario,
  overrides: Partial<AccountNotificationsSurfaceProps> = {},
) {
  const props: AccountNotificationsSurfaceProps = {
    ...scenario,
    onSave: vi.fn(),
    onRetry: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<AccountNotificationsSurface {...props} />);
  return { props, view };
}

function saveButton(copy: ReturnType<typeof notificationsCopyFixture>) {
  return screen.getByRole("button", {
    name: new RegExp(`^(${copy.save}|${copy.saving})$`),
  }) as HTMLButtonElement;
}

describe("AccountNotificationsSurface", () => {
  it("FUI[account.notifications:render] shows real switches named by their label and described by their explanation", () => {
    for (const locale of LOCALES) {
      const copy = notificationsCopyFixture(locale);
      const { view } = renderNotifications(
        notificationsDefaultScenario(locale),
      );

      expect(
        screen.getByRole("heading", { level: 1, name: copy.title }),
      ).toBeTruthy();
      const email = screen.getByRole("switch", {
        name: copy.channels.email.label,
      });
      const push = screen.getByRole("switch", {
        name: copy.channels.push.label,
      });
      // A native button, so Space and Enter activate it (proved in the e2e).
      expect(email.tagName).toBe("BUTTON");
      expect(email.getAttribute("aria-checked")).toBe("true");
      expect(push.getAttribute("aria-checked")).toBe("false");
      expect(
        document.getElementById(email.getAttribute("aria-describedby") ?? "")
          ?.textContent,
      ).toBe(copy.channels.email.description);
      expect(screen.getAllByRole("switch")).toHaveLength(2);
      view.unmount();
    }
  });

  it("FUI[account.notifications:dirty] keeps Save disabled until a switch changes", () => {
    for (const locale of LOCALES) {
      const copy = notificationsCopyFixture(locale);
      const { view } = renderNotifications(
        notificationsDefaultScenario(locale),
      );
      const save = saveButton(copy);
      expect(save.disabled).toBe(true);

      const push = screen.getByRole("switch", {
        name: copy.channels.push.label,
      });
      fireEvent.click(push);
      expect(push.getAttribute("aria-checked")).toBe("true");
      expect(save.disabled).toBe(false);

      fireEvent.click(push);
      expect(save.disabled).toBe(true);
      view.unmount();
    }
  });

  it("FUI[account.notifications:save] sends both channel values through the callback", () => {
    for (const locale of LOCALES) {
      const copy = notificationsCopyFixture(locale);
      const { props, view } = renderNotifications(
        notificationsDefaultScenario(locale),
      );
      fireEvent.click(
        screen.getByRole("switch", { name: copy.channels.email.label }),
      );
      fireEvent.click(saveButton(copy));
      expect(props.onSave).toHaveBeenCalledTimes(1);
      expect(props.onSave).toHaveBeenCalledWith({
        emailEnabled: false,
        pushEnabled: false,
      });
      view.unmount();
    }
  });

  it("FUI[account.notifications:saving] disables the switches and names the pending save", () => {
    for (const locale of LOCALES) {
      const copy = notificationsCopyFixture(locale);
      const { view } = renderNotifications(notificationsSavingScenario(locale));
      expect(
        (
          screen.getByRole("button", {
            name: copy.saving,
          }) as HTMLButtonElement
        ).disabled,
      ).toBe(true);
      for (const switchControl of screen.getAllByRole("switch")) {
        expect((switchControl as HTMLButtonElement).disabled).toBe(true);
      }
      view.unmount();
    }
  });

  it("FUI[account.notifications:saved] confirms the save and announces a failed one", () => {
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

  it("FUI[account.notifications:loading] shows a named loading state without switches", () => {
    for (const locale of LOCALES) {
      const copy = notificationsCopyFixture(locale);
      const { view } = renderNotifications(
        notificationsLoadingScenario(locale),
      );
      expect(screen.getByRole("status", { name: copy.loading })).toBeTruthy();
      expect(screen.queryByRole("switch")).toBeNull();
      view.unmount();
    }
  });

  it("FUI[account.notifications:error] explains the failed load, offers a retry and still shows the defaults", () => {
    for (const locale of LOCALES) {
      const copy = notificationsCopyFixture(locale);
      const { props, view } = renderNotifications(
        notificationsErrorScenario(locale),
      );
      const alert = screen.getByRole("alert");
      expect(alert.textContent).toContain(copy.loadError);
      fireEvent.click(within(alert).getByRole("button", { name: copy.retry }));
      expect(props.onRetry).toHaveBeenCalledTimes(1);
      expect(
        screen
          .getByRole("switch", { name: copy.channels.push.label })
          .getAttribute("aria-checked"),
      ).toBe(String(NOTIFICATION_DEFAULTS.pushEnabled));
      view.unmount();
    }
  });

  it("FUI[account.notifications:edge] shows every channel off in Vietnamese after a failed save", () => {
    const copy = notificationsCopyFixture("vi");
    const { view } = renderNotifications(notificationsEdgeDataScenario());
    for (const switchControl of screen.getAllByRole("switch")) {
      expect(switchControl.getAttribute("aria-checked")).toBe("false");
    }
    expect(screen.getByRole("alert").textContent).toContain(copy.saveError);
    view.unmount();
  });

  it("FUI[account.notifications:copy] leaves no English literal in the Vietnamese page", () => {
    const { view } = renderNotifications(notificationsSaveErrorScenario("vi"));
    const text = document.body.textContent ?? "";
    for (const literal of [
      en.title,
      en.description,
      en.channels.email.label,
      en.channels.push.label,
      en.channels.push.description,
      en.save,
      en.saveError,
    ]) {
      expect(text).not.toContain(literal);
    }
    view.unmount();
  });
});
