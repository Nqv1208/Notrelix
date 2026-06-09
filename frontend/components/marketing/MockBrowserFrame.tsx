"use client"

import React from "react"
import { ShieldAlert, Globe, RotateCw } from "lucide-react"

interface MockBrowserFrameProps {
  children: React.ReactNode
  url?: string
}

export function MockBrowserFrame({ children, url = "notrelix.com/workspace/product-launch" }: MockBrowserFrameProps) {
  return (
    <div className="relative w-full rounded-2xl border border-zinc-200 bg-white shadow-2xl shadow-zinc-900/10 dark:border-zinc-850 dark:bg-zinc-900 dark:shadow-black/40">
      {/* Top Header Bar */}
      <div className="flex h-11 items-center justify-between border-b border-zinc-200/80 bg-zinc-50 px-4 dark:border-zinc-800 dark:bg-zinc-900/90 rounded-t-2xl">
        {/* Window Controls */}
        <div className="flex items-center gap-1.5">
          <span className="h-3 w-3 rounded-full bg-red-400/90" />
          <span className="h-3 w-3 rounded-full bg-amber-400/90" />
          <span className="h-3 w-3 rounded-full bg-emerald-400/90" />
        </div>

        {/* Address Bar */}
        <div className="flex max-w-sm flex-1 items-center gap-2 rounded-lg border border-zinc-200/60 bg-white px-3 py-1 text-[11px] text-zinc-500 shadow-xs dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
          <Globe className="h-3.5 w-3.5 shrink-0 text-zinc-400 dark:text-zinc-500" />
          <span className="truncate select-none font-medium">{url}</span>
          <RotateCw className="ml-auto h-3 w-3 shrink-0 text-zinc-400 dark:text-zinc-500" />
        </div>

        {/* Action icons placeholders */}
        <div className="flex items-center gap-2">
          <span className="h-1.5 w-6 rounded bg-zinc-200 dark:bg-zinc-800" />
          <span className="h-4 w-4 rounded-full bg-zinc-200 dark:bg-zinc-800" />
        </div>
      </div>

      {/* Content Inner Container */}
      <div className="relative overflow-hidden bg-zinc-50 dark:bg-zinc-950 rounded-b-2xl aspect-video md:aspect-[16/10] select-none text-left">
        {children}
      </div>
    </div>
  )
}
