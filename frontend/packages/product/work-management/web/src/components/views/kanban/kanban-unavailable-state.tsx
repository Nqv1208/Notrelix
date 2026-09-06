import { AlertCircle } from "lucide-react";

export function KanbanUnavailableState() {
  return (
    <div className="p-4 sm:p-6">
      <div className="rounded-2xl border border-border bg-card p-8 text-center shadow-xs">
        <AlertCircle className="mx-auto mb-3 size-8 text-destructive" />
        <h2 className="text-lg font-semibold text-foreground font-display">
          Board unavailable
        </h2>
        <p className="mt-2 text-sm text-muted-foreground">
          The Kanban board could not be loaded.
        </p>
      </div>
    </div>
  );
}
