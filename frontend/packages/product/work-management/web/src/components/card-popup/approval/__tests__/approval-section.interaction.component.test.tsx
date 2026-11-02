import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import type { ItemSheetApproval } from "../../../card-detail/task-detail-types";
import { en } from "../../card-popup.copy";
import {
  CardPopupProvider,
  type CardPopupModel,
} from "../../card-popup-context";
import { ApprovalSection } from "../approval-section";
import { approvalCandidates, approvalFailure } from "../approval-model";

const ME = "u-me";
const OTHER = "u-other";

function approval(patch: Partial<ItemSheetApproval>): ItemSheetApproval {
  return {
    id: "ap-1",
    status: "pending",
    approverUserId: ME,
    approverName: "Alex Rivera",
    requestedByUserId: OTHER,
    requestedByName: "Jordan Lee",
    note: "Please look",
    requestedAt: "2026-10-12T02:40:00Z",
    decidedAt: null,
    decisionNote: null,
    ...patch,
  };
}

function show(
  data: Partial<CardPopupModel["data"]>,
  callbacks: Partial<CardPopupModel["callbacks"]> = {},
  mode: CardPopupModel["mode"] = "view",
) {
  const model = {
    mode,
    copy: en,
    now: new Date("2026-10-12T04:00:00Z"),
    data: { viewerUserId: ME, ...data },
    callbacks: {
      onDecideApproval: vi.fn(),
      onCancelApproval: vi.fn(),
      ...callbacks,
    },
  } as unknown as CardPopupModel;
  renderPureUi(
    <I18nProvider catalogs={[]} initialLocale="en">
      <CardPopupProvider value={model}>
        <ApprovalSection />
      </CardPopupProvider>
    </I18nProvider>,
  );
  return model;
}

const region = () => screen.getByRole("region", { name: "Approval" });

describe("ApprovalSection", () => {
  it("is absent until a request was asked", () => {
    show({ approvals: [] });
    expect(screen.queryByRole("region", { name: "Approval" })).toBeNull();
  });

  it("is absent while a card is being created", () => {
    show({ approvals: [approval({})] }, {}, "create");
    expect(screen.queryByRole("region", { name: "Approval" })).toBeNull();
  });

  it("shows a skeleton while the first read is out", () => {
    show({ approvalsLoading: true });
    expect(
      within(region()).getByRole("status", {
        name: "Loading approval requests",
      }),
    ).toBeTruthy();
  });

  it("says the read failed and reads again on Try again", () => {
    const onReloadApprovals = vi.fn();
    show({ approvalsFailed: true }, { onReloadApprovals });
    expect(within(region()).getByRole("alert").textContent).toContain(
      "could not be loaded",
    );
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(onReloadApprovals).toHaveBeenCalledTimes(1);
  });

  it("lets the named approver approve", () => {
    const model = show({ approvals: [approval({})] });
    expect(within(region()).getByText(/asked you to approve/)).toBeTruthy();
    expect(within(region()).getByText(/Please look/)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Approve" }));
    expect(model.callbacks.onDecideApproval).toHaveBeenCalledWith(
      "ap-1",
      "approve",
      null,
    );
  });

  it("opens the reason in place and rejects with it", () => {
    const model = show({ approvals: [approval({})] });
    fireEvent.click(screen.getByRole("button", { name: "Reject" }));
    fireEvent.change(
      screen.getByRole("textbox", { name: "Reason for rejecting (optional)" }),
      { target: { value: "  Needs a budget " } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Reject request" }));
    expect(model.callbacks.onDecideApproval).toHaveBeenCalledWith(
      "ap-1",
      "reject",
      "Needs a budget",
    );
  });

  it("goes back from the reason without sending anything", () => {
    const model = show({ approvals: [approval({})] });
    fireEvent.click(screen.getByRole("button", { name: "Reject" }));
    fireEvent.click(screen.getByRole("button", { name: "Back" }));
    expect(screen.getByRole("button", { name: "Approve" })).toBeTruthy();
    expect(model.callbacks.onDecideApproval).not.toHaveBeenCalled();
  });

  it("holds the buttons while an answer is on its way", () => {
    show({ approvals: [approval({})], approvalAnswering: true });
    expect(
      (screen.getByRole("button", { name: "Approve" }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
  });

  it("lets the requester cancel and never decide", () => {
    const model = show({
      approvals: [approval({ approverUserId: OTHER, requestedByUserId: ME })],
    });
    expect(screen.queryByRole("button", { name: "Approve" })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Cancel request" }));
    expect(model.callbacks.onCancelApproval).toHaveBeenCalledWith("ap-1");
  });

  it("gives anyone else a read-only view with the reason", () => {
    show({
      viewerUserId: "u-third",
      approvals: [approval({})],
    });
    expect(within(region()).getByText("Waiting for Alex Rivera")).toBeTruthy();
    expect(within(region()).getByText(/Only the person asked/)).toBeTruthy();
    expect(screen.queryByRole("button")).toBeNull();
  });

  it("shows an approval and a rejection with the answer and its note", () => {
    show({
      approvals: [
        approval({
          id: "ap-2",
          status: "approved",
          decidedAt: "2026-10-12T03:15:00Z",
        }),
        approval({
          id: "ap-3",
          status: "rejected",
          decisionNote: "Needs a budget",
        }),
      ],
    });
    expect(within(region()).getByText(/Approved by Alex Rivera/)).toBeTruthy();
    expect(within(region()).getByText(/Rejected by Alex Rivera/)).toBeTruthy();
    expect(within(region()).getByText(/Needs a budget/)).toBeTruthy();
    expect(within(region()).getAllByRole("listitem")).toHaveLength(2);
  });

  it("names why an answer was refused", () => {
    show({ approvals: [approval({})], approvalAnswerError: "conflict" });
    expect(within(region()).getByRole("alert").textContent).toContain(
      "answered this request first",
    );
  });

  it("names why asking was refused", () => {
    show({ approvals: [], approvalError: "alreadyAsked" });
    expect(within(region()).getByRole("alert").textContent).toBe(
      "That person was already asked about this item.",
    );
  });
});

describe("approvalFailure", () => {
  it("tells a lost race, a refusal and anything else apart", () => {
    expect(approvalFailure(null)).toBeNull();
    expect(approvalFailure({ kind: "conflict" })).toBe("conflict");
    expect(approvalFailure({ kind: "forbidden" })).toBe("forbidden");
    expect(approvalFailure(new Error("x"))).toBe("failed");
  });
});

describe("approvalCandidates", () => {
  const people = [
    { userId: ME, name: "Me", onBoard: true },
    { userId: "u-a", name: "On board", onBoard: true },
    { userId: "u-b", name: "Off board", onBoard: false },
  ].map((person) => ({ ...person, department: null, openCount: null }));
  const board = (visibility: "private" | "workspace") =>
    ({ visibility, members: [] }) as never;

  it("offers board members, never the viewer", () => {
    expect(
      approvalCandidates(board("private"), { people, canInvite: false }, ME),
    ).toEqual([{ id: "u-a", label: "On board" }]);
  });

  it("offers everyone on a workspace-wide board", () => {
    expect(
      approvalCandidates(board("workspace"), { people, canInvite: false }, ME),
    ).toHaveLength(2);
  });
});
