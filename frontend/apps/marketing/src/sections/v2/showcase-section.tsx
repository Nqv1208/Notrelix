'use client'

import * as React from 'react'
import {
  ArrowRight,
  BarChart3,
  CalendarDays,
  Check,
  CircleDot,
  GitBranch,
  LayoutGrid,
  ListChecks,
  Play,
  Sparkles,
  Timer,
} from 'lucide-react'

import { Reveal } from '../../components/v2/reveal'
import { SectionHeading } from '../../components/v2/section-heading'

type ShowcaseTab = 'plan' | 'progress' | 'automation' | 'reporting'

const tabs: { id: ShowcaseTab; label: string; icon: typeof LayoutGrid }[] = [
  { id: 'plan', label: 'Lập kế hoạch', icon: CalendarDays },
  { id: 'progress', label: 'Theo dõi tiến độ', icon: CircleDot },
  { id: 'automation', label: 'Tự động hóa', icon: GitBranch },
  { id: 'reporting', label: 'Báo cáo', icon: BarChart3 },
]

function PlanPanel() {
  return (
    <div className="grid gap-5 lg:grid-cols-[1.15fr_0.85fr]">
      <div className="rounded-2xl border border-[var(--v2-line)] bg-white p-5">
        <div className="flex items-center justify-between"><div><p className="text-xs font-semibold text-[var(--v2-ink)]">Lịch triển khai tháng 6</p><p className="mt-1 text-[0.65rem] text-[var(--v2-muted)]">Các mốc quan trọng của Product team</p></div><button type="button" aria-label="Thêm mốc lịch" className="flex size-8 items-center justify-center rounded-lg bg-[var(--v2-lilac)] text-[var(--v2-cobalt)]"><CalendarDays className="size-3.5" /></button></div>
        <div className="mt-6 grid grid-cols-7 gap-1 text-center text-[0.55rem] text-[var(--v2-muted)]">{['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'].map((day) => <span key={day}>{day}</span>)}</div>
        <div className="mt-2 grid grid-cols-7 gap-1.5">{Array.from({ length: 28 }, (_, index) => <span key={index} className={`flex aspect-square items-center justify-center rounded-md text-[0.6rem] ${index === 14 ? 'bg-[var(--v2-cobalt)] font-semibold text-white' : index > 9 && index < 14 ? 'bg-[var(--v2-lilac)] text-[var(--v2-cobalt)]' : 'text-[var(--v2-muted)] hover:bg-[var(--v2-surface)]'}`}>{index + 1}</span>)}</div>
      </div>
      <div className="rounded-2xl border border-[var(--v2-line)] bg-white p-5"><div className="flex items-center gap-2 text-xs font-semibold text-[var(--v2-ink)]"><ListChecks className="size-4 text-[var(--v2-teal)]" /> Mốc cần chuẩn bị</div><div className="mt-5 space-y-3">{['Chốt scope với Sales', 'Review brief trải nghiệm', 'Gửi roadmap cho team'].map((item, index) => <div key={item} className="flex items-start gap-3 rounded-xl bg-[var(--v2-surface)] p-3"><span className={`mt-0.5 flex size-5 items-center justify-center rounded-full ${index === 2 ? 'bg-emerald-100 text-emerald-600' : 'bg-white text-[var(--v2-cobalt)]'}`}>{index === 2 ? <Check className="size-3" /> : <span className="text-[0.6rem] font-bold">{index + 1}</span>}</span><span className="text-xs font-medium text-[var(--v2-ink)]">{item}<small className="mt-1 block text-[0.62rem] font-normal text-[var(--v2-muted)]">{index === 0 ? 'Hôm nay, 15:30' : index === 1 ? 'Thứ 4, 09:00' : 'Thứ 6, 17:00'}</small></span></div>)}</div></div>
    </div>
  )
}

function ProgressPanel() {
  const columns = [
    { title: 'Chưa bắt đầu', tone: 'bg-slate-400', items: ['Nghiên cứu onboarding', 'Cập nhật pricing page'] },
    { title: 'Đang thực hiện', tone: 'bg-[var(--v2-coral)]', items: ['Flow mời thành viên', 'Bản đồ trải nghiệm'] },
    { title: 'Hoàn thành', tone: 'bg-[var(--v2-teal)]', items: ['Brief Q3', 'Review KPI tháng'] },
  ]
  return <div className="grid gap-4 lg:grid-cols-3">{columns.map((column) => <div key={column.title} className="rounded-2xl border border-[var(--v2-line)] bg-white p-4"><div className="flex items-center gap-2 text-xs font-semibold text-[var(--v2-ink)]"><span className={`size-2 rounded-full ${column.tone}`} />{column.title}<span className="ml-auto text-[0.65rem] text-[var(--v2-muted)]">{column.items.length}</span></div><div className="mt-4 space-y-2.5">{column.items.map((item, index) => <div key={item} className="rounded-xl border border-[var(--v2-line)] p-3 shadow-sm"><div className="flex items-start justify-between gap-2"><span className="text-xs font-medium leading-5 text-[var(--v2-ink)]">{item}</span><span className={`size-5 shrink-0 rounded-full ${index === 0 ? 'bg-[var(--v2-peach)]' : 'bg-[var(--v2-lilac)]'}`} /></div><div className="mt-3 flex items-center justify-between text-[0.6rem] text-[var(--v2-muted)]"><span>{index + 2} người liên quan</span><Timer className="size-3" /></div></div>)}</div></div>)}</div>
}

function AutomationPanel() {
  return <div className="mx-auto max-w-3xl rounded-2xl border border-[var(--v2-line)] bg-white p-5 sm:p-7"><div className="flex items-center justify-between border-b border-[var(--v2-line)] pb-5"><div><p className="text-sm font-semibold text-[var(--v2-ink)]">Khi một item được hoàn thành</p><p className="mt-1 text-xs text-[var(--v2-muted)]">Luồng tự động đang hoạt động</p></div><span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 px-2.5 py-1 text-[0.65rem] font-semibold text-emerald-700"><span className="size-1.5 rounded-full bg-emerald-500" /> Đang bật</span></div><div className="mt-7 grid gap-3 sm:grid-cols-[1fr_auto_1fr_auto_1fr] sm:items-center"><div className="rounded-xl border border-[var(--v2-line)] bg-[var(--v2-surface)] p-4"><GitBranch className="size-4 text-[var(--v2-cobalt)]" /><p className="mt-3 text-xs font-semibold text-[var(--v2-ink)]">Trigger</p><p className="mt-1 text-[0.65rem] text-[var(--v2-muted)]">Status thay đổi</p></div><ArrowRight className="hidden size-4 text-[var(--v2-muted)] sm:block" /><div className="rounded-xl border border-[var(--v2-line)] bg-[var(--v2-surface)] p-4"><Sparkles className="size-4 text-[var(--v2-coral)]" /><p className="mt-3 text-xs font-semibold text-[var(--v2-ink)]">Điều kiện</p><p className="mt-1 text-[0.65rem] text-[var(--v2-muted)]">Status = Hoàn thành</p></div><ArrowRight className="hidden size-4 text-[var(--v2-muted)] sm:block" /><div className="rounded-xl border border-[var(--v2-line)] bg-[var(--v2-surface)] p-4"><Check className="size-4 text-[var(--v2-teal)]" /><p className="mt-3 text-xs font-semibold text-[var(--v2-ink)]">Action</p><p className="mt-1 text-[0.65rem] text-[var(--v2-muted)]">Gửi thông báo team</p></div></div></div>
}

function ReportingPanel() {
  return <div className="grid gap-4 sm:grid-cols-3"><div className="rounded-2xl border border-[var(--v2-line)] bg-white p-5 sm:col-span-2"><div className="flex items-center justify-between"><div><p className="text-xs font-semibold text-[var(--v2-ink)]">Công việc hoàn thành</p><p className="mt-1 text-[0.65rem] text-[var(--v2-muted)]">Trong 8 tuần gần nhất</p></div><BarChart3 className="size-4 text-[var(--v2-teal)]" /></div><div className="mt-8 flex h-32 items-end gap-2">{[32, 46, 39, 58, 52, 76, 68, 92].map((height, index) => <div key={index} className="relative flex-1 rounded-t-lg bg-gradient-to-t from-[var(--v2-cobalt)] to-[#9aa1ff]" style={{ height: `${height}%` }}><span className="absolute -top-5 left-1/2 -translate-x-1/2 text-[0.55rem] text-[var(--v2-muted)]">{height}</span></div>)}</div></div><div className="rounded-2xl border border-[var(--v2-line)] bg-white p-5"><p className="text-xs font-semibold text-[var(--v2-ink)]">Tín hiệu tuần này</p><div className="mt-6 text-4xl font-semibold tracking-[-0.06em] text-[var(--v2-ink)]">+28%</div><p className="mt-1 text-xs text-emerald-600">Năng suất tăng so với tuần trước</p><div className="mt-7 h-2 overflow-hidden rounded-full bg-[var(--v2-surface)]"><div className="h-full w-[78%] rounded-full bg-[var(--v2-teal)]" /></div><p className="mt-2 text-[0.62rem] text-[var(--v2-muted)]">78% mục tiêu tháng</p></div></div>
}

const panels: Record<ShowcaseTab, React.ReactNode> = {
  plan: <PlanPanel />,
  progress: <ProgressPanel />,
  automation: <AutomationPanel />,
  reporting: <ReportingPanel />,
}

export function ShowcaseSection() {
  const [activeTab, setActiveTab] = React.useState<ShowcaseTab>('plan')
  const tabRefs = React.useRef<Array<HTMLButtonElement | null>>([])

  const moveTab = (direction: 1 | -1) => {
    const index = tabs.findIndex((tab) => tab.id === activeTab)
    const nextIndex = (index + direction + tabs.length) % tabs.length
    const nextTab = tabs[nextIndex]
    if (!nextTab) return
    setActiveTab(nextTab.id)
    tabRefs.current[nextIndex]?.focus()
  }

  return (
    <section id="showcase" className="v2-section bg-[var(--v2-surface)]">
      <div className="v2-container">
        <Reveal>
          <SectionHeading
            align="center"
            eyebrow="Trải nghiệm sản phẩm"
            title={<>Một workspace theo cách <span className="v2-gradient-text">đội ngũ bạn làm việc.</span></>}
            description="Chọn một góc nhìn để thấy Notrelix biến dữ liệu công việc thành hành động rõ ràng."
          />
        </Reveal>

        <Reveal delay={100} className="mt-12">
          <div role="tablist" aria-label="Các góc nhìn sản phẩm" className="v2-tabs-wrap">
            {tabs.map((tab, index) => {
              const Icon = tab.icon
              const selected = activeTab === tab.id
              return <button key={tab.id} ref={(element) => { tabRefs.current[index] = element }} type="button" role="tab" id={`tab-${tab.id}`} aria-selected={selected} aria-controls={`panel-${tab.id}`} tabIndex={selected ? 0 : -1} onClick={() => setActiveTab(tab.id)} onKeyDown={(event) => { if (event.key === 'ArrowRight') moveTab(1); if (event.key === 'ArrowLeft') moveTab(-1) }} className={`v2-tab ${selected ? 'is-active' : ''}`}><Icon className="size-4" />{tab.label}</button>
            })}
          </div>
        </Reveal>

        <div className="v2-showcase-shell" role="tabpanel" id={`panel-${activeTab}`} aria-labelledby={`tab-${activeTab}`}>
          <div className="flex items-center justify-between border-b border-[var(--v2-line)] px-4 py-3 sm:px-6"><div className="flex items-center gap-1.5"><span className="size-2 rounded-full bg-[#ef7866]" /><span className="size-2 rounded-full bg-[#e9b64e]" /><span className="size-2 rounded-full bg-[#55b985]" /></div><span className="text-[0.65rem] font-medium text-[var(--v2-muted)]">workspace.notrelix.com</span><div className="hidden items-center gap-2 text-[var(--v2-muted)] sm:flex"><Play className="size-3.5" /><span className="text-[0.62rem]">Live preview</span></div></div>
          <div className="min-h-[360px] p-4 sm:p-8">{panels[activeTab]}</div>
        </div>
      </div>
    </section>
  )
}
