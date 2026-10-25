import { CheckCheck } from "lucide-react";
import type { Board } from "@notrelix/work-management-core";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import type {
  ItemSheetApproval,
  TaskDetailData,
} from "../card-detail/task-detail-types";
import type { ItemPageCopy } from "./item-page.copy";
import type { ItemPagePeople } from "./item-page-types";
import { BUTTON_SM_VARIANT_CLASS } from "./item-page-parts";
import { ItemListPicker, PickerPopover } from "./item-pickers";

/**
 * The item's approvals: who was asked and what they answered, and, for a person who can edit the item,
 * "Request approval" with a board member picker (never the viewer). Absent when the host offers no
 * approvals at all.
 */
export function ItemApprovalSection({
  board,
  people,
  viewerUserId,
  data,
  canRequest,
  onRequest,
  copy,
}: {
  board: Board;
  /** The workspace's people; the board's members without it. */
  people?: ItemPagePeople;
  viewerUserId?: string;
  data: TaskDetailData | undefined;
  canRequest: boolean;
  onRequest?: (approverUserId: string) => void;
  copy: ItemPageCopy;
}) {
  const t = copy.approval;
  const approvals = data?.approvals;
  if (approvals === undefined) return null;
  const askable = canRequest && onRequest !== undefined;
  if (!askable && approvals.length === 0) return null;

  // Whoever can read the board may be asked: its members, and everyone when it is workspace-wide.
  const candidates = people
    ? people.people.filter(
        (person) => person.onBoard || board.visibility === "workspace",
      )
    : board.members;
  const options = candidates
    .filter((person) => person.userId !== viewerUserId && person.name)
    .map((person) => ({ id: person.userId, label: person.name }));

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
      {askable ? (
        <div>
          <PickerPopover
            trigger={
              <button
                type="button"
                data-f="request-approval"
                className={BUTTON_SM_VARIANT_CLASS.outline}
              >
                <CheckCheck aria-hidden="true" className="size-3.5" />
                {t.request}
              </button>
            }
          >
            {(close) => (
              <ItemListPicker
                idPrefix="item-approver"
                options={options}
                currentId={null}
                searchPlaceholder={t.searchPlaceholder}
                searchLabel={t.searchLabel}
                noMatch={t.noMatch}
                onSelect={(userId) => {
                  onRequest?.(userId);
                  close();
                }}
              />
            )}
          </PickerPopover>
        </div>
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
