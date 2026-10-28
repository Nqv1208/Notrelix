import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import { ItemApprovalSection } from "../item-page-approval";
import { en, vi as viCopy } from "../item-page.copy";
import type {
  ItemSheetApproval,
  TaskDetailData,
} from "../../card-detail/task-detail-types";

const ME = "u-me";
const OTHER = "u-other";

function pending(
  overrides: Partial<ItemSheetApproval> = {},
): ItemSheetApproval {
  return {
    id: "req-1",
    status: "pending",
    approverUserId: ME,
    approverName: "Linh",
    requestedByUserId: OTHER,
    ...overrides,
  };
}

function data(
  approvals: readonly ItemSheetApproval[],
  extra: Partial<TaskDetailData> = {},
): TaskDetailData {
  return {
    updates: [],
    updatesLoading: false,
    files: [],
    filesLoading: false,
    activity: [],
    activityLoading: false,
    activityFetching: false,
    approvals,
    ...extra,
  };
}

function setup(
  approvals: readonly ItemSheetApproval[],
  viewer: string | null,
  extra: Partial<TaskDetailData> = {},
  copy = en,
) {
  const callbacks = { onDecideApproval: vi.fn(), onCancelApproval: vi.fn() };
  renderPureUi(
    <ItemApprovalSection
      data={data(approvals, extra)}
      copy={copy}
      viewerUserId={viewer}
      callbacks={callbacks}
    />,
  );
  return callbacks;
}

describe.each([
  ["en", en],
  ["vi", viCopy],
] as const)("item approval section (%s)", (_locale, copy) => {
  const t = copy.approval;

  it("lets the named approver approve", () => {
    const callbacks = setup([pending()], ME, {}, copy);
    fireEvent.click(screen.getByRole("button", { name: t.approve }));
    expect(callbacks.onDecideApproval).toHaveBeenCalledExactlyOnceWith(
      "req-1",
      "approve",
      null,
    );
    expect(screen.queryByRole("button", { name: t.cancelRequest })).toBeNull();
  });

  it("asks for a reason in place before rejecting, and sends it", () => {
    const callbacks = setup([pending()], ME, {}, copy);
    fireEvent.click(screen.getByRole("button", { name: t.reject }));
    // The form grows in place: the decision buttons give way to it.
    expect(screen.queryByRole("button", { name: t.approve })).toBeNull();
    fireEvent.change(screen.getByRole("textbox", { name: t.reasonLabel }), {
      target: { value: "  Thiếu số liệu  " },
    });
    fireEvent.click(screen.getByRole("button", { name: t.confirmReject }));
    expect(callbacks.onDecideApproval).toHaveBeenCalledExactlyOnceWith(
      "req-1",
      "reject",
      "Thiếu số liệu",
    );
  });

  it("rejects without a reason when none is typed, and can go back", () => {
    const callbacks = setup([pending()], ME, {}, copy);
    fireEvent.click(screen.getByRole("button", { name: t.reject }));
    fireEvent.click(screen.getByRole("button", { name: t.back }));
    expect(screen.getByRole("button", { name: t.approve })).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: t.reject }));
    fireEvent.click(screen.getByRole("button", { name: t.confirmReject }));
    expect(callbacks.onDecideApproval).toHaveBeenCalledExactlyOnceWith(
      "req-1",
      "reject",
      null,
    );
  });

  it("lets the requester cancel but not decide", () => {
    const callbacks = setup(
      [pending({ approverUserId: OTHER, requestedByUserId: ME })],
      ME,
      {},
      copy,
    );
    expect(screen.queryByRole("button", { name: t.approve })).toBeNull();
    expect(screen.queryByRole("button", { name: t.reject })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: t.cancelRequest }));
    expect(callbacks.onCancelApproval).toHaveBeenCalledExactlyOnceWith("req-1");
  });

  it("shows everyone else the state alone", () => {
    setup(
      [
        pending({ approverUserId: OTHER, requestedByUserId: OTHER }),
        pending({
          id: "req-2",
          status: "rejected",
          approverName: "Hà",
          decisionNote: "Thiếu số liệu",
        }),
      ],
      ME,
      {},
      copy,
    );
    expect(screen.queryAllByRole("button")).toHaveLength(0);
    const section = screen.getByRole("region", { name: t.heading });
    expect(
      within(section).getByText(t.pending.replace("{{name}}", "Linh")),
    ).toBeTruthy();
    expect(
      within(section).getByText(t.rejected.replace("{{name}}", "Hà")),
    ).toBeTruthy();
    expect(within(section).getByText("Thiếu số liệu")).toBeTruthy();
  });

  it("offers no buttons once the request is answered, or without a viewer", () => {
    setup([pending({ status: "approved" })], ME, {}, copy);
    expect(screen.queryAllByRole("button")).toHaveLength(0);
  });

  it("offers no buttons when the viewer is unknown", () => {
    setup([pending()], null, {}, copy);
    expect(screen.queryAllByRole("button")).toHaveLength(0);
  });

  it("says when an answer was refused", () => {
    setup([pending()], ME, { approvalAnswerError: true }, copy);
    expect(screen.getByRole("alert").textContent).toBe(t.answerFailed);
  });

  it("announces the new state through a status region", () => {
    setup([pending()], ME, {}, copy);
    expect(screen.getByRole("status").textContent).toBe(
      t.pending.replace("{{name}}", "Linh"),
    );
  });
});
