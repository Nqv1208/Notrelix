import { useState } from "react";
import type { BreadcrumbItem } from "@notrelix/docs-core";
import { Button } from "@notrelix/ui-web";
import {
  BookOpen,
  ChevronRight,
  MoreHorizontal,
  Share2,
  Star,
} from "lucide-react";
import type { DocPageSurfaceCallbacks } from "./doc-page-surface-models";

export function DocPageHeaderSurface({
  pageTitle,
  breadcrumbs,
  isFavorited,
  callbacks,
}: {
  pageTitle: string;
  breadcrumbs: BreadcrumbItem[];
  isFavorited: boolean;
  callbacks?: DocPageSurfaceCallbacks;
}) {
  const [showActions, setShowActions] = useState(false);

  return (
    <header className="border-b bg-background">
      <div className="flex items-center gap-1.5 px-6 py-2 text-xs text-muted-foreground">
        <BookOpen className="h-3.5 w-3.5" />
        {breadcrumbs.map((breadcrumb, index) => (
          <span key={breadcrumb.id} className="flex min-w-0 items-center gap-1">
            {index > 0 ? <ChevronRight className="h-3 w-3" /> : null}
            <span className="truncate">
              {breadcrumb.icon ? `${breadcrumb.icon} ` : ""}
              {breadcrumb.title}
            </span>
          </span>
        ))}
      </div>

      <div className="flex items-center justify-between gap-3 px-6 py-3">
        <h1 className="min-w-0 truncate text-lg font-semibold">{pageTitle}</h1>
        <div className="flex shrink-0 items-center gap-1">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="gap-1.5"
            aria-pressed={isFavorited}
            onClick={() => callbacks?.onToggleFavorite?.()}
          >
            <Star
              className={`h-3.5 w-3.5 ${isFavorited ? "fill-yellow-500 text-yellow-500" : ""}`}
            />
            {isFavorited ? "Favorited" : "Favorite"}
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="gap-1.5"
            onClick={() => callbacks?.onShare?.()}
          >
            <Share2 className="h-3.5 w-3.5" />
            Share
          </Button>
          <div className="relative">
            <Button
              type="button"
              variant="ghost"
              size="icon"
              aria-label="Document actions"
              aria-expanded={showActions}
              onClick={() => setShowActions((value) => !value)}
            >
              <MoreHorizontal className="h-4 w-4" />
            </Button>
            {showActions ? (
              <div className="absolute right-0 top-10 z-20 w-48 overflow-hidden rounded-lg border bg-popover p-1 shadow-lg">
                {["Duplicate", "Move to...", "Export", "Delete"].map(
                  (action) => (
                    <button
                      key={action}
                      type="button"
                      className="w-full rounded-md px-3 py-2 text-left text-sm hover:bg-muted"
                      onClick={() => callbacks?.onAction?.(action)}
                    >
                      {action}
                    </button>
                  ),
                )}
              </div>
            ) : null}
          </div>
        </div>
      </div>
    </header>
  );
}