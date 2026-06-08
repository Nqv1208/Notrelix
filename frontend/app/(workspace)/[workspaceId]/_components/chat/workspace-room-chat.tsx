"use client"

import { useState } from "react"
import {
  Bell,
  Bot,
  FileText,
  MessageSquareText,
  MoreHorizontal,
  PanelRight,
  PanelRightClose,
  Paperclip,
  Phone,
  Pin,
  Plug,
  Search,
  Send,
  Settings2,
  ShieldCheck,
  SmilePlus,
  Sparkles,
  Video,
  X,
} from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Separator } from "@/components/ui/separator"
import { cn } from "@/lib/utils"
import type { WorkspaceMember } from "../dashboard/workspace-data"

type ChatMessage = {
  id: string
  author: string
  initials: string
  color: string
  time: string
  body: string
  channel: string
}

interface WorkspaceRoomChatProps {
  members: WorkspaceMember[]
  messages: ChatMessage[]
}

const pinnedNotes = [
  "Keep workspace decisions in this room before turning them into docs or board tasks.",
  "Daily sync starts at 09:30. Async blockers should be posted with owner and next step.",
]

const sharedFiles = [
  { name: "Docs MVP notes", type: "Doc", updatedAt: "12m ago" },
  { name: "Sprint-room-recording", type: "Media", updatedAt: "Today" },
  { name: "Board workflow brief", type: "PDF", updatedAt: "Yesterday" },
]

const roomActions = [
  { label: "Summarize unread", icon: Sparkles },
  { label: "Create follow-up tasks", icon: Bot },
  { label: "Connect board updates", icon: Plug },
  { label: "Notification rules", icon: Bell },
  { label: "Room permissions", icon: ShieldCheck },
  { label: "Room settings", icon: Settings2 },
]

export function WorkspaceRoomChat({ members, messages }: WorkspaceRoomChatProps) {
  const [detailsOpen, setDetailsOpen] = useState(true)
  const onlineMembers = members.filter((member) => member.status === "active" || member.status === "in-call")

  return (
    <main
      className={cn(
        "relative grid h-full min-h-0 bg-card text-foreground transition-[grid-template-columns]",
        detailsOpen ? "xl:grid-cols-[minmax(0,1fr)_340px]" : "xl:grid-cols-[minmax(0,1fr)_0px]"
      )}
    >
      <section className="flex min-w-0 flex-col">
        <header className="flex h-16 shrink-0 items-center justify-between border-b border-border bg-card/90 px-5 backdrop-blur-xl">
          <div className="min-w-0 pl-10 lg:pl-0">
            <div className="flex items-center gap-2">
              <MessageSquareText className="size-4 text-primary" />
              <h1 className="truncate text-base font-semibold text-foreground">Project room</h1>
              <Badge className="rounded-full">{onlineMembers.length} online</Badge>
            </div>
            <p className="mt-0.5 truncate text-xs text-muted-foreground">Workspace conversation, decisions, and async updates</p>
          </div>

          <div className="flex items-center gap-1">
            <Button variant="ghost" size="icon-sm" aria-label="Search room">
              <Search className="size-4" />
            </Button>
            <Button variant="ghost" size="icon-sm" aria-label="Start voice call">
              <Phone className="size-4" />
            </Button>
            <Button variant="ghost" size="icon-sm" aria-label="Start video meeting">
              <Video className="size-4" />
            </Button>
            <Button
              variant={detailsOpen ? "secondary" : "ghost"}
              size="icon-sm"
              onClick={() => setDetailsOpen((open) => !open)}
              aria-pressed={detailsOpen}
              aria-label={detailsOpen ? "Hide room details" : "Show room details"}
            >
              {detailsOpen ? <PanelRightClose className="size-4" /> : <PanelRight className="size-4" />}
            </Button>
            <Button variant="ghost" size="icon-sm" aria-label="More room actions">
              <MoreHorizontal className="size-4" />
            </Button>
          </div>
        </header>

        <ScrollArea className="min-h-0 flex-1">
          <div className="mx-auto w-full max-w-4xl space-y-4 px-4 py-6 sm:px-6">
            <div className="rounded-2xl border border-border bg-card p-4 shadow-sm">
              <div className="mb-2 flex items-center gap-2 text-sm font-semibold text-foreground">
                <Pin className="size-4 text-primary" />
                Room focus
              </div>
              <div className="space-y-2">
                {pinnedNotes.map((note) => (
                  <p key={note} className="rounded-xl bg-muted px-3 py-2 text-sm leading-6 text-muted-foreground">{note}</p>
                ))}
              </div>
            </div>

            {messages.map((message) => (
              <article key={message.id} className="group flex gap-3 rounded-2xl px-2 py-2 transition hover:bg-muted/60">
                <span className="mt-1 flex size-9 shrink-0 items-center justify-center rounded-full text-[10px] font-semibold text-primary-foreground" style={{ backgroundColor: message.color }}>
                  {message.initials}
                </span>
                <div className="min-w-0 flex-1">
                  <div className="mb-1 flex flex-wrap items-center gap-x-2 gap-y-1">
                    <span className="text-sm font-semibold text-foreground">{message.author}</span>
                    <span className="text-xs text-muted-foreground">{message.time}</span>
                    <Badge variant="secondary" className="rounded-full">{message.channel}</Badge>
                  </div>
                  <div className="rounded-2xl rounded-tl-md border border-border bg-card px-4 py-3 shadow-sm">
                    <p className="text-sm leading-6 text-muted-foreground">{message.body}</p>
                  </div>
                  <div className="mt-2 flex items-center gap-1 opacity-0 transition group-hover:opacity-100">
                    <Button variant="ghost" size="sm" className="h-7 rounded-full text-xs">
                      <SmilePlus className="size-3.5" />
                      React
                    </Button>
                    <Button variant="ghost" size="sm" className="h-7 rounded-full text-xs">Reply</Button>
                  </div>
                </div>
              </article>
            ))}
          </div>
        </ScrollArea>

        <div className="shrink-0 border-t border-border bg-card/90 p-4 backdrop-blur-xl">
          <div className="mx-auto flex max-w-4xl items-center gap-2 rounded-2xl border border-border bg-card p-2 shadow-sm">
            <Button variant="ghost" size="icon-sm" aria-label="Add attachment">
              <Paperclip className="size-4" />
            </Button>
            <Input className="h-10 flex-1 border-0 bg-muted shadow-none focus-visible:ring-0" placeholder="Message Project room..." />
            <Button variant="ghost" size="icon-sm" aria-label="Add reaction">
              <SmilePlus className="size-4" />
            </Button>
            <Button size="icon-sm" className="rounded-full" aria-label="Send message">
              <Send className="size-4" />
            </Button>
          </div>
        </div>
      </section>

      {detailsOpen ? (
        <RoomDetailsSidebar members={members} pinnedNotes={pinnedNotes} onClose={() => setDetailsOpen(false)} />
      ) : null}
    </main>
  )
}

function RoomDetailsSidebar({ members, pinnedNotes, onClose }: { members: WorkspaceMember[]; pinnedNotes: string[]; onClose: () => void }) {
  return (
    <aside className="fixed bottom-0 right-0 top-14 z-50 flex w-[min(92vw,360px)] min-h-0 flex-col border-l border-border bg-card shadow-xl xl:static xl:z-auto xl:w-auto xl:shadow-none">
      <div className="flex shrink-0 items-start justify-between gap-3 border-b border-border p-4">
        <div>
          <h2 className="text-sm font-semibold text-foreground">Room details</h2>
          <p className="mt-1 text-xs leading-5 text-muted-foreground">Context, controls, and collaboration signals for this workspace room.</p>
        </div>
        <Button variant="ghost" size="icon-sm" className="xl:hidden" onClick={onClose} aria-label="Close room details">
          <X className="size-4" />
        </Button>
      </div>

      <ScrollArea className="min-h-0 flex-1">
        <div className="space-y-4 p-4">
          <section className="rounded-2xl border border-border bg-card p-4">
            <div className="mb-3 flex items-center justify-between">
              <p className="text-xs font-semibold uppercase tracking-[0.08em] text-muted-foreground">Room members</p>
              <Badge variant="secondary" className="rounded-full">{members.length}</Badge>
            </div>
            <div className="space-y-2">
              {members.map((member) => (
                <div key={member.id} className="flex items-center gap-2 rounded-lg py-1">
                  <span className="relative flex size-8 items-center justify-center rounded-full text-[10px] font-semibold text-primary-foreground" style={{ backgroundColor: member.color }}>
                    {member.initials}
                    <span className={cn("absolute -bottom-0.5 -right-0.5 size-2 rounded-full ring-2 ring-card", member.status === "active" ? "bg-emerald-500" : member.status === "in-call" ? "bg-primary" : member.status === "idle" ? "bg-amber-500" : "bg-muted-foreground")} />
                  </span>
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium text-foreground">{member.name}</p>
                    <p className="text-xs text-muted-foreground">{member.role} · {member.status}</p>
                  </div>
                </div>
              ))}
            </div>
          </section>

          <section className="rounded-2xl border border-border bg-card p-4">
            <p className="mb-3 text-xs font-semibold uppercase tracking-[0.08em] text-muted-foreground">Pinned messages</p>
            <div className="space-y-2">
              {pinnedNotes.map((note) => (
                <p key={note} className="rounded-xl bg-muted px-3 py-2 text-sm leading-6 text-muted-foreground">{note}</p>
              ))}
            </div>
          </section>

          <section className="rounded-2xl border border-border bg-card p-4">
            <p className="mb-3 text-xs font-semibold uppercase tracking-[0.08em] text-muted-foreground">Media & files</p>
            <div className="space-y-2">
              {sharedFiles.map((file) => (
                <div key={file.name} className="flex items-center gap-2 rounded-xl bg-muted px-3 py-2">
                  <FileText className="size-4 text-primary" />
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium text-foreground">{file.name}</p>
                    <p className="text-xs text-muted-foreground">{file.type} · {file.updatedAt}</p>
                  </div>
                </div>
              ))}
            </div>
          </section>

          <section className="rounded-2xl border border-border bg-card p-4">
            <p className="mb-3 text-xs font-semibold uppercase tracking-[0.08em] text-muted-foreground">Controls</p>
            <div className="space-y-1">
              {roomActions.map((action) => (
                <button key={action.label} type="button" className="flex h-9 w-full items-center gap-2 rounded-lg px-2 text-sm font-medium text-muted-foreground transition hover:bg-muted hover:text-foreground">
                  <action.icon className="size-4" />
                  <span className="min-w-0 flex-1 truncate text-left">{action.label}</span>
                </button>
              ))}
            </div>
          </section>
        </div>
      </ScrollArea>

      <div className="shrink-0 border-t border-border p-4">
        <Separator className="mb-3" />
        <p className="text-xs leading-5 text-muted-foreground">AI summaries and integrations are mock controls until the real workspace API is connected.</p>
      </div>
    </aside>
  )
}
