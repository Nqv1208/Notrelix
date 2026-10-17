/**
 * Deterministic fixtures for the frame of the account pages.
 */
import { accountNavCopy } from "../web/nav/account-nav.copy";
import type { AccountFrameProps, AccountTab } from "../web/frame/account-frame";

export function frameCopyFixture(locale: "en" | "vi" = "en") {
  return accountNavCopy(locale);
}

const TABS: readonly AccountTab[] = [
  "profile",
  "appearance",
  "security",
  "notifications",
  "workspaces",
];

export function frameLinksFixture(
  onNavigate: () => void = () => undefined,
): AccountFrameProps["links"] {
  return Object.fromEntries(
    TABS.map((tab) => [tab, { href: `/account/${tab}`, onNavigate }]),
  ) as AccountFrameProps["links"];
}

export function frameDefaultScenario(
  locale: "en" | "vi" = "en",
): Omit<AccountFrameProps, "children"> {
  return {
    copy: frameCopyFixture(locale),
    identity: {
      name: "Nguyễn Thu Hà",
      email: "thuha.nguyen@example.com",
      avatarUrl: null,
    },
    active: "appearance",
    links: frameLinksFixture(),
  };
}

/** A long Vietnamese name and address: the head truncates, nothing wraps out. */
export function frameEdgeDataScenario(): Omit<AccountFrameProps, "children"> {
  return {
    ...frameDefaultScenario("vi"),
    identity: {
      name: "Nguyễn Thị Phương Anh Trần Hoàng Bảo Ngọc Lê Văn Đức Minh",
      email:
        "nguyen.thi.phuong.anh.tran.hoang.bao.ngoc@phong-ke-hoach-van-hanh-khu-vuc-dong-nam-a.example.com",
      avatarUrl: null,
    },
  };
}
