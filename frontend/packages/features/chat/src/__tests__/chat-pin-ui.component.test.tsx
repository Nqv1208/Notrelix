import { afterEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, within } from "@notrelix/testing";
import { en, vi as viCopy } from "../web/components/chat.copy";
import {
  PinLimitBanner,
  PinRowMenu,
  UnpinDialog,
} from "../web/components/chat-pin-ui";
import { ThreadMoreMenu } from "../web/components/chat-thread-menu";

const originalMatchMedia = Object.getOwnPropertyDescriptor(
  window,
  "matchMedia",
);

/** A phone viewport: useIsMobile reads innerWidth and listens on matchMedia. */
function phone() {
  Object.defineProperty(window, "innerWidth", {
    configurable: true,
    value: 390,
  });
  Object.defineProperty(window, "matchMedia", {
    configurable: true,
    value: () => ({
      matches: true,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
    }),
  });
}

afterEach(() => {
  Object.defineProperty(window, "innerWidth", {
    configurable: true,
    value: 1024,
  });
  if (originalMatchMedia) {
    Object.defineProperty(window, "matchMedia", originalMatchMedia);
  } else {
    Reflect.deleteProperty(window, "matchMedia");
  }
});

function unpinDialog(overrides: Partial<Parameters<typeof UnpinDialog>[0]>) {
  return (
    <UnpinDialog
      open
      copy={en}
      actorName="Thu Ha"
      busy={false}
      failed={false}
      onOpenChange={vi.fn()}
      onConfirm={vi.fn()}
      {...overrides}
    />
  );
}

describe("UnpinDialog", () => {
  it("asks first, says what everyone will see, and confirms or cancels", () => {
    const onConfirm = vi.fn();
    const onOpenChange = vi.fn();
    render(unpinDialog({ onConfirm, onOpenChange }));
    const dialog = screen.getByRole("dialog", { name: "Unpin this message?" });
    expect(dialog.textContent).toContain(
      "Everyone will see the line “Thu Ha unpinned a message”.",
    );
    fireEvent.click(within(dialog).getByRole("button", { name: "Unpin" }));
    expect(onConfirm).toHaveBeenCalledTimes(1);
    fireEvent.click(within(dialog).getByRole("button", { name: "Cancel" }));
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it("disables the confirm while the request runs and says when it failed", () => {
    const { rerender } = render(unpinDialog({ busy: true }));
    expect(
      (screen.getByRole("button", { name: "Unpin" }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    expect(screen.queryByRole("alert")).toBeNull();
    rerender(unpinDialog({ failed: true }));
    expect(screen.getByRole("alert").textContent).toBe(en.pins.unpinRetry);
    expect(
      (screen.getByRole("button", { name: "Unpin" }) as HTMLButtonElement)
        .disabled,
    ).toBe(false);
  });

  it("reads in Vietnamese through the copy files", () => {
    render(unpinDialog({ copy: viCopy, actorName: "Thu Hà" }));
    const dialog = screen.getByRole("dialog", {
      name: "Bỏ ghim tin nhắn này?",
    });
    expect(dialog.textContent).toContain(
      "Mọi người sẽ thấy dòng “Thu Hà đã bỏ ghim một tin nhắn”.",
    );
    expect(
      within(dialog).getByRole("button", { name: "Bỏ ghim" }),
    ).toBeTruthy();
  });

  it("is a bottom sheet with two stacked buttons on a phone", () => {
    phone();
    const onConfirm = vi.fn();
    render(unpinDialog({ onConfirm, copy: viCopy }));
    const sheet = screen.getByTestId("chat-unpin-sheet");
    const buttons = within(sheet).getAllByRole("button");
    expect(buttons.map((b) => b.textContent)).toEqual([
      "",
      "Bỏ ghim",
      viCopy.list.cancel,
    ]);
    fireEvent.click(within(sheet).getByRole("button", { name: "Bỏ ghim" }));
    expect(onConfirm).toHaveBeenCalledTimes(1);
  });
});

describe("PinRowMenu", () => {
  function menu(onUnpin?: () => void) {
    const handlers = {
      onOpenMessage: vi.fn(),
      onCopyLink: vi.fn(),
    };
    render(
      <PinRowMenu
        copy={en}
        snippet="Go-live plan"
        onUnpin={onUnpin}
        {...handlers}
      />,
    );
    return handlers;
  }
  const openMenu = () =>
    fireEvent.keyDown(
      screen.getByRole("button", { name: "More: Go-live plan" }),
      {
        key: "Enter",
      },
    );

  it("offers Open message, Copy link and a red Unpin after a rule", async () => {
    const onUnpin = vi.fn();
    const handlers = menu(onUnpin);
    openMenu();
    const items = await screen.findAllByRole("menuitem");
    expect(items.map((item) => item.textContent)).toEqual([
      "Open message",
      "Copy link",
      "Unpin",
    ]);
    expect(items[2]!.getAttribute("data-variant")).toBe("destructive");
    fireEvent.click(items[0]!);
    expect(handlers.onOpenMessage).toHaveBeenCalledTimes(1);
    openMenu();
    fireEvent.click((await screen.findAllByRole("menuitem"))[1]!);
    expect(handlers.onCopyLink).toHaveBeenCalledTimes(1);
    openMenu();
    fireEvent.click((await screen.findAllByRole("menuitem"))[2]!);
    expect(onUnpin).toHaveBeenCalledTimes(1);
  });

  it("leaves Unpin out when the caller may not unpin", async () => {
    menu(undefined);
    openMenu();
    const items = await screen.findAllByRole("menuitem");
    expect(items.map((item) => item.textContent)).toEqual([
      "Open message",
      "Copy link",
    ]);
  });

  it("is a bottom sheet titled Pinned messages on a phone", () => {
    phone();
    const onUnpin = vi.fn();
    menu(onUnpin);
    fireEvent.click(screen.getByRole("button", { name: "More: Go-live plan" }));
    const sheet = screen.getByTestId("chat-pin-sheet");
    expect(sheet.textContent).toContain("Pinned messages");
    fireEvent.click(within(sheet).getByRole("button", { name: "Unpin" }));
    expect(onUnpin).toHaveBeenCalledTimes(1);
  });
});

describe("PinLimitBanner", () => {
  it("says the limit was reached and opens the pins", () => {
    const onView = vi.fn();
    render(<PinLimitBanner copy={viCopy} onView={onView} />);
    const banner = screen.getByRole("alert");
    expect(banner.textContent).toContain(
      "Kênh đã đạt giới hạn tin ghim. Bỏ ghim một tin để ghim tin mới.",
    );
    fireEvent.click(
      within(banner).getByRole("button", { name: "Xem tin đã ghim" }),
    );
    expect(onView).toHaveBeenCalledTimes(1);
  });
});

describe("ThreadMoreMenu", () => {
  function threadMenu(following: boolean, muted = false, busy = false) {
    const handlers = {
      onToggleFollow: vi.fn(),
      onToggleMute: vi.fn(),
      onCopyLink: vi.fn(),
    };
    render(
      <ThreadMoreMenu
        copy={viCopy}
        following={following}
        muted={muted}
        busy={busy}
        {...handlers}
      />,
    );
    return handlers;
  }
  const open = () =>
    fireEvent.keyDown(screen.getByRole("button", { name: "Tuỳ chọn luồng" }), {
      key: "Enter",
    });

  it("unfollows, mutes and copies the link while following, in Vietnamese", async () => {
    const handlers = threadMenu(true);
    open();
    const items = await screen.findAllByRole("menuitem");
    expect(items.map((item) => item.textContent)).toEqual([
      "Tắt theo dõi luồng",
      "Tắt tiếng",
      "Sao chép liên kết",
    ]);
    fireEvent.click(items[0]!);
    expect(handlers.onToggleFollow).toHaveBeenCalledTimes(1);
  });

  it("offers Follow and no mute while not following", async () => {
    threadMenu(false);
    open();
    const items = await screen.findAllByRole("menuitem");
    expect(items.map((item) => item.textContent)).toEqual([
      "Theo dõi luồng",
      "Sao chép liên kết",
    ]);
  });

  it("holds the follow entries while a change is in flight but still copies the link", async () => {
    threadMenu(true, false, true);
    open();
    const items = await screen.findAllByRole("menuitem");
    expect(items[0]!.getAttribute("aria-disabled")).toBe("true");
    expect(items[2]!.getAttribute("aria-disabled")).toBeNull();
  });

  it("is a bottom sheet titled Thread on a phone", () => {
    phone();
    const handlers = threadMenu(true);
    fireEvent.click(screen.getByRole("button", { name: "Tuỳ chọn luồng" }));
    const sheet = screen.getByTestId("chat-thread-sheet");
    expect(sheet.textContent).toContain("Luồng trả lời");
    fireEvent.click(
      within(sheet).getByRole("button", { name: "Sao chép liên kết" }),
    );
    expect(handlers.onCopyLink).toHaveBeenCalledTimes(1);
  });
});
