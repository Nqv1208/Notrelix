"use client"

import type { ReactNode } from "react"
import Link from "next/link"
import { usePathname, useSearchParams } from "next/navigation"
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
  Plus,
  Search,
  Settings,
  Sparkles,
  Star,
  UserRoundCheck,
  Users,
  Workflow,
} from "lucide-react"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { mockWorkspaceSnapshot } from "@/features/workspace/mock/mock-data"
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
  const searchParams = useSearchParams()
  const panel = searchParams.get("panel")
  const workspace = mockWorkspaceSnapshot.workspace
  const members = mockWorkspaceSnapshot.members
  const primaryNav: NavItem[] = [
    { label: "Home", icon: Home, href: `/${workspaceId}` },
    { label: "My Work", icon: UserRoundCheck, href: `/${workspaceId}?panel=my-work`, panel: "my-work" },
    { label: "Inbox", icon: Inbox, href: `/${workspaceId}?panel=inbox`, panel: "inbox" },
    { label: "Chat Rooms", icon: MessageSquareText, href: `/${workspaceId}/chat`, path: `/${workspaceId}/chat` },
  ]
  const managementNav: NavItem[] = [
    { label: "Members", icon: Users, href: `/${workspaceId}?panel=members`, panel: "members" },
    { label: "Settings", icon: Settings, href: `/${workspaceId}?panel=settings`, panel: "settings" },
    { label: "Permissions", icon: LockKeyhole, href: `/${workspaceId}?panel=permissions`, panel: "permissions" },
    { label: "Integrations", icon: Sparkles, href: `/${workspaceId}?panel=integrations`, panel: "integrations" },
    { label: "Automations", icon: Workflow, href: `/${workspaceId}?panel=automations`, panel: "automations" },
    { label: "Activity Logs", icon: Activity, href: `/${workspaceId}?panel=activity`, panel: "activity" },
  ]
  const supportNav: NavItem[] = [
    { label: "Notifications", icon: Bell, href: `/${workspaceId}?panel=notifications`, panel: "notifications" },
    { label: "Help / Support", icon: LifeBuoy, href: `/${workspaceId}?panel=support`, panel: "support" },
  ]

  return (
    <aside
      className={cn(
        "group/workspace-sidebar relative z-20 h-full shrink-0 border-r border-border bg-card/95 text-card-foreground backdrop-blur-xl transition-[width] duration-200",
        inSheet ? "block" : "hidden lg:block",
        inSheet && "h-svh",
        collapsed ? "w-[68px]" : "w-[292px]"
      )}
    >
      <div className="flex h-full flex-col">
        {/* <div className="flex h-14 items-center gap-2 border-b border-border px-3">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                className={cn(
                  "flex min-w-0 flex-1 items-center gap-2 rounded-lg text-left outline-none transition hover:bg-muted focus-visible:ring-2 focus-visible:ring-ring",
                  collapsed && "justify-center"
                )}
              >
                <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-primary text-sm font-semibold text-primary-foreground">
                  {workspace.icon}
                </div>
                {!collapsed ? (
                  <>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-semibold text-foreground">{workspace.name}</p>
                      <p className="text-[11px] text-muted-foreground">Workspace switcher</p>
                    </div>
                    <ChevronDown className="size-4 text-muted-foreground" />
                  </>
                ) : null}
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="w-64">
              <DropdownMenuLabel>Workspaces</DropdownMenuLabel>
              <DropdownMenuItem>
                <span className="flex size-6 items-center justify-center rounded-md bg-primary text-[10px] font-semibold text-primary-foreground">
                  {workspace.icon}
                </span>
                {workspace.name}
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem>
                <Plus className="size-4" />
                Create workspace
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div> */}

        <div className="p-4">
          <button
            className={cn(
              "flex h-9 w-full items-center gap-2 rounded-lg border border-border bg-muted px-3 text-sm text-muted-foreground transition hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
              collapsed && "justify-center px-0"
            )}
          >
            <Search className="size-4" />
            {!collapsed ? <span className="min-w-0 flex-1 text-left">Search workspace</span> : null}
          </button>
        </div>

        <ScrollArea className="min-h-0 flex-1 px-4">
          <nav className="space-y-1">
            {primaryNav.map((item) => (
              <SidebarNavLink
                key={item.label}
                item={item}
                collapsed={collapsed}
                active={isActiveWorkspaceNav({ item, pathname, panel, workspaceId })}
              />
            ))}
          </nav>

          {!collapsed ? (
            <>
              <SidebarSection title="Workspace management">
                <div className="space-y-1">
                  {managementNav.map((item) => (
                    <SidebarNavLink
                      key={item.label}
                      item={item}
                      collapsed={false}
                      active={isActiveWorkspaceNav({ item, pathname, panel, workspaceId })}
                    />
                  ))}
                </div>
              </SidebarSection>

              <SidebarSection title="Quick access">
                <div className="space-y-1">
                  {mockWorkspaceSnapshot.favorites.map((item) => (
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

              <SidebarSection title="Recently viewed">
                <div className="space-y-1">
                  {mockWorkspaceSnapshot.recent.map((item) => (
                    <Link
                      key={item.id}
                      href={item.href as never}
                      className="group flex items-center gap-2 rounded-lg px-2 py-1.5 text-sm text-muted-foreground transition hover:bg-muted hover:text-foreground"
                    >
                      <span className="flex size-5 shrink-0 items-center justify-center rounded-md bg-muted text-[11px] text-foreground">
                        {item.icon}
                      </span>
                      <span className="min-w-0 flex-1 truncate">{item.title}</span>
                      <span className="text-[11px] text-muted-foreground">{item.updatedAt}</span>
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

              <div className="mb-5 space-y-1">
                {supportNav.map((item) => (
                  <SidebarNavLink
                    key={item.label}
                    item={item}
                    collapsed={false}
                    active={isActiveWorkspaceNav({ item, pathname, panel, workspaceId })}
                  />
                ))}
              </div>
            </>
          ) : (
            <div className="mt-4 space-y-1">
              {[...managementNav, ...supportNav].map((item) => (
                <SidebarNavLink
                  key={item.label}
                  item={item}
                  collapsed
                  active={isActiveWorkspaceNav({ item, pathname, panel, workspaceId })}
                />
              ))}
            </div>
          )}
        </ScrollArea>

        <div className="border-t border-border p-3">
          <Button className={cn("w-full rounded-full", collapsed && "px-0")} size={collapsed ? "icon-sm" : "sm"}>
            <Plus className="size-4" />
            {!collapsed ? "Create item" : null}
          </Button>
        </div>
      </div>
      {!inSheet ? (
        <button
          type="button"
          aria-label={collapsed ? "Expand workspace sidebar" : "Collapse workspace sidebar"}
          onClick={collapsed ? onExpand : onCollapse}
          className="absolute right-[-14px] top-5 z-30 hidden size-7 items-center justify-center rounded-full border border-border bg-card text-muted-foreground opacity-0 shadow-sm transition group-hover/workspace-sidebar:opacity-100 hover:bg-muted hover:text-foreground lg:flex"
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
