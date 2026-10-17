import { Check, CircleAlert, Info, X } from "lucide-react";
import { toast } from "sonner";
import type { ItemPageToast } from "@notrelix/work-management-web";

/**
 * The notice of the item page (the States mockup): a dark card at the bottom
 * left with the message, what it adds, and the one thing that can be done
 * about it. A success goes by itself after a few seconds and carries its undo;
 * a failure stays until it is dealt with.
 */
const ICON = {
  success: <Check aria-hidden="true" className="size-4 text-inverse-success" />,
  error: (
    <CircleAlert aria-hidden="true" className="size-4 text-inverse-danger" />
  ),
  info: <Info aria-hidden="true" className="size-4 text-inverse-muted" />,
} as const;

function Title({ notice }: { notice: ItemPageToast }) {
  const { emphasis, title } = notice;
  const at = emphasis ? title.indexOf(emphasis) : -1;
  if (!emphasis || at < 0) return <>{title}</>;
  return (
    <>
      {title.slice(0, at)}
      <u>{emphasis}</u>
      {title.slice(at + emphasis.length)}
    </>
  );
}

function NoticeCard({
  id,
  notice,
}: {
  id: string | number;
  notice: ItemPageToast;
}) {
  const { action } = notice;
  return (
    <div
      role={notice.kind === "error" ? "alert" : "status"}
      className="flex w-105 items-center gap-2.5 rounded-[10px] bg-inverse-surface py-2.5 pr-2.5 pl-3.5 text-compact text-inverse-foreground shadow-lg"
    >
      {ICON[notice.kind]}
      <span className="min-w-0 flex-1">
        <span className="block">
          <Title notice={notice} />
        </span>
        {notice.description ? (
          <small className="block text-inverse-muted">
            {notice.description}
          </small>
        ) : null}
      </span>
      {action ? (
        <button
          type="button"
          onClick={() => {
            toast.dismiss(id);
            action.onAction();
          }}
          className="inline-flex shrink-0 cursor-pointer items-center gap-1.5 rounded-md border-0 bg-transparent px-2 py-1 font-semibold text-inverse-foreground"
        >
          {action.label}
          {action.hint ? (
            <kbd className="rounded-[4px] border border-inverse-muted px-1 font-mono text-key font-normal text-inverse-muted">
              {action.hint}
            </kbd>
          ) : null}
        </button>
      ) : null}
      {notice.kind === "error" ? (
        <button
          type="button"
          aria-label={notice.dismissLabel}
          onClick={() => toast.dismiss(id)}
          className="grid size-6.5 shrink-0 cursor-pointer place-items-center rounded-md border-0 bg-transparent text-inverse-muted"
        >
          <X aria-hidden="true" className="size-3.5" />
        </button>
      ) : null}
    </div>
  );
}

/** Shows the notice; returns its id so the caller can drop it when it is undone. */
export function showItemNotice(
  notice: ItemPageToast,
  onGone: (id: string | number) => void,
): string | number {
  const id: string | number = toast.custom(
    (toastId) => <NoticeCard id={toastId} notice={notice} />,
    {
      // A failure stays until it is dealt with; a success goes by itself.
      duration: notice.kind === "error" ? Infinity : undefined,
      onDismiss: () => onGone(id),
      onAutoClose: () => onGone(id),
    },
  );
  return id;
}
