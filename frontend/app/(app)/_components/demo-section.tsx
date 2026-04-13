"use client"

import * as React from "react"
import {
  Check,
  GripVertical,
  Plus,
  MoreHorizontal,
  Tag,
  User,
  Clock,
} from "lucide-react"
import { Badge } from "@/registry/new-york-v4/ui/badge"
import { cn } from "@/lib/utils"

type Tab = "docs" | "boards"

const kanbanColumns = [
  {
    title: "To Do",
    color: "bg-slate-500",
    cards: [
      {
        title: "Design new dashboard layout",
        labels: [{ text: "Design", color: "bg-pink-500" }],
        avatar: "TH",
      },
      {
        title: "Write API documentation",
        labels: [{ text: "Docs", color: "bg-blue-500" }],
        avatar: "HL",
      },
    ],
  },
  {
    title: "In Progress",
    color: "bg-amber-500",
    cards: [
      {
        title: "Implement drag & drop for blocks",
        labels: [{ text: "Feature", color: "bg-violet-500" }, { text: "P1", color: "bg-red-500" }],
        avatar: "MA",
      },
      {
        title: "User authentication flow",
        labels: [{ text: "Backend", color: "bg-emerald-500" }],
        avatar: "HL",
      },
    ],
  },
  {
    title: "Review",
    color: "bg-blue-500",
    cards: [
      {
        title: "Fix mobile responsiveness",
        labels: [{ text: "Bug", color: "bg-red-500" }],
        avatar: "TH",
      },
    ],
  },
  {
    title: "Done",
    color: "bg-emerald-500",
    cards: [
      {
        title: "Setup project structure",
        labels: [{ text: "Infra", color: "bg-slate-500" }],
        avatar: "MA",
        done: true,
      },
    ],
  },
]

function DocumentMockup() {
  return (
    <div className="p-6 sm:p-8 space-y-4 max-w-2xl mx-auto">
      <div className="flex items-center gap-2 mb-2">
        <span className="text-3xl">🗺️</span>
        <h2 className="text-2xl font-bold">Product Roadmap</h2>
      </div>

      <p className="text-muted-foreground leading-relaxed">
        Our plan for building the best workspace tool in 2026. Each quarter focuses on a core theme.
      </p>

      <div className="border-l-[3px] border-violet-400/60 pl-4 py-1 italic text-muted-foreground">
        &ldquo;The best way to predict the future is to create it.&rdquo;
      </div>

      <h3 className="text-lg font-semibold mt-6">Q2 — Foundation</h3>
      <div className="space-y-2 ml-1">
        {[
          { text: "Auth & workspace management", checked: true },
          { text: "Block-based document editor", checked: true },
          { text: "Kanban board module", checked: false },
        ].map((item) => (
          <div key={item.text} className="flex items-center gap-2.5">
            <div
              className={cn(
                "size-4 rounded border-2 flex items-center justify-center shrink-0",
                item.checked
                  ? "border-emerald-500 bg-emerald-500"
                  : "border-muted-foreground/30"
              )}
            >
              {item.checked && <Check className="size-2.5 text-white" />}
            </div>
            <span className={cn(item.checked && "line-through text-muted-foreground")}>
              {item.text}
            </span>
          </div>
        ))}
      </div>

      <h3 className="text-lg font-semibold mt-6">Q3 — Collaboration</h3>
      <ul className="space-y-1.5 ml-1">
        {["Real-time comments & mentions", "File attachments & media", "Smart notifications"].map(
          (text) => (
            <li key={text} className="flex items-start gap-2">
              <span className="text-muted-foreground mt-[0.35em]">•</span>
              {text}
            </li>
          )
        )}
      </ul>

      <div className="rounded-lg bg-muted/50 border overflow-hidden mt-4">
        <div className="flex items-center justify-between px-4 py-2 border-b bg-muted/30">
          <span className="text-xs text-muted-foreground font-mono">typescript</span>
        </div>
        <pre className="p-4 font-mono text-sm leading-relaxed overflow-x-auto">
          <code>{`const workspace = await api.create({
  name: "My Team",
  plan: "pro"
})`}</code>
        </pre>
      </div>
    </div>
  )
}

function KanbanMockup() {
  return (
    <div className="p-4 sm:p-6 overflow-x-auto">
      <div className="flex gap-4 min-w-[800px]">
        {kanbanColumns.map((col) => (
          <div key={col.title} className="flex-1 min-w-[200px]">
            <div className="flex items-center gap-2 mb-3 px-1">
              <div className={`size-2.5 rounded-full ${col.color}`} />
              <span className="text-sm font-semibold">{col.title}</span>
              <span className="text-xs text-muted-foreground ml-auto">{col.cards.length}</span>
            </div>
            <div className="space-y-2.5">
              {col.cards.map((card) => (
                <div
                  key={card.title}
                  className="rounded-xl border bg-card p-3 shadow-sm hover:shadow-md transition-shadow cursor-pointer group"
                >
                  <div className="flex gap-1.5 mb-2">
                    {card.labels.map((label) => (
                      <span
                        key={label.text}
                        className={`${label.color} text-white text-[10px] font-medium px-1.5 py-0.5 rounded`}
                      >
                        {label.text}
                      </span>
                    ))}
                  </div>
                  <p className={cn("text-sm font-medium mb-2.5", "done" in card && card.done && "line-through text-muted-foreground")}>
                    {card.title}
                  </p>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center justify-center size-6 rounded-full bg-gradient-to-br from-violet-500 to-indigo-500 text-white text-[10px] font-medium">
                      {card.avatar}
                    </div>
                    <div className="flex items-center gap-2 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity">
                      <Clock className="size-3" />
                      <MoreHorizontal className="size-3.5" />
                    </div>
                  </div>
                </div>
              ))}
              <button className="w-full flex items-center gap-1.5 px-3 py-2 text-xs text-muted-foreground hover:text-foreground hover:bg-accent/50 rounded-lg transition-colors">
                <Plus className="size-3.5" />
                Add card
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

export function DemoSection() {
  const [activeTab, setActiveTab] = React.useState<Tab>("docs")

  return (
    <section id="showcase" className="py-28">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center mb-12">
          <Badge variant="outline" className="mb-4 text-xs font-semibold tracking-wider uppercase">
            Product
          </Badge>
          <h2 className="text-3xl sm:text-4xl lg:text-5xl font-bold tracking-tight mb-5">
            Two powerful tools, one workspace
          </h2>
          <p className="text-lg text-muted-foreground max-w-2xl mx-auto">
            Switch seamlessly between rich documents and visual boards.
            Everything lives in one place.
          </p>
        </div>

        <div className="max-w-5xl mx-auto">
          <div className="flex items-center justify-center gap-2 mb-8">
            <button
              onClick={() => setActiveTab("docs")}
              className={cn(
                "flex items-center gap-2 px-5 py-2.5 rounded-full text-sm font-medium transition-all",
                activeTab === "docs"
                  ? "bg-gradient-to-r from-violet-600 to-indigo-600 text-white shadow-lg shadow-violet-500/20"
                  : "bg-muted text-muted-foreground hover:text-foreground"
              )}
            >
              <GripVertical className="size-4" />
              Document Editor
            </button>
            <button
              onClick={() => setActiveTab("boards")}
              className={cn(
                "flex items-center gap-2 px-5 py-2.5 rounded-full text-sm font-medium transition-all",
                activeTab === "boards"
                  ? "bg-gradient-to-r from-violet-600 to-indigo-600 text-white shadow-lg shadow-violet-500/20"
                  : "bg-muted text-muted-foreground hover:text-foreground"
              )}
            >
              <Tag className="size-4" />
              Kanban Board
            </button>
          </div>

          <div className="relative">
            <div className="absolute -inset-3 bg-gradient-to-r from-violet-500/10 via-indigo-500/10 to-purple-500/10 rounded-[1.5rem] blur-xl" />
            <div className="relative rounded-2xl border bg-card shadow-2xl shadow-violet-500/5 overflow-hidden min-h-[460px]">
              <div className="flex items-center gap-2 px-4 py-2.5 bg-muted/50 border-b">
                <div className="flex gap-1.5">
                  <div className="size-2.5 rounded-full bg-red-400/80" />
                  <div className="size-2.5 rounded-full bg-yellow-400/80" />
                  <div className="size-2.5 rounded-full bg-green-400/80" />
                </div>
                <div className="flex-1 text-center text-xs text-muted-foreground">
                  {activeTab === "docs" ? "Product Roadmap — Craftboard" : "Sprint Board — Craftboard"}
                </div>
              </div>

              {activeTab === "docs" ? <DocumentMockup /> : <KanbanMockup />}
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
