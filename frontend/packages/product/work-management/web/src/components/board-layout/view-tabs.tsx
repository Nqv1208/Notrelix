import { LayoutGrid, Table, Calendar, GanttChart, Plus } from 'lucide-react';
import { Button } from '@notrelix/ui-web';
import { useState } from 'react';

const VIEW_TYPES = [
  { type: 'kanban', label: 'Board', icon: LayoutGrid },
  { type: 'table', label: 'Table', icon: Table },
  { type: 'calendar', label: 'Calendar', icon: Calendar },
  { type: 'timeline', label: 'Timeline', icon: GanttChart },
] as const;

interface ViewTabsProps {
  activeView: string;
  onViewChange: (viewType: string) => void;
}

export function ViewTabs({ activeView, onViewChange }: ViewTabsProps) {
  return (
    <div className="flex items-center gap-1">
      {VIEW_TYPES.map((view) => {
        const Icon = view.icon;
        const isActive = activeView === view.type;
        return (
          <button
            key={view.type}
            onClick={() => onViewChange(view.type)}
            className={`flex items-center gap-1.5 px-2.5 py-1.5 rounded-md text-xs font-medium transition-colors ${
              isActive
                ? 'bg-muted text-foreground'
                : 'text-muted-foreground hover:bg-muted/50 hover:text-foreground'
            }`}
          >
            <Icon className="h-3.5 w-3.5" />
            {view.label}
          </button>
        );
      })}
    </div>
  );
}

export function AddViewMenu({ onAddView }: { onAddView: (type: string) => void }) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <div className="relative">
      <Button
        variant="ghost"
        size="sm"
        className="h-7 gap-1 text-xs text-muted-foreground hover:text-foreground"
        onClick={() => setIsOpen(!isOpen)}
      >
        <Plus className="h-3.5 w-3.5" />
        Add view
      </Button>
      {isOpen && (
        <>
          <div className="fixed inset-0 z-40" onClick={() => setIsOpen(false)} />
          <div className="absolute right-0 top-8 z-50 w-40 bg-popover border border-border rounded-lg shadow-lg overflow-hidden">
            <div className="p-1">
              {VIEW_TYPES.map((view) => {
                const Icon = view.icon;
                return (
                  <button
                    key={view.type}
                    onClick={() => {
                      onAddView(view.type);
                      setIsOpen(false);
                    }}
                    className="w-full flex items-center gap-2 px-3 py-2 text-sm rounded-md hover:bg-muted transition-colors"
                  >
                    <Icon className="h-4 w-4 text-muted-foreground" />
                    {view.label}
                  </button>
                );
              })}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
