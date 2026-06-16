import type { Metadata } from "next"
import { Fraunces, Archivo, IBM_Plex_Mono } from "next/font/google"

import { cn } from "@/lib/utils"
import "./editorial.css"

// Display — characterful optical serif for editorial headlines
const editorialSerif = Fraunces({
  subsets: ["latin"],
  variable: "--font-editorial-serif",
  display: "swap",
})

// Body — neutral grotesque with Swiss DNA
const editorialSans = Archivo({
  subsets: ["latin"],
  variable: "--font-editorial-sans",
  display: "swap",
})

// Labels, indices, captions
const editorialMono = IBM_Plex_Mono({
  subsets: ["latin"],
  variable: "--font-editorial-mono",
  weight: ["400", "500"],
  display: "swap",
})

export const metadata: Metadata = {
  title: "Notrelix — One workspace for docs, boards & work",
  description:
    "Notrelix brings flexible documents, drag-and-drop boards, and a synced calendar into a single workspace — so your team plans, writes, and ships in the same place.",
  openGraph: {
    title: "Notrelix — One workspace for docs, boards & work",
    description:
      "Documents, boards, and calendar in one calm, fast workspace built for modern teams.",
    type: "website",
    siteName: "Notrelix",
  },
}

export default function LandingV2Layout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <div
      className={cn(
        editorialSerif.variable,
        editorialSans.variable,
        editorialMono.variable,
        "editorial-root min-h-screen antialiased"
      )}
    >
      {children}
    </div>
  )
}
