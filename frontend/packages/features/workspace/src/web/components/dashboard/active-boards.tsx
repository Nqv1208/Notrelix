import { Link } from "@tanstack/react-router";
import { LayoutGrid, ArrowRight, Users } from "lucide-react";
import { cn } from "@notrelix/ui-web";

interface Board {
  id: string;
  title: string;
  description?: string;
}

interface ActiveBoardsProps {
  workspaceId: string;
  boards: Board[];
  isLoading?: boolean;
}

const accentColors = [
  "bg-violet-500",
  "bg-sky-500",
  "bg-amber-500",
  "bg-emerald-500",
  "bg-rose-500",
  "bg-cyan-500",
];

export function ActiveBoards({
  workspaceId,
  boards,
  isLoading,
}: ActiveBoardsProps) {
  return (
    <div className="rounded-xl border border-border/60 bg-card/50 p-5">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <LayoutGrid className="size-4 text-muted-foreground" />
          <h3 className="font-semibold text-sm">Active Boards</h3>
        </div>
        {boards.length > 0 && (
          <Link
            to="/workspaces/$workspaceId"
            params={{ workspaceId }}
            className="text-xs text-muted-foreground hover:text-foreground transition-colors"
          >
            All boards
          </Link>
        )}
      </div>

      {isLoading ? (
        <div className="space-y-3">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-16 bg-muted rounded-lg animate-pulse" />
          ))}
        </div>
      ) : boards.length === 0 ? (
        <p className="text-sm text-muted-foreground py-4">
          No boards yet. Create your first board to get started.
        </p>
      ) : (
        <div className="space-y-2">
          {boards.slice(0, 4).map((board, i) => (
            <Link
              key={board.id}
              to="/workspaces/$workspaceId/boards/$boardId"
              params={{ workspaceId, boardId: board.id }}
              className="group flex items-start gap-3 p-3 rounded-lg hover:bg-muted/50 transition-colors"
            >
              <div
                className={cn(
                  "size-1 rounded-full mt-2 shrink-0",
                  accentColors[i % accentColors.length],
                )}
              />
              <div className="flex-1 min-w-0">
                <p className="font-medium text-sm truncate group-hover:text-foreground text-foreground">
                  {board.title}
                </p>
                {board.description && (
                  <p className="text-xs text-muted-foreground truncate mt-0.5">
                    {board.description}
                  </p>
                )}
              </div>
              <ArrowRight className="size-3.5 text-muted-foreground shrink-0 mt-1 opacity-0 group-hover:opacity-100 transition-opacity" />
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
