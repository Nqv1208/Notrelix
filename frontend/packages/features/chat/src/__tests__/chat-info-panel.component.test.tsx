import { describe, expect, it, vi } from "vitest";
import { Phone, Search } from "lucide-react";
import { AtSign, Pin, Users } from "lucide-react";
import { fireEvent, render, screen } from "@notrelix/testing";
import {
  ChatInfoPanel,
  type ChatInfoPanelProps,
} from "../web/components/chat-info-panel";

const sections: ChatInfoPanelProps["sections"] = [
  {
    key: "members",
    label: "Members",
    count: 3,
    icon: Users,
    content: <p>member rows</p>,
  },
  {
    key: "pins",
    label: "Pinned",
    count: 2,
    icon: Pin,
    content: <p>pin rows</p>,
  },
  {
    key: "mentions",
    label: "Mentions",
    count: null,
    icon: AtSign,
    content: <p>mention rows</p>,
  },
];

function setup(over: Partial<ChatInfoPanelProps> = {}) {
  const onClose = vi.fn();
  const onSearch = vi.fn();
  const utils = render(
    <ChatInfoPanel
      avatar={<span>tile</span>}
      title="erp-rollout"
      subtitle="24 members"
      regionLabel="Channel info"
      closeLabel="Close"
      actions={[
        { key: "voice", label: "Voice call", icon: Phone, onClick: vi.fn() },
        { key: "search", label: "Search", icon: Search, onClick: onSearch },
      ]}
      sections={sections}
      onClose={onClose}
      {...over}
    />,
  );
  return { ...utils, onClose, onSearch };
}

const header = (name: string) =>
  screen.getByRole("button", { name: new RegExp(`^${name}`) });

describe("ChatInfoPanel", () => {
  it("is one column with no tabs: close, name, status, quick actions and sections with counts", () => {
    setup();
    expect(screen.queryByRole("tablist")).toBeNull();
    expect(
      screen.getByRole("heading", { level: 2, name: "erp-rollout" }),
    ).toBeTruthy();
    expect(screen.getByText("24 members")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Voice call" })).toBeTruthy();
    expect(header("Members").textContent).toContain("3");
    expect(header("Pinned").textContent).toContain("2");
    // A count that is still loading shows nothing instead of a wrong number.
    expect(header("Mentions").textContent).toBe("Mentions");
  });

  it("opens the first section and collapses and expands the others on click", () => {
    setup();
    expect(header("Members").getAttribute("aria-expanded")).toBe("true");
    expect(screen.getByText("member rows")).toBeTruthy();
    expect(header("Pinned").getAttribute("aria-expanded")).toBe("false");
    expect(screen.queryByText("pin rows")).toBeNull();
    fireEvent.click(header("Pinned"));
    expect(screen.getByText("pin rows")).toBeTruthy();
    fireEvent.click(header("Members"));
    expect(screen.queryByText("member rows")).toBeNull();
    expect(header("Members").getAttribute("aria-expanded")).toBe("false");
  });

  it("deep-opens a section expanded and scrolls it into view, again for a repeated request", () => {
    const scroll = vi.fn();
    Element.prototype.scrollIntoView = scroll;
    const { rerender, onClose } = setup({ focus: { key: "pins", seq: 1 } });
    expect(header("Pinned").getAttribute("aria-expanded")).toBe("true");
    expect(scroll).toHaveBeenCalledTimes(1);
    fireEvent.click(header("Pinned"));
    expect(header("Pinned").getAttribute("aria-expanded")).toBe("false");
    rerender(
      <ChatInfoPanel
        avatar={<span>tile</span>}
        title="erp-rollout"
        subtitle="24 members"
        regionLabel="Channel info"
        closeLabel="Close"
        actions={[]}
        sections={sections}
        focus={{ key: "pins", seq: 2 }}
        onClose={onClose}
      />,
    );
    expect(header("Pinned").getAttribute("aria-expanded")).toBe("true");
    expect(scroll).toHaveBeenCalledTimes(2);
  });

  it("runs a quick action and closes from the cross", () => {
    const { onClose, onSearch } = setup();
    fireEvent.click(screen.getByRole("button", { name: "Search" }));
    expect(onSearch).toHaveBeenCalledOnce();
    fireEvent.click(screen.getByRole("button", { name: "Close" }));
    expect(onClose).toHaveBeenCalledOnce();
  });

  it("closes on Escape pressed inside the panel, but not when a menu already took the key", () => {
    const { onClose } = setup();
    const event = new KeyboardEvent("keydown", {
      key: "Escape",
      bubbles: true,
      cancelable: true,
    });
    event.preventDefault();
    header("Members").dispatchEvent(event);
    expect(onClose).not.toHaveBeenCalled();
    fireEvent.keyDown(header("Members"), { key: "Escape" });
    expect(onClose).toHaveBeenCalledOnce();
  });

  it("keeps the focus order: close, quick actions, then the section rows", () => {
    setup();
    const order = screen
      .getAllByRole("button")
      .map(
        (b) =>
          b.getAttribute("aria-label") ?? b.textContent?.replace(/\d+$/, ""),
      );
    expect(order).toEqual([
      "Close",
      "Voice call",
      "Search",
      "Members",
      "Pinned",
      "Mentions",
    ]);
  });
});
