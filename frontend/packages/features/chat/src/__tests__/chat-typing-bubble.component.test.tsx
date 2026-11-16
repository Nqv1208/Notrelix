import { describe, expect, it } from "vitest";
import { render, screen } from "@notrelix/testing";
import { ChatTypingBubble } from "../web/components/chat-typing-indicator";
import { en } from "../web/components/chat.copy";

describe("ChatTypingBubble", () => {
  it("waves three dots in a staggered, soft loop instead of bouncing, and is still under reduced motion", () => {
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
      expect(dot.className).toContain("motion-reduce:animate-none");
    }
    const delays = dots.map(
      (dot) => /animation-delay:(\d+)ms/.exec(dot.className)?.[1],
    );
    expect(delays).toEqual(["0", "150", "300"]);
  });
});
