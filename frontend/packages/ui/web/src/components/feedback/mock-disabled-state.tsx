import * as React from "react";
import { Lock } from "lucide-react";

interface MockDisabledStateProps {
  featureName: string;
  title?: string;
  description?: string;
  action?: React.ReactNode;
}

export function MockDisabledState({
  featureName,
  title = "Tính năng đang được phát triển",
  description,
  action,
}: MockDisabledStateProps) {
  return (
    <div className="flex flex-col items-center justify-center p-8 text-center min-h-[300px] border border-dashed rounded-lg bg-muted/10 space-y-4">
      <div className="flex items-center justify-center w-12 h-12 rounded-full bg-primary/10 text-primary">
        <Lock className="h-6 w-6" />
      </div>
      <div className="space-y-1 max-w-sm">
        <h3 className="text-lg font-semibold tracking-tight">{title}</h3>
        <p className="text-sm text-muted-foreground">
          {description ||
            `Chế độ dữ liệu giả lập (Mock Mode) đang tắt. Tính năng "${featureName}" đang được kết nối với hệ thống backend.`}
        </p>
      </div>
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}
