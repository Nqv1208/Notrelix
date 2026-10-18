import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  inboxAllScenario,
  inboxCopyFixture,
  inboxDefaultScenario,
  inboxEdgeDataScenario,
  inboxEmptyScenario,
  inboxErrorScenario,
  inboxHighDensityScenario,
  inboxLoadingScenario,
  inboxProducedOnlyScenario,
} from "../../../verification/workspace-inbox-fixtures";
import {
  WorkspaceInboxSurface,
  type WorkspaceInboxSurfaceProps,
} from "../workspace-inbox-surface";

const copy = inboxCopyFixture("vi");

function renderInbox(
  scenario: ReturnType<typeof inboxDefaultScenario> = inboxDefaultScenario(),
  overrides: Partial<WorkspaceInboxSurfaceProps> = {},
) {
  const props: WorkspaceInboxSurfaceProps = {
    ...scenario,
    onMarkRead: vi.fn(),
    onMarkAllRead: vi.fn(),
    onOpenSettings: vi.fn(),
    onRetry: vi.fn(),
    ...overrides,
  };
  renderPureUi(<WorkspaceInboxSurface {...props} />);
  return props;
}

const rows = () => [
  ...document.querySelectorAll<HTMLElement>('[data-f="items"] [data-f="it"]'),
];

describe("WorkspaceInboxSurface", () => {
  it("FUI[workspace.inbox:render] draws the title, tabs with counts, the unread rows and the open one in the reading pane", () => {
    renderInbox();
    expect(
      screen.getByRole("heading", { level: 1, name: "Hộp thư" }),
    ).toBeTruthy();
    expect(
      screen.getByText("Chỉ workspace Minh An Group ·", { exact: false }),
    ).toBeTruthy();
    expect(screen.getAllByRole("tab").map((tab) => tab.textContent)).toEqual([
      "Cần xử lý3",
      "Nhắc tên1",
      "Được giao1",
      "Đang theo dõi",
      "Tất cả",
    ]);
    // The need tab with the unread toggle: the three unread need rows.
    expect(rows()).toHaveLength(3);
    expect(rows()[0]!.textContent).toContain("Chưa đọc. ");
    expect(rows()[0]!.textContent).toContain("Cần duyệt");
    expect(rows()[1]!.textContent).toContain("Cần trả lời");
    expect(rows()[2]!.textContent).toContain("Quá hạn");
    expect(rows()[0]!.textContent).toContain("12 phút");
    expect(rows()[1]!.textContent).toContain("1 giờ");
    expect(rows()[2]!.textContent).toContain("08:00");
    expect(rows()[0]!.getAttribute("aria-current")).toBe("true");

    const pane = screen.getByRole("main", { name: copy.read.label });
    expect(
      within(pane).getByRole("heading", {
        level: 2,
        name: "Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
      }),
    ).toBeTruthy();
    expect(pane.textContent).toContain("Microsoft Entra ID");
    expect(pane.textContent).toContain("Chờ duyệt");
    expect(pane.textContent).toContain("Ưu tiên Cao");
    expect(pane.textContent).toContain("· 4 bình luận");
    expect(
      within(pane).getByRole("link", { name: copy.read.openItem }),
    ).toBeTruthy();
  });

  it("FUI[workspace.inbox:days] groups the all tab by day with the day headers of the design", () => {
    renderInbox(inboxAllScenario());
    const days = [...document.querySelectorAll('[data-f="day"]')].map(
      (day) => day.textContent,
    );
    expect(days).toEqual(["Hôm nay", "Hôm qua"]);
    expect(rows()).toHaveLength(6);
    // A read row is quieter: no unread dot, no unread label.
    expect(rows()[3]!.textContent).not.toContain("Chưa đọc");
    expect(document.querySelectorAll('[data-f="ud"]')).toHaveLength(3);
    // The all tab shows the unread toggle off.
    expect(
      screen
        .getByRole("button", { name: copy.unreadOnly })
        .getAttribute("aria-pressed"),
    ).toBe("false");
  });

  it("FUI[workspace.inbox:tabs] filters by tab and keeps tabs with no data inert", () => {
    renderInbox();
    fireEvent.click(screen.getByRole("tab", { name: /^Nhắc tên/ }));
    expect(rows()).toHaveLength(1);
    expect(rows()[0]!.textContent).toContain("Lý Thu Trang");
    fireEvent.click(screen.getByRole("tab", { name: /^Được giao/ }));
    // Unread only is still on and the assignment was read.
    expect(screen.getByText(copy.empty.title)).toBeTruthy();
    fireEvent.click(screen.getByRole("tab", { name: /^Tất cả/ }));
    // Switching to All turns the toggle off.
    expect(rows().length).toBeGreaterThan(3);

    renderInbox(inboxProducedOnlyScenario());
    const inert = screen.getAllByRole("tab", { name: "Cần xử lý" })[0]!;
    expect(inert.getAttribute("aria-disabled")).toBe("true");
    expect(inert.getAttribute("title")).toBe(copy.tabUnavailable);
    fireEvent.click(inert);
    expect(inert.getAttribute("aria-selected")).toBe("false");
  });

  it("FUI[workspace.inbox:unread] toggles between unread only and everything", () => {
    renderInbox(inboxAllScenario());
    const toggle = screen.getByRole("button", { name: copy.unreadOnly });
    fireEvent.click(toggle);
    expect(toggle.getAttribute("aria-pressed")).toBe("true");
    expect(rows()).toHaveLength(3);
    fireEvent.click(toggle);
    expect(rows()).toHaveLength(6);
  });

  it("FUI[workspace.inbox:select] opens a notification in the pane, in place, and keeps a modified click a link", () => {
    renderInbox();
    const second = rows()[1]!;
    fireEvent.click(second, { button: 0 });
    expect(second.getAttribute("aria-current")).toBe("true");
    expect(rows()[0]!.getAttribute("aria-current")).toBeNull();
    const pane = screen.getByRole("main", { name: copy.read.label });
    expect(pane.textContent).toContain("Lý Thu Trang");
    expect(pane.textContent).toContain("cần bạn trả lời");
    expect(second.getAttribute("href")).toBe("#n-invoice");
  });

  it("FUI[workspace.inbox:move] moves with the previous and next buttons and with J and K", () => {
    renderInbox();
    const next = screen.getByRole("button", { name: copy.read.next });
    const previous = screen.getByRole("button", { name: copy.read.previous });
    expect(previous.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(next);
    expect(rows()[1]!.getAttribute("aria-current")).toBe("true");
    fireEvent.keyDown(window, { key: "j" });
    expect(rows()[2]!.getAttribute("aria-current")).toBe("true");
    expect(next.getAttribute("aria-disabled")).toBe("true");
    fireEvent.keyDown(window, { key: "k" });
    expect(rows()[1]!.getAttribute("aria-current")).toBe("true");
    fireEvent.click(previous);
    expect(rows()[0]!.getAttribute("aria-current")).toBe("true");
  });

  it("FUI[workspace.inbox:handled] marks the open notification handled with the button and with E, and not an already read one", () => {
    const props = renderInbox();
    fireEvent.click(screen.getByRole("button", { name: copy.read.handled }));
    expect(props.onMarkRead).toHaveBeenCalledWith("n-sso");
    fireEvent.keyDown(window, { key: "e" });
    expect(props.onMarkRead).toHaveBeenCalledTimes(2);
  });

  it("FUI[workspace.inbox:handled-inert] draws Đã xử lý inert when the host has no write or the notification is read", () => {
    renderInbox(inboxDefaultScenario(), { onMarkRead: null });
    const handled = screen.getByRole("button", { name: copy.read.handled });
    expect(handled.getAttribute("aria-disabled")).toBe("true");
    expect(handled.getAttribute("title")).toBe(copy.read.handledUnavailable);

    renderInbox(inboxAllScenario(), {
      initialView: { tab: "all", unreadOnly: false, selectedId: "n-assign" },
    });
    // The assignment is read: nothing to handle.
    const buttons = screen.getAllByRole("button", { name: copy.read.handled });
    expect(buttons[buttons.length - 1]!.getAttribute("aria-disabled")).toBe(
      "true",
    );
  });

  it("FUI[workspace.inbox:mark-all] marks every notification read through the host, and draws it inert without one", () => {
    const props = renderInbox();
    fireEvent.click(screen.getByRole("button", { name: copy.markAllRead }));
    expect(props.onMarkAllRead).toHaveBeenCalledTimes(1);

    renderInbox(inboxDefaultScenario(), { onMarkAllRead: null });
    const inert = screen.getAllByRole("button", { name: copy.markAllRead })[1]!;
    expect(inert.getAttribute("aria-disabled")).toBe("true");
  });

  it("FUI[workspace.inbox:bundle] opens a bundle row in place and never selects it", () => {
    renderInbox(inboxAllScenario());
    const bundle = screen.getByRole("button", { name: "Mở 6 thay đổi" });
    expect(bundle.getAttribute("aria-expanded")).toBe("false");
    fireEvent.click(bundle);
    expect(
      screen
        .getByRole("button", { name: "Ẩn 6 thay đổi" })
        .getAttribute("aria-expanded"),
    ).toBe("true");
    expect(
      bundle.closest('[data-f="it"]')!.getAttribute("aria-current"),
    ).toBeNull();
  });

  it("FUI[workspace.inbox:settings] opens notification settings or says they are not available", () => {
    const props = renderInbox();
    const link = screen.getByRole("link", { name: copy.settings });
    expect(link.getAttribute("href")).toBe("#settings");
    fireEvent.click(link, { button: 0 });
    expect(props.onOpenSettings).toHaveBeenCalledTimes(1);

    renderInbox(inboxProducedOnlyScenario());
    const inert = screen.getByRole("button", { name: copy.settings });
    expect(inert.getAttribute("aria-disabled")).toBe("true");
    expect(inert.getAttribute("title")).toBe(copy.settingsUnavailable);
  });

  it("FUI[workspace.inbox:empty] says nothing is left and empties the pane", () => {
    renderInbox(inboxEmptyScenario());
    expect(screen.getByText(copy.empty.title)).toBeTruthy();
    expect(screen.getByText(copy.empty.readTitle)).toBeTruthy();
    expect(screen.getAllByRole("tab").map((tab) => tab.textContent)).toEqual([
      "Cần xử lý0",
      "Nhắc tên1",
      "Được giao1",
      "Đang theo dõi",
      "Tất cả",
    ]);
    expect(
      screen.getByRole("button", { name: copy.empty.settings }),
    ).toBeTruthy();
  });

  it("FUI[workspace.inbox:filtered-empty] offers all notifications when everything unread is read", () => {
    const read = inboxProducedOnlyScenario();
    renderInbox(read, {
      state: {
        status: "ready",
        data: [
          ...(read.state.status === "ready" ? read.state.data : []).map(
            (item) => ({
              ...item,
              unread: false,
            }),
          ),
        ],
      },
      initialView: { tab: "all", unreadOnly: true },
    });
    expect(screen.getByText(copy.filteredEmpty.title)).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: copy.filteredEmpty.showAll }),
    );
    expect(rows()).toHaveLength(2);
  });

  it("FUI[workspace.inbox:no-notifications] says there is nothing yet when the list is empty on the all tab", () => {
    renderInbox({
      ...inboxProducedOnlyScenario(),
      state: { status: "ready", data: [] },
    });
    expect(screen.getByText(copy.allEmpty.title)).toBeTruthy();
  });

  it("FUI[workspace.inbox:loading] shows stable skeleton rows while loading", () => {
    renderInbox(inboxLoadingScenario());
    expect(screen.getByRole("status", { name: copy.loading })).toBeTruthy();
    expect(screen.queryByText("Trần Minh Quân")).toBeNull();
  });

  it("FUI[workspace.inbox:states] retries a failed load", () => {
    const props = renderInbox(inboxErrorScenario());
    expect(screen.getByRole("alert").textContent).toContain(copy.error);
    fireEvent.click(screen.getByRole("button", { name: copy.retry }));
    expect(props.onRetry).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.inbox:produced-only] draws only what the host fills in: no scope, person, context or card", () => {
    renderInbox(inboxProducedOnlyScenario());
    expect(screen.queryByText(/Chỉ workspace/)).toBeNull();
    expect(rows()).toHaveLength(2);
    expect(rows()[0]!.textContent).toContain("Notification 1");
    expect(document.querySelectorAll('[data-f="res"]')).toHaveLength(0);
    expect(document.querySelectorAll('[data-f="need"]')).toHaveLength(0);
    expect(document.querySelectorAll('[data-f="kd"]')).toHaveLength(0);
    expect(document.querySelectorAll('[data-f="card"]')).toHaveLength(0);
    // Without an item to open, "Mở công việc" says so.
    const open = screen.getByRole("button", { name: copy.read.openItem });
    expect(open.getAttribute("aria-disabled")).toBe("true");
    expect(open.getAttribute("title")).toBe(copy.read.openUnavailable);
    // Snooze and mute have no write.
    for (const name of [copy.read.snooze, copy.read.mute]) {
      expect(
        screen
          .getByRole("button", { name: new RegExp(name) })
          .getAttribute("aria-disabled"),
      ).toBe("true");
    }
  });

  it("FUI[workspace.inbox:edge] keeps long names and context readable", () => {
    renderInbox(inboxEdgeDataScenario());
    expect(rows()[0]!.textContent).toContain("Nguyễn Thị Phương Thảo Nguyên");
    expect(rows()[0]!.textContent).toContain(
      "Phòng Công nghệ · board riêng tư",
    );
  });

  it("FUI[workspace.inbox:high-density] renders 150 rows", () => {
    renderInbox(inboxHighDensityScenario());
    expect(rows()).toHaveLength(150);
  });
});
