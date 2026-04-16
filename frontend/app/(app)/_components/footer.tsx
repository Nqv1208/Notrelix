import Link from "next/link"
import { Layers } from "lucide-react"

import { Separator } from "@/registry/new-york-v4/ui/separator"
import { routes } from "@/lib/routes"

const footerSections = {
  product: {
    title: "Product",
    links: [
      { label: "Features", href: "#features" },
      { label: "Pricing", href: "#pricing" },
      { label: "Changelog", href: "#" },
      { label: "Roadmap", href: "#" },
    ],
  },
  resources: {
    title: "Resources",
    links: [
      { label: "Documentation", href: "#" },
      { label: "API Reference", href: "#" },
      { label: "Templates", href: "#" },
      { label: "Blog", href: "#" },
    ],
  },
  company: {
    title: "Company",
    links: [
      { label: "About", href: "#" },
      { label: "Careers", href: "#" },
      { label: "Contact", href: routes.contact },
      { label: "Press Kit", href: "#" },
    ],
  },
  legal: {
    title: "Legal",
    links: [
      { label: "Privacy", href: routes.auth.privacy },
      { label: "Terms", href: routes.auth.terms },
      { label: "Security", href: "#" },
    ],
  },
}

export function Footer() {
  const year = new Date().getFullYear()

  return (
    <footer className="border-t bg-muted/30">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8 py-16">
        <div className="grid grid-cols-2 md:grid-cols-5 gap-8 lg:gap-12">
          <div className="col-span-2 md:col-span-1">
            <Link href={routes.home} className="flex items-center gap-2 mb-4">
              <div className="flex items-center justify-center size-8 rounded-xl bg-gradient-to-br from-violet-600 to-indigo-600">
                <Layers className="size-4 text-white" />
              </div>
              <span className="font-bold text-lg">
                Notre<span className="bg-gradient-to-r from-violet-600 to-indigo-600 bg-clip-text text-transparent">lix</span>
              </span>
            </Link>
            <p className="text-sm text-muted-foreground leading-relaxed">
              Docs, tasks, and boards — unified in one beautiful workspace.
            </p>
          </div>

          {Object.entries(footerSections).map(([key, section]) => (
            <div key={key}>
              <h4 className="font-semibold text-sm mb-4">{section.title}</h4>
              <ul className="space-y-2.5">
                {section.links.map((link) => (
                  <li key={link.label}>
                    <a
                      href={link.href}
                      className="text-sm text-muted-foreground hover:text-foreground transition-colors"
                    >
                      {link.label}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <Separator className="my-10" />

        <div className="flex flex-col md:flex-row items-center justify-between gap-4 text-sm text-muted-foreground">
          <p>&copy; {year} Notrelix, Inc. All rights reserved.</p>
          <div className="flex items-center gap-4">
            <span className="flex items-center gap-1.5">
              <span className="size-2 rounded-full bg-emerald-500 animate-pulse" />
              All systems operational
            </span>
          </div>
        </div>
      </div>
    </footer>
  )
}
