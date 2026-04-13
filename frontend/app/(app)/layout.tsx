import type { Metadata } from "next"

export const metadata: Metadata = {
  title: "Craftboard — Docs, Tasks & Boards in One Workspace",
  description:
    "Craftboard combines the power of a Notion-like document editor with Trello-style project boards. Write, plan, and ship — all in one place.",
  keywords: [
    "project management",
    "notion alternative",
    "trello alternative",
    "wiki",
    "kanban",
    "document editor",
    "collaboration",
    "SaaS",
    "productivity",
  ],
  authors: [{ name: "Craftboard Team" }],
  openGraph: {
    title: "Craftboard — Docs, Tasks & Boards in One Workspace",
    description:
      "Write docs like Notion. Manage tasks like Trello. Collaborate like a team that ships.",
    type: "website",
    siteName: "Craftboard",
  },
  twitter: {
    card: "summary_large_image",
    title: "Craftboard — Docs, Tasks & Boards in One Workspace",
    description:
      "Write docs like Notion. Manage tasks like Trello. Collaborate like a team that ships.",
  },
}

export default function AppLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return children
}
