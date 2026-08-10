import {
  createUsePageBreadcrumb,
  createUseToggleFavorite,
  type DocsApiClient,
  type PageApiEndpoints,
} from "@notrelix/docs-state";
import { Button } from "@notrelix/ui-web";
import {
  Star,
  Share2,
  MoreHorizontal,
  ChevronRight,
  BookOpen,
} from "lucide-react";
import { useState } from "react";

interface DocPageHeaderProps {
  api: DocsApiClient;
  endpoints: PageApiEndpoints;
  workspaceId: string;
  pageId: string;
  pageTitle: string;
  isFavorited: boolean;
}

export function DocPageHeader({
  api,
  endpoints,
  workspaceId,
  pageId,
  isFavorited,
}: DocPageHeaderProps) {
  const usePageBreadcrumb = createUsePageBreadcrumb(api, endpoints);
  const useToggleFavorite = createUseToggleFavorite(api, endpoints);

  const { data: breadcrumbs = [] } = usePageBreadcrumb(workspaceId, pageId);
  const toggleFavoriteMutation = useToggleFavorite(workspaceId, pageId);

  const [showActions, setShowActions] = useState(false);

  return (
    <div className="border-b">
      {/* Breadcrumb */}
      <div className="px-8 py-2 flex items-center gap-1.5 text-xs text-muted-foreground">
        <BookOpen className="h-3.5 w-3.5" />
        <ChevronRight className="h-3 w-3" />
        {breadcrumbs.map((bc, idx) => (
          <div key={bc.id} className="flex items-center gap-1">
            <span>
              {bc.icon && `${bc.icon} `}
              {bc.title}
            </span>
            {idx < breadcrumbs.length - 1 && (
              <ChevronRight className="h-3 w-3" />
            )}
          </div>
        ))}
      </div>

      {/* Actions bar */}
      <div className="px-8 py-2 flex items-center justify-between">
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            className={`h-7 gap-1.5 text-xs ${
              isFavorited
                ? "text-yellow-500 hover:text-yellow-600"
                : "text-muted-foreground hover:text-foreground"
            }`}
            onClick={() => toggleFavoriteMutation.mutate()}
          >
            <Star
              className={`h-3.5 w-3.5 ${isFavorited ? "fill-yellow-500" : ""}`}
            />
            {isFavorited ? "Favorited" : "Favorite"}
          </Button>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 gap-1.5 text-xs text-muted-foreground hover:text-foreground"
          >
            <Share2 className="h-3.5 w-3.5" />
            Share
          </Button>
        </div>

        <div className="relative">
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7 text-muted-foreground hover:text-foreground"
            onClick={() => setShowActions(!showActions)}
          >
            <MoreHorizontal className="h-4 w-4" />
          </Button>
          {showActions && (
            <>
              <div
                className="fixed inset-0 z-40"
                onClick={() => setShowActions(false)}
              />
              <div className="absolute right-0 top-8 z-50 w-48 bg-popover border border-border rounded-lg shadow-lg overflow-hidden">
                <div className="p-1">
                  <button className="w-full text-left px-3 py-2 text-sm rounded-md hover:bg-muted transition-colors">
                    Duplicate
                  </button>
                  <button className="w-full text-left px-3 py-2 text-sm rounded-md hover:bg-muted transition-colors">
                    Move to...
                  </button>
                  <button className="w-full text-left px-3 py-2 text-sm rounded-md hover:bg-muted transition-colors">
                    Export
                  </button>
                  <div className="h-px bg-border my-1" />
                  <button className="w-full text-left px-3 py-2 text-sm rounded-md hover:bg-destructive/10 text-destructive transition-colors">
                    Delete
                  </button>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
