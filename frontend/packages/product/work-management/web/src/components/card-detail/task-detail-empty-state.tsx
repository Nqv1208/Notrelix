import type { LucideIcon } from "lucide-react";

export function TaskDetailEmptyState({
  icon: Icon,
  title,
  description,
}: {
  icon: LucideIcon;
  title: string;
  description: string;
}) {
  return (
    <div className="flex min-h-56 flex-col items-center justify-center rounded-lg border border-dashed border-border bg-card p-6 text-center">
      <div className="mb-3 flex size-10 items-center justify-center rounded-full bg-muted text-primary">
        <Icon className="size-5" />
      </div>
      <h3 className="text-sm font-semibold text-foreground">{title}</h3>
      <p className="mt-1 max-w-xs text-sm leading-6 text-muted-foreground">
        {description}
      </p>
    </div>
  );
}
