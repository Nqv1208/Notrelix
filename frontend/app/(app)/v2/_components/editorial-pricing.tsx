import Link from "next/link"
import { Check } from "lucide-react"

import { cn } from "@/lib/utils"
import { routes } from "@/lib/routes"
import { SectionLabel } from "./editorial-section-label"
import { Reveal } from "./editorial-reveal"

type Tier = {
  name: string
  price: string
  unit: string
  blurb: string
  features: string[]
  cta: string
  href: string
  featured?: boolean
}

const tiers: Tier[] = [
  {
    name: "Free",
    price: "$0",
    unit: "forever",
    blurb: "For individuals and small teams getting organized.",
    features: ["Unlimited docs & boards", "Up to 5 members", "Calendar sync", "Community support"],
    cta: "Start for free",
    href: routes.auth.register,
  },
  {
    name: "Pro",
    price: "$9",
    unit: "/ user / mo",
    blurb: "For growing teams that need automation and depth.",
    features: ["Everything in Free", "Automations & AI", "Custom fields & views", "Priority support"],
    cta: "Start free trial",
    href: routes.auth.register,
    featured: true,
  },
  {
    name: "Enterprise",
    price: "Custom",
    unit: "talk to us",
    blurb: "For organizations rolling out at scale.",
    features: ["SSO, SCIM & audit logs", "Data residency", "99.99% uptime SLA", "Dedicated success"],
    cta: "Book a demo",
    href: routes.contact,
  },
]

export function EditorialPricing() {
  return (
    <section id="pricing" className="mx-auto max-w-[88rem] px-5 py-20 sm:px-8 sm:py-28">
      <SectionLabel index="06" label="Pricing" />
      <Reveal>
        <h2 className="ed-serif mt-8 max-w-3xl text-balance text-4xl leading-[1.02] tracking-tight sm:text-5xl lg:text-6xl">
          Simple pricing. No surprises.
        </h2>
      </Reveal>

      <div className="ed-rule-strong mt-14 grid grid-cols-1 gap-px border [background-color:var(--rule-strong)] lg:grid-cols-3">
        {tiers.map((t, i) => (
          <Reveal key={t.name} delay={i * 0.06}>
            <div
              className={cn(
                "flex h-full flex-col p-7 sm:p-8",
                t.featured ? "ed-ink-block" : "ed-paper"
              )}
            >
              <div className="flex items-center justify-between">
                <span
                  className={cn(
                    "ed-mono text-[0.7rem] uppercase tracking-[0.14em]",
                    t.featured ? "opacity-70" : "ed-ink-soft"
                  )}
                >
                  {t.name}
                </span>
                {t.featured ? (
                  <span className="ed-bg-accent px-2 py-0.5 text-[0.65rem] font-medium uppercase tracking-wider">
                    Popular
                  </span>
                ) : null}
              </div>

              <div className="mt-6 flex items-baseline gap-2">
                <span className="ed-serif text-5xl tracking-tight tabular-nums">
                  {t.price}
                </span>
                <span
                  className={cn(
                    "ed-mono text-xs",
                    t.featured ? "opacity-60" : "ed-ink-faint"
                  )}
                >
                  {t.unit}
                </span>
              </div>

              <p
                className={cn(
                  "mt-4 text-sm leading-relaxed",
                  t.featured ? "opacity-70" : "ed-ink-soft"
                )}
              >
                {t.blurb}
              </p>

              <ul className="mt-7 flex flex-1 flex-col gap-3">
                {t.features.map((f) => (
                  <li key={f} className="flex items-start gap-3 text-sm">
                    <Check
                      className="mt-0.5 size-4 shrink-0 [color:var(--accent)]"
                      strokeWidth={2}
                    />
                    <span className={t.featured ? "opacity-90" : "ed-ink"}>{f}</span>
                  </li>
                ))}
              </ul>

              <Link
                href={t.href}
                className={cn(
                  "mt-8 inline-flex items-center justify-center px-5 py-3 text-sm font-medium tracking-tight transition-colors",
                  t.featured
                    ? "ed-bg-accent hover:opacity-90"
                    : "ed-rule-strong ed-ink border hover:[background-color:var(--paper-ink)] hover:[color:var(--paper)]"
                )}
              >
                {t.cta}
              </Link>
            </div>
          </Reveal>
        ))}
      </div>
    </section>
  )
}
