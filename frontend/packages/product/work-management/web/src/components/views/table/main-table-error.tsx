import { AlertCircle } from "lucide-react";
import { mainTableCopy, type MainTableCopy } from "./main-table.copy";

/** The table could not be loaded. */
export function MainTableError({
  copy = mainTableCopy("vi"),
  message,
}: {
  copy?: MainTableCopy;
  /** Replaces the default description. */
  message?: string;
}) {
  return (
    <div className="px-4 py-6 sm:px-6" data-slot="main-table-unavailable">
      <div
        role="status"
        className="rounded-[12px] border border-border bg-popover p-8 text-center dark:bg-card"
      >
        <AlertCircle
          className="mx-auto mb-3 size-8 text-destructive"
          aria-hidden="true"
        />
        <h2 className="text-[0.9375rem] font-semibold text-foreground">
          {copy.unavailable.title}
        </h2>
        <p className="mt-2 text-nav font-normal text-muted-foreground">
          {message ?? copy.unavailable.description}
        </p>
      </div>
    </div>
  );
}
