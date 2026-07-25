import { Loader2 } from 'lucide-react';

export function RootSuspenseFallback() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="flex flex-col items-center gap-3">
        <Loader2 className="w-8 h-8 animate-spin text-primary" />
        <p className="text-xs text-muted-foreground font-medium">Loading Notrelix Workspace...</p>
      </div>
    </div>
  );
}
