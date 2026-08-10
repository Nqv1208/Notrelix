import * as React from "react";
import { LockKeyhole } from "lucide-react";

import { cn } from "../../lib/cn";

export interface UpgradeRequiredStateProps {
  title?: string;
  description?: string;
  action?: React.ReactNode;
  className?: string;
}

export function UpgradeRequiredState({
  title = "Upgrade required",
  description = "Your current plan does not include access to this capability.",
  action,
  className,
}: UpgradeRequiredStateProps) {
  return (
    <div
      className={cn(
        "flex min-h-[300px] flex-col items-center justify-center rounded-lg border border-primary/15 bg-primary/5 p-8 text-center space-y-4",
        className,
      )}
    >
      <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
        <LockKeyhole className="h-6 w-6" />
      </div>
      <div className="max-w-sm space-y-1">
        <h3 className="text-lg font-semibold tracking-tight text-foreground">
          {title}
        </h3>
        <p className="text-sm text-muted-foreground">{description}</p>
      </div>
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}
