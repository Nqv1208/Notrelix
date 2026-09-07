import { Skeleton } from "@notrelix/ui-web";
import { ErrorState, NotFoundState } from "@notrelix/ui-web";
import type { ReactNode } from "react";

export type BoardWorkspaceSurfaceStatus =
  | "ready"
  | "loading"
  | "error"
  | "unsupported";

interface BoardWorkspaceSurfaceProps {
  status: BoardWorkspaceSurfaceStatus;
  viewContent?: ReactNode;
  skeletonRows?: number;
  error?: unknown;
  errorTitle?: string;
  errorDescription?: string;
  unsupportedViewName?: string;
}

export function BoardWorkspaceSurface({
  status,
  viewContent,
  skeletonRows = 6,
  error,
  errorTitle = "Bảng công việc không khả dụng",
  errorDescription = "Bảng công việc có thể đã bị di chuyển, lưu trữ hoặc bạn không có quyền truy cập.",
  unsupportedViewName,
}: BoardWorkspaceSurfaceProps) {
  if (status === "loading") return <ViewSkeleton rows={skeletonRows} />;
  if (status === "error") {
    return (
      <div className="p-4 sm:p-6">
        <ErrorState
          error={error}
          title={errorTitle}
          description={errorDescription}
        />
      </div>
    );
  }
  if (status === "unsupported") {
    return (
      <div className="p-4 sm:p-6">
        <NotFoundState
          title={`${unsupportedViewName ?? "Chế độ xem"} không phải là chế độ xem bảng`}
          description="Vui lòng sử dụng các tab workspace để mở chế độ xem bảng hoặc chuyển sang phần Tài liệu."
        />
      </div>
    );
  }
  return <div className="h-full overflow-auto p-4 sm:p-6">{viewContent}</div>;
}

function ViewSkeleton({ rows }: { rows: number }) {
  return (
    <div className="p-4 sm:p-6">
      <div className="rounded-2xl border border-border bg-card p-4">
        <Skeleton className="mb-4 h-10 rounded-xl" />
        {Array.from({ length: rows }).map((_, index) => (
          <Skeleton key={index} className="mb-2 h-12 rounded-xl last:mb-0" />
        ))}
      </div>
    </div>
  );
}
