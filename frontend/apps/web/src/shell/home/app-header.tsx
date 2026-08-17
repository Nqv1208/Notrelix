import { useMemo, type ComponentType } from "react";
import { Link } from "@tanstack/react-router";
import { Grip, HelpCircle, Inbox, Puzzle, Search } from "lucide-react";
import { createNotificationBell } from "@notrelix/features-notifications";
import { useAppRuntime } from "@notrelix/runtime-web";
import { NotrelixLogoMark } from "@notrelix/ui-web";
import { UserMenu } from "./user-menu";

function UtilityButton({
  icon: Icon,
  label,
}: {
  icon: ComponentType<{ className?: string }>;
  label: string;
}) {
  return (
    <button
      type="button"
      disabled
      aria-label={label}
      title={label}
      className="relative rounded-lg p-2 text-muted-foreground transition-colors disabled:cursor-not-allowed disabled:opacity-60"
    >
      <Icon className="size-[18px]" />
    </button>
  );
}

export function AppHeader({ workspaceId }: { workspaceId?: string }) {
  const { api: runtimeClient, env: runtimeEnv } = useAppRuntime();
  const NotificationBell = useMemo(
    () =>
      createNotificationBell({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
        options: { mockMode: runtimeEnv.mockApi },
      }),
    [runtimeClient, runtimeEnv.mockApi],
  );

  return (
    <header className="sticky top-0 z-[80] flex h-12 shrink-0 items-center justify-between border-b border-border bg-card/95 px-4 shadow-sm backdrop-blur-xl">
      <Link to="/home" className="flex min-w-0 items-center gap-2">
        <NotrelixLogoMark className="h-5 w-auto" aria-hidden="true" />
        <span className="hidden truncate text-lg font-bold text-foreground sm:block">
          Notrelix{" "}
          <span className="font-normal text-muted-foreground">
            work management
          </span>
        </span>
      </Link>

      <div className="flex items-center gap-1 sm:gap-2">
        <NotificationBell />
        <UtilityButton icon={Inbox} label="Inbox" />
        <UtilityButton icon={Puzzle} label="Integrations" />
        <UtilityButton icon={Search} label="Search" />
        <UtilityButton icon={HelpCircle} label="Help" />
        <div className="mx-1 h-6 w-px bg-border" />
        <UtilityButton icon={Grip} label="Notrelix apps" />
        <UserMenu workspaceId={workspaceId} />
      </div>
    </header>
  );
}
