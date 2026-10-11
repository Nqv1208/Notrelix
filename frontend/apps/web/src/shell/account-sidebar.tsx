import { Link, useLocation } from "@tanstack/react-router";
import { ArrowLeft, Eye, Bell, Shield, User } from "lucide-react";
import { useAccountNavCopy } from "@notrelix/features-account";
import { cn } from "@notrelix/ui-web";

const navItems = [
  {
    key: "profile",
    icon: User,
    to: "/workspaces/$workspaceId/account/profile" as const,
  },
  {
    key: "appearance",
    icon: Eye,
    to: "/workspaces/$workspaceId/account/appearance" as const,
  },
  {
    key: "security",
    icon: Shield,
    to: "/workspaces/$workspaceId/account/security" as const,
  },
  {
    key: "notifications",
    icon: Bell,
    to: "/workspaces/$workspaceId/account/notifications" as const,
  },
] as const;

export function AccountSidebar({ workspaceId }: { workspaceId: string }) {
  const location = useLocation();
  const copy = useAccountNavCopy();

  return (
    <aside className="hidden w-56 shrink-0 border-r border-border bg-card md:block">
      <div className="flex h-full flex-col p-4">
        <Link
          to="/workspaces/$workspaceId"
          params={{ workspaceId }}
          className="mb-4 flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft aria-hidden="true" className="size-4 shrink-0" />
          {copy.back}
        </Link>
        <nav aria-label={copy.label} className="flex flex-col gap-1">
          {navItems.map((item) => {
            const isActive = location.pathname.includes(
              item.to.split("/").pop() ?? "",
            );
            return (
              <Link
                key={item.to}
                to={item.to}
                params={{ workspaceId }}
                aria-current={isActive ? "page" : undefined}
                className={cn(
                  "flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
                  isActive
                    ? "bg-primary/10 text-link"
                    : "text-muted-foreground hover:bg-muted hover:text-foreground",
                )}
              >
                <item.icon aria-hidden="true" className="size-4 shrink-0" />
                {copy[item.key]}
              </Link>
            );
          })}
        </nav>
      </div>
    </aside>
  );
}
