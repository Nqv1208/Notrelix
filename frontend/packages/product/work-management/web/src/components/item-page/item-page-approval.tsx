import type { Board } from "@notrelix/work-management-core";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import type {
  ItemSheetApproval,
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
}: {
  data: TaskDetailData | undefined;
  copy: ItemPageCopy;
}) {
  const t = copy.approval;
  const approvals = data?.approvals ?? [];
  if (approvals.length === 0 && !data?.approvalError) return null;

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
            <ApprovalRow key={approval.id} approval={approval} copy={copy} />
          ))}
        </ul>
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
}: {
  approval: ItemSheetApproval;
  copy: ItemPageCopy;
}) {
  const t = copy.approval;
  const name = approval.approverName?.trim() || t.unknownPerson;
  return (
    <li
      data-approval-status={approval.status}
      className="flex flex-col gap-0.5 text-item-small"
    >
      <span className={`font-medium ${STATUS_CLASS[approval.status]}`}>
        {fillSheetCopy(t[approval.status], { name })}
      </span>
      {approval.decisionNote ? (
        <span className="text-item-note text-muted-foreground">
          {approval.decisionNote}
        </span>
      ) : null}
    </li>
  );
}
