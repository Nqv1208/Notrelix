"use client"

import * as React from "react"
import Link from "next/link"
import {
  Home,
  Search,
  Bell,
  Calendar,
  Settings,
  Plus,
  ChevronRight,
  MoreHorizontal,
  FolderKanban,
  FileText,
  CheckSquare,
  Users,
  Star,
  Trash2,
  LogOut,
  ChevronsUpDown,
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

// Sample data - sẽ thay thế bằng API
const mainMenuItems = [
  { icon: Home, label: "Dashboard", href: "/dashboard", badge: null },
  { icon: Search, label: "Search", href: "/dashboard/search", badge: null },
  { icon: Bell, label: "Notifications", href: "/dashboard/notifications", badge: "12" },
  { icon: Calendar, label: "Calendar", href: "/dashboard/calendar", badge: null },
  { icon: Settings, label: "Settings", href: "/dashboard/settings", badge: null },
]

const workspaces = [
  {
    id: "1",
    name: "Craftboard Project",
    icon: "🎨",
    color: "bg-violet-500",
    pages: [
      { id: "1-1", name: "Sprint Tasks", icon: "🏃", type: "todo" },
      { id: "1-2", name: "Project Roadmap", icon: "🗺️", type: "page" },
      { id: "1-3", name: "Team Wiki", icon: "📚", type: "page" },
    ]
  },
  {
    id: "2",
    name: "Personal Tasks",
    icon: "📝",
    color: "bg-emerald-500",
    pages: [
      { id: "2-1", name: "My Tasks", icon: "✅", type: "todo" },
      { id: "2-2", name: "Notes", icon: "📓", type: "page" },
    ]
  },
  {
    id: "3",
    name: "Marketing",
    icon: "📣",
    color: "bg-orange-500",
    pages: [
      { id: "3-1", name: "Campaign Tasks", icon: "🎯", type: "todo" },
      { id: "3-2", name: "Content Calendar", icon: "📅", type: "page" },
    ]
  },
]

const favorites = [
  { id: "fav-1", name: "Sprint Tasks", icon: "🏃", href: "/dashboard/workspace/1/page/1-1" },
  { id: "fav-2", name: "My Tasks", icon: "✅", href: "/dashboard/workspace/2/page/2-1" },
]

export function AppSidebar() {
  const { state } = useSidebar()

  return (
    <Sidebar collapsible="icon" className="border-r">
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <SidebarMenuButton size="lg" className="data-[state=open]:bg-sidebar-accent">
                  <div className="flex size-8 items-center justify-center rounded-lg bg-gradient-to-br from-violet-500 to-purple-600 text-white font-bold">
                    T
                  </div>
                  <div className="flex flex-col gap-0.5 leading-none">
                    <span className="font-semibold">TodoApp</span>
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
        {/* Main Menu */}
        <SidebarGroup>
          <SidebarGroupLabel>Main Menu</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {mainMenuItems.map((item) => (
                <SidebarMenuItem key={item.label}>
                  <SidebarMenuButton asChild tooltip={item.label}>
                    <Link href={item.href}>
                      <item.icon className="size-4" />
                      <span>{item.label}</span>
                    </Link>
                  </SidebarMenuButton>
                  {item.badge && (
                    <SidebarMenuAction className="bg-violet-500 text-white text-[10px] rounded-full size-5 flex items-center justify-center">
                      {item.badge}
                    </SidebarMenuAction>
                  )}
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarSeparator />

        {/* Favorites */}
        <SidebarGroup>
          <SidebarGroupLabel>Favorites</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {favorites.map((item) => (
                <SidebarMenuItem key={item.id}>
                  <SidebarMenuButton asChild tooltip={item.name}>
                    <Link href={item.href}>
                      <span className="text-base">{item.icon}</span>
                      <span>{item.name}</span>
                    </Link>
                  </SidebarMenuButton>
                  <SidebarMenuAction showOnHover>
                    <Star className="size-4 fill-yellow-400 text-yellow-400" />
                  </SidebarMenuAction>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarSeparator />

        {/* Workspaces / Pages */}
        <SidebarGroup>
          <SidebarGroupLabel>My Pages</SidebarGroupLabel>
          <SidebarGroupAction>
            <Plus className="size-4" />
            <span className="sr-only">Add Page</span>
          </SidebarGroupAction>
          <SidebarGroupContent>
            <SidebarMenu>
              {workspaces.map((workspace) => (
                <Collapsible key={workspace.id} defaultOpen className="group/collapsible">
                  <SidebarMenuItem>
                    <CollapsibleTrigger asChild>
                      <SidebarMenuButton tooltip={workspace.name}>
                        <div className={`size-5 rounded flex items-center justify-center text-xs ${workspace.color} text-white`}>
                          {workspace.icon}
                        </div>
                        <span className="font-medium">{workspace.name}</span>
                        <ChevronRight className="ml-auto size-4 transition-transform group-data-[state=open]/collapsible:rotate-90" />
                      </SidebarMenuButton>
                    </CollapsibleTrigger>
                    <SidebarMenuAction showOnHover>
                      <MoreHorizontal className="size-4" />
                    </SidebarMenuAction>
                    <CollapsibleContent>
                      <SidebarMenuSub>
                        {workspace.pages.map((page) => (
                          <SidebarMenuSubItem key={page.id}>
                            <SidebarMenuSubButton asChild>
                              <Link href={`/dashboard/workspace/${workspace.id}/page/${page.id}`}>
                                <span>{page.icon}</span>
                                <span>{page.name}</span>
                              </Link>
                            </SidebarMenuSubButton>
                          </SidebarMenuSubItem>
                        ))}
                        <SidebarMenuSubItem>
                          <SidebarMenuSubButton className="text-muted-foreground hover:text-foreground">
                            <Plus className="size-4" />
                            <span>Add Page</span>
                          </SidebarMenuSubButton>
                        </SidebarMenuSubItem>
                      </SidebarMenuSub>
                    </CollapsibleContent>
                  </SidebarMenuItem>
                </Collapsible>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarSeparator />

        {/* Trash */}
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton asChild tooltip="Trash">
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
