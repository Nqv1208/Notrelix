import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@notrelix/testing";
import { ChatComposer } from "../web/components/chat-composer";
import { en } from "../web/components/chat.copy";

function Harness({
  onSend,
  disabledReason = null,
}: {
  onSend: (text: string) => void;
  disabledReason?: string | null;
}) {
  const [value, setValue] = useState("");
  return (
    <ChatComposer
      channelName="general"
      value={value}
      disabledReason={disabledReason}
      copy={en}
      onChange={setValue}
      onSend={onSend}
    />
  );
}

describe("ChatComposer", () => {
  it("sends the trimmed text on Enter and keeps Shift+Enter for a new line", () => {
    const onSend = vi.fn();
    render(<Harness onSend={onSend} />);
    const box = screen.getByRole("textbox", { name: "Message #general" });
    fireEvent.change(box, { target: { value: "  hello  " } });
    fireEvent.keyDown(box, { key: "Enter", shiftKey: true });
    expect(onSend).not.toHaveBeenCalled();
    fireEvent.keyDown(box, { key: "Enter" });
    expect(onSend).toHaveBeenCalledWith("hello");
  });

  it("does not send blank text", () => {
    const onSend = vi.fn();
    render(<Harness onSend={onSend} />);
    const box = screen.getByRole("textbox", { name: "Message #general" });
    fireEvent.change(box, { target: { value: "   " } });
    fireEvent.keyDown(box, { key: "Enter" });
    expect(onSend).not.toHaveBeenCalled();
    expect(
      (screen.getByRole("button", { name: "Send" }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
  });

  it("shows the counter from 3800 characters and refuses text over 4000", () => {
    const onSend = vi.fn();
    render(<Harness onSend={onSend} />);
    const box = screen.getByRole("textbox", { name: "Message #general" });
    fireEvent.change(box, { target: { value: "a".repeat(3800) } });
    expect(screen.getByText("3800 of 4000")).not.toBeNull();
    fireEvent.change(box, { target: { value: "a".repeat(4001) } });
    expect(
      screen.getByText("Shorten the message to 4000 characters."),
    ).not.toBeNull();
    expect(
      (screen.getByRole("button", { name: "Send" }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    fireEvent.keyDown(box, { key: "Enter" });
    expect(onSend).not.toHaveBeenCalled();
  });

  it("is disabled with the reason in an archived channel", () => {
    render(<Harness onSend={vi.fn()} disabledReason={en.composer.archived} />);
    expect(
      (
        screen.getByRole("textbox", {
          name: "Message #general",
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    expect(screen.getByText(en.composer.archived)).not.toBeNull();
  });
});
