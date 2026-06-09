"use client"

import { useEffect, useState } from "react"
import { cn } from "@/lib/utils"

interface StreamingTextProps {
  text: string
  className?: string
  showCursor?: boolean
}

export function StreamingText({ text, className, showCursor = true }: StreamingTextProps) {
  const [blink, setBlink] = useState(true)

  useEffect(() => {
    if (!showCursor) return
    const interval = setInterval(() => {
      setBlink((b) => !b)
    }, 500)
    return () => clearInterval(interval)
  }, [showCursor])

  return (
    <span className={cn("inline font-mono text-sm tracking-tight whitespace-pre-wrap", className)}>
      {text}
      {showCursor && (
        <span
          className={cn(
            "ml-0.5 inline-block w-1.5 h-4 translate-y-0.5 bg-blue-500 transition-opacity duration-100",
            blink ? "opacity-100" : "opacity-0"
          )}
        />
      )}
    </span>
  )
}
