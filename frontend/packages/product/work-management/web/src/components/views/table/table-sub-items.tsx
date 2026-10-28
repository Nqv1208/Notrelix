import { Check } from "lucide-react";
import { useItemChildItems } from "@notrelix/work-management-state";
import type { MainTableCopy } from "./main-table.copy";

/**
 * The sub-items of one table row, loaded when the row is expanded: key, title
 * (opens the sub-item) and whether it is done.
 */
export function TableSubItems({
  itemId,
  workspaceId,
  boardId,
  copy,
  onOpen,
}: {
  itemId: string;
  workspaceId: string;
  boardId: string;
  copy: MainTableCopy;
  onOpen: (id: string) => void;
}) {
  const children = useItemChildItems(itemId, workspaceId, boardId);
  if (children.isPending) {
    return (
      <p role="status" className="m-0 py-1 text-crumb text-meta-foreground">
        {copy.row.childrenLoading}
      </p>
    );
  }
  if (children.isError) {
    return (
      <p role="alert" className="m-0 py-1 text-crumb text-danger-text">
        {copy.row.childrenFailed}
      </p>
    );
  }
  return (
    <ul className="m-0 flex list-none flex-col p-0">
      {(children.data ?? []).map((child) => (
        <li
          key={child.id}
          data-sub-item=""
          className="flex h-7 items-center gap-2 text-nav"
        >
          <span
            aria-hidden="true"
            className="grid size-4 shrink-0 place-items-center text-meta-foreground"
          >
            {child.done ? <Check className="size-4" strokeWidth={2} /> : null}
          </span>
          {child.key ? (
            <span className="font-mono text-metadata text-meta-foreground">
              {child.key}
            </span>
          ) : null}
          <button
            type="button"
            onClick={() => onOpen(child.id)}
            className={
              "min-w-0 truncate border-0 bg-transparent p-0 text-left text-nav focus-visible:outline-2 focus-visible:outline-ring " +
              (child.done
                ? "text-meta-foreground line-through"
                : "text-foreground")
            }
          >
            {child.title}
          </button>
        </li>
      ))}
    </ul>
  );
}
