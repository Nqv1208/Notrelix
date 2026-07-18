'use client'

import * as React from 'react'
import { ArrowLeft, ArrowRight, Quote } from 'lucide-react'

import { Avatar, AvatarFallback } from '@notrelix/ui-web/components/ui/avatar'
import { Reveal } from '../../components/v2/reveal'

const testimonials = [
  { quote: 'Trước đây chúng tôi mất cả buổi để nối brief với công việc. Giờ team mở Notrelix là biết ngay bối cảnh và bước tiếp theo.', name: 'Linh Nguyễn', role: 'Head of Operations', company: 'Morrow Studio', initials: 'LN', tone: 'bg-[var(--v2-peach)] text-[#a24831]' },
  { quote: 'Điều có giá trị nhất không phải thêm một Board, mà là Board nằm ngay cạnh tài liệu và quyết định đã tạo ra nó.', name: 'Minh Trần', role: 'Product Lead', company: 'Northstar Labs', initials: 'MT', tone: 'bg-[var(--v2-lilac)] text-[var(--v2-cobalt)]' },
  { quote: 'Notrelix giúp leadership nhìn thấy tín hiệu mà không cần yêu cầu năm bản cập nhật khác nhau từ các team.', name: 'An Phạm', role: 'COO', company: 'Cedar Collective', initials: 'AP', tone: 'bg-[#d8f4e5] text-[#1b7c62]' },
]

export function TestimonialsSection() {
  const [active, setActive] = React.useState(0)
  const testimonial = testimonials[active] ?? testimonials[0]!

  return (
    <section id="testimonials" className="v2-section">
      <div className="v2-container">
        <Reveal>
          <div className="mx-auto max-w-4xl rounded-[2rem] border border-[var(--v2-line)] bg-white p-7 shadow-[0_24px_80px_rgba(21,32,57,0.06)] sm:p-12 lg:p-16">
            <div className="flex items-center justify-between"><span className="v2-eyebrow">Đội ngũ nói gì</span><Quote className="size-8 text-[var(--v2-lilac-strong)]" /></div>
            <blockquote className="mt-10 max-w-3xl text-2xl font-medium leading-[1.2] tracking-[-0.04em] text-[var(--v2-ink)] sm:text-4xl">“{testimonial.quote}”</blockquote>
            <div className="mt-10 flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between"><div className="flex items-center gap-3"><Avatar className="size-11"><AvatarFallback className={testimonial.tone}>{testimonial.initials}</AvatarFallback></Avatar><div><p className="text-sm font-semibold text-[var(--v2-ink)]">{testimonial.name}</p><p className="mt-1 text-xs text-[var(--v2-muted)]">{testimonial.role} · {testimonial.company}</p></div></div><div className="flex items-center gap-2"><button type="button" aria-label="Testimonial trước" onClick={() => setActive((current) => (current - 1 + testimonials.length) % testimonials.length)} className="v2-icon-button"><ArrowLeft className="size-4" /></button><button type="button" aria-label="Testimonial tiếp theo" onClick={() => setActive((current) => (current + 1) % testimonials.length)} className="v2-icon-button"><ArrowRight className="size-4" /></button><div className="ml-2 flex items-center gap-1.5" aria-label={`Đang xem testimonial ${active + 1} trên ${testimonials.length}`}>{testimonials.map((item, index) => <button key={item.name} type="button" aria-label={`Xem testimonial của ${item.name}`} aria-current={active === index} onClick={() => setActive(index)} className={`size-1.5 rounded-full transition-all ${active === index ? 'w-5 bg-[var(--v2-cobalt)]' : 'bg-[var(--v2-line-strong)]'}`} />)}</div></div></div>
          </div>
        </Reveal>
      </div>
    </section>
  )
}
