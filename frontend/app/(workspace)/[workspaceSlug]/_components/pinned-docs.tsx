"use client"

import { Pin, ArrowRight } from "lucide-react"
import { PINNED_DOCS } from "./workspace-mock-data"
import { cn } from "@/lib/utils"

export function PinnedDocs() {
  return (
    <section>
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Pin className="size-4 text-muted-foreground -rotate-45" />
          <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
            Pinned Documents
          </h3>
        </div>
        <button className="flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors">
          View all <ArrowRight className="size-3" />
        </button>
      </div>

      <div className="space-y-1.5">
        {PINNED_DOCS.map((doc) => (
          <div
            key={doc.id}
            className="group flex items-center gap-3 rounded-xl border border-transparent bg-card px-4 py-3 transition-all duration-150 hover:border-border/60 hover:shadow-sm hover:-translate-y-px cursor-pointer"
          >
            {/* Icon */}
            <span className="text-lg shrink-0 select-none">{doc.icon}</span>

            {/* Content */}
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-foreground truncate group-hover:text-violet-700 dark:group-hover:text-violet-300 transition-colors">
                {doc.title}
              </p>
              <p className="text-xs text-muted-foreground mt-0.5">
                {doc.updatedBy} · {doc.updatedAt}
              </p>
            </div>

            {/* Status Badge */}
            <span
              className={cn(
                "shrink-0 text-[10px] font-medium px-2 py-0.5 rounded-full",
                doc.status === "published"
                  ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-400"
                  : "bg-amber-50 text-amber-700 dark:bg-amber-950/40 dark:text-amber-400"
              )}
            >
              {doc.status === "published" ? "Published" : "Draft"}
            </span>
          </div>
        ))}
      </div>
    </section>
  )
}
