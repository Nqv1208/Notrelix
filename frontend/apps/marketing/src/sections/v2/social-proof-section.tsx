import { CheckCircle2 } from 'lucide-react'

import { Reveal } from '../../components/v2/reveal'

const teams = ['Northstar', 'Morrow', 'Cedar Labs', 'Orbit', 'Kite Studio', 'Aster']

export function SocialProofSection() {
  return (
    <section className="border-y border-[var(--v2-line)] bg-white/55 py-9">
      <div className="v2-container">
        <Reveal className="flex flex-col items-center gap-7 lg:flex-row lg:justify-between">
          <p className="text-center text-sm font-medium text-[var(--v2-muted)] lg:text-left">
            Được các đội ngũ hiện đại tin dùng để biến kế hoạch thành tiến độ.
          </p>
          <div className="grid w-full grid-cols-2 items-center gap-x-7 gap-y-4 sm:grid-cols-3 lg:w-auto lg:grid-cols-6">
            {teams.map((team) => (
              <span key={team} className="text-center text-sm font-semibold tracking-[-0.03em] text-[var(--v2-ink)]/45 transition-colors hover:text-[var(--v2-ink)]/75">
                {team}
              </span>
            ))}
          </div>
        </Reveal>
      </div>
    </section>
  )
}

export function TrustStrip() {
  return (
    <div className="mt-8 flex flex-wrap items-center justify-center gap-4 text-xs text-[var(--v2-muted)]">
      <span className="inline-flex items-center gap-1.5"><CheckCircle2 className="size-3.5 text-[var(--v2-teal)]" /> Phân quyền theo workspace</span>
      <span className="inline-flex items-center gap-1.5"><CheckCircle2 className="size-3.5 text-[var(--v2-teal)]" /> Dữ liệu luôn thuộc về đội ngũ</span>
    </div>
  )
}
