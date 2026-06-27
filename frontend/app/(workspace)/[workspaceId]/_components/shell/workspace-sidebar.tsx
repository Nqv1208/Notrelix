"use client"

import type { ReactNode } from "react"
import { useState } from "react"
import Link from "next/link"
import { usePathname, useRouter, useSearchParams } from "next/navigation"
import {
  Activity,
  Bell,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Home,
  Inbox,
  LifeBuoy,
  LockKeyhole,
  MessageSquareText,
  MoreHorizontal,
  Search,
  Settings,
  Sparkles,
  Star,
  UserRoundCheck,
  Users,
  Workflow,
} from "lucide-react"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { useWorkspaceSnapshot } from "@/features/workspace"
import { GlobalSearchDialog } from "@/app/(dashboard)/_components/header/global-search-dialog"
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
  const router = useRouter()
  // react-doctor-disable-next-line react-doctor/nextjs-no-use-search-params-without-suspense
  const searchParams = useSearchParams()
  const panel = searchParams.get("panel")
  const [searchOpen, setSearchOpen] = useState(false)

  const { data: snapshot } = useWorkspaceSnapshot(workspaceId)

  const workspace = snapshot?.workspace ?? { id: workspaceId, name: "Loading...", slug: workspaceId, icon: "W" }
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
                {favorites.map((item) => (
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
                {members.map((member) => (
                  <div key={member.id} className="flex items-center gap-2 rounded-lg px-2 py-1.5 text-sm">
                    <Avatar className="size-6">
                      <AvatarFallback className="text-[10px] text-primary-foreground" style={{ backgroundColor: member.color }}>
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
