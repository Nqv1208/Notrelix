import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { describe, expect, it, vi } from "vitest";
import { ChatPanelTabs } from "../web/components/chat-panel-tabs";
import { vi as copy } from "../web/components/chat.copy";

function setup(active: "details" | "mentions" = "details") {
  const onSelect = vi.fn();
  const onClose = vi.fn();
  render(
    <ChatPanelTabs
      active={active}
      details="group"
      mentionBadge="3"
      panelId="panel"
      hidden={false}
      copy={copy}
      onSelect={onSelect}
      onClose={onClose}
    />,
  );
  return { onSelect, onClose };
}

describe("ChatPanelTabs", () => {
  it("shows short labels with the full name as title, a count badge and a close control outside the tablist", () => {
    setup();
    const tabs = screen.getAllByRole("tab");
    expect(tabs.map((tab) => tab.textContent)).toEqual([
      "Thông tin",
      "Ghim",
      "Theo dõi",
      "Nhắc đến3",
    ]);
    expect(tabs.map((tab) => tab.getAttribute("title"))).toEqual([
      "Thông tin nhóm",
      "Tin đã ghim",
      "Đang theo dõi",
      "Lượt nhắc đến chưa đọc",
    ]);
    expect(tabs[0]?.getAttribute("aria-selected")).toBe("true");
    expect(tabs[0]?.getAttribute("aria-controls")).toBe("panel");
    for (const tab of tabs) {
      expect(tab.className).toContain("whitespace-nowrap");
      expect(tab.className).toContain("shrink-0");
    }
    const close = screen.getByRole("button", { name: copy.pins.close });
    expect(screen.getByRole("tablist").contains(close)).toBe(false);
  });

  it("moves focus with the arrows, Home and End and opens a tab with Enter", async () => {
    const { onSelect } = setup();
    const tabs = screen.getAllByRole("tab");
    tabs[0]!.focus();
    fireEvent.keyDown(tabs[0]!, { key: "ArrowRight" });
    await waitFor(() => expect(document.activeElement).toBe(tabs[1]));
    fireEvent.keyDown(tabs[1]!, { key: "End" });
    await waitFor(() => expect(document.activeElement).toBe(tabs[3]));
    fireEvent.keyDown(tabs[3]!, { key: "ArrowRight" });
    await waitFor(() => expect(document.activeElement).toBe(tabs[0]));
    fireEvent.keyDown(tabs[0]!, { key: "ArrowLeft" });
    await waitFor(() => expect(document.activeElement).toBe(tabs[3]));
    fireEvent.keyDown(tabs[3]!, { key: "Home" });
    await waitFor(() => expect(document.activeElement).toBe(tabs[0]));
    fireEvent.keyDown(tabs[0]!, { key: "ArrowRight" });
    fireEvent.keyDown(tabs[1]!, { key: "Enter" });
    expect(onSelect).toHaveBeenCalledWith("pins");
  });

  it("closes from the cross", () => {
    const { onClose } = setup("mentions");
    fireEvent.click(screen.getByRole("button", { name: copy.pins.close }));
    expect(onClose).toHaveBeenCalledOnce();
  });
});
