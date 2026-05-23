import type { Metadata } from "next"
import { Instrument_Serif } from "next/font/google"

import { cn } from "@/lib/utils"

const landingSerif = Instrument_Serif({
  subsets: ["latin", "latin-ext"],
  variable: "--font-landing-serif",
  weight: "400",
  display: "swap",
})

export const metadata: Metadata = {
  title: "Notrelix — Workspace cho docs, board & công việc",
  description:
    "Ghi chú như Notion, kéo thả như Trello — một nơi để viết, lên kế hoạch và ship. Phiên bản landing mới.",
  openGraph: {
    title: "Notrelix — Workspace cho docs, board & công việc",
    description: "Viết, lên kế hoạch và phối hợp trong một workspace hiện đại.",
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
        landingSerif.variable,
        "min-h-screen bg-zinc-50 text-zinc-950 antialiased dark:bg-zinc-950 dark:text-zinc-50"
      )}
    >
      {children}
    </div>
  )
}
