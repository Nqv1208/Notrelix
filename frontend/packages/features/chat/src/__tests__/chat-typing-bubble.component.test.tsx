import { describe, expect, it } from "vitest";
import { render, screen } from "@notrelix/testing";
import { ChatTypingBubble } from "../web/components/chat-typing-indicator";
import { en } from "../web/components/chat.copy";

describe("ChatTypingBubble", () => {
  it("waves three dots in a staggered, soft loop instead of bouncing", () => {
    render(<ChatTypingBubble ids={["u1"]} authorName={() => "An"} copy={en} />);
    const dots = Array.from(
      screen
        .getByTestId("chat-typing-bubble")
        .querySelectorAll('span[class*="size-1.5"]'),
    );
    expect(dots).toHaveLength(3);
    for (const dot of dots) {
      expect(dot.className).not.toContain("animate-bounce");
      expect(dot.className).toContain("chat-typing-wave");
    }
    // The stagger is the .chat-typing-wave:nth-child rule in globals.css (checked in the browser by the mock e2e):
    // the dots must be the only children of one parent, and carry no delay utility that the shorthand would reset.
    expect(new Set(dots.map((dot) => dot.parentElement)).size).toBe(1);
    for (const dot of dots)
      expect(dot.className).not.toContain("animation-delay");
  });
});
