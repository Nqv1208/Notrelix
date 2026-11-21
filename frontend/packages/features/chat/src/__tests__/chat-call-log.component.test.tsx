import { fireEvent, render, screen, within } from "@notrelix/testing";
import { describe, expect, it, vi } from "vitest";
import type { ChatMessage } from "../core/types/chat";
import { ChatCallRedialProvider } from "../web/components/chat-call-redial";
import { ChatMessageRow } from "../web/components/chat-message-row";
import { en, vi as viCopy } from "../web/components/chat.copy";

const system = (content: string): ChatMessage =>
  ({
    id: "m1",
    kind: "system",
    content,
    createdAt: "2026-11-19T09:14:00.000Z",
    deleted: false,
  }) as unknown as ChatMessage;

function row(content: string, isOwn: boolean, redial = vi.fn()) {
  render(
    <ChatCallRedialProvider value={redial}>
      <ChatMessageRow
        message={system(content)}
        author={{ name: "Quân" }}
        continued={false}
        locale="vi"
        timeZone="UTC"
        isOwn={isOwn}
        copy={viCopy}
      />
    </ChatCallRedialProvider>,
  );
  return redial;
}

describe("the call line in the timeline (Chat-Var-Call-Log)", () => {
  it("shows a missed call as a danger line with the caller and a Gọi lại button that calls back with the same kind", () => {
    const redial = row("missed video call", false);
    const line = screen.getByTestId("chat-system-message");
    expect(line.getAttribute("data-call-line")).toBe("missed");
    expect(line.textContent).toContain("Cuộc gọi nhỡ từ Quân");
    fireEvent.click(
      within(line).getByRole("button", { name: viCopy.calls.redial }),
    );
    expect(redial).toHaveBeenCalledWith("video");
  });

  it("says an own missed call was not answered, and still offers the call back", () => {
    const redial = row("missed voice call", true);
    expect(screen.getByTestId("chat-system-message").textContent).toContain(
      "Cuộc gọi nhỡ không được trả lời",
    );
    fireEvent.click(screen.getByRole("button", { name: viCopy.calls.redial }));
    expect(redial).toHaveBeenCalledWith("voice");
  });

  it("shows a finished call as a plain line with its length and no button", () => {
    row("video call ended, lasted 24m 0s", false);
    const line = screen.getByTestId("chat-system-message");
    expect(line.getAttribute("data-call-line")).toBe("plain");
    expect(line.textContent).toContain("Cuộc gọi video 24m 0s");
    expect(within(line).queryByRole("button")).toBeNull();
  });

  it.each([
    ["voice call declined", "Cuộc gọi thoại bị từ chối"],
    ["voice call cancelled", "Cuộc gọi thoại đã bị hủy"],
    [
      "video call not answered, the other person is in a call",
      "Cuộc gọi video người kia đang trong cuộc gọi khác",
    ],
    ["voice call failed", "Cuộc gọi thoại không kết nối được"],
  ])("says %s in Vietnamese", (stored, text) => {
    row(stored, false);
    expect(screen.getByTestId("chat-system-message").textContent).toContain(
      text,
    );
  });

  it("offers no button without a call provider (a channel or a Guest)", () => {
    render(
      <ChatMessageRow
        message={system("missed voice call")}
        author={{ name: "Quân" }}
        continued={false}
        locale="vi"
        timeZone="UTC"
        isOwn={false}
        copy={en}
      />,
    );
    expect(screen.queryByRole("button")).toBeNull();
    expect(screen.getByTestId("chat-system-message").textContent).toContain(
      "Missed call from Quân",
    );
  });
});
