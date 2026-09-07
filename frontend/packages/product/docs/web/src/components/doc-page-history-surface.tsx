import type { PageActivity } from "@notrelix/docs-core";
import { Clock, FileText, MessageSquare, Sparkles } from "lucide-react";
import {
  actionLabel,
  formatDate,
} from "./doc-page-surface-models";

export function DocHistorySurface({ history }: { history: PageActivity[] }) {
  return (
    <section className="space-y-4" aria-label="Document history">
      <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
        <Clock className="h-4 w-4" />
        History ({history.length})
      </div>
      {history.length === 0 ? (
        <p className="text-sm italic text-muted-foreground">No history yet</p>
      ) : (
        <div className="space-y-1">
          {history.map((activity) => (
            <div
              key={activity.id}
              className="flex items-start gap-3 rounded-lg px-2 py-2.5 hover:bg-muted/50"
            >
              <div className="mt-0.5 text-muted-foreground">
                {activity.action === "published" ? (
                  <Sparkles className="h-3.5 w-3.5" />
                ) : activity.action === "commented" ? (
                  <MessageSquare className="h-3.5 w-3.5" />
                ) : (
                  <FileText className="h-3.5 w-3.5" />
                )}
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-sm">
                  <span className="font-medium">{activity.actorId}</span>{" "}
                  <span className="text-muted-foreground">
                    {actionLabel(activity.action)}
                  </span>
                </p>
                {activity.targetLabel ? (
                  <p className="mt-0.5 truncate text-xs text-muted-foreground">
                    {activity.targetLabel}
                  </p>
                ) : null}
              </div>
              <span className="whitespace-nowrap text-xs text-muted-foreground">
                {formatDate(activity.createdAt)}
              </span>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}