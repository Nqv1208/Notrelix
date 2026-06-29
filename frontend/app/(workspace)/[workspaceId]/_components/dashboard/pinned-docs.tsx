"use client"

import { Pin, ArrowRight } from "lucide-react"
import Link from "next/link"
import { PINNED_DOCS } from "./workspace-mock-data"
import { isMockModeEnabled } from "@/lib/config/mock-mode"
import { usePageList } from "@/features/docs"
import { getWorkspaceDocHref, getWorkspaceDocsHref } from "@/features/workspace"
import { cn } from "@/lib/utils"
import { LoadingState, EmptyState, ErrorState } from "@/components/feedback"

interface PinnedDocsProps {
  workspaceId: string
}

export function PinnedDocs({ workspaceId }: PinnedDocsProps) {
  const isMock = isMockModeEnabled("docs")
  const realPages = usePageList(workspaceId)

  if (isMock) {
    return (
      <section className="rounded-2xl border border-border/50 bg-card p-5">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Pin className="size-4 text-primary -rotate-45" />
            <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
              Pinned Documents
            </h3>
          </div>
          <Link
            href={getWorkspaceDocsHref(workspaceId) as never}
            className="flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors"
          >
            View all <ArrowRight className="size-3" />
          </Link>
        </div>

        <div className="space-y-1.5">
          {PINNED_DOCS.map((doc) => (
            <div
              key={doc.id}
              className="group flex items-center gap-3 rounded-xl border border-transparent bg-muted/30 px-4 py-3 transition-all duration-150 hover:border-border/60 hover:shadow-sm hover:-translate-y-px cursor-pointer"
            >
              <span className="text-lg shrink-0 select-none">{doc.icon}</span>

              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-foreground truncate group-hover:text-primary transition-colors">
                  {doc.title}
                </p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  {doc.updatedBy} · {doc.updatedAt}
                </p>
              </div>

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

  // Real data mode
  if (realPages.isLoading) {
    return (
      <section className="rounded-2xl border border-border/50 bg-card p-5">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Pin className="size-4 text-primary -rotate-45" />
            <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
              Pinned Documents
            </h3>
          </div>
        </div>
        <LoadingState className="py-6" />
      </section>
    )
  }

  if (realPages.error) {
    return (
      <section className="rounded-2xl border border-border/50 bg-card p-5">
        <ErrorState error={realPages.error} className="py-4" />
      </section>
    )
  }

  if (!realPages.data || realPages.data.length === 0) {
    return (
      <section className="rounded-2xl border border-border/50 bg-card p-5">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Pin className="size-4 text-primary -rotate-45" />
            <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
              Pinned Documents
            </h3>
          </div>
        </div>
        <EmptyState
          title="No documents yet"
          description="Create your first document in this workspace to collaborate with your team."
          className="py-6"
        />
      </section>
    )
  }

  // Render first 4 pages as "pinned/recent" docs
  return (
    <section className="rounded-2xl border border-border/50 bg-card p-5">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Pin className="size-4 text-primary -rotate-45" />
          <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
            Recent Documents
          </h3>
        </div>
        <Link
          href={getWorkspaceDocsHref(workspaceId) as never}
          className="flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors"
        >
          View all <ArrowRight className="size-3" />
        </Link>
      </div>

      <div className="space-y-1.5">
        {realPages.data.slice(0, 4).map((doc) => (
          <Link
            key={doc.id}
            href={getWorkspaceDocHref(workspaceId, doc.id) as never}
            className="group flex items-center gap-3 rounded-xl border border-transparent bg-muted/30 px-4 py-3 transition-all duration-150 hover:border-border/60 hover:shadow-sm hover:-translate-y-px cursor-pointer"
          >
            <span className="text-lg shrink-0 select-none">📄</span>

            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-foreground truncate group-hover:text-primary transition-colors">
                {doc.title}
              </p>
              <p className="text-xs text-muted-foreground mt-0.5">
                Edited recently
              </p>
            </div>

            <span
              className={cn(
                "shrink-0 text-[10px] font-medium px-2 py-0.5 rounded-full capitalize",
                doc.status === "published"
                  ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-400"
                  : "bg-amber-50 text-amber-700 dark:bg-amber-950/40 dark:text-amber-400"
              )}
            >
              {doc.status}
            </span>
          </Link>
        ))}
      </div>
    </section>
  )
}
