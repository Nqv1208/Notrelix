"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { FileText, Star, SquareKanban, Workflow } from "lucide-react"
import {
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar"
import { getWorkspaceBoardHref, getWorkspaceDocHref, getWorkspaceRootHref } from "@/features/workspace"
import { recentBoards, recentDocs, recentWorkspaces } from "../home-data"

const recents = [
  ...recentWorkspaces.map((item) => ({
    id: item.id,
    title: item.name,
    subtitle: "Workspace",
    href: getWorkspaceRootHref(item.id),
    icon: Workflow,
  })),
  ...recentDocs.map((item) => ({
    id: item.id,
    title: item.title,
    subtitle: "Doc",
    href: getWorkspaceDocHref(item.workspaceId, item.id),
    icon: FileText,
  })),
  ...recentBoards.map((item) => ({
    id: item.id,
    title: item.title,
    subtitle: "Board",
    href: getWorkspaceBoardHref(item.workspaceId, item.id),
    icon: SquareKanban,
  })),
].slice(0, 8)

export function RecentSection() {
  const pathname = usePathname()

  return (
    <SidebarGroup className="mt-2">
      <SidebarGroupLabel className="px-2 py-1 text-[12px] font-semibold uppercase tracking-[0.06em] text-muted-foreground group-data-[collapsible=icon]:hidden">
        Recently viewed
      </SidebarGroupLabel>
      <SidebarGroupContent>
        <SidebarMenu>
          {recents.map((rec) => {
            const active = pathname === rec.href
            return (
              <SidebarMenuItem key={`${rec.subtitle}-${rec.id}`} className="group/recent">
                <SidebarMenuButton asChild isActive={active} tooltip={rec.title} className="h-9">
                  <Link href={rec.href as never} className="flex items-center justify-between pr-1 group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:pr-0">
                    <span className="flex min-w-0 items-center gap-2">
                      <rec.icon className="size-4 text-muted-foreground" />
                      <span className="min-w-0 group-data-[collapsible=icon]:hidden">
                        <span className="block truncate text-[13px] text-foreground">{rec.title}</span>
                        <span className="block truncate text-[11px] text-muted-foreground">{rec.subtitle}</span>
                      </span>
                    </span>
                    <Star className="size-3.5 text-muted-foreground opacity-0 transition group-hover/recent:opacity-100 group-data-[collapsible=icon]:hidden" />
                  </Link>
                </SidebarMenuButton>
              </SidebarMenuItem>
            )
          })}
        </SidebarMenu>
      </SidebarGroupContent>
    </SidebarGroup>
  )
}
