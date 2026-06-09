"use client"

import { ShieldCheck, Key, RefreshCw, BarChart2 } from "lucide-react"
import { cn } from "@/lib/utils"

export function EnterpriseSection() {
  const enterpriseSpecs = [
    {
      icon: ShieldCheck,
      title: "SOC 2 Type II Certified",
      desc: "Our platform underwent rigorous security audit reports to guarantee safety compliance standards.",
    },
    {
      icon: Key,
      title: "Enterprise SSO & SAML",
      desc: "Connect seamlessly with Okta, Active Directory, Google Workspace, or custom Identity providers.",
    },
    {
      icon: RefreshCw,
      title: "Automated Data Backups",
      desc: "Point-in-time database restore snapshots, S3-powered file storage redundancy, and continuous health checks.",
    },
    {
      icon: BarChart2,
      title: "Granular RBAC Policies",
      desc: "Configure strict folder permissions, read/write/edit levels, and track actions using system logs.",
    },
  ]

  return (
    <section className="py-20 border-t border-zinc-200/80 bg-zinc-50/50 dark:border-zinc-800 dark:bg-zinc-950/20">
      <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 lg:grid-cols-5 gap-12 items-center">
          
          {/* Left Summary Text */}
          <div className="lg:col-span-2 space-y-4 text-left">
            <span className="text-[10px] font-extrabold tracking-wider text-blue-600 uppercase dark:text-blue-450">
              ENTERPRISE READY
            </span>
            <h2
              className={cn(
                "text-3xl font-extrabold tracking-tight text-zinc-950 sm:text-4xl dark:text-white leading-tight",
                "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
              )}
            >
              Uptime, compliance and security. Guaranteed.
            </h2>
            <p className="text-xs leading-relaxed text-zinc-650 dark:text-zinc-400">
              Notrelix is engineered from the ground up to support high-growth teams, SaaS builders, and enterprise entities requiring rigorous data controls and robust SLA coverage.
            </p>
            
            {/* Minimalist badges list */}
            <div className="pt-2 flex flex-wrap gap-2">
              {["SOC 2 Type II", "GDPR Compliant", "SSO Enabled", "99.9% Uptime SLA"].map((badge) => (
                <span
                  key={badge}
                  className="rounded-full bg-white border border-zinc-200 px-3 py-1 text-[10px] font-bold text-zinc-700 shadow-2xs dark:bg-zinc-900 dark:border-zinc-800 dark:text-zinc-350"
                >
                  {badge}
                </span>
              ))}
            </div>
          </div>

          {/* Right Cards List */}
          <div className="lg:col-span-3 grid grid-cols-1 sm:grid-cols-2 gap-6">
            {enterpriseSpecs.map((spec, index) => {
              const Icon = spec.icon
              return (
                <div
                  key={index}
                  className="rounded-xl border border-zinc-200 bg-white p-5.5 shadow-xs dark:border-zinc-850 dark:bg-zinc-900"
                >
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-50 text-blue-600 dark:bg-blue-950/40 dark:text-blue-400 mb-4.5">
                    <Icon className="h-5 w-5 shrink-0" />
                  </div>
                  <h3 className="text-sm font-bold text-zinc-950 dark:text-white mb-2">
                    {spec.title}
                  </h3>
                  <p className="text-[11px] leading-relaxed text-zinc-600 dark:text-zinc-400">
                    {spec.desc}
                  </p>
                </div>
              )
            })}
          </div>

        </div>
      </div>
    </section>
  )
}
