import Link from 'next/link'
import { ArrowUpRight, BriefcaseBusiness, CodeXml, MessageCircle } from 'lucide-react'

import Image from 'next/image'

const footerSections = [
  {
    title: 'Sản phẩm',
    links: [
      { label: 'Tổng quan', href: '#showcase' },
      { label: 'Tính năng', href: '#features' },
      { label: 'Bảng giá', href: '#pricing' },
      { label: 'Tích hợp', href: '#features' },
    ],
  },
  {
    title: 'Giải pháp',
    links: [
      { label: 'Marketing', href: '#use-cases' },
      { label: 'Product', href: '#use-cases' },
      { label: 'Operations', href: '#use-cases' },
      { label: 'Lãnh đạo', href: '#use-cases' },
    ],
  },
  {
    title: 'Tài nguyên',
    links: [
      { label: 'Trung tâm trợ giúp', href: '/contact' },
      { label: 'Liên hệ đội ngũ', href: '/contact' },
      { label: 'Trạng thái hệ thống', href: '#status' },
      { label: 'Cập nhật sản phẩm', href: '#resources' },
    ],
  },
  {
    title: 'Pháp lý',
    links: [
      { label: 'Chính sách bảo mật', href: '/legal/privacy' },
      { label: 'Điều khoản sử dụng', href: '/legal/terms' },
      { label: 'Bảo mật doanh nghiệp', href: '#security' },
    ],
  },
]

export function MarketingFooter() {
  return (
    <footer id="resources" className="border-t border-[var(--v2-line)] bg-[var(--v2-surface)]">
      <div className="v2-container py-16 sm:py-20">
        <div className="grid gap-12 lg:grid-cols-[1.35fr_2fr]">
          <div className="max-w-xs">
            <Link href="/" aria-label="Notrelix - Trang chủ">
              <Image src="/logo.svg" alt="" width={36} height={28} aria-hidden="true" className="h-9 w-auto" />
            </Link>
            <p className="mt-5 text-sm leading-6 text-[var(--v2-muted)]">
              Không gian làm việc hợp nhất để đội ngũ viết, lập kế hoạch và hoàn thành công việc cùng nhau.
            </p>
            <a href="mailto:hello@notrelix.com" className="mt-5 inline-flex items-center gap-1.5 text-sm font-semibold text-[var(--v2-ink)] hover:text-[var(--v2-cobalt)]">
              hello@notrelix.com <ArrowUpRight className="size-3.5" />
            </a>
          </div>

          <div className="grid grid-cols-2 gap-x-6 gap-y-10 sm:grid-cols-4">
            {footerSections.map((section) => (
              <div key={section.title}>
                <h2 className="text-sm font-semibold text-[var(--v2-ink)]">{section.title}</h2>
                <ul className="mt-4 space-y-3">
                  {section.links.map((link) => (
                    <li key={link.label}>
                      <a href={link.href} className="text-sm text-[var(--v2-muted)] transition-colors hover:text-[var(--v2-ink)]">
                        {link.label}
                      </a>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>

        <div className="mt-14 flex flex-col gap-5 border-t border-[var(--v2-line)] pt-6 text-sm text-[var(--v2-muted)] sm:flex-row sm:items-center sm:justify-between">
          <p>© {new Date().getFullYear()} Notrelix. Xây dựng cách đội ngũ làm việc tốt hơn.</p>
          <div className="flex items-center gap-4">
            <span id="status" className="inline-flex items-center gap-2">
              <span className="size-2 rounded-full bg-emerald-500" aria-hidden="true" />
              Hệ thống đang hoạt động
            </span>
            <a href="https://github.com/notrelix" target="_blank" rel="noreferrer" aria-label="Notrelix trên GitHub" className="transition-colors hover:text-[var(--v2-ink)]">
              <CodeXml className="size-4" />
            </a>
            <a href="https://www.linkedin.com/company/notrelix" target="_blank" rel="noreferrer" aria-label="Notrelix trên LinkedIn" className="transition-colors hover:text-[var(--v2-ink)]">
              <BriefcaseBusiness className="size-4" />
            </a>
            <a href="mailto:hello@notrelix.com" aria-label="Liên hệ Notrelix" className="transition-colors hover:text-[var(--v2-ink)]">
              <MessageCircle className="size-4" />
            </a>
          </div>
        </div>
      </div>
    </footer>
  )
}
