import { describe, expect, it, vi } from "vitest";
import { toast } from "sonner";

vi.mock("sonner", () => {
  const toastFn = vi.fn();
  return { toast: Object.assign(toastFn, { error: vi.fn() }) };
});

import {
  DECISION_UNDO_MS,
  toastDecisionWithUndo,
} from "../form-decision-toast";

const copy = { undo: "Hoàn tác", undoFailed: "Chưa hoàn tác được." };

describe("toastDecisionWithUndo", () => {
  it("shows the decision with an undo for the undo window", () => {
    toastDecisionWithUndo("Đã từ chối", copy, async () => ({ ok: true }));
    expect(toast).toHaveBeenCalledWith(
      "Đã từ chối",
      expect.objectContaining({
        duration: DECISION_UNDO_MS,
        action: expect.objectContaining({ label: "Hoàn tác" }),
      }),
    );
  });

  it("runs the undo from the action, and says so when it is refused", async () => {
    const undo = vi.fn(async () => ({ ok: false, reason: "failed" }) as const);
    toastDecisionWithUndo("Đã xoá phản hồi", copy, undo);
    const options = vi.mocked(toast).mock.lastCall?.[1] as unknown as {
      action: { onClick: () => void };
    };
    options.action.onClick();
    await vi.waitFor(() =>
      expect(toast.error).toHaveBeenCalledWith("Chưa hoàn tác được."),
    );
    expect(undo).toHaveBeenCalledTimes(1);
  });
});
