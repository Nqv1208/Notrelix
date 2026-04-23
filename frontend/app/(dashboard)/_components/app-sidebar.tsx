"use client"

import * as React from "react"
import Link from "next/link"
import { useRouter, usePathname } from "next/navigation"
import {
  Kanban,
  FileText,
  ListTodo,
  Clock,
  Settings,
  Plus,
  ChevronRight,
  MoreHorizontal,
  Star,
  Trash2,
  LogOut,
  ChevronsUpDown,
  Users,
  StarOff,
} from "lucide-react"

import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupAction,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuAction,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarSeparator,
} from "@/components/ui/sidebar"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible"
import { cn } from "@/lib/utils"
import { routes } from "@/lib/routes"
import { useEditorStore } from "@/features/documents/store/editor-store"
import type { Page } from "@/features/documents/types/document.types"

/** Trạng thái active giống SaaS: item “All Tasks” chỉ sáng đúng trang chủ dashboard */
function isGeneralNavActive(pathname: string, href: string, id: string) {
  if (id === "all-tasks") {
    return pathname === "/dashboard" || pathname === "/dashboard/"
  }
  return pathname === href || pathname.startsWith(`${href}/`)
}

const generalNavItems = [
  {
    id: "all-tasks",
    icon: Kanban,
    label: "All Tasks",
    href: "/dashboard",
  },
  { id: "docs", icon: FileText, label: "Docs", href: "/dashboard/search" },
  { id: "todo", icon: ListTodo, label: "To-Do List", href: "/dashboard/notifications" },
  { id: "time", icon: Clock, label: "Time Tracker", href: "/dashboard/calendar" },
  { id: "settings", icon: Settings, label: "Settings", href: "/dashboard/settings" },
] as const

const navActiveClass =
  "data-[active=true]:bg-violet-100 data-[active=true]:text-violet-700 data-[active=true]:font-medium dark:data-[active=true]:bg-violet-950/45 dark:data-[active=true]:text-violet-200"

function PageTreeItem({ page, workspaceId, depth = 0 }: { page: Page; workspaceId: string; depth?: number }) {
  const router = useRouter()
  const pathname = usePathname()
  const { addPage } = useEditorStore()
  const href = routes.workspace.page(workspaceId, page.id) as string
  const isActive = pathname === href
  const hasChildren = page.children && page.children.length > 0

  const handleAddSubPage = (e: React.MouseEvent) => {
    e.stopPropagation()
    const newId = addPage(workspaceId, page.id)
    router.push(routes.workspace.page(workspaceId, newId) as never)
  }

  if (hasChildren) {
    return (
      <Collapsible defaultOpen className="group/collapsible">
        <SidebarMenuSubItem>
          <div className="flex min-w-0 items-center gap-0.5 pr-1">
            <SidebarMenuSubButton
              asChild
              className={cn(
                "group/item min-w-0 flex-1 justify-start rounded-md",
                isActive && "bg-sidebar-accent font-medium"
              )}
            >
              <Link href={href as never} className="flex min-w-0 flex-1 items-center gap-1.5">
                <span className="shrink-0 text-sm">{page.icon}</span>
                <span className="truncate">{page.title}</span>
              </Link>
            </SidebarMenuSubButton>
            <CollapsibleTrigger asChild>
              <button
                type="button"
                className="text-sidebar-foreground ring-sidebar-ring hover:bg-sidebar-accent hover:text-sidebar-accent-foreground inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-md outline-hidden focus-visible:ring-2 [&>svg]:size-4"
                aria-label={page.title ? `Expand: ${page.title}` : "Expand page branch"}
              >
                <ChevronRight className="size-3.5 text-muted-foreground transition-transform group-data-[state=open]/collapsible:rotate-90" />
              </button>
            </CollapsibleTrigger>
          </div>
          <PageContextMenu page={page} workspaceId={workspaceId} onAddSubPage={handleAddSubPage} />
          <CollapsibleContent>
            <SidebarMenuSub>
              {page.children!.filter((c) => !c.isDeleted).map((child) => (
                <PageTreeItem key={child.id} page={child} workspaceId={workspaceId} depth={depth + 1} />
              ))}
              <SidebarMenuSubItem>
                <SidebarMenuSubButton
                  onClick={handleAddSubPage}
                  className="text-muted-foreground hover:text-foreground"
                >
                  <Plus className="size-3.5" />
                  <span>Add a page</span>
                </SidebarMenuSubButton>
              </SidebarMenuSubItem>
            </SidebarMenuSub>
          </CollapsibleContent>
        </SidebarMenuSubItem>
      </Collapsible>
    )
  }

  return (
    <SidebarMenuSubItem>
      <SidebarMenuSubButton asChild className={cn("rounded-md", isActive && "bg-sidebar-accent font-medium")}>
        <Link href={href as never}>
          <span className="text-sm">{page.icon}</span>
          <span className="truncate">{page.title}</span>
        </Link>
      </SidebarMenuSubButton>
      <PageContextMenu page={page} workspaceId={workspaceId} onAddSubPage={handleAddSubPage} />
    </SidebarMenuSubItem>
  )
}

function PageContextMenu({
  page,
  workspaceId,
  onAddSubPage,
}: {
  page: Page
  workspaceId: string
  onAddSubPage: (e: React.MouseEvent) => void
}) {
  const { deletePage, toggleFavorite } = usePageActions(page, workspaceId)

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <SidebarMenuAction showOnHover className="data-[state=open]:opacity-100">
          <MoreHorizontal className="size-4" />
        </SidebarMenuAction>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-48">
        <DropdownMenuItem onClick={onAddSubPage}>
          <Plus className="mr-2 size-4" />
          Add sub-page
        </DropdownMenuItem>
        <DropdownMenuItem onClick={toggleFavorite}>
          {page.isFavorite ? (
            <>
              <StarOff className="mr-2 size-4" />
              Remove from favorites
            </>
          ) : (
            <>
              <Star className="mr-2 size-4" />
              Add to favorites
            </>
          )}
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={deletePage} className="text-destructive">
          <Trash2 className="mr-2 size-4" />
          Delete
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

function usePageActions(page: Page, workspaceId: string) {
  const store = useEditorStore()
  return {
    deletePage: () => store.deletePage(page.id),
    toggleFavorite: () => store.toggleFavorite(page.id),
  }
}

export function AppSidebar() {
  const pathname = usePathname()
  const router = useRouter()
  const { workspaces, addPage, getFavoritePages } = useEditorStore()
  const favorites = getFavoritePages()

  const handleAddPage = (wsId: string) => {
    const newId = addPage(wsId)
    router.push(routes.workspace.page(wsId, newId) as never)
  }

  return (
    <Sidebar collapsible="icon" className="border-r border-border/60 bg-card">
      <SidebarHeader className="gap-3 border-b border-border/50 px-3 pb-4 pt-3">
        <SidebarMenu>
          <SidebarMenuItem>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <SidebarMenuButton
                  size="lg"
                  className="h-auto min-h-14 gap-3 rounded-xl border border-border/40 bg-background px-3 py-2.5 shadow-sm transition-colors hover:bg-muted/50 data-[state=open]:bg-muted/60"
                >
                  <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-linear-to-br from-violet-500 to-purple-600 text-lg text-white shadow-sm">
                    <span aria-hidden className="font-semibold tracking-tight">
                      N
                    </span>
                  </div>
                  <div className="flex min-w-0 flex-1 flex-col gap-0.5 text-left leading-tight">
                    <span className="truncate font-semibold tracking-tight text-foreground">Notrelix</span>
                    <span className="truncate text-xs text-muted-foreground">Free Version</span>
                  </div>
                  <ChevronsUpDown className="ml-auto size-4 shrink-0 text-muted-foreground" />
                </SidebarMenuButton>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="w-[--radix-dropdown-menu-trigger-width]">
                <DropdownMenuItem>
                  <Settings className="mr-2 size-4" />
                  Workspace settings
                </DropdownMenuItem>
                <DropdownMenuItem>
                  <Users className="mr-2 size-4" />
                  Invite members
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem>
                  <Plus className="mr-2 size-4" />
                  Create workspace
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>

      <SidebarContent className="gap-0 px-2 py-4">
        <SidebarGroup className="p-0">
          <SidebarGroupLabel className="mb-2 px-2 text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/90">
            General
          </SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu className="gap-0.5">
              {generalNavItems.map((item) => {
                const active = isGeneralNavActive(pathname, item.href, item.id)
                return (
                  <SidebarMenuItem key={item.id}>
                    <SidebarMenuButton
                      asChild
                      tooltip={item.label}
                      isActive={active}
                      className={cn(
                        "h-9 rounded-lg px-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted/80 hover:text-foreground",
                        navActiveClass
                      )}
                    >
                      <Link href={item.href as never} className="flex w-full items-center gap-3">
                        <item.icon
                          className={cn(
                            "size-[18px] shrink-0 stroke-[1.75]",
                            active && "text-violet-600 dark:text-violet-300"
                          )}
                          aria-hidden
                        />
                        <span className="truncate">{item.label}</span>
                      </Link>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                )
              })}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        {favorites.length > 0 && (
          <>
            <SidebarSeparator className="my-4 bg-border/60" />
            <SidebarGroup className="p-0">
              <SidebarGroupLabel className="mb-2 px-2 text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/90">
                Favorites
              </SidebarGroupLabel>
              <SidebarGroupContent>
                <SidebarMenu className="gap-0.5">
                  {favorites.map((page) => (
                    <SidebarMenuItem key={page.id}>
                      <SidebarMenuButton
                        asChild
                        tooltip={page.title}
                        isActive={
                          pathname === (routes.workspace.page(page.workspaceId, page.id) as string)
                        }
                        className={cn("h-9 rounded-lg", navActiveClass)}
                      >
                        <Link
                          href={routes.workspace.page(page.workspaceId, page.id) as never}
                          className="flex w-full min-w-0 items-center gap-3"
                        >
                          <span className="text-base leading-none">{page.icon}</span>
                          <span className="truncate">{page.title}</span>
                        </Link>
                      </SidebarMenuButton>
                      <SidebarMenuAction
                        showOnHover
                        onClick={() => useEditorStore.getState().toggleFavorite(page.id)}
                      >
                        <Star className="size-4 fill-amber-400 text-amber-500" />
                      </SidebarMenuAction>
                    </SidebarMenuItem>
                  ))}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          </>
        )}

        {workspaces.map((workspace) => (
          <React.Fragment key={workspace.id}>
            <SidebarSeparator className="my-4 bg-border/60" />
            <SidebarGroup className="p-0">
              <SidebarGroupLabel className="mb-2 flex items-center gap-1.5 px-2 text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/90">
                <span>{workspace.icon}</span>
                <span className="truncate normal-case tracking-normal">{workspace.name}</span>
              </SidebarGroupLabel>
              <SidebarGroupAction
                onClick={() => handleAddPage(workspace.id)}
                className="text-muted-foreground hover:text-foreground"
              >
                <Plus className="size-4" />
                <span className="sr-only">Add page</span>
              </SidebarGroupAction>
              <SidebarGroupContent>
                <SidebarMenu>
                  <SidebarMenuSub className="mx-0 border-l border-border/50 pl-2">
                    {workspace.pages
                      .filter((p) => !p.isDeleted && !p.parentId)
                      .map((page) => (
                        <PageTreeItem key={page.id} page={page} workspaceId={workspace.id} />
                      ))}
                    <SidebarMenuSubItem>
                      <SidebarMenuSubButton
                        onClick={() => handleAddPage(workspace.id)}
                        className="rounded-md text-muted-foreground hover:text-foreground"
                      >
                        <Plus className="size-3.5" />
                        <span>New page</span>
                      </SidebarMenuSubButton>
                    </SidebarMenuSubItem>
                  </SidebarMenuSub>
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          </React.Fragment>
        ))}

        <SidebarSeparator className="my-4 bg-border/60" />

        <SidebarGroup className="p-0">
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton
                  asChild
                  tooltip="Trash"
                  isActive={pathname === "/dashboard/trash"}
                  className={cn("h-9 rounded-lg", navActiveClass)}
                >
                  <Link href="/dashboard/trash" className="flex w-full items-center gap-3">
                    <Trash2 className="size-[18px] shrink-0 stroke-[1.75]" aria-hidden />
                    <span>Trash</span>
                  </Link>
                </SidebarMenuButton>
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter className="border-t border-border/50 p-2">
        <SidebarMenu>
          <SidebarMenuItem>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <SidebarMenuButton
                  size="lg"
                  className="h-auto min-h-14 gap-3 rounded-xl border border-transparent px-2 py-2 hover:bg-muted/70 data-[state=open]:bg-muted/80"
                >
                  <Avatar className="size-9 border border-border/50 shadow-sm">
                    <AvatarImage src="/avatars/user.jpg" alt="" />
                    <AvatarFallback className="bg-linear-to-br from-violet-500 to-purple-600 text-sm text-white">
                      AD
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex min-w-0 flex-1 flex-col gap-0.5 text-left leading-tight">
                    <span className="truncate font-medium">Admin</span>
                    <span className="truncate text-xs text-muted-foreground">admin@notrelix.app</span>
                  </div>
                  <ChevronsUpDown className="ml-auto size-4 shrink-0 text-muted-foreground" />
                </SidebarMenuButton>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" side="top" className="w-[--radix-dropdown-menu-trigger-width]">
                <DropdownMenuItem>
                  <Settings className="mr-2 size-4" />
                  Account settings
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem className="text-destructive">
                  <LogOut className="mr-2 size-4" />
                  Sign out
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>
    </Sidebar>
  )
}
