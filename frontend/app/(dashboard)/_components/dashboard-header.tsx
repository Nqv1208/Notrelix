"use client"

import { usePathname } from "next/navigation"
import { SidebarTrigger } from "@/components/ui/sidebar"
import { Separator } from "@/components/ui/separator"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb"
import { Search, Bell, Plus } from "lucide-react"
import { routes } from "@/lib/routes"
import { useEditorStore } from "@/features/documents/store/editor-store"

export function DashboardHeader() {
  const pathname = usePathname()
  const { workspaces, getPage } = useEditorStore()

  const breadcrumbs = getBreadcrumbs(pathname, workspaces, getPage)

  return (
    <header className="sticky top-0 z-10 flex h-12 items-center gap-3 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 px-3">
      <SidebarTrigger className="-ml-1" />
      <Separator orientation="vertical" className="h-5" />

      <Breadcrumb className="hidden md:flex">
        <BreadcrumbList>
          {breadcrumbs.map((crumb, i) => (
            <span key={crumb.label} className="contents">
              {i > 0 && <BreadcrumbSeparator />}
              <BreadcrumbItem>
                {i === breadcrumbs.length - 1 ? (
                  <BreadcrumbPage className="flex items-center gap-1.5">
                    {crumb.icon && <span className="text-sm">{crumb.icon}</span>}
                    {crumb.label}
                  </BreadcrumbPage>
                ) : (
                  <BreadcrumbLink href={crumb.href} className="flex items-center gap-1.5">
                    {crumb.icon && <span className="text-sm">{crumb.icon}</span>}
                    {crumb.label}
                  </BreadcrumbLink>
                )}
              </BreadcrumbItem>
            </span>
          ))}
        </BreadcrumbList>
      </Breadcrumb>

      <div className="ml-auto flex items-center gap-2">
        <div className="relative hidden lg:block">
          <Search className="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            placeholder="Search..."
            className="w-56 pl-8 h-8 text-sm"
          />
        </div>

        <Button variant="ghost" size="icon-sm" className="relative">
          <Bell className="size-4" />
          <span className="absolute -top-0.5 -right-0.5 size-4 rounded-full bg-violet-500 text-[10px] text-white flex items-center justify-center">
            3
          </span>
        </Button>

        <Button size="sm" className="gap-1.5 h-8 bg-gradient-to-r from-violet-500 to-purple-600 hover:from-violet-600 hover:to-purple-700">
          <Plus className="size-3.5" />
          <span className="hidden sm:inline text-sm">New Page</span>
        </Button>
      </div>
    </header>
  )
}

type BreadcrumbInfo = {
  label: string
  href: string
  icon?: string
}

type WorkspaceInfo = {
  id: string
  name: string
  icon: string
}

function getBreadcrumbs(
  pathname: string,
  workspaces: WorkspaceInfo[],
  getPage: (pageId: string) => { title: string; icon: string } | undefined
): BreadcrumbInfo[] {
  const crumbs: BreadcrumbInfo[] = [
    { label: "Home", href: routes.dashboard.root },
  ]

  if (pathname === routes.dashboard.root) {
    crumbs.push({ label: "Dashboard", href: routes.dashboard.root })
    return crumbs
  }

  if (pathname === routes.dashboard.settings) {
    crumbs.push({ label: "Settings", href: routes.dashboard.settings })
    return crumbs
  }

  if (pathname === routes.dashboard.notifications) {
    crumbs.push({ label: "Notifications", href: routes.dashboard.notifications })
    return crumbs
  }

  const wsPageMatch = pathname.match(/\/dashboard\/workspace\/([^/]+)\/page\/([^/]+)/)
  if (wsPageMatch) {
    const [, workspaceId, pageId] = wsPageMatch
    const ws = workspaces.find((w: WorkspaceInfo) => w.id === workspaceId)
    if (ws) {
      crumbs.push({
        label: ws.name,
        href: routes.dashboard.root,
        icon: ws.icon,
      })
    }
    const page = getPage(pageId)
    if (page) {
      crumbs.push({
        label: page.title || "Untitled",
        href: pathname,
        icon: page.icon,
      })
    }
  }

  return crumbs
}
