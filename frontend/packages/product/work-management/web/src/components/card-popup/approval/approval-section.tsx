import { useId, useState, type ReactNode } from "react";
import { Check, Clock, X } from "lucide-react";
import { Skeleton, Textarea, cn } from "@notrelix/ui-web";
import { personInitials } from "@notrelix/work-management-core";
import type { ItemSheetApproval } from "../../card-detail/task-detail-types";
import { describeSheetTime } from "../../card-detail/item-sheet-format";
import { fillSheetCopy } from "../../card-detail/item-sheet.copy";
import { useItemSheetCopy } from "../../card-detail/use-item-sheet-copy";
import { useCardPopup } from "../card-popup-context";
import { PanelBtn, PersonMark } from "../popup-ui";

// The artboard's `.btn`: 32 px, 7 px radius, 6 px between the icon and the words.
const APPROVAL_BTN = "gap-1.5 rounded-[7px]";

const BANNER =
  "flex items-center gap-2.5 rounded-[8px] px-3 py-2.5 text-[13px] leading-[1.45]";

/**
 * The approvals asked about the item, in the side column above the comments: who was asked, what they
 * answered, and the buttons their role allows (the person asked approves or rejects, the requester
 * cancels, everyone else reads). Absent until one was asked; asking lives in the "⋯" menu.
 */
export function ApprovalSection() {
  const { data, copy, mode, now, timeZone } = useCardPopup();
  const t = copy.approval;
  const approvals = data.approvals ?? [];
  const sheetCopy = useItemSheetCopy();
  const firstSent = approvals[0]?.requestedAt
    ? fillSheetCopy(t.sent, {
        when: describeSheetTime(
          approvals[0].requestedAt,
          now,
          sheetCopy,
          timeZone,
        ),
      })
    : null;
  if (mode !== "view") return null;
  const failedRead = data.approvalsFailed && approvals.length === 0;
  const loading = data.approvalsLoading && approvals.length === 0;
  if (
    !loading &&
    !failedRead &&
    approvals.length === 0 &&
    !data.approvalError &&
    !data.approvalAnswerError
  )
    return null;

  return (
    <section
      aria-label={t.heading}
      data-f="item-approval"
      className="flex flex-col gap-3 rounded-[12px] border border-line bg-popover p-4"
    >
      <div className="flex items-center gap-2">
        <h3 className="m-0 text-[14px] leading-[1.45] font-[650]">
          {t.heading}
        </h3>
        {firstSent ? (
          <span className="ml-auto text-[12px] text-meta-foreground">
            {firstSent}
          </span>
        ) : null}
      </div>
      {loading ? (
        <div
          role="status"
          aria-label={t.loading}
          className="flex flex-col gap-2"
        >
          <Skeleton className="h-10 rounded-[8px]" />
        </div>
      ) : null}
      {failedRead ? (
        <div role="alert" className="flex flex-col items-start gap-2">
          <span className="text-[13px] text-danger-text">{t.loadFailed}</span>
          <RetryRead />
        </div>
      ) : null}
      {approvals.length > 0 ? (
        <ul
          aria-label={t.list}
          className="m-0 flex list-none flex-col gap-3 p-0"
        >
          {approvals.map((approval, index) => (
            <ApprovalRow
              key={approval.id}
              approval={approval}
              first={index === 0}
            />
          ))}
        </ul>
      ) : null}
      {data.approvalAnswerError ? (
        <p role="alert" className="m-0 text-[12.5px] text-danger-text">
          {data.approvalAnswerError === "conflict"
            ? t.answerConflict
            : data.approvalAnswerError === "forbidden"
              ? t.answerForbidden
              : t.answerFailed}
        </p>
      ) : null}
      {data.approvalError ? (
        <p role="alert" className="m-0 text-[12.5px] text-danger-text">
          {data.approvalError === "alreadyAsked" ? t.alreadyAsked : t.failed}
        </p>
      ) : null}
    </section>
  );
}

function RetryRead() {
  const { callbacks, copy } = useCardPopup();
  return callbacks.onReloadApprovals ? (
    <PanelBtn small onClick={callbacks.onReloadApprovals}>
      {copy.approval.retry}
    </PanelBtn>
  ) : null;
}

function ApprovalRow({
  approval,
  first,
}: {
  approval: ItemSheetApproval;
  first: boolean;
}) {
  const { data, callbacks, copy, now, timeZone } = useCardPopup();
  const sheet = useItemSheetCopy();
  const t = copy.approval;
  const viewerUserId = data.viewerUserId ?? null;
  const approver = approval.approverName?.trim() || t.unknownPerson;
  const requester = approval.requestedByName?.trim() || t.unknownPerson;
  const [rejecting, setRejecting] = useState(false);
  const [reason, setReason] = useState("");
  const reasonId = useId();
  const pending = approval.status === "pending";
  const busy = data.approvalAnswering === true;
  const mayDecide =
    pending &&
    viewerUserId !== null &&
    approval.approverUserId === viewerUserId &&
    callbacks.onDecideApproval !== undefined;
  const mayCancel =
    pending &&
    viewerUserId !== null &&
    approval.requestedByUserId === viewerUserId &&
    callbacks.onCancelApproval !== undefined;
  const when = (value?: string | null) =>
    value ? describeSheetTime(value, now, sheet, timeZone) : "";
  const sent = when(approval.requestedAt);
  const answered = when(approval.decidedAt);

  return (
    <li
      data-approval-status={approval.status}
      className={cn(
        "flex flex-col gap-3 text-[13px]",
        !first && "border-t border-line pt-3",
      )}
    >
      {sent && !first ? (
        <span className="self-end text-[12px] text-meta-foreground">
          {fillSheetCopy(t.sent, { when: sent })}
        </span>
      ) : null}
      {mayDecide ? (
        <div className="flex items-center gap-2.5">
          <PersonMark
            member={{
              userId: approval.requestedByUserId ?? requester,
              name: requester,
              initials: personInitials(requester),
            }}
            size={24}
            className="size-[26px] text-[11px]"
          />
          <p className="m-0">
            <b className="font-semibold">{requester}</b> {t.asksYou}
          </p>
        </div>
      ) : null}
      {mayDecide && approval.note ? (
        <p className="m-0 rounded-[8px] bg-background px-3 py-2.5 text-muted-foreground">
          “{approval.note}”
        </p>
      ) : null}
      {pending && !mayDecide ? (
        <Banner
          tone="warn"
          icon={<Clock aria-hidden="true" className="size-4" />}
        >
          {fillSheetCopy(t.pending, { name: approver })}
        </Banner>
      ) : null}
      {approval.status === "approved" || approval.status === "rejected" ? (
        <Banner
          tone={approval.status === "approved" ? "ok" : "bad"}
          icon={
            approval.status === "approved" ? (
              <Check aria-hidden="true" className="size-4" />
            ) : (
              <X aria-hidden="true" className="size-4" />
            )
          }
        >
          <span className="flex flex-col gap-1">
            <span>
              {fillSheetCopy(t[approval.status], { name: approver })}
              {answered ? ` · ${answered}` : ""}
            </span>
            {approval.decisionNote ? (
              <span className="font-normal text-foreground">
                “{approval.decisionNote}”
              </span>
            ) : null}
          </span>
        </Banner>
      ) : null}
      {approval.status === "cancelled" ? (
        <p className="m-0 text-meta-foreground">
          {fillSheetCopy(t.cancelled, { name: approver })}
        </p>
      ) : null}
      {pending && !mayDecide && !mayCancel ? (
        <p className="m-0 text-[12.5px] text-meta-foreground">
          {fillSheetCopy(t.onlyApprover, { name: requester })}
        </p>
      ) : null}
      {mayDecide && !rejecting ? (
        <div className="flex flex-wrap gap-2">
          <PanelBtn
            small
            className={APPROVAL_BTN}
            variant="primary"
            disabled={busy}
            onClick={() =>
              callbacks.onDecideApproval?.(approval.id, "approve", null)
            }
          >
            <Check aria-hidden="true" className="size-4" />
            {t.approve}
          </PanelBtn>
          <PanelBtn
            small
            className={APPROVAL_BTN}
            disabled={busy}
            onClick={() => setRejecting(true)}
          >
            <X aria-hidden="true" className="size-4" />
            {t.reject}
          </PanelBtn>
        </div>
      ) : null}
      {mayDecide && rejecting ? (
        <div className="flex flex-col gap-1.5">
          <label
            htmlFor={reasonId}
            className="text-[12.5px] font-semibold text-muted-foreground"
          >
            {t.reasonLabel}
          </label>
          <Textarea
            id={reasonId}
            autoFocus
            rows={3}
            value={reason}
            placeholder={t.reasonPlaceholder}
            onChange={(event) => setReason(event.target.value)}
            className="min-h-[72px] resize-none text-[13.5px]"
          />
          <span className="text-[12px] text-meta-foreground">
            {fillSheetCopy(t.reasonNote, { name: requester })}
          </span>
          <div className="flex flex-wrap justify-end gap-2">
            <PanelBtn
              small
              className={APPROVAL_BTN}
              variant="ghost"
              onClick={() => setRejecting(false)}
            >
              {t.back}
            </PanelBtn>
            <PanelBtn
              small
              className={APPROVAL_BTN}
              variant="danger"
              disabled={busy}
              onClick={() =>
                callbacks.onDecideApproval?.(
                  approval.id,
                  "reject",
                  reason.trim() || null,
                )
              }
            >
              {t.confirmReject}
            </PanelBtn>
          </div>
        </div>
      ) : null}
      {mayCancel ? (
        <div>
          <PanelBtn
            small
            className={APPROVAL_BTN}
            disabled={busy}
            onClick={() => callbacks.onCancelApproval?.(approval.id)}
          >
            {t.cancelRequest}
          </PanelBtn>
        </div>
      ) : null}
    </li>
  );
}

const BANNER_TONE = {
  warn: "bg-warning-subtle font-medium text-warning-text",
  ok: "bg-success-subtle font-semibold text-success-subtle-foreground",
  bad: "bg-danger-subtle font-semibold text-danger-subtle-foreground",
} as const;

function Banner({
  tone,
  icon,
  children,
}: {
  tone: keyof typeof BANNER_TONE;
  icon: ReactNode;
  children: ReactNode;
}) {
  return (
    // A live region: the new state is read out once the answer is in.
    <div role="status" className={cn(BANNER, BANNER_TONE[tone])}>
      <span className="flex-none">{icon}</span>
      {children}
    </div>
  );
}
