"use client"

import type { ReactNode } from "react"
import { useState } from "react"
import Link from "next/link"
import { usePathname, useSearchParams } from "next/navigation"
import {
  Bell,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Home,
  Inbox,
  LifeBuoy,
  MessageSquareText,
  MoreHorizontal,
  Search,
  Settings,
  Star,
  UserRoundCheck,
} from "lucide-react"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { useWorkspaceSnapshot } from "../hooks/queries/use-workspace-snapshot"
import type { WorkspaceMember, WorkspaceFavorite } from "../types"
import { GlobalSearchDialog } from "@/features/search"
import { cn } from "@/lib/utils"

interface WorkspaceSidebarProps {
  workspaceId: string
  collapsed: boolean
  onCollapse: () => void
  onExpand?: () => void
  inSheet?: boolean
}

type NavItem = {
  label: string
  icon: typeof Home
  href: string
  panel?: string
  path?: string
}

export function WorkspaceSidebar({ workspaceId, collapsed, onCollapse, onExpand, inSheet }: WorkspaceSidebarProps) {
  const pathname = usePathname()
  // react-doctor-disable-next-line react-doctor/nextjs-no-use-search-params-without-suspense
  const searchParams = useSearchParams()
  const panel = searchParams.get("panel")
  const [searchOpen, setSearchOpen] = useState(false)

  const { data: snapshot } = useWorkspaceSnapshot(workspaceId)

  const members = snapshot?.members ?? []
  const favorites = snapshot?.favorites ?? []

  const primaryNav: NavItem[] = [
    { label: "Home", icon: Home, href: `/${workspaceId}` },
    { label: "My Work", icon: UserRoundCheck, href: `/${workspaceId}?panel=my-work`, panel: "my-work" },
    { label: "Inbox", icon: Inbox, href: `/${workspaceId}?panel=inbox`, panel: "inbox" },
    { label: "Notifications", icon: Bell, href: `/${workspaceId}?panel=notifications`, panel: "notifications" },
    { label: "Chat Rooms", icon: MessageSquareText, href: `/${workspaceId}/chat`, path: `/${workspaceId}/chat` },
  ]
  const supportNav: NavItem[] = [
    { label: "Help / Support", icon: LifeBuoy, href: `/${workspaceId}?panel=support`, panel: "support" },
    { label: "Settings", icon: Settings, href: `/${workspaceId}?panel=settings`, panel: "settings" },
  ]

  const avatarColors = [
    "bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300",
    "bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300",
    "bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300",
    "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300",
    "bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300",
    "bg-cyan-100 text-cyan-700 dark:bg-cyan-900/40 dark:text-cyan-300",
    "bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300",
  ]

  return (
    <aside
      className={cn(
        "group/workspace-sidebar relative z-20 shrink-0 bg-card/90 text-card-foreground backdrop-blur-xl shadow-sm rounded-l-xl ml-2 h-full transition-all duration-300 ease-in-out",
        inSheet ? "block" : "hidden lg:block",
        inSheet && "h-svh",
        collapsed ? "w-4 hover:w-6" : "w-[280px]"
      )}
    >
      <div className={cn("flex h-full flex-col transition-opacity duration-200", collapsed ? "opacity-0 pointer-events-none" : "opacity-100")}>

        <div className="p-4">
          <button
            onClick={() => setSearchOpen(true)}
            className="flex h-9 w-full items-center gap-2 rounded-lg border border-border bg-muted px-3 text-sm text-muted-foreground transition hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            <Search className="size-4" />
            <span className="min-w-0 flex-1 text-left">Search workspace</span>
          </button>
        </div>

        <ScrollArea className="min-h-0 flex-1 px-4">
          <nav className="space-y-1.5">
            {primaryNav.map((item) => (
              <SidebarNavLink
                key={item.label}
                item={item}
                collapsed={false}
                active={isActiveWorkspaceNav({ item, pathname, panel, workspaceId })}
              />
            ))}
          </nav>

          <div className="space-y-6 mt-6 pb-6">
            <SidebarSection title="Quick access">
              <div className="space-y-1">
                {favorites.map((item: WorkspaceFavorite) => (
                  <Link
                    key={item.id}
                    href={item.href as never}
                    className="group flex items-center gap-2 rounded-lg px-2 py-1.5 text-sm text-muted-foreground transition hover:bg-muted hover:text-foreground"
                  >
                    <Star className="size-3.5 text-primary" />
                    <span className="min-w-0 flex-1 truncate">{item.title}</span>
                    <MoreHorizontal className="size-3.5 opacity-0 group-hover:opacity-100" />
                  </Link>
                ))}
              </div>
            </SidebarSection>

            <SidebarSection title="Team online">
              <div className="space-y-1">
                {members.map((member: WorkspaceMember, i: number) => (
                  <div key={member.id} className="flex items-center gap-2 rounded-lg px-2 py-1.5 text-sm">
                    <Avatar className="size-6">
                      <AvatarFallback className={cn("text-[10px]", avatarColors[i % avatarColors.length])}>
                        {member.initials}
                      </AvatarFallback>
                    </Avatar>
                    <span className="min-w-0 flex-1 truncate text-muted-foreground">{member.name}</span>
                    <span
                      className={cn(
                        "size-2 rounded-full",
                        member.status === "active"
                          ? "bg-primary"
                          : member.status === "in-call"
                            ? "bg-accent-foreground"
                            : member.status === "idle"
                              ? "bg-muted-foreground"
                              : "bg-border"
                      )}
                    />
                  </div>
                ))}
              </div>
            </SidebarSection>
          </div>
        </ScrollArea>

        {/* Footer Area - Fixed at bottom via flex layout */}
        <div className="mt-auto border-t border-border/60 bg-muted/5 p-4 shrink-0">
          <div className="space-y-1">
            {supportNav.map((item) => (
              <SidebarNavLink
                key={item.label}
                item={item}
                collapsed={false}
                active={isActiveWorkspaceNav({ item, pathname, panel, workspaceId })}
              />
            ))}
          </div>
        </div>
      </div>
      
      <GlobalSearchDialog open={searchOpen} onOpenChange={setSearchOpen} />

      {!inSheet ? (
        <button
          type="button"
          aria-label={collapsed ? "Expand workspace sidebar" : "Collapse workspace sidebar"}
          onClick={collapsed ? onExpand : onCollapse}
          className={cn(
            "absolute right-[-14px] top-5 z-30 hidden size-7 items-center justify-center rounded-full border border-border bg-card text-muted-foreground transition hover:bg-muted hover:text-foreground lg:flex",
            collapsed ? "opacity-100 shadow-md" : "opacity-0 group-hover/workspace-sidebar:opacity-100 shadow-sm"
          )}
        >
          {collapsed ? <ChevronRight className="size-4" /> : <ChevronLeft className="size-4" />}
        </button>
      ) : null}
    </aside>
  )
}

function isActiveWorkspaceNav({
  item,
  pathname,
  panel,
  workspaceId,
}: {
  item: NavItem
  pathname: string
  panel: string | null
  workspaceId: string
}) {
  if (item.path) return pathname.startsWith(item.path)
  if (item.panel) return pathname === `/${workspaceId}` && panel === item.panel
  return pathname === item.href
}

function SidebarNavLink({ item, collapsed, active }: { item: NavItem; collapsed: boolean; active: boolean }) {
  const content = (
    <Link
      href={item.href as never}
      className={cn(
        "flex h-9 items-center gap-2 rounded-lg px-2 text-sm font-medium text-muted-foreground transition hover:bg-muted hover:text-foreground",
        active && "bg-accent text-accent-foreground",
        collapsed && "justify-center"
      )}
    >
      <item.icon className="size-4 shrink-0" />
      {!collapsed ? <span className="min-w-0 flex-1 truncate">{item.label}</span> : null}
    </Link>
  )

  if (!collapsed) return content

  return (
    <Tooltip>
      <TooltipTrigger asChild>{content}</TooltipTrigger>
      <TooltipContent side="right">{item.label}</TooltipContent>
    </Tooltip>
  )
}

function SidebarSection({ title, children }: { title: string; children: ReactNode }) {
  return (
    <Collapsible defaultOpen className="mt-5">
      <CollapsibleTrigger className="mb-2 flex w-full items-center justify-between px-2 text-[11px] font-semibold uppercase tracking-[0.08em] text-muted-foreground">
        {title}
        <ChevronDown className="size-3.5" />
      </CollapsibleTrigger>
      <CollapsibleContent>{children}</CollapsibleContent>
    </Collapsible>
  )
}
