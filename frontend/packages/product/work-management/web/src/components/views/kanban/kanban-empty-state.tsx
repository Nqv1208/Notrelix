import { Inbox } from "lucide-react";

/** A board without lanes; the container passes the localised message. */
export function KanbanEmptyState({ message }: { message: string }) {
  return (
    <div className="flex flex-col items-center justify-center p-8 text-center">
      <Inbox aria-hidden="true" className="size-12 text-muted-foreground" />
      <p className="mt-2 text-label text-muted-foreground">{message}</p>
    </div>
  );
}
