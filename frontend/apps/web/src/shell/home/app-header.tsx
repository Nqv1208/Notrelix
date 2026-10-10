import { Link } from "@tanstack/react-router";
import { Menu } from "lucide-react";
import { Button, NotrelixLogoMark } from "@notrelix/ui-web";
import { UserMenu } from "./user-menu";

export function AppHeader({
  workspaceId,
  onOpenNavigation,
}: {
  workspaceId?: string;
  onOpenNavigation?: () => void;
}) {
  return (
    <header className="sticky top-0 z-[80] flex h-12 shrink-0 items-center justify-between border-b border-border bg-card/95 px-4 shadow-sm backdrop-blur-xl">
      <div className="flex min-w-0 items-center gap-2">
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label="Open navigation"
          className="md:hidden"
          onClick={onOpenNavigation}
        >
          <Menu className="size-5" />
        </Button>
        <Link to="/home" className="flex min-w-0 items-center gap-2">
          <NotrelixLogoMark className="h-5 w-auto" aria-hidden="true" />
          <span className="hidden truncate text-base font-bold text-foreground sm:block">
            Notrelix
          </span>
        </Link>
      </div>

      <UserMenu workspaceId={workspaceId} />
    </header>
  );
}
