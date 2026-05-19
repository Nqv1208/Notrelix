"use client"

import Link from "next/link"
import { ChevronRight, FileText, Search, SquareKanban, Star } from "lucide-react"
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import {
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar"

const favorites = [
  { title: "Docs MVP specification", type: "Doc", href: "/notrelix-os/docs/docs-mvp-spec", icon: FileText },
  { title: "Product delivery", type: "Board", href: "/notrelix-os/boards/board-product", icon: SquareKanban },
]

export function FavoritesSection() {
  return (
    <Collapsible defaultOpen className="group/collapsible">
      <SidebarGroup>
        <SidebarGroupLabel asChild className="px-2 py-1 text-[12px] font-semibold uppercase tracking-[0.06em] text-muted-foreground group-data-[collapsible=icon]:hidden">
          <CollapsibleTrigger className="flex w-full items-center gap-1">
            Favorites
            <ChevronRight className="size-3.5 transition-transform group-data-[state=open]/collapsible:rotate-90" />
            <Search className="ml-auto size-3.5 opacity-0 transition group-hover/collapsible:opacity-100" />
          </CollapsibleTrigger>
        </SidebarGroupLabel>
        <CollapsibleContent>
          <SidebarGroupContent>
            <SidebarMenu>
              {favorites.map((item) => (
                <SidebarMenuItem key={item.href}>
                  <SidebarMenuButton asChild tooltip={item.title} className="h-9">
                    <Link href={item.href as never} className="group-data-[collapsible=icon]:justify-center">
                      <item.icon className="size-4 text-muted-foreground" />
                      <span className="min-w-0 group-data-[collapsible=icon]:hidden">
                        <span className="block truncate text-[13px] text-foreground">{item.title}</span>
                        <span className="block text-[11px] text-muted-foreground">{item.type}</span>
                      </span>
                      <Star className="ml-auto size-3.5 fill-amber-500 text-amber-500 group-data-[collapsible=icon]:hidden" />
                    </Link>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </CollapsibleContent>
      </SidebarGroup>
    </Collapsible>
  )
}
