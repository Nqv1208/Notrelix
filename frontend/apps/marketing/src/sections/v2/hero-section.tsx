import {
  ArrowRight,
  Check,
  FileText,
  LayoutGrid,
  MessageCircle,
  MoreHorizontal,
  Plus,
  Sparkles,
} from "lucide-react";

import { Badge } from "@notrelix/ui-web/components/ui/badge";
import { env } from "../../config/env";
import { Reveal } from "../../components/v2/reveal";

const boardRows = [
  {
    title: "Ra mắt workspace mới",
    owner: "AN",
    status: "Đang làm",
    tone: "amber",
    due: "Hôm nay",
  },
  {
    title: "Chuẩn hóa quy trình nội dung",
    owner: "ML",
    status: "Đang xem",
    tone: "violet",
    due: "18 Th6",
  },
  {
    title: "Đồng bộ tài liệu sản phẩm",
    owner: "HT",
    status: "Hoàn thành",
    tone: "emerald",
    due: "16 Th6",
  },
  {
    title: "Phỏng vấn khách hàng quý 3",
    owner: "QN",
    status: "Chưa bắt đầu",
    tone: "slate",
    due: "22 Th6",
  },
];

const statusStyles = {
  amber: "bg-amber-100 text-amber-700",
  violet: "bg-violet-100 text-violet-700",
  emerald: "bg-emerald-100 text-emerald-700",
  slate: "bg-slate-100 text-slate-600",
} as const;

function ProductWindow() {
  return (
    <div className="v2-product-window">
      <div className="flex items-center justify-between border-b border-[var(--v2-line)] px-4 py-3">
        <div className="flex items-center gap-2">
          <span className="flex size-6 items-center justify-center rounded-lg bg-[var(--v2-lilac)] text-[var(--v2-cobalt)]">
            <LayoutGrid className="size-3.5" />
          </span>
          <span className="text-xs font-semibold text-[var(--v2-ink)]">
            Không gian Acme
          </span>
          <span className="rounded-md bg-[var(--v2-surface)] px-2 py-1 text-[0.65rem] text-[var(--v2-muted)]">
            Board
          </span>
        </div>
        <div className="flex items-center gap-2 text-[var(--v2-muted)]">
          <MessageCircle className="size-3.5" />
          <MoreHorizontal className="size-4" />
        </div>
      </div>
      <div className="grid min-h-[330px] grid-cols-[150px_1fr]">
        <aside className="hidden border-r border-[var(--v2-line)] bg-[var(--v2-surface)] p-3 sm:block">
          <div className="mb-4 flex items-center gap-2 px-2">
            <span className="flex size-7 items-center justify-center rounded-lg bg-[var(--v2-ink)] text-[0.65rem] font-bold text-white">
              A
            </span>
            <span className="text-[0.68rem] font-semibold text-[var(--v2-ink)]">
              Acme Team
            </span>
          </div>
          <div className="space-y-1 text-[0.68rem]">
            {[
              { label: "Tổng quan", active: false, icon: Sparkles },
              { label: "Roadmap Q3", active: true, icon: LayoutGrid },
              { label: "Tài liệu", active: false, icon: FileText },
            ].map((item) => {
              const Icon = item.icon;
              return (
                <div
                  key={item.label}
                  className={`flex items-center gap-2 rounded-lg px-2 py-2 ${item.active ? "bg-white font-semibold text-[var(--v2-ink)] shadow-sm" : "text-[var(--v2-muted)]"}`}
                >
                  <Icon className="size-3.5" />
                  {item.label}
                </div>
              );
            })}
          </div>
          <div className="mt-7 px-2 text-[0.58rem] font-bold uppercase tracking-[0.16em] text-[var(--v2-muted)]">
            Views
          </div>
          <div className="mt-2 space-y-1 text-[0.68rem] text-[var(--v2-muted)]">
            <div className="rounded-lg px-2 py-2">Bảng công việc</div>
            <div className="rounded-lg px-2 py-2">Lịch triển khai</div>
          </div>
        </aside>
        <div className="min-w-0 bg-white p-4 sm:p-5">
          <div className="mb-5 flex items-start justify-between gap-3">
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-base font-semibold tracking-[-0.02em] text-[var(--v2-ink)] sm:text-lg">
                  Roadmap Q3
                </h3>
                <span className="rounded-full bg-emerald-50 px-2 py-0.5 text-[0.6rem] font-semibold text-emerald-700">
                  Live
                </span>
              </div>
              <p className="mt-1 text-[0.68rem] text-[var(--v2-muted)]">
                Đội ngũ sản phẩm · Cập nhật 2 phút trước
              </p>
            </div>
            <button
              type="button"
              aria-label="Thêm mục"
              className="flex size-8 items-center justify-center rounded-lg border border-[var(--v2-line)] text-[var(--v2-muted)]"
            >
              <Plus className="size-3.5" />
            </button>
          </div>
          <div className="overflow-hidden rounded-xl border border-[var(--v2-line)]">
            <div className="grid grid-cols-[minmax(130px,1.8fr)_0.8fr_0.9fr_0.7fr] gap-2 border-b border-[var(--v2-line)] bg-[var(--v2-surface)] px-3 py-2 text-[0.58rem] font-semibold uppercase tracking-[0.12em] text-[var(--v2-muted)]">
              <span>Công việc</span>
              <span>Người phụ trách</span>
              <span>Trạng thái</span>
              <span>Hạn</span>
            </div>
            {boardRows.map((row) => (
              <div
                key={row.title}
                className="grid grid-cols-[minmax(130px,1.8fr)_0.8fr_0.9fr_0.7fr] items-center gap-2 border-b border-[var(--v2-line)] px-3 py-3 text-[0.64rem] last:border-b-0 sm:text-[0.68rem]"
              >
                <span className="truncate font-medium text-[var(--v2-ink)]">
                  {row.title}
                </span>
                <span className="flex size-6 items-center justify-center rounded-full bg-[var(--v2-peach)] text-[0.55rem] font-bold text-[#a24831]">
                  {row.owner}
                </span>
                <span
                  className={`w-fit rounded-full px-2 py-1 text-[0.55rem] font-semibold ${statusStyles[row.tone as keyof typeof statusStyles]}`}
                >
                  {row.status}
                </span>
                <span className="text-[var(--v2-muted)]">{row.due}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

export function HeroSection() {
  return (
    <section
      id="hero"
      className="relative overflow-hidden pb-24 pt-16 sm:pb-32 sm:pt-24 lg:pt-28"
    >
      <div className="v2-container">
        <div className="mx-auto max-w-4xl text-center">
          <Reveal>
            <Badge className="v2-beta-badge">
              <span
                className="size-1.5 rounded-full bg-[var(--v2-teal)]"
                aria-hidden="true"
              />
              Work OS cho đội ngũ hiện đại
            </Badge>
          </Reveal>
          <Reveal delay={70}>
            <h1 className="mt-7 text-5xl font-semibold leading-[0.98] tracking-[-0.065em] text-[var(--v2-ink)] sm:text-6xl lg:text-[5.7rem]">
              Từ ý tưởng đến
              <span className="v2-gradient-text block">kết quả rõ ràng.</span>
            </h1>
          </Reveal>
          <Reveal delay={140}>
            <p className="mx-auto mt-7 max-w-2xl text-base leading-7 text-[var(--v2-muted)] sm:text-lg sm:leading-8">
              Notrelix kết nối tài liệu, Board, quy trình và dữ liệu trong một
              workspace duy nhất để đội ngũ biết việc gì cần làm tiếp theo.
            </p>
          </Reveal>
          <Reveal delay={210}>
            <div className="mt-9 flex flex-col items-center justify-center gap-3 sm:flex-row">
              <a
                href={`${env.webAppUrl}/sign-up`}
                className="v2-primary-button inline-flex h-12 w-full items-center justify-center gap-2 px-6 text-sm sm:w-auto"
              >
                Bắt đầu miễn phí <ArrowRight className="size-4" />
              </a>
              <a
                href="#showcase"
                className="v2-secondary-button inline-flex h-12 w-full items-center justify-center gap-2 px-6 text-sm sm:w-auto"
              >
                Xem Notrelix hoạt động
                <span className="flex size-5 items-center justify-center rounded-full bg-[var(--v2-lilac)] text-[var(--v2-cobalt)]">
                  ▶
                </span>
              </a>
            </div>
          </Reveal>
          <Reveal delay={280}>
            <div className="mt-7 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 text-xs text-[var(--v2-muted)] sm:text-sm">
              {[
                "Không cần thẻ tín dụng",
                "Thiết lập trong vài phút",
                "Miễn phí cho đội nhỏ",
              ].map((item) => (
                <span key={item} className="inline-flex items-center gap-1.5">
                  <Check className="size-3.5 text-[var(--v2-teal)]" /> {item}
                </span>
              ))}
            </div>
          </Reveal>
        </div>

        <Reveal
          delay={120}
          className="relative mx-auto mt-16 max-w-6xl sm:mt-20"
        >
          <div className="v2-hero-glow" aria-hidden="true" />
          <ProductWindow />
          <div className="v2-float-card v2-float-card--left hidden sm:block">
            <span className="flex size-8 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
              <Check className="size-4" />
            </span>
            <span>
              <strong>12 việc</strong>
              <small>đã hoàn thành hôm nay</small>
            </span>
          </div>
          <div className="v2-float-card v2-float-card--right hidden sm:flex">
            <span className="flex size-8 items-center justify-center rounded-xl bg-[var(--v2-peach)] text-[#bb5339]">
              <Sparkles className="size-4" />
            </span>
            <span>
              <strong>Tiến độ tốt</strong>
              <small>+24% so với tuần trước</small>
            </span>
          </div>
        </Reveal>
      </div>
    </section>
  );
}
