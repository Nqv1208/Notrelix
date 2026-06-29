"use client"

import Link from "next/link"
import {
  ArrowRight,
  Check,
  FileText,
  LayoutGrid,
  GripVertical,
  MessageSquare,
} from "lucide-react"

import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { useAuthUser } from "@/features/auth"
import { routes } from "@/lib/routes"

export function HeroSection() {
  const { isAuthenticated } = useAuthUser()

  return (
    <section className="relative pt-20 pb-28 overflow-hidden">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="max-w-4xl mx-auto text-center mb-16">
          <Badge
            variant="secondary"
            className="mb-6 px-4 py-1.5 text-sm font-medium bg-violet-100 dark:bg-violet-950/50 text-violet-700 dark:text-violet-300 border-violet-200/60 dark:border-violet-800/60"
          >
            Now in public beta
          </Badge>

          <h1 className="text-4xl sm:text-5xl lg:text-7xl font-bold tracking-tight mb-6 leading-[1.1]">
            <span className="block">Write like Notion.</span>
            <span className="block">Plan like Trello.</span>
            <span className="block mt-1 bg-gradient-to-r from-violet-600 via-indigo-600 to-purple-600 bg-clip-text text-transparent">
              Ship like a pro.
            </span>
          </h1>

          <p className="text-lg sm:text-xl text-muted-foreground mb-10 max-w-2xl mx-auto leading-relaxed">
            Notrelix unifies documents, wikis, and project boards into one
            workspace. No more tab-switching — just focus and flow.
          </p>

          <div className="flex flex-col sm:flex-row items-center justify-center gap-3 mb-10">
            <Link href={isAuthenticated ? (routes.dashboard.root as never) : (routes.auth.register as never)}>
              <Button
                size="lg"
                className="w-full sm:w-auto bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-700 hover:to-indigo-700 text-white border-0 shadow-xl shadow-violet-500/25 px-8 h-12 text-base"
              >
                {isAuthenticated ? "Open Dashboard" : "Start free"}
                <ArrowRight className="size-4 ml-2" />
              </Button>
            </Link>
            <a href="#showcase">
              <Button variant="outline" size="lg" className="w-full sm:w-auto px-8 h-12 text-base">
                See it in action
              </Button>
            </a>
          </div>

          <div className="flex items-center justify-center gap-6 text-sm text-muted-foreground">
            {["No credit card required", "Free for small teams", "Setup in 30s"].map(
              (text) => (
                <div key={text} className="flex items-center gap-1.5">
                  <div className="flex items-center justify-center size-4 rounded-full bg-emerald-500/15">
                    <Check className="size-2.5 text-emerald-600" />
                  </div>
                  {text}
                </div>
              )
            )}
          </div>
        </div>

        <div className="relative max-w-5xl mx-auto">
          <div className="absolute -inset-4 bg-gradient-to-r from-violet-500/20 via-indigo-500/20 to-purple-500/20 rounded-[2rem] blur-2xl" />

          <div className="relative rounded-2xl border bg-card shadow-2xl shadow-violet-500/5 overflow-hidden">
            <div className="flex items-center gap-2 px-4 py-3 bg-muted/50 border-b">
              <div className="flex gap-1.5">
                <div className="size-3 rounded-full bg-red-400/80" />
                <div className="size-3 rounded-full bg-yellow-400/80" />
                <div className="size-3 rounded-full bg-green-400/80" />
              </div>
              <div className="flex-1 flex items-center justify-center">
                <div className="flex items-center gap-2 px-3 py-1 rounded-md bg-background/60 text-xs text-muted-foreground">
                  <div className="size-3 rounded bg-gradient-to-br from-violet-500 to-indigo-500" />
                  app.notrelix.io
                </div>
              </div>
            </div>

            <div className="grid grid-cols-[240px_1fr] min-h-[420px]">
              <div className="border-r bg-muted/30 p-3 hidden sm:block">
                <div className="text-xs font-semibold text-muted-foreground mb-3 px-2">WORKSPACE</div>
                {[
                  { icon: "🚀", label: "Getting Started", active: true },
                  { icon: "🗺️", label: "Product Roadmap", active: false },
                  { icon: "📝", label: "Meeting Notes", active: false },
                  { icon: "🎯", label: "Design System", active: false },
                ].map((item) => (
                  <div
                    key={item.label}
                    className={`flex items-center gap-2 px-2 py-1.5 rounded-md text-sm mb-0.5 ${
                      item.active ? "bg-accent font-medium" : "text-muted-foreground"
                    }`}
                  >
                    <span className="text-base">{item.icon}</span>
                    {item.label}
                  </div>
                ))}
                <div className="mt-4 text-xs font-semibold text-muted-foreground mb-3 px-2">BOARDS</div>
                {[
                  { icon: <LayoutGrid className="size-4 text-violet-500" />, label: "Sprint Board" },
                  { icon: <LayoutGrid className="size-4 text-emerald-500" />, label: "Bug Tracker" },
                ].map((item) => (
                  <div
                    key={item.label}
                    className="flex items-center gap-2 px-2 py-1.5 rounded-md text-sm text-muted-foreground mb-0.5"
                  >
                    {item.icon}
                    {item.label}
                  </div>
                ))}
              </div>

              <div className="p-6 sm:p-8">
                <div className="flex items-center gap-2 mb-1">
                  <span className="text-3xl">🚀</span>
                  <h2 className="text-2xl font-bold">Getting Started</h2>
                </div>
                <div className="space-y-3 mt-6">
                  <div className="flex items-start gap-3 group">
                    <GripVertical className="size-4 text-muted-foreground/30 mt-1 shrink-0" />
                    <div className="text-xl font-semibold">Welcome to Notrelix</div>
                  </div>
                  <div className="flex items-start gap-3 group">
                    <GripVertical className="size-4 text-muted-foreground/30 mt-1 shrink-0" />
                    <p className="text-muted-foreground">
                      Your all-in-one workspace for documents, wikis, and project management.
                    </p>
                  </div>
                  <div className="flex items-start gap-3 ml-1 group">
                    <GripVertical className="size-4 text-muted-foreground/30 mt-0.5 shrink-0" />
                    <div className="flex gap-3 rounded-lg bg-violet-50 dark:bg-violet-950/30 border border-violet-200/60 dark:border-violet-800/40 p-3 flex-1">
                      <span className="text-lg">💡</span>
                      <span className="text-sm">Type <kbd className="px-1.5 py-0.5 rounded bg-background border text-xs font-mono">/</kbd> to insert blocks — headings, lists, code, images, and more.</span>
                    </div>
                  </div>
                  <div className="flex items-start gap-3 group">
                    <GripVertical className="size-4 text-muted-foreground/30 mt-1 shrink-0" />
                    <div className="flex items-center gap-2">
                      <div className="size-4 rounded border-2 border-emerald-500 bg-emerald-500 flex items-center justify-center">
                        <Check className="size-2.5 text-white" />
                      </div>
                      <span className="line-through text-muted-foreground">Create your first page</span>
                    </div>
                  </div>
                  <div className="flex items-start gap-3 group">
                    <GripVertical className="size-4 text-muted-foreground/30 mt-1 shrink-0" />
                    <div className="flex items-center gap-2">
                      <div className="size-4 rounded border-2 border-muted-foreground/30" />
                      <span>Invite team members</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
