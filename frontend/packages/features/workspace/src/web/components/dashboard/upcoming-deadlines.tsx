import { Clock } from "lucide-react";
import { cn } from "@notrelix/ui-web";

interface Deadline {
  id: string;
  title: string;
  dueDate: string;
  priority: "urgent" | "high" | "medium" | "low";
  source?: string;
}

interface UpcomingDeadlinesProps {
  deadlines: Deadline[];
  isLoading?: boolean;
}

const priorityDot: Record<string, string> = {
  urgent: "bg-red-500",
  high: "bg-amber-500",
  medium: "bg-violet-500",
  low: "bg-slate-400",
};

function formatDateLabel(dateStr: string) {
  const date = new Date(dateStr);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const target = new Date(date);
  target.setHours(0, 0, 0, 0);
  const diffDays = Math.round((target.getTime() - today.getTime()) / 86400000);

  if (diffDays === 0) return "Today";
  if (diffDays === 1) return "Tomorrow";
  if (diffDays < 7)
    return date.toLocaleDateString("en-US", { weekday: "long" });
  return date.toLocaleDateString("en-US", { month: "short", day: "numeric" });
}

export function UpcomingDeadlines({
  deadlines,
  isLoading,
}: UpcomingDeadlinesProps) {
  const grouped = deadlines.reduce<Record<string, Deadline[]>>((acc, d) => {
    const label = formatDateLabel(d.dueDate);
    (acc[label] ??= []).push(d);
    return acc;
  }, {});

  return (
    <div className="rounded-xl border border-border/60 bg-card/50 p-5">
      <div className="flex items-center gap-2 mb-4">
        <Clock className="size-4 text-muted-foreground" />
        <h3 className="font-semibold text-sm">Upcoming Deadlines</h3>
      </div>

      {isLoading ? (
        <div className="space-y-3">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-10 bg-muted rounded animate-pulse" />
          ))}
        </div>
      ) : deadlines.length === 0 ? (
        <p className="text-sm text-muted-foreground py-4">
          Your tasks with due dates will appear in this timeline.
        </p>
      ) : (
        <div className="space-y-4">
          {Object.entries(grouped).map(([label, items]) => (
            <div key={label}>
              <p className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground mb-2">
                {label}
              </p>
              <div className="space-y-1">
                {items.map((item) => (
                  <div
                    key={item.id}
                    className="flex items-center gap-3 p-2 rounded-md hover:bg-muted/30 transition-colors"
                  >
                    <span
                      className={cn(
                        "size-2 rounded-full shrink-0",
                        priorityDot[item.priority],
                      )}
                    />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm truncate text-foreground">
                        {item.title}
                      </p>
                      {item.source && (
                        <p className="text-[11px] text-muted-foreground truncate">
                          {item.source}
                        </p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
