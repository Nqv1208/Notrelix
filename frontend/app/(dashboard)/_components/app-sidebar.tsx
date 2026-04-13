"use client"

import * as React from "react"
import Link from "next/link"
import { useRouter, usePathname } from "next/navigation"
import {
  Home,
  Search,
  Bell,
  Calendar,
  Settings,
  Plus,
  ChevronRight,
  MoreHorizontal,
  FileText,
  Star,
  Trash2,
  LogOut,
  ChevronsUpDown,
  Users,
  Pencil,
  Copy,
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
  useSidebar,
} from "@/registry/new-york-v4/ui/sidebar"
import { Avatar, AvatarFallback, AvatarImage } from "@/registry/new-york-v4/ui/avatar"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/registry/new-york-v4/ui/dropdown-menu"
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/registry/new-york-v4/ui/collapsible"
import { cn } from "@/lib/utils"
import { routes } from "@/lib/routes"
import { useEditorStore } from "@/features/documents/store/editor-store"
import type { Page } from "@/features/documents/types/document.types"

const mainMenuItems = [
  { icon: Home, label: "Dashboard", href: "/dashboard", badge: null },
  { icon: Search, label: "Search", href: "/dashboard/search", badge: null },
  { icon: Bell, label: "Notifications", href: "/dashboard/notifications", badge: "3" },
  { icon: Calendar, label: "Calendar", href: "/dashboard/calendar", badge: null },
  { icon: Settings, label: "Settings", href: "/dashboard/settings", badge: null },
]

function PageTreeItem({ page, workspaceId, depth = 0 }: { page: Page; workspaceId: string; depth?: number }) {
  const router = useRouter()
  const pathname = usePathname()
  const { addPage, deletePage, toggleFavorite, updatePage } = useEditorStore()
  const href = routes.dashboard.workspacePage(workspaceId, page.id) as string
  const isActive = pathname === href
  const hasChildren = page.children && page.children.length > 0

  const handleAddSubPage = (e: React.MouseEvent) => {
    e.stopPropagation()
    const newId = addPage(workspaceId, page.id)
    router.push(routes.dashboard.workspacePage(workspaceId, newId) as never)
  }

  if (hasChildren) {
    return (
      <Collapsible defaultOpen className="group/collapsible">
        <SidebarMenuSubItem>
          <CollapsibleTrigger asChild>
            <SidebarMenuSubButton
              className={cn(
                "justify-between group/item",
                isActive && "bg-accent font-medium"
              )}
            >
              <Link href={href as never} className="flex items-center gap-1.5 min-w-0 flex-1">
                <span className="text-sm shrink-0">{page.icon}</span>
                <span className="truncate">{page.title}</span>
              </Link>
              <ChevronRight className="size-3.5 shrink-0 text-muted-foreground transition-transform group-data-[state=open]/collapsible:rotate-90" />
            </SidebarMenuSubButton>
          </CollapsibleTrigger>
          <PageContextMenu page={page} workspaceId={workspaceId} onAddSubPage={handleAddSubPage} />
          <CollapsibleContent>
            <SidebarMenuSub>
              {page.children!.filter(c => !c.isDeleted).map((child) => (
                <PageTreeItem
                  key={child.id}
                  page={child}
                  workspaceId={workspaceId}
                  depth={depth + 1}
                />
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
      <SidebarMenuSubButton
        asChild
        className={cn(isActive && "bg-accent font-medium")}
      >
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
  const { deletePage, toggleFavorite, duplicatePageFn } = usePageActions(page, workspaceId)

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <SidebarMenuAction showOnHover className="data-[state=open]:opacity-100">
          <MoreHorizontal className="size-4" />
        </SidebarMenuAction>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-48">
        <DropdownMenuItem onClick={onAddSubPage}>
          <Plus className="size-4 mr-2" />
          Add sub-page
        </DropdownMenuItem>
        <DropdownMenuItem onClick={toggleFavorite}>
          {page.isFavorite ? (
            <>
              <StarOff className="size-4 mr-2" />
              Remove from favorites
            </>
          ) : (
            <>
              <Star className="size-4 mr-2" />
              Add to favorites
            </>
          )}
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={deletePage} className="text-destructive">
          <Trash2 className="size-4 mr-2" />
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
    duplicatePageFn: () => {},
  }
}

export function AppSidebar() {
  const { state } = useSidebar()
  const router = useRouter()
  const pathname = usePathname()
  const { workspaces, addPage, getFavoritePages } = useEditorStore()
  const favorites = getFavoritePages()

  const handleAddPage = (wsId: string) => {
    const newId = addPage(wsId)
    router.push(routes.dashboard.workspacePage(wsId, newId) as never)
  }

  return (
    <Sidebar collapsible="icon" className="border-r">
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <SidebarMenuButton size="lg" className="data-[state=open]:bg-sidebar-accent">
                  <div className="flex size-8 items-center justify-center rounded-lg bg-gradient-to-br from-violet-500 to-purple-600 text-white font-bold">
                    C
                  </div>
                  <div className="flex flex-col gap-0.5 leading-none">
                    <span className="font-semibold">Craftboard</span>
                    <span className="text-xs text-muted-foreground">Pro Plan</span>
                  </div>
                  <ChevronsUpDown className="ml-auto size-4" />
                </SidebarMenuButton>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="w-[--radix-dropdown-menu-trigger-width]">
                <DropdownMenuItem>
                  <Settings className="mr-2 size-4" />
                  Workspace Settings
                </DropdownMenuItem>
                <DropdownMenuItem>
                  <Users className="mr-2 size-4" />
                  Invite Members
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem>
                  <Plus className="mr-2 size-4" />
                  Create Workspace
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>

      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              {mainMenuItems.map((item) => (
                <SidebarMenuItem key={item.label}>
                  <SidebarMenuButton
                    asChild
                    tooltip={item.label}
                    isActive={pathname === item.href}
                  >
                    <Link href={item.href as never}>
                      <item.icon className="size-4" />
                      <span>{item.label}</span>
                    </Link>
                  </SidebarMenuButton>
                  {item.badge && (
                    <SidebarMenuAction className="bg-violet-500 text-white text-[10px] rounded-full size-5 flex items-center justify-center pointer-events-none">
                      {item.badge}
                    </SidebarMenuAction>
                  )}
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarSeparator />

        {favorites.length > 0 && (
          <>
            <SidebarGroup>
              <SidebarGroupLabel>Favorites</SidebarGroupLabel>
              <SidebarGroupContent>
                <SidebarMenu>
                  {favorites.map((page) => (
                    <SidebarMenuItem key={page.id}>
                      <SidebarMenuButton
                        asChild
                        tooltip={page.title}
                        isActive={pathname === (routes.dashboard.workspacePage(page.workspaceId, page.id) as string)}
                      >
                        <Link href={routes.dashboard.workspacePage(page.workspaceId, page.id) as never}>
                          <span className="text-base">{page.icon}</span>
                          <span className="truncate">{page.title}</span>
                        </Link>
                      </SidebarMenuButton>
                      <SidebarMenuAction
                        showOnHover
                        onClick={() => useEditorStore.getState().toggleFavorite(page.id)}
                      >
                        <Star className="size-4 fill-yellow-400 text-yellow-400" />
                      </SidebarMenuAction>
                    </SidebarMenuItem>
                  ))}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
            <SidebarSeparator />
          </>
        )}

        {workspaces.map((workspace) => (
          <React.Fragment key={workspace.id}>
            <SidebarGroup>
              <SidebarGroupLabel>
                <span className="mr-1.5">{workspace.icon}</span>
                {workspace.name}
              </SidebarGroupLabel>
              <SidebarGroupAction onClick={() => handleAddPage(workspace.id)}>
                <Plus className="size-4" />
                <span className="sr-only">Add Page</span>
              </SidebarGroupAction>
              <SidebarGroupContent>
                <SidebarMenu>
                  <SidebarMenuSub>
                    {workspace.pages
                      .filter((p) => !p.isDeleted && !p.parentId)
                      .map((page) => (
                        <PageTreeItem
                          key={page.id}
                          page={page}
                          workspaceId={workspace.id}
                        />
                      ))}
                    <SidebarMenuSubItem>
                      <SidebarMenuSubButton
                        onClick={() => handleAddPage(workspace.id)}
                        className="text-muted-foreground hover:text-foreground"
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

        <SidebarSeparator />

        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton asChild tooltip="Trash" isActive={pathname === "/dashboard/trash"}>
                  <Link href="/dashboard/trash">
                    <Trash2 className="size-4" />
                    <span>Trash</span>
                  </Link>
                </SidebarMenuButton>
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter>
        <SidebarMenu>
          <SidebarMenuItem>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <SidebarMenuButton size="lg" className="data-[state=open]:bg-sidebar-accent">
                  <Avatar className="size-8">
                    <AvatarImage src="/avatars/user.jpg" alt="User" />
                    <AvatarFallback className="bg-gradient-to-br from-violet-500 to-purple-600 text-white">
                      AD
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex flex-col gap-0.5 leading-none">
                    <span className="font-medium">Admin User</span>
                    <span className="text-xs text-muted-foreground">admin@todoapp.com</span>
                  </div>
                  <ChevronsUpDown className="ml-auto size-4" />
                </SidebarMenuButton>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" side="top" className="w-[--radix-dropdown-menu-trigger-width]">
                <DropdownMenuItem>
                  <Settings className="mr-2 size-4" />
                  Account Settings
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem className="text-destructive">
                  <LogOut className="mr-2 size-4" />
                  Sign Out
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>
    </Sidebar>
  )
}
