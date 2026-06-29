"use client"

import Link from "next/link"
import { ChevronRight, Home } from "lucide-react"
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb"
import type { BreadcrumbItem as DocsBreadcrumbItem } from "../types/page.types"
import { cn } from "@/lib/utils"

interface BreadcrumbNavProps {
  breadcrumb: DocsBreadcrumbItem[]
  workspaceId: string
  compact?: boolean
}

export function BreadcrumbNav({ breadcrumb, workspaceId, compact }: BreadcrumbNavProps) {
  return (
    <Breadcrumb className={cn("py-4", compact && "py-0")}>
      <BreadcrumbList>
        <BreadcrumbItem>
          <BreadcrumbLink asChild>
            <Link href={`/${workspaceId}/docs`} className="flex items-center gap-1">
              <Home className="size-3.5" />
              Docs
            </Link>
          </BreadcrumbLink>
        </BreadcrumbItem>
        {breadcrumb.map((item, index) => {
          const isLast = index === breadcrumb.length - 1
          return (
            <span key={item.id} className="contents">
              <BreadcrumbSeparator>
                <ChevronRight className="size-3.5" />
              </BreadcrumbSeparator>
              <BreadcrumbItem>
                {isLast ? (
                  <BreadcrumbPage className="max-w-[180px] truncate">
                    {item.icon} {item.title}
                  </BreadcrumbPage>
                ) : (
                  <BreadcrumbLink asChild>
                    <Link href={`/${workspaceId}/docs/${item.id}`} className="max-w-[160px] truncate">
                      {item.icon} {item.title}
                    </Link>
                  </BreadcrumbLink>
                )}
              </BreadcrumbItem>
            </span>
          )
        })}
      </BreadcrumbList>
    </Breadcrumb>
  )
}
