import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@notrelix/testing";
import { ChatHeaderActions } from "../web/components/chat-header-actions";
import { en } from "../web/components/chat.copy";

function setup(over: Partial<Parameters<typeof ChatHeaderActions>[0]> = {}) {
  const handlers = {
    onSearch: vi.fn(),
    onToggleInfo: vi.fn(),
    onOpenSection: vi.fn(),
    onStartCall: vi.fn(),
  };
  render(
    <ChatHeaderActions copy={en} infoOpen={false} {...handlers} {...over} />,
  );
  return handlers;
}

const names = () =>
  screen.getAllByRole("button").map((b) => b.getAttribute("aria-label"));

describe("ChatHeaderActions", () => {
  it("has no call buttons while calls are off, never a dead one", () => {
    setup({ callsEnabled: false });
    expect(names()).toEqual([
      en.search.open,
      en.dm.view.details,
      en.panel.more,
    ]);
  });

  it("has no call buttons when the flag is on but nothing handles a call", () => {
    setup({ callsEnabled: true, onStartCall: undefined });
    expect(
      screen.queryByRole("button", { name: en.panel.voiceCall }),
    ).toBeNull();
  });

  it("draws voice and video first when calls are enabled, and each starts its own kind", () => {
    const h = setup({ callsEnabled: true });
    expect(names()).toEqual([
      en.panel.voiceCall,
      en.panel.videoCall,
      en.search.open,
      en.dm.view.details,
      en.panel.more,
    ]);
    fireEvent.click(screen.getByRole("button", { name: en.panel.voiceCall }));
    fireEvent.click(screen.getByRole("button", { name: en.panel.videoCall }));
    expect(h.onStartCall.mock.calls).toEqual([["voice"], ["video"]]);
  });

  it("names every button by label and tooltip and shows no key hint", () => {
    setup({ callsEnabled: true });
    for (const button of screen.getAllByRole("button")) {
      expect(button.getAttribute("title")).toBe(
        button.getAttribute("aria-label"),
      );
      expect(button.querySelector("kbd")).toBeNull();
    }
  });

  it("marks the info button pressed while the panel is open and toggles it", () => {
    const h = setup({ infoOpen: true });
    const info = screen.getByRole("button", { name: en.dm.view.details });
    expect(info.getAttribute("aria-pressed")).toBe("true");
    fireEvent.click(info);
    expect(h.onToggleInfo).toHaveBeenCalledOnce();
  });

  it("holds Pinned and Following in the menu, which open the panel at that section, then the extra entries", async () => {
    const h = setup({ menu: <div role="menuitem">Hide</div> });
    fireEvent.keyDown(screen.getByRole("button", { name: en.panel.more }), {
      key: "Enter",
    });
    const items = await screen.findAllByRole("menuitem");
    expect(items.map((i) => i.textContent)).toEqual([
      en.pins.title,
      en.thread.followedTab,
      "Hide",
    ]);
    fireEvent.click(items[1]!);
    expect(h.onOpenSection).toHaveBeenCalledWith("following");
  });

  it("closes the menu with Escape", async () => {
    setup();
    fireEvent.keyDown(screen.getByRole("button", { name: en.panel.more }), {
      key: "Enter",
    });
    await screen.findAllByRole("menuitem");
    fireEvent.keyDown(screen.getAllByRole("menuitem")[0]!, { key: "Escape" });
    await vi.waitFor(() => expect(screen.queryByRole("menu")).toBeNull());
  });
});
