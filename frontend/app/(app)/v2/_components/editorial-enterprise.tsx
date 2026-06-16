import { ShieldCheck, Lock, Globe, Clock } from "lucide-react"

import { SectionLabel } from "./editorial-section-label"
import { Reveal, RevealGroup, RevealItem } from "./editorial-reveal"

const badges = ["SOC 2 Type II", "ISO 27001", "GDPR", "HIPAA"]

const features = [
  { icon: Lock, title: "Private by default", body: "Data encrypted in transit and at rest, with AI more private than consumer chat." },
  { icon: ShieldCheck, title: "SSO & SCIM", body: "SAML single sign-on, directory sync, and enforced access policies." },
  { icon: Globe, title: "Data residency", body: "Choose where your workspace data lives, with full audit trails." },
  { icon: Clock, title: "24/7 support", body: "Priority response and a dedicated success partner for every plan." },
]

export function EditorialEnterprise() {
  return (
    <section id="enterprise" className="ed-ink-block">
      <div className="mx-auto max-w-[88rem] px-5 py-20 sm:px-8 sm:py-28">
        <div className="flex items-center gap-4">
          <span className="ed-bg-accent size-2" aria-hidden />
          <span className="ed-mono text-[0.7rem] uppercase tracking-[0.14em] opacity-60">
            05 / 08 — Enterprise
          </span>
        </div>

        <div className="mt-8 grid gap-10 lg:grid-cols-12 lg:gap-16">
          <div className="lg:col-span-5">
            <Reveal>
              <h2 className="ed-serif text-balance text-4xl leading-[1.02] tracking-tight sm:text-5xl">
                Enterprise-grade,
                <br />
                out of the box.
              </h2>
            </Reveal>
            <Reveal delay={0.05}>
              <p className="mt-6 max-w-md text-base leading-relaxed opacity-65">
                Security and governance built in from day one — so you can roll out
                across the whole company with confidence.
              </p>
            </Reveal>
            <Reveal delay={0.1}>
              <div className="mt-8 flex flex-wrap gap-2">
                {badges.map((b) => (
                  <span
                    key={b}
                    className="ed-mono border px-3 py-1.5 text-[0.7rem] uppercase tracking-wider [border-color:color-mix(in_oklab,var(--paper)_24%,transparent)]"
                  >
                    {b}
                  </span>
                ))}
              </div>
            </Reveal>
          </div>

          <RevealGroup className="grid grid-cols-1 gap-px sm:grid-cols-2 lg:col-span-7">
            {features.map((f) => (
              <RevealItem key={f.title}>
                <div className="flex h-full flex-col p-6 [background-color:color-mix(in_oklab,var(--paper)_5%,transparent)]">
                  <f.icon className="size-5 [color:var(--accent)]" strokeWidth={1.5} />
                  <h3 className="ed-serif mt-6 text-xl tracking-tight">{f.title}</h3>
                  <p className="mt-2 text-sm leading-relaxed opacity-65">{f.body}</p>
                </div>
              </RevealItem>
            ))}
          </RevealGroup>
        </div>
      </div>
    </section>
  )
}
