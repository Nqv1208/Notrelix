import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, within } from "@notrelix/testing";
import { ChatComposer } from "../web/components/chat-composer";
import { en } from "../web/components/chat.copy";
import { CHAT_LIMITS } from "../core/types/chat";

type SendArgs = [string, unknown, readonly File[] | undefined];

function Harness({
  onSend,
  disabledReason = null,
  withAttachments = true,
}: {
  onSend: (...args: SendArgs) => void;
  disabledReason?: string | null;
  withAttachments?: boolean;
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
      attachments={withAttachments ? { draftKey: "c1" } : undefined}
    />
  );
}

function pick(files: File[]) {
  const input = screen.getByTestId("chat-file-input") as HTMLInputElement;
  fireEvent.change(input, { target: { files } });
}

function textFile(name: string, body = "hello") {
  return new File([body], name, { type: "text/plain" });
}

describe("ChatComposer attachments", () => {
  it("has no attach control without the attachments prop", () => {
    render(<Harness onSend={vi.fn()} withAttachments={false} />);
    expect(
      screen.queryByRole("button", { name: en.attachments.attach }),
    ).toBeNull();
  });

  it("shows a chip with name and size for each picked file, and removes one", () => {
    render(<Harness onSend={vi.fn()} />);
    pick([textFile("a.txt", "12345"), textFile("b.txt")]);
    const list = screen.getByRole("list", {
      name: /Files to send \(2 of 10\)/,
    });
    const items = within(list).getAllByRole("listitem");
    expect(items).toHaveLength(2);
    expect(within(items[0]!).getByText("a.txt")).not.toBeNull();
    expect(within(items[0]!).getByText("5 B")).not.toBeNull();
    fireEvent.click(
      screen.getByRole("button", {
        name: "Remove a.txt from the files to send",
      }),
    );
    expect(within(list).getAllByRole("listitem")).toHaveLength(1);
    expect(screen.queryByText("a.txt")).toBeNull();
  });

  it("opens the file chooser from the attach button", () => {
    render(<Harness onSend={vi.fn()} />);
    const input = screen.getByTestId("chat-file-input") as HTMLInputElement;
    const click = vi.spyOn(input, "click");
    fireEvent.click(screen.getByRole("button", { name: "Attach files" }));
    expect(click).toHaveBeenCalled();
  });

  it("holds at most 10 files and says which one was left out", () => {
    render(<Harness onSend={vi.fn()} />);
    pick(Array.from({ length: 11 }, (_, i) => textFile(`f${i}.txt`)));
    expect(
      within(screen.getByTestId("chat-pending-files")).getAllByRole("listitem"),
    ).toHaveLength(CHAT_LIMITS.attachmentsPerMessageMax);
    expect(screen.getByRole("alert").textContent).toContain(
      "f10.txt was not added: a message holds at most 10 files.",
    );
  });

  it("refuses an empty file and a file over 100 MB before anything is sent", () => {
    render(<Harness onSend={vi.fn()} />);
    const big = textFile("big.bin");
    Object.defineProperty(big, "size", {
      value: CHAT_LIMITS.attachmentMaxBytes + 1,
    });
    pick([new File([], "empty.txt"), big]);
    const alert = screen.getByRole("alert").textContent ?? "";
    expect(alert).toContain("empty.txt was not added: the file is empty.");
    expect(alert).toContain(
      "big.bin was not added: a file can be up to 100 MB.",
    );
    expect(screen.queryByTestId("chat-pending-files")).toBeNull();
  });

  it("does not send files without text and says so", () => {
    const onSend = vi.fn();
    render(<Harness onSend={onSend} />);
    pick([textFile("a.txt")]);
    expect(screen.getByText(en.attachments.needsText)).not.toBeNull();
    const box = screen.getByRole("textbox", { name: "Message #general" });
    fireEvent.keyDown(box, { key: "Enter" });
    expect(onSend).not.toHaveBeenCalled();
    expect(
      (screen.getByRole("button", { name: "Send" }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
  });

  it("sends the text with the files and empties the strip", () => {
    const onSend = vi.fn();
    render(<Harness onSend={onSend} />);
    const a = textFile("a.txt");
    pick([a]);
    const box = screen.getByRole("textbox", { name: "Message #general" });
    fireEvent.change(box, { target: { value: "see attached" } });
    fireEvent.keyDown(box, { key: "Enter" });
    expect(onSend).toHaveBeenCalledWith("see attached", undefined, [a]);
    expect(screen.queryByTestId("chat-pending-files")).toBeNull();
  });

  it("sends plain text with no third argument when nothing is attached", () => {
    const onSend = vi.fn();
    render(<Harness onSend={onSend} />);
    const box = screen.getByRole("textbox", { name: "Message #general" });
    fireEvent.change(box, { target: { value: "hi" } });
    fireEvent.keyDown(box, { key: "Enter" });
    expect(onSend).toHaveBeenCalledWith("hi");
  });

  it("adds files that are dropped on the composer and pasted into the text", () => {
    render(<Harness onSend={vi.fn()} />);
    const dropped = textFile("dropped.txt");
    const form = screen.getByRole("textbox").closest("form")!;
    fireEvent.dragOver(form, { dataTransfer: { types: ["Files"], files: [] } });
    expect(screen.getByText(en.attachments.dropHint)).not.toBeNull();
    fireEvent.drop(form, {
      dataTransfer: { types: ["Files"], files: [dropped] },
    });
    expect(screen.getByText("dropped.txt")).not.toBeNull();
    expect(screen.queryByText(en.attachments.dropHint)).toBeNull();

    const pasted = new File(["png"], "shot.png", { type: "image/png" });
    fireEvent.paste(screen.getByRole("textbox"), {
      clipboardData: { files: [pasted], types: ["Files"] },
    });
    expect(screen.getByText("shot.png")).not.toBeNull();
  });

  it("leaves a text paste alone", () => {
    render(<Harness onSend={vi.fn()} />);
    const notPrevented = fireEvent.paste(screen.getByRole("textbox"), {
      clipboardData: { files: [], types: ["text/plain"] },
    });
    expect(notPrevented).toBe(true);
    expect(screen.queryByTestId("chat-pending-files")).toBeNull();
  });

  it("disables attaching in a read-only channel and ignores a drop", () => {
    render(<Harness onSend={vi.fn()} disabledReason={en.composer.archived} />);
    expect(
      (
        screen.getByRole("button", {
          name: "Attach files",
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    const form = screen.getByRole("textbox").closest("form")!;
    fireEvent.drop(form, {
      dataTransfer: { types: ["Files"], files: [textFile("x.txt")] },
    });
    expect(screen.queryByTestId("chat-pending-files")).toBeNull();
  });
});
