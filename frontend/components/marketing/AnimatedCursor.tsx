"use client"

import { MousePointer, Grip } from "lucide-react"
import { cn } from "@/lib/utils"

interface AnimatedCursorProps {
  x: number
  y: number
  action: "idle" | "pointer" | "clicking" | "dragging"
}

export function AnimatedCursor({ x, y, action }: AnimatedCursorProps) {
  const isReduced = typeof window !== "undefined" && window.matchMedia("(prefers-reduced-motion: reduce)").matches

  if (isReduced) return null

  return (
    <div
      className="pointer-events-none absolute z-50 transition-all duration-800 cubic-bezier-[0.25,1,0.5,1]"
      style={{
        left: `${x}%`,
        top: `${y}%`,
        transform: "translate(-8px, -8px)", // offset to align tip of cursor
      }}
    >
      {/* Ripple ring for clicking */}
      {action === "clicking" && (
        <span className="absolute -left-3 -top-3 h-10 w-10 animate-ping rounded-full border border-blue-500 bg-blue-500/20 opacity-75 duration-700" />
      )}

      {/* Actual Cursor Icon */}
      {action === "dragging" ? (
        <div className="flex h-7 w-7 items-center justify-center rounded-full bg-blue-600 text-white shadow-lg ring-2 ring-white">
          <Grip className="h-4 w-4 animate-pulse" />
        </div>
      ) : (
        <div
          className={cn(
            "relative text-zinc-900 transition-transform duration-200 dark:text-white",
            action === "clicking" && "scale-90"
          )}
        >
          <MousePointer className="h-5 w-5 fill-current stroke-white stroke-2 drop-shadow-md" />
        </div>
      )}
    </div>
  )
}
