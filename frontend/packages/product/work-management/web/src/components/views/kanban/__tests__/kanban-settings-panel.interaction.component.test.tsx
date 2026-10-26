import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import type { KanbanViewSettings } from "@notrelix/work-management-core";
import { kanbanLaneCopy } from "../kanban-lane.copy";
import { KanbanSettingsPanel } from "../kanban-settings-panel";

const DEFAULTS: KanbanViewSettings = {
  laneCardLimit: 4,
  statusManageRoles: ["manager", "editor"],
  keyboardMove: true,
  wipLimits: {},
};

function renderPanel(settings: Partial<KanbanViewSettings> = {}) {
  const onChange = vi.fn();
  const copy = kanbanLaneCopy("vi");
  renderPureUi(
    <KanbanSettingsPanel
      copy={copy}
      settings={{ ...DEFAULTS, ...settings }}
      onChange={onChange}
    />,
  );
  return { onChange, copy };
}

describe("KanbanSettingsPanel", () => {
  it("FUI[wm.kanban.board:settings] steps and types the lane limit inside 1..20", () => {
    const { onChange, copy } = renderPanel();
    fireEvent.click(
      screen.getByRole("button", { name: copy.laneLimitIncrease }),
    );
    expect(onChange).toHaveBeenLastCalledWith({ laneCardLimit: 5 });
    fireEvent.click(
      screen.getByRole("button", { name: copy.laneLimitDecrease }),
    );
    expect(onChange).toHaveBeenLastCalledWith({ laneCardLimit: 3 });
    const input = screen.getByRole("spinbutton", { name: copy.laneLimitLabel });
    fireEvent.change(input, { target: { value: "12" } });
    expect(onChange).toHaveBeenLastCalledWith({ laneCardLimit: 12 });
    onChange.mockClear();
    fireEvent.change(input, { target: { value: "99" } });
    fireEvent.change(input, { target: { value: "0" } });
    expect(onChange).not.toHaveBeenCalled();
  });

  it("stops the stepper at the ends", () => {
    const { copy } = renderPanel({ laneCardLimit: 20 });
    expect(
      (
        screen.getByRole("button", {
          name: copy.laneLimitIncrease,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });

  it("keeps Manager on and locked, and toggles Editors", () => {
    const { onChange, copy } = renderPanel();
    const manager = screen.getByRole("checkbox", { name: copy.roleManager });
    expect(manager.getAttribute("aria-checked")).toBe("true");
    expect((manager as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(screen.getByRole("checkbox", { name: copy.roleEditor }));
    expect(onChange).toHaveBeenLastCalledWith({
      statusManageRoles: ["manager"],
    });
  });

  it("switches keyboard move", () => {
    const { onChange, copy } = renderPanel();
    fireEvent.click(
      screen.getByRole("switch", { name: copy.keyboardMoveLabel }),
    );
    expect(onChange).toHaveBeenLastCalledWith({ keyboardMove: false });
  });
});
