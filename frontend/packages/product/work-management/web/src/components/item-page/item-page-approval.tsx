import { useId, useState } from "react";
import { Button, Textarea } from "@notrelix/ui-web";
import type { Board } from "@notrelix/work-management-core";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import type {
  ItemSheetApproval,
  TaskDetailCallbacks,
  TaskDetailData,
} from "../card-detail/task-detail-types";
import type { ItemPageCopy } from "./item-page.copy";
import type { ItemPagePeople } from "./item-page-types";

/**
 * The people who may be asked to approve the item: whoever can read the board (its members, and everyone
 * when it is workspace-wide), never the viewer.
 */
export function approvalCandidates(
  board: Board,
  people: ItemPagePeople | undefined,
  viewerUserId: string | undefined,
): { id: string; label: string }[] {
  const candidates = people
    ? people.people.filter(
        (person) => person.onBoard || board.visibility === "workspace",
      )
    : board.members;
  return candidates
    .filter((person) => person.userId !== viewerUserId && person.name)
    .map((person) => ({ id: person.userId, label: person.name }));
}

/**
 * The approvals asked about the item: who was asked and what they answered. Absent until one was asked
 * (asking lives in the item menu), so the rail keeps its shape.
 */
export function ItemApprovalSection({
  data,
  copy,
  viewerUserId,
  callbacks,
}: {
  data: TaskDetailData | undefined;
  copy: ItemPageCopy;
  /** The signed-in member: the named approver decides, the requester cancels. */
  viewerUserId?: string | null;
  callbacks?: Pick<
    TaskDetailCallbacks,
    "onDecideApproval" | "onCancelApproval"
  >;
}) {
  const t = copy.approval;
  const approvals = data?.approvals ?? [];
  if (
    approvals.length === 0 &&
    !data?.approvalError &&
    !data?.approvalAnswerError
  )
    return null;

  return (
    <section
      aria-label={t.heading}
      data-f="item-approval"
      className="flex flex-col gap-2"
    >
      <div className="flex items-center justify-between text-item-note font-semibold text-meta-foreground">
        {t.heading}
      </div>
      {approvals.length > 0 ? (
        <ul
          aria-label={t.list}
          className="m-0 flex list-none flex-col gap-1.5 p-0"
        >
          {approvals.map((approval) => (
            <ApprovalRow
              key={approval.id}
              approval={approval}
              copy={copy}
              viewerUserId={viewerUserId ?? data?.viewerUserId ?? null}
              callbacks={callbacks}
            />
          ))}
        </ul>
      ) : null}
      {data?.approvalAnswerError ? (
        <p role="alert" className="m-0 text-item-note text-danger-text">
          {t.answerFailed}
        </p>
      ) : null}
      {data?.approvalError ? (
        <p role="alert" className="m-0 text-item-note text-danger-text">
          {data.approvalError === "alreadyAsked" ? t.alreadyAsked : t.failed}
        </p>
      ) : null}
    </section>
  );
}

const STATUS_CLASS: Readonly<Record<ItemSheetApproval["status"], string>> = {
  pending: "text-warning-text",
  approved: "text-success-text",
  rejected: "text-danger-text",
  cancelled: "text-meta-foreground",
};

function ApprovalRow({
  approval,
  copy,
  viewerUserId,
  callbacks,
}: {
  approval: ItemSheetApproval;
  copy: ItemPageCopy;
  viewerUserId: string | null;
  callbacks?: Pick<
    TaskDetailCallbacks,
    "onDecideApproval" | "onCancelApproval"
  >;
}) {
  const t = copy.approval;
  const name = approval.approverName?.trim() || t.unknownPerson;
  const [rejecting, setRejecting] = useState(false);
  const [reason, setReason] = useState("");
  const reasonId = useId();
  const pending = approval.status === "pending";
  const mayDecide =
    pending &&
    viewerUserId !== null &&
    approval.approverUserId === viewerUserId &&
    callbacks?.onDecideApproval !== undefined;
  const mayCancel =
    pending &&
    viewerUserId !== null &&
    approval.requestedByUserId === viewerUserId &&
    callbacks?.onCancelApproval !== undefined;
  return (
    <li
      data-approval-status={approval.status}
      className="flex flex-col gap-1.5 text-item-small"
    >
      <div className="flex flex-col gap-0.5">
        {/* A live region: the new state is read out once the answer is in. */}
        <span
          role="status"
          className={`font-medium ${STATUS_CLASS[approval.status]}`}
        >
          {fillSheetCopy(t[approval.status], { name })}
        </span>
        {approval.decisionNote ? (
          <span className="text-item-note text-muted-foreground">
            {approval.decisionNote}
          </span>
        ) : null}
      </div>
      {mayDecide && !rejecting ? (
        <div className="flex flex-wrap gap-1.5">
          <Button
            type="button"
            size="sm"
            onClick={() =>
              callbacks?.onDecideApproval?.(approval.id, "approve", null)
            }
          >
            {t.approve}
          </Button>
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={() => setRejecting(true)}
          >
            {t.reject}
          </Button>
        </div>
      ) : null}
      {mayDecide && rejecting ? (
        <div className="flex flex-col gap-1.5">
          <label
            htmlFor={reasonId}
            className="text-item-note font-medium text-meta-foreground"
          >
            {t.reasonLabel}
          </label>
          <Textarea
            id={reasonId}
            autoFocus
            rows={2}
            value={reason}
            placeholder={t.reasonPlaceholder}
            onChange={(event) => setReason(event.target.value)}
          />
          <div className="flex flex-wrap gap-1.5">
            <Button
              type="button"
              size="sm"
              variant="destructive"
              onClick={() =>
                callbacks?.onDecideApproval?.(
                  approval.id,
                  "reject",
                  reason.trim() || null,
                )
              }
            >
              {t.confirmReject}
            </Button>
            <Button
              type="button"
              size="sm"
              variant="ghost"
              onClick={() => setRejecting(false)}
            >
              {t.back}
            </Button>
          </div>
        </div>
      ) : null}
      {mayCancel ? (
        <div>
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={() => callbacks?.onCancelApproval?.(approval.id)}
          >
            {t.cancelRequest}
          </Button>
        </div>
      ) : null}
    </li>
  );
}
