import { ArrowUpRight, Clock3, ListChecks, TrendingUp } from 'lucide-react'

import { Reveal } from '../../components/v2/reveal'

const metrics = [
  { value: '30%', label: 'ít thời gian tìm thông tin hơn', icon: Clock3, tone: 'violet' },
  { value: '2,4×', label: 'tốc độ hoàn thành dự án', icon: TrendingUp, tone: 'teal' },
  { value: '40%', label: 'công việc thủ công được tự động hóa', icon: ListChecks, tone: 'coral' },
]

export function MetricsSection() {
  return (
    <section className="v2-section v2-metrics-section">
      <div className="v2-container">
        <div className="grid items-end gap-10 lg:grid-cols-[0.8fr_1.2fr]">
          <Reveal>
            <div>
              <span className="v2-eyebrow text-white/70">Kết quả nhìn thấy được</span>
              <h2 className="mt-4 max-w-md text-3xl font-semibold tracking-[-0.045em] text-white sm:text-4xl">Ít chuyển tab hơn. Nhiều tiến độ hơn.</h2>
              <p className="mt-5 max-w-md text-sm leading-6 text-white/65">Khi cả team làm việc trên cùng một nguồn dữ liệu, những thay đổi nhỏ tạo ra khác biệt lớn.</p>
              <a href="/contact" className="mt-7 inline-flex items-center gap-2 text-sm font-semibold text-white hover:gap-3">Trao đổi với đội ngũ <ArrowUpRight className="size-4" /></a>
            </div>
          </Reveal>
          <div className="grid gap-3 sm:grid-cols-3">
            {metrics.map((metric, index) => {
              const Icon = metric.icon
              return <Reveal key={metric.value} delay={index * 80}><div className="v2-metric-card"><div className={`v2-metric-icon v2-metric-icon--${metric.tone}`}><Icon className="size-4" /></div><div className="mt-8 text-4xl font-semibold tracking-[-0.06em] text-white">{metric.value}</div><p className="mt-2 text-sm leading-5 text-white/65">{metric.label}</p></div></Reveal>
            })}
          </div>
        </div>
      </div>
    </section>
  )
}
