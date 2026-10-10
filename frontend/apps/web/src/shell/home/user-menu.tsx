import { useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import {
  BellRing,
  Laptop,
  LogOut,
  Moon,
  Palette,
  Sun,
  User,
} from "lucide-react";
import { createUseLogout, useCurrentUser } from "@notrelix/features-auth";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  Avatar,
  AvatarFallback,
  AvatarImage,
  cn,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  useTheme,
} from "@notrelix/ui-web";

export function UserMenu({ workspaceId }: { workspaceId?: string }) {
  const user = useCurrentUser();
  const navigate = useNavigate();
  const { api: runtimeClient } = useAppRuntime();
  const { theme, setTheme } = useTheme();

  const useLogout = useMemo(
    () =>
      createUseLogout({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
        navigate: (options) =>
          navigate({ to: options.to, replace: options.replace }),
        getSearchParams: () => new URLSearchParams(window.location.search),
      }),
    [navigate, runtimeClient],
  );
  const logoutMutation = useLogout();

  const displayName = user?.name || "Notrelix User";
  const initials = displayName
    .split(" ")
    .filter(Boolean)
    .map((part) => part.charAt(0))
    .join("")
    .toUpperCase()
    .slice(0, 2);

  const navigateToAccount = (
    page: "profile" | "appearance" | "notifications",
  ) => {
    if (!workspaceId) return;
    navigate({
      to: `/workspaces/$workspaceId/account/${page}`,
      params: { workspaceId },
    });
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          aria-label="User settings"
          className="relative size-8 rounded-full focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        >
          <Avatar className="size-8">
            <AvatarImage src={user?.avatarUrl || ""} alt={displayName} />
            <AvatarFallback className="bg-primary text-xs font-semibold text-primary-foreground">
              {initials || "NU"}
            </AvatarFallback>
          </Avatar>
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent
        aria-label="User tools"
        className="w-72 rounded-xl border-border p-2 shadow-lg"
        align="end"
        sideOffset={8}
      >
        <div className="flex items-center gap-3 px-2 py-2">
          <Avatar className="size-9">
            <AvatarImage src={user?.avatarUrl || ""} alt={displayName} />
            <AvatarFallback className="bg-primary text-xs font-semibold text-primary-foreground">
              {initials || "NU"}
            </AvatarFallback>
          </Avatar>
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-foreground">
              {displayName}
            </p>
            <p className="truncate text-xs text-muted-foreground">
              {user?.email || "Account settings"}
            </p>
          </div>
        </div>

        <DropdownMenuSeparator />

        <DropdownMenuItem
          disabled={!workspaceId}
          onSelect={() => navigateToAccount("profile")}
          className="gap-2"
        >
          <User className="size-4 text-muted-foreground" />
          My profile
        </DropdownMenuItem>
        <DropdownMenuItem
          disabled={!workspaceId}
          onSelect={() => navigateToAccount("notifications")}
          className="gap-2"
        >
          <BellRing className="size-4 text-muted-foreground" />
          Notifications
        </DropdownMenuItem>
        <DropdownMenuItem
          disabled={!workspaceId}
          onSelect={() => navigateToAccount("appearance")}
          className="gap-2"
        >
          <Palette className="size-4 text-muted-foreground" />
          Appearance
        </DropdownMenuItem>

        <div className="mt-1 flex items-center justify-between rounded-lg bg-muted px-2 py-2">
          <span className="text-xs font-medium text-muted-foreground">
            Theme
          </span>
          <div className="flex items-center gap-1">
            {(
              [
                ["light", Sun, "Light theme"],
                ["dark", Moon, "Dark theme"],
                ["system", Laptop, "System theme"],
              ] as const
            ).map(([value, Icon, label]) => (
              <button
                key={value}
                type="button"
                aria-label={label}
                onClick={(event) => {
                  event.stopPropagation();
                  setTheme(value);
                }}
                className={cn(
                  "rounded-md p-1.5 text-muted-foreground transition hover:text-foreground",
                  theme === value && "bg-background text-primary shadow-sm",
                )}
              >
                <Icon className="size-3.5" />
              </button>
            ))}
          </div>
        </div>

        <DropdownMenuSeparator />

        <DropdownMenuItem
          disabled={logoutMutation.isPending}
          onSelect={() => logoutMutation.mutate()}
          className="gap-2 text-destructive focus:text-destructive"
        >
          <LogOut className="size-4" />
          {logoutMutation.isPending ? "Logging out..." : "Log out"}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
