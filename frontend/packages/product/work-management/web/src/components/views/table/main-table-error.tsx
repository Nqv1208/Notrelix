import { AlertCircle } from "lucide-react";

export function MainTableError({
  message = "The board table could not be loaded.",
}: {
  message?: string;
}) {
  return (
    <div className="p-4" data-slot="main-table-unavailable">
      <div
        role="status"
        className="rounded-lg border border-border bg-card p-8 text-center"
      >
        <AlertCircle className="mx-auto mb-3 size-8 text-destructive" />
        <h2 className="text-lg font-semibold text-foreground">
          Table unavailable
        </h2>
        <p className="mt-2 text-sm text-muted-foreground">{message}</p>
      </div>
    </div>
  );
}
