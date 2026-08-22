import { useMemo, type ComponentType, type ReactNode } from "react";
import { useNavigate } from "@tanstack/react-router";
import {
  Archive,
  BellRing,
  ChevronRight,
  Code2,
  Command,
  Download,
  FlaskConical,
  Gem,
  HelpCircle,
  Laptop,
  LogOut,
  Moon,
  Palette,
  Puzzle,
  Rocket,
  Settings,
  Smartphone,
  Sparkles,
  Sun,
  Trash2,
  User,
  UserPlus,
  Users,
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
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  useTheme,
} from "@notrelix/ui-web";

interface UserMenuItemProps {
  icon: ComponentType<{ className?: string }>;
  label: string;
  badge?: string;
  rightElement?: ReactNode;
  onSelect?: () => void;
  danger?: boolean;
  disabled?: boolean;
}

function UserMenuItem({
  icon: Icon,
  label,
  badge,
  rightElement,
  onSelect,
  danger,
  disabled,
}: UserMenuItemProps) {
  return (
    <DropdownMenuItem
      disabled={disabled}
      onSelect={onSelect}
      className="cursor-pointer gap-3 rounded-md px-2 py-1.5 text-popover-foreground focus:bg-muted"
    >
      <Icon
        className={cn(
          "size-4 text-muted-foreground",
          danger && "text-destructive",
        )}
      />
      <span
        className={cn(
          "flex-1 text-[13px] text-foreground",
          danger && "text-destructive",
        )}
      >
        {label}
      </span>
      {badge ? (
        <span className="rounded border border-primary px-1.5 py-0.5 text-[10px] font-medium text-primary">
          {badge}
        </span>
      ) : null}
      {rightElement}
    </DropdownMenuItem>
  );
}

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
    if (page === "profile") {
      navigate({
        to: "/workspaces/$workspaceId/account/profile",
        params: { workspaceId },
      });
    } else if (page === "appearance") {
      navigate({
        to: "/workspaces/$workspaceId/account/appearance",
        params: { workspaceId },
      });
    } else {
      navigate({
        to: "/workspaces/$workspaceId/account/notifications",
        params: { workspaceId },
      });
    }
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          aria-label="User settings"
          className="relative ml-2 size-8 rounded-full focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
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
        className="w-[500px] rounded-xl border-border p-0 shadow-lg"
        align="end"
        sideOffset={8}
      >
        <div className="flex items-center gap-3 p-4">
          <div className="flex size-6 items-center justify-center rounded-md bg-primary text-xs font-bold text-primary-foreground">
            N
          </div>
          <span className="text-[15px] font-medium text-foreground">
            {displayName}&apos;s team
          </span>
        </div>
        <DropdownMenuSeparator className="m-0" />

        <div className="flex">
          <div className="flex-1 border-r border-border p-2">
            <DropdownMenuLabel className="mb-1 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Account
            </DropdownMenuLabel>
            <UserMenuItem
              icon={User}
              label="My profile"
              disabled={!workspaceId}
              onSelect={() => navigateToAccount("profile")}
            />
            <UserMenuItem icon={Download} label="Import data" disabled />
            <UserMenuItem icon={Code2} label="Developers" disabled />
            <UserMenuItem icon={Rocket} label="Spaces" badge="Alpha" disabled />
            <UserMenuItem icon={Trash2} label="Trash" disabled />
            <UserMenuItem icon={Archive} label="Archive" disabled />
            <UserMenuItem icon={Sparkles} label="AI usage" disabled />
            <UserMenuItem
              icon={Settings}
              label="Administration"
              disabled={!workspaceId}
              onSelect={() => navigateToAccount("profile")}
            />
            <UserMenuItem icon={Users} label="Teams" disabled />
            <UserMenuItem
              icon={LogOut}
              label={logoutMutation.isPending ? "Logging out..." : "Log out"}
              danger
              disabled={logoutMutation.isPending}
              onSelect={() => logoutMutation.mutate()}
            />
          </div>

          <div className="flex-1 p-2">
            <DropdownMenuLabel className="mb-1 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Explore
            </DropdownMenuLabel>
            <UserMenuItem icon={Puzzle} label="Marketplace" disabled />
            <UserMenuItem icon={Smartphone} label="Mobile app" disabled />
            <UserMenuItem icon={FlaskConical} label="notrelix.labs" disabled />
            <UserMenuItem icon={Command} label="Shortcuts" disabled />
            <DropdownMenuSeparator className="my-2" />
            <UserMenuItem icon={UserPlus} label="Invite members" disabled />
            <UserMenuItem icon={HelpCircle} label="Help" disabled />

            <DropdownMenuItem
              onSelect={(event) => event.preventDefault()}
              className="flex cursor-default items-center justify-between gap-2 rounded-md px-2 py-1.5 focus:bg-transparent"
            >
              <button
                type="button"
                disabled={!workspaceId}
                onClick={() => navigateToAccount("appearance")}
                className="flex items-center gap-3 text-[13px] text-foreground disabled:opacity-50"
              >
                <Palette className="size-4 text-muted-foreground" />
                Theme
              </button>
              <div className="flex items-center gap-0.5 rounded-lg border border-border bg-muted p-0.5">
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
                      "rounded-md p-1 text-muted-foreground transition",
                      theme === value && "bg-background text-primary shadow-sm",
                    )}
                  >
                    <Icon className="size-3" />
                  </button>
                ))}
              </div>
            </DropdownMenuItem>

            <div className="mt-4 px-2">
              <button
                type="button"
                disabled
                className="flex w-full items-center justify-center gap-2 rounded-md bg-primary py-1.5 text-[13px] font-medium text-primary-foreground disabled:opacity-60"
              >
                <Gem className="size-3.5" />
                Upgrade
              </button>
            </div>
          </div>
        </div>

        <DropdownMenuSeparator className="m-0" />
        <button
          type="button"
          disabled={!workspaceId}
          onClick={() => navigateToAccount("notifications")}
          className="flex w-full items-center justify-between rounded-b-xl p-3 text-left transition hover:bg-muted disabled:opacity-50"
        >
          <span className="flex items-center gap-2 text-[13px] text-foreground">
            <BellRing className="size-3.5 text-muted-foreground" />
            Do not disturb
          </span>
          <span className="flex items-center gap-1 text-xs text-muted-foreground">
            More <ChevronRight className="size-3" />
          </span>
        </button>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
