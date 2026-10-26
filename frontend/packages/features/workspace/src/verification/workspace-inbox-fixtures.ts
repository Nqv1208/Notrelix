/**
 * Deterministic fixtures and scenarios for the Inbox (Final-Inbox), at the
 * mockup's clock (Monday 12 October 2026, 10:40 in the member's calendar).
 * No randomness, no network.
 */
import type { InboxItemView } from "../web/inbox/inbox-models";
import type { SectionState } from "../web/home/home-models";
import type { WorkspaceInboxSurfaceProps } from "../web/inbox/workspace-inbox-surface";
import {
  workspaceInboxCopy,
  type WorkspaceInboxCopy,
} from "../web/inbox/workspace-inbox.copy";
import { HOME_MOCKUP_NOW } from "./workspace-home-fixtures";

export function inboxCopyFixture(
  locale: "en" | "vi" = "vi",
): WorkspaceInboxCopy {
  return workspaceInboxCopy(locale);
}

const MIN = 60 * 1000;
const at = (offsetMs: number) =>
  new Date(HOME_MOCKUP_NOW + offsetMs).toISOString();
const clock = (day: number, hour: number, minute: number) =>
  new Date(2026, 9, day, hour, minute).toISOString();

const CN = { name: "Công nghệ", markTone: "blue" } as const;
const KT = { name: "Kế toán", markTone: "gold" } as const;
const NS = { name: "Nhân sự", markTone: "sage" } as const;

/** The three notifications of Final-Inbox `binh-thuong` (the need tab). */
export function inboxNeedItems(): InboxItemView[] {
  return [
    {
      id: "n-sso",
      unread: true,
      at: at(-12 * MIN),
      kind: "approve",
      person: { name: "Trần Minh Quân", tone: "blue" },
      segments: [
        { text: "Trần Minh Quân", strong: true },
        { text: " gửi bạn duyệt " },
        {
          text: "Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
          strong: true,
        },
      ],
      context: { department: CN, label: "Triển khai ERP Q4 · ERP-138" },
      need: "approve",
      following: false,
      bundle: null,
      href: "#n-sso",
      detail: {
        crumb: { department: CN, board: "Triển khai ERP Q4", key: "ERP-138" },
        event: {
          person: { name: "Trần Minh Quân", tone: "blue" },
          segments: [
            { text: "Trần Minh Quân", strong: true },
            { text: " gửi bạn duyệt cấu hình đăng nhập một lần." },
          ],
          when: "Hôm nay 10:28 · bạn là người duyệt duy nhất · cần duyệt trước thứ Tư 14/10",
        },
        card: {
          title: "Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
          status: { bucket: "review", label: "Chờ duyệt" },
          priority: "Cao",
          due: "14/10",
          comments: 4,
          changes: [
            {
              label: "Nhà cung cấp",
              value: "Microsoft Entra ID",
              changed: "Microsoft Entra ID",
            },
            {
              label: "Áp dụng cho",
              value: "Tất cả phòng ban, trừ khách bên ngoài",
              changed: "Tất cả phòng ban",
            },
            {
              label: "Bắt buộc từ",
              value: "Thứ Năm 15/10",
              changed: "Thứ Năm 15/10",
            },
            { label: "Đăng nhập bằng mật khẩu", value: "Tắt sau 30 ngày" },
          ],
          approvable: true,
        },
        comment: {
          person: { name: "Trần Minh Quân", tone: "blue" },
          time: "10:28",
          text: "Đã thử với 5 tài khoản phòng Kế toán, đăng nhập ổn. Chị duyệt giúp trước thứ Tư để kịp thông báo toàn công ty nhé.",
        },
        replyTo: "Trần Minh Quân",
        openHref: "#board",
      },
    },
    {
      id: "n-invoice",
      unread: true,
      at: at(-60 * MIN),
      kind: "mention",
      person: { name: "Lý Thu Trang", tone: "coral" },
      segments: [
        { text: "Lý Thu Trang", strong: true },
        { text: " nhắc tên bạn trong " },
        { text: "Quy trình nhập hoá đơn đầu vào", strong: true },
        { text: ": “chị xem giúp đề xuất 3 ngày đối chiếu…”" },
      ],
      context: { department: KT, label: "Tài liệu · Kế toán" },
      need: "reply",
      following: false,
      bundle: null,
      href: "#n-invoice",
      detail: {
        crumb: {
          department: KT,
          board: "Quy trình nhập hoá đơn đầu vào",
          key: null,
        },
        event: {
          person: { name: "Lý Thu Trang", tone: "coral" },
          segments: [
            { text: "Lý Thu Trang", strong: true },
            { text: " nhắc tên bạn trong " },
            { text: "Quy trình nhập hoá đơn đầu vào", strong: true },
            { text: "." },
          ],
          when: "Hôm nay 09:40 · cần bạn trả lời",
        },
        card: null,
        comment: {
          person: { name: "Lý Thu Trang", tone: "coral" },
          time: "09:40",
          text: "@Hà chị xem giúp đề xuất 3 ngày đối chiếu cuối tháng trong quy trình nhập hoá đơn.",
        },
        replyTo: "Lý Thu Trang",
        openHref: "#doc",
      },
    },
    {
      id: "n-overdue",
      unread: true,
      at: clock(12, 8, 0),
      kind: "overdue",
      person: null,
      segments: [
        {
          text: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
          strong: true,
        },
        { text: " đã quá hạn 2 ngày. Bạn đang theo dõi việc này." },
      ],
      context: {
        department: CN,
        label: "Triển khai ERP Q4 · ERP-142 · Trần Minh Quân phụ trách",
      },
      need: "overdue",
      following: true,
      bundle: null,
      href: "#n-overdue",
      detail: {
        crumb: { department: CN, board: "Triển khai ERP Q4", key: "ERP-142" },
        event: {
          person: null,
          segments: [
            {
              text: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
              strong: true,
            },
            { text: " đã quá hạn 2 ngày." },
          ],
          when: "Hôm nay 08:00 · bạn đang theo dõi việc này",
        },
        card: null,
        comment: null,
        replyTo: null,
        openHref: "#board",
      },
    },
  ];
}

/** The read notifications Final-Inbox `tat-ca` adds. */
export function inboxReadItems(): InboxItemView[] {
  return [
    {
      id: "n-assign",
      unread: false,
      at: clock(12, 7, 45),
      kind: "assign",
      person: { name: "Đỗ Khánh Linh", tone: "gold" },
      segments: [
        { text: "Đỗ Khánh Linh", strong: true },
        { text: " giao cho bạn " },
        { text: "Đối chiếu sao kê ngân hàng tháng 9", strong: true },
        { text: ", hạn hôm nay 17:00" },
      ],
      context: { department: KT, label: "Đóng sổ tháng 10 · KT-88" },
      need: null,
      following: false,
      bundle: null,
      href: "#n-assign",
      detail: null,
    },
    {
      id: "n-bundle",
      unread: false,
      at: clock(11, 17, 10),
      kind: "change",
      person: null,
      avatarText: "+4",
      segments: [
        { text: "6 thay đổi", strong: true },
        { text: " trong " },
        { text: "Triển khai ERP Q4", strong: true },
        { text: " từ Lê Hoàng Phúc, Vũ Thanh và 2 người khác" },
      ],
      context: {
        department: null,
        label: "Đổi trạng thái 3 việc · 2 bình luận · đổi hạn 1 việc",
      },
      need: null,
      following: false,
      bundle: { count: 6, summary: "" },
      href: null,
      detail: null,
    },
    {
      id: "n-invite",
      unread: false,
      at: clock(11, 10, 20),
      kind: "system",
      person: { name: "Phạm Thu Hằng", tone: "sage" },
      segments: [
        { text: "Phạm Thu Hằng", strong: true },
        { text: " mời bạn vào board " },
        { text: "Tuyển dụng Q4", strong: true },
        { text: " với quyền Chỉ xem" },
      ],
      context: { department: NS, label: "Phòng Nhân sự · board riêng tư" },
      need: null,
      following: false,
      bundle: null,
      href: "#n-invite",
      detail: null,
    },
  ];
}

type InboxScenario = Omit<
  WorkspaceInboxSurfaceProps,
  "onMarkRead" | "onMarkAllRead" | "onOpenSettings" | "onRetry"
>;

const ALL_TABS = { need: true, mention: true, assigned: true, follow: true };

/** Final-Inbox `binh-thuong`: the need tab, unread only, the approval open. */
export function inboxDefaultScenario(
  locale: "en" | "vi" = "vi",
): InboxScenario {
  return {
    copy: inboxCopyFixture(locale),
    now: HOME_MOCKUP_NOW,
    state: {
      status: "ready",
      data: [...inboxNeedItems(), ...inboxReadItems()],
    },
    scope: { workspaceName: "Minh An Group", allHref: "#all-workspaces" },
    tabsAvailable: ALL_TABS,
    settingsHref: "#settings",
  };
}

/** Final-Inbox `tat-ca`: every notification, read ones included. */
export function inboxAllScenario(): InboxScenario {
  return {
    ...inboxDefaultScenario(),
    initialView: { tab: "all", unreadOnly: false },
  };
}

/** Final-Inbox `het-viec`: nothing left to handle. */
export function inboxEmptyScenario(): InboxScenario {
  return {
    ...inboxDefaultScenario(),
    state: { status: "ready", data: [] },
    counts: { need: 0, mention: 1, assigned: 1 },
  };
}

export function inboxLoadingScenario(): InboxScenario {
  return { ...inboxDefaultScenario(), state: { status: "loading" } };
}

export function inboxErrorScenario(): InboxScenario {
  return { ...inboxDefaultScenario(), state: { status: "error" } };
}

/** What the host fills today: titles and messages only, no type or person. */
export function inboxProducedOnlyScenario(): InboxScenario {
  const plain = (
    id: string,
    unread: boolean,
    offset: number,
    title: string,
    message: string,
  ): InboxItemView => ({
    id,
    unread,
    at: at(offset),
    kind: "system",
    person: null,
    segments: [{ text: title, strong: true }, { text: ` ${message}` }],
    context: null,
    need: null,
    following: false,
    bundle: null,
    href: null,
    detail: {
      crumb: null,
      event: {
        person: null,
        segments: [{ text: title, strong: true }, { text: ` ${message}` }],
        when: "Hôm nay",
      },
      card: null,
      comment: null,
      replyTo: null,
      openHref: null,
    },
  });
  const data: SectionState<readonly InboxItemView[]> = {
    status: "ready",
    data: [
      plain(
        "p-1",
        true,
        -10 * MIN,
        "Notification 1",
        "You were mentioned in a card (notification 1).",
      ),
      plain(
        "p-2",
        false,
        -25 * 60 * MIN,
        "Notification 0",
        "You were mentioned in a card (notification 0).",
      ),
    ],
  };
  return {
    ...inboxDefaultScenario(),
    state: data,
    scope: null,
    tabsAvailable: {},
    settingsHref: null,
  };
}

/** Long names and text, a notification with no context, many rows. */
export function inboxEdgeDataScenario(): InboxScenario {
  const long: InboxItemView = {
    ...inboxNeedItems()[0]!,
    id: "n-long",
    segments: [
      { text: "Nguyễn Thị Phương Thảo Nguyên", strong: true },
      { text: " gửi bạn duyệt " },
      {
        text: "Thiết lập đăng nhập một lần (SSO) và chính sách mật khẩu cho toàn bộ công ty, kể cả khách bên ngoài và nhà thầu",
        strong: true,
      },
    ],
    context: {
      department: CN,
      label:
        "Triển khai ERP Q4 · ERP-138 · Trần Minh Quân phụ trách · Phòng Công nghệ · board riêng tư",
    },
  };
  return {
    ...inboxDefaultScenario(),
    state: { status: "ready", data: [long] },
  };
}

/** 150 notifications across two days. */
export function inboxHighDensityScenario(): InboxScenario {
  const items: InboxItemView[] = Array.from({ length: 150 }, (_, index) => ({
    id: `n-dense-${index}`,
    unread: index % 4 === 0,
    at: at(-index * 25 * MIN),
    kind: "system",
    person: { name: "Lê Hoàng Phúc", tone: "ember" },
    segments: [
      { text: "Lê Hoàng Phúc", strong: true },
      { text: ` cập nhật việc số ${index + 1}` },
    ],
    context: {
      department: CN,
      label: `Triển khai ERP Q4 · ERP-${200 + index}`,
    },
    need: null,
    following: false,
    bundle: null,
    href: null,
    detail: null,
  }));
  return {
    ...inboxDefaultScenario(),
    state: { status: "ready", data: items },
    initialView: { tab: "all", unreadOnly: false },
  };
}

/** The Done filter on: the entries the member finished, each with "move back to inbox". */
export function inboxDoneScenario(): InboxScenario {
  const done = inboxReadItems()
    .slice(0, 2)
    .map((item) => ({ ...item, done: true, unread: false }));
  return {
    ...inboxDefaultScenario(),
    state: { status: "ready", data: done },
    done: { active: true, onToggle: () => undefined },
    initialView: { tab: "all", unreadOnly: false },
  };
}
