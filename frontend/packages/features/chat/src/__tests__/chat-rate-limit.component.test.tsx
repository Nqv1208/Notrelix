import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { act, fireEvent, render, screen } from "@notrelix/testing";
import { ChatRateLimitNotice } from "../web/components/chat-rate-limit";
import { ChatComposer } from "../web/components/chat-composer";
import { ChatSidebar } from "../web/components/chat-sidebar";
import { RenameChannelDialog } from "../web/components/chat-dialogs";
import { en } from "../web/components/chat.copy";

describe("ChatRateLimitNotice", () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it("counts down, announces the wait once, and offers the retry when it is over", () => {
    const onRetry = vi.fn();
    render(
      <ChatRateLimitNotice
        seconds={3}
        limit="rate-limited"
        copy={en}
        onRetry={onRetry}
      />,
    );
    expect(screen.getByRole("status")).not.toBeNull();
    expect(screen.getByTestId("chat-rate-limit-countdown").textContent).toBe(
      "You are sending too fast. Try again in 3 s.",
    );
    expect(screen.queryByRole("button", { name: "Send again" })).toBeNull();
    act(() => void vi.advanceTimersByTime(1000));
    expect(
      screen.getByTestId("chat-rate-limit-countdown").textContent,
    ).toContain("2 s");
    act(() => void vi.advanceTimersByTime(2000));
    expect(screen.getByText(en.rateLimit.ready)).not.toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Send again" }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it("says slow mode for a slow-mode answer", () => {
    render(
      <ChatRateLimitNotice
        seconds={60}
        limit="slow-mode"
        copy={en}
        onRetry={vi.fn()}
      />,
    );
    expect(screen.getByTestId("chat-rate-limit-countdown").textContent).toBe(
      "Slow mode is on. You can write again in 1 min.",
    );
  });
});

describe("slow mode on the composer and the settings", () => {
  it("shows the slow-mode hint on the composer only when it is on, and no keyboard hint", () => {
    const base = {
      channelName: "general",
      value: "",
      disabledReason: null,
      copy: en,
      onChange: vi.fn(),
      onSend: vi.fn(),
    };
    const { rerender } = render(<ChatComposer {...base} />);
    expect(screen.queryByText(en.composer.hint)).toBeNull();
    rerender(<ChatComposer {...base} slowModeSeconds={30} />);
    expect(
      screen.getByText("Slow mode: one message every 30 s."),
    ).not.toBeNull();
  });

  it("offers the slow-mode choice in the channel settings and submits it", () => {
    const onSubmit = vi.fn();
    render(
      <RenameChannelDialog
        open
        busy={false}
        initialName="general"
        initialTopic={null}
        initialSlowMode={0}
        error={null}
        copy={en}
        onOpenChange={vi.fn()}
        onSubmit={onSubmit}
      />,
    );
    const select = screen.getByLabelText("Slow mode") as HTMLSelectElement;
    expect(select.value).toBe("0");
    fireEvent.change(select, { target: { value: "30" } });
    fireEvent.click(screen.getByRole("button", { name: "Save changes" }));
    expect(onSubmit).toHaveBeenCalledWith({
      name: "general",
      topic: "",
      slowModeSeconds: 30,
    });
  });
});

describe("the sidebar of a Guest (UC-CHT-H02)", () => {
  it("says which channels appear and offers no create control", () => {
    render(
      <ChatSidebar
        channels={[]}
        status="ready"
        activeChannelId={null}
        unreadBySpace={new Map()}
        browse={null}
        canCreate={false}
        guest
        copy={en}
        onSelect={vi.fn()}
        onCreate={vi.fn()}
        onJoin={vi.fn()}
        onRetry={vi.fn()}
        joiningId={null}
      />,
    );
    expect(screen.getByText(en.sidebar.guestEmpty)).not.toBeNull();
    expect(
      screen.queryByRole("button", { name: en.sidebar.newChannel }),
    ).toBeNull();
  });
});
