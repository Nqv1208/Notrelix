import { createRef } from "react";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { TableSplitDivider } from "../table-split-divider";
import { SPLIT_PANE_STEP } from "../table-split-width";

// jsdom has no PointerEvent: build the pointer as a mouse event and set the pointer id on it.
function pointer(
  target: HTMLElement,
  type: "pointerdown" | "pointermove" | "pointerup",
  init: { pointerId: number; button?: number; clientX?: number },
) {
  const event = new MouseEvent(type, {
    bubbles: true,
    cancelable: true,
    button: init.button ?? 0,
    clientX: init.clientX ?? 0,
  });
  Object.defineProperty(event, "pointerId", { value: init.pointerId });
  fireEvent(target, event);
}

function renderDivider(width: number, rect = { left: 0, width: 1000 }) {
  const containerRef = createRef<HTMLElement>();
  const onPreview = vi.fn();
  const onCommit = vi.fn();
  renderPureUi(
    <div
      ref={(node) => {
        if (node) {
          node.getBoundingClientRect = () =>
            ({ ...rect, right: rect.left + rect.width }) as DOMRect;
        }
        (containerRef as { current: HTMLElement | null }).current = node;
      }}
    >
      <TableSplitDivider
        containerRef={containerRef}
        paneId="table-split-pane"
        width={width}
        onPreview={onPreview}
        onCommit={onCommit}
        label="Resize the item pane"
      />
    </div>,
  );
  return {
    grip: screen.getByRole("button", { name: "Resize the item pane" }),
    onPreview,
    onCommit,
  };
}

describe("TableSplitDivider (the grip on the tablet split)", () => {
  it("names the pane it controls and draws the grip as a button", () => {
    const { grip } = renderDivider(0.46);
    expect(grip.getAttribute("aria-controls")).toBe("table-split-pane");
  });

  it("Left widens the pane and Right narrows it, one step at a time, saved on the key press", () => {
    const { grip, onCommit } = renderDivider(0.46);
    fireEvent.keyDown(grip, { key: "ArrowLeft" });
    expect(onCommit).toHaveBeenLastCalledWith(0.46 + SPLIT_PANE_STEP);
    fireEvent.keyDown(grip, { key: "ArrowRight" });
    expect(onCommit).toHaveBeenLastCalledWith(0.46 - SPLIT_PANE_STEP);
  });

  it("does not move on other keys", () => {
    const { grip, onCommit } = renderDivider(0.46);
    fireEvent.keyDown(grip, { key: "Enter" });
    expect(onCommit).not.toHaveBeenCalled();
  });

  it("follows a drag as a preview and saves the width on release", () => {
    const { grip, onPreview, onCommit } = renderDivider(0.46);
    pointer(grip, "pointerdown", { pointerId: 7 });
    pointer(grip, "pointermove", { pointerId: 7, clientX: 400 });
    expect(onPreview).toHaveBeenLastCalledWith(0.6);
    pointer(grip, "pointerup", { pointerId: 7 });
    expect(onCommit).toHaveBeenCalledWith(0.6);
  });

  it("ignores pointer moves that did not start on the grip", () => {
    const { grip, onPreview } = renderDivider(0.46);
    pointer(grip, "pointermove", { pointerId: 3, clientX: 400 });
    expect(onPreview).not.toHaveBeenCalled();
  });
});
