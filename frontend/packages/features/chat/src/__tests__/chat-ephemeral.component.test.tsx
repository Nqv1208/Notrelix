import { act } from "react";
import { describe, expect, it, vi } from "vitest";
import { render, renderHook, screen } from "@notrelix/testing";
import { ChatTypingIndicator } from "../web/components/chat-typing-indicator";
import { PresenceDot } from "../web/components/chat-presence";
import { ChatComposer } from "../web/components/chat-composer";
import { fireEvent } from "@notrelix/testing";
import { en } from "../web/components/chat.copy";
import {
  useChatEphemeral,
  useTypingSignal,
} from "../web/realtime/use-ephemeral";
import type {
  ChatEphemeralEvent,
  ChatEphemeralPort,
} from "../core/model/ephemeral-model";

function fakePort() {
  const listeners = new Map<string, Set<(e: ChatEphemeralEvent) => void>>();
  const port: ChatEphemeralPort = {
    subscribe: vi.fn((spaceId, listener) => {
      const set = listeners.get(spaceId) ?? new Set();
      set.add(listener);
      listeners.set(spaceId, set);
      return () => set.delete(listener);
    }),
    sendTyping: vi.fn(),
    sendPresence: vi.fn(),
  };
  return {
    port,
    emit: (spaceId: string, event: ChatEphemeralEvent) =>
      listeners.get(spaceId)?.forEach((l) => l(event)),
    listenerCount: (spaceId: string) => listeners.get(spaceId)?.size ?? 0,
  };
}

describe("typing indicator", () => {
  it("is a polite, atomic status region that is always present and empty without typing", () => {
    const { rerender } = render(<ChatTypingIndicator names={[]} copy={en} />);
    const region = screen.getByRole("status");
    expect(region.getAttribute("aria-live")).toBe("polite");
    expect(region.getAttribute("aria-atomic")).toBe("true");
    expect(region.textContent).toBe("");

    rerender(<ChatTypingIndicator names={["Ann"]} copy={en} />);
    expect(screen.getByRole("status").textContent).toBe("Ann is typing…");
    rerender(<ChatTypingIndicator names={["Ann", "Bob"]} copy={en} />);
    expect(screen.getByRole("status").textContent).toBe(
      "Ann and Bob are typing…",
    );
  });
});

describe("presence dot", () => {
  it("is decorative and tells the state by data and shape", () => {
    const { container } = render(<PresenceDot state="idle" />);
    const dot = container.querySelector("[data-presence]")!;
    expect(dot.getAttribute("aria-hidden")).toBe("true");
    expect(dot.getAttribute("data-presence")).toBe("idle");
  });
});

describe("composer typing signal", () => {
  it("calls onTyping for a non-empty draft only", () => {
    const onTyping = vi.fn();
    render(
      <ChatComposer
        channelName="general"
        value=""
        disabledReason={null}
        copy={en}
        onChange={() => {}}
        onSend={() => {}}
        onTyping={onTyping}
      />,
    );
    const box = screen.getByRole("textbox", { name: "Message #general" });
    fireEvent.change(box, { target: { value: "  " } });
    expect(onTyping).not.toHaveBeenCalled();
    fireEvent.change(box, { target: { value: "hi" } });
    expect(onTyping).toHaveBeenCalledTimes(1);
  });
});

describe("useTypingSignal", () => {
  it("sends at most once per 3 seconds per space", () => {
    vi.useFakeTimers();
    try {
      vi.setSystemTime(new Date("2026-01-01T00:00:00Z"));
      const { port } = fakePort();
      const { result } = renderHook(() => useTypingSignal(port, "space-1"));
      act(() => {
        result.current();
        result.current();
      });
      expect(port.sendTyping).toHaveBeenCalledTimes(1);
      vi.setSystemTime(new Date("2026-01-01T00:00:02Z"));
      act(() => result.current());
      expect(port.sendTyping).toHaveBeenCalledTimes(1);
      vi.setSystemTime(new Date("2026-01-01T00:00:03.100Z"));
      act(() => result.current());
      expect(port.sendTyping).toHaveBeenCalledTimes(2);
      expect(port.sendTyping).toHaveBeenLastCalledWith("space-1");
    } finally {
      vi.useRealTimers();
    }
  });

  it("sends nothing without a port or a space", () => {
    const { result } = renderHook(() => useTypingSignal(undefined, "s"));
    expect(() => result.current()).not.toThrow();
  });
});

describe("useChatEphemeral", () => {
  it("listens to the spaces given, feeds the stores and lets go on unmount", () => {
    const fake = fakePort();
    const { result, unmount } = renderHook(() =>
      useChatEphemeral(fake.port, ["a", "b"]),
    );
    expect(fake.listenerCount("a")).toBe(1);
    expect(fake.listenerCount("b")).toBe(1);

    act(() => {
      fake.emit("a", {
        kind: "typing",
        channelId: "a",
        userId: "ann",
        expiresInMs: 5000,
      });
      fake.emit("b", {
        kind: "presence",
        channelId: "b",
        userId: "bob",
        state: "online",
        ttlMs: 75000,
      });
    });
    expect(result.current.typing.typingIn("a")).toEqual(["ann"]);
    expect(result.current.presence.stateOf("bob")).toBe("online");

    unmount();
    expect(fake.listenerCount("a")).toBe(0);
    expect(fake.listenerCount("b")).toBe(0);
  });

  it("does nothing without a port", () => {
    const { result } = renderHook(() => useChatEphemeral(undefined, ["a"]));
    expect(result.current.typing.size()).toBe(0);
  });
});
