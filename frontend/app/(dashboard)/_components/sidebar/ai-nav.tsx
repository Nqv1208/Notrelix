"use client"

import {
  Sparkles,
  Heart,
  GitBranch,
  Bot,
  Mic,
  ChevronRight,
} from "lucide-react"
import Link from "next/link"
import { usePathname } from "next/navigation"

import {
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar"
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"

const aiItems = [
  { title: "AI Sidekick", url: "/ai-sidekick", icon: Sparkles },
  { title: "Vibe", url: "/vibe", icon: Heart },
  { title: "AI Workflows", url: "/ai-workflows", icon: GitBranch },
  { title: "AI Agents", url: "/ai-agents", icon: Bot },
  { title: "AI Notetaker", url: "/ai-notetaker", icon: Mic },
]

export function AINav() {
  const pathname = usePathname()

  return (
    <Collapsible defaultOpen className="group/collapsible">
      <SidebarGroup className="">
        <SidebarGroupLabel asChild className="hover:bg-transparent text-[13px] font-bold px-2 py-0 tracking-normal flex items-center justify-between group-data-[collapsible=icon]:hidden" style={{ color: "var(--color-graphite)", fontFamily: "var(--font-display)" }}>
          <CollapsibleTrigger>
            Notrelix AI
            <ChevronRight className="ml-1 mr-auto transition-transform group-data-[state=open]/collapsible:rotate-90" size={14} style={{ color: "var(--color-slate)" }} />
          </CollapsibleTrigger>
        </SidebarGroupLabel>
        <CollapsibleContent>
          <SidebarGroupContent>
            <SidebarMenu>
              {aiItems.map((item) => {
                const isActive = pathname.startsWith(item.url)
                return (
                  <SidebarMenuItem key={item.title}>
                    <SidebarMenuButton asChild isActive={isActive} tooltip={item.title} className="h-9">
                      <Link
                        href={item.url as never}
                        style={{
                          fontFamily: "var(--font-body)",
                          color: isActive ? "var(--color-brand-violet)" : "var(--color-graphite)",
                          fontWeight: isActive ? 500 : 400,
                        }}
                      >
                        <item.icon style={{ 
                          // Simulating colorful icons from monday.com using a CSS filter trick or just specific colors based on title
                          color: 
                            item.title === "AI Sidekick" ? "var(--color-surface-gold)" : 
                            item.title === "Vibe" ? "var(--color-surface-fuchsia)" : 
                            item.title === "AI Workflows" ? "var(--color-brand-ocean)" : 
                            item.title === "AI Agents" ? "var(--color-surface-grape)" : 
                            "var(--color-brand-ocean)"
                        }} size={16} />
                        <span className="text-[14px] group-data-[collapsible=icon]:hidden">{item.title}</span>
                      </Link>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                )
              })}
            </SidebarMenu>
          </SidebarGroupContent>
        </CollapsibleContent>
      </SidebarGroup>
    </Collapsible>
  )
}
