import type { PageActivity } from "@notrelix/docs-core";

export interface DocPageSurfaceCallbacks {
  onAddPage?: (parentId: string | null) => void;
  onToggleFavorite?: () => void;
  onShare?: () => void;
  onAction?: (action: string) => void;
  onUpdateBlockText?: (blockId: string, text: string) => void;
  onToggleTodo?: (blockId: string, checked: boolean) => void;
  onDeleteBlock?: (blockId: string) => void;
  onCreateComment?: (body: string) => void;
  onDeleteComment?: (commentId: string) => void;
}

export function formatDate(value: string): string {
  return value.slice(0, 10);
}

export function actionLabel(action: PageActivity["action"]): string {
  switch (action) {
    case "created":
      return "created this page";
    case "edited":
      return "edited this page";
    case "commented":
      return "commented on this page";
    case "shared":
      return "shared this page";
    case "moved":
      return "moved this page";
    case "published":
      return "published this page";
  }
}
