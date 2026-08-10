import * as React from "react";
import { ShieldAlert } from "lucide-react";

export interface AccessDeniedStateProps {
  title?: string;
  description?: string;
  action?: React.ReactNode;
}

export function AccessDeniedState({
  title = "Không có quyền truy cập",
  description = "Tài khoản của bạn không được cấp quyền để xem hoặc thực hiện thao tác này.",
  action,
}: AccessDeniedStateProps) {
  return (
    <div className="flex flex-col items-center justify-center p-8 text-center min-h-[300px] border border-amber-100 dark:border-amber-950/30 rounded-lg bg-amber-50/50 dark:bg-amber-950/10 space-y-4">
      <div className="flex items-center justify-center w-12 h-12 rounded-full bg-amber-100 dark:bg-amber-950 text-amber-600 dark:text-amber-400">
        <ShieldAlert className="h-6 w-6" />
      </div>
      <div className="space-y-1 max-w-sm">
        <h3 className="text-lg font-semibold tracking-tight text-amber-900 dark:text-amber-200">
          {title}
        </h3>
        <p className="text-sm text-amber-700/85 dark:text-amber-400/80">
          {description}
        </p>
      </div>
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}
