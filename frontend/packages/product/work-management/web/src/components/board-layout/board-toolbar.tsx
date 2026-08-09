import { Search, Filter, Settings } from "lucide-react";
import { Button, Input } from "@notrelix/ui-web";
import { ViewTabs, AddViewMenu } from "./view-tabs";

interface BoardToolbarProps {
  boardTitle: string;
  activeView: string;
  onViewChange: (viewType: string) => void;
  searchQuery?: string;
  onSearchChange?: (query: string) => void;
}

export function BoardToolbar({
  boardTitle,
  activeView,
  onViewChange,
  searchQuery = "",
  onSearchChange,
}: BoardToolbarProps) {
  return (
    <div className="flex items-center gap-3 border-b px-4 py-2">
      <h2 className="text-sm font-semibold text-foreground truncate">
        {boardTitle}
      </h2>

      <div className="h-4 w-px bg-border" />

      <ViewTabs activeView={activeView} onViewChange={onViewChange} />

      <div className="flex-1" />

      {onSearchChange && (
        <div className="relative">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
          <Input
            placeholder="Search..."
            value={searchQuery}
            onChange={(e) => onSearchChange(e.target.value)}
            className="h-7 w-48 pl-8 text-xs"
          />
        </div>
      )}

      <Button
        variant="ghost"
        size="icon"
        className="h-7 w-7 text-muted-foreground hover:text-foreground"
      >
        <Filter className="h-3.5 w-3.5" />
      </Button>

      <AddViewMenu onAddView={onViewChange} />
    </div>
  );
}
