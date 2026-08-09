import {
  ArrowUpRight,
  BarChart3,
  CalendarDays,
  Check,
  FileText,
  GitBranch,
  LayoutGrid,
  MessageCircle,
  Sparkles,
} from "lucide-react";

import { Reveal } from "../../components/v2/reveal";

function WorkspaceVisual() {
  return (
    <div className="v2-story-visual v2-story-visual--lilac">
      <div className="v2-mini-window">
        <div className="flex items-center justify-between border-b border-[var(--v2-line)] px-4 py-3">
          <div className="flex items-center gap-2 text-xs font-semibold text-[var(--v2-ink)]">
            <FileText className="size-3.5 text-[var(--v2-cobalt)]" /> Product
            brief
          </div>
          <span className="text-[0.62rem] text-[var(--v2-muted)]">Đã lưu</span>
        </div>
        <div className="grid grid-cols-[1fr_115px] gap-4 p-5">
          <div>
            <div className="h-3 w-3/4 rounded-full bg-[var(--v2-ink)]/85" />
            <div className="mt-3 h-2 w-full rounded-full bg-[var(--v2-ink)]/10" />
            <div className="mt-2 h-2 w-11/12 rounded-full bg-[var(--v2-ink)]/10" />
            <div className="mt-7 rounded-xl border border-[var(--v2-line)] bg-white p-3">
              <div className="flex items-center gap-2 text-[0.62rem] font-semibold text-[var(--v2-ink)]">
                <LayoutGrid className="size-3 text-[var(--v2-coral)]" /> Hạng
                mục liên quan
              </div>
              <div className="mt-3 space-y-2">
                {[
                  "Nghiên cứu người dùng",
                  "Xác định phạm vi",
                  "Lập lịch triển khai",
                ].map((item, index) => (
                  <div
                    key={item}
                    className="flex items-center gap-2 text-[0.62rem] text-[var(--v2-muted)]"
                  >
                    <span
                      className={`size-2 rounded-full ${index === 1 ? "bg-[var(--v2-coral)]" : "bg-[var(--v2-teal)]"}`}
                    />
                    {item}
                  </div>
                ))}
              </div>
            </div>
          </div>
          <div className="hidden rounded-xl bg-[var(--v2-surface)] p-3 sm:block">
            <span className="text-[0.58rem] font-semibold uppercase tracking-[0.12em] text-[var(--v2-muted)]">
              Người xem
            </span>
            <div className="mt-3 flex -space-x-2">
              {["AN", "ML", "QN"].map((initials) => (
                <span
                  key={initials}
                  className="flex size-7 items-center justify-center rounded-full border-2 border-[var(--v2-surface)] bg-[var(--v2-peach)] text-[0.55rem] font-bold text-[#a24831]"
                >
                  {initials}
                </span>
              ))}
            </div>
            <div className="mt-8 rounded-lg bg-white p-2 text-[0.6rem] text-[var(--v2-muted)] shadow-sm">
              <MessageCircle className="mb-1 size-3 text-[var(--v2-cobalt)]" />{" "}
              4 bình luận mới
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function AutomationVisual() {
  return (
    <div className="v2-story-visual v2-story-visual--peach">
      <div className="mx-auto max-w-sm rounded-2xl border border-white/80 bg-white/90 p-5 shadow-xl shadow-[#c56b4a]/10">
        <div className="flex items-center justify-between">
          <div>
            <div className="text-[0.65rem] font-semibold uppercase tracking-[0.15em] text-[var(--v2-muted)]">
              Automation
            </div>
            <div className="mt-1 text-lg font-semibold tracking-[-0.03em] text-[var(--v2-ink)]">
              Triển khai nhanh hơn
            </div>
          </div>
          <span className="flex size-9 items-center justify-center rounded-xl bg-[var(--v2-peach)] text-[#bb5339]">
            <Sparkles className="size-4" />
          </span>
        </div>
        <div className="mt-6 space-y-3">
          <div className="v2-flow-node">
            <span className="flex size-7 items-center justify-center rounded-lg bg-[var(--v2-lilac)] text-[var(--v2-cobalt)]">
              <GitBranch className="size-3.5" />
            </span>
            <span>
              <b>Khi trạng thái thay đổi</b>
              <small>BoardField: Status → Hoàn thành</small>
            </span>
          </div>
          <div className="ml-4 h-4 border-l border-dashed border-[var(--v2-coral)]/50" />
          <div className="v2-flow-node">
            <span className="flex size-7 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600">
              <MessageCircle className="size-3.5" />
            </span>
            <span>
              <b>Gửi cập nhật đến team</b>
              <small>Thông báo trong workspace</small>
            </span>
          </div>
          <div className="ml-4 h-4 border-l border-dashed border-[var(--v2-coral)]/50" />
          <div className="v2-flow-node">
            <span className="flex size-7 items-center justify-center rounded-lg bg-sky-50 text-sky-600">
              <CalendarDays className="size-3.5" />
            </span>
            <span>
              <b>Tạo mốc tiếp theo</b>
              <small>Deadline sau 2 ngày làm việc</small>
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}

function InsightVisual() {
  return (
    <div className="v2-story-visual v2-story-visual--mint">
      <div className="mx-auto max-w-lg rounded-2xl border border-white/80 bg-white/90 p-5 shadow-xl shadow-[#3e8a77]/10">
        <div className="flex items-center justify-between">
          <div>
            <div className="text-[0.65rem] font-semibold uppercase tracking-[0.15em] text-[var(--v2-muted)]">
              Team pulse
            </div>
            <div className="mt-1 text-lg font-semibold tracking-[-0.03em] text-[var(--v2-ink)]">
              Tiến độ tuần này
            </div>
          </div>
          <BarChart3 className="size-5 text-[var(--v2-teal)]" />
        </div>
        <div className="mt-6 grid grid-cols-[1fr_110px] gap-4">
          <div className="flex h-36 items-end gap-2 rounded-xl bg-[var(--v2-surface)] px-4 pb-4 pt-6">
            {[38, 52, 44, 74, 62, 88, 78, 96].map((height, index) => (
              <div
                key={index}
                className="group relative flex-1 rounded-t-md bg-gradient-to-t from-[var(--v2-teal)] to-[#83d8c3]"
                style={{ height: `${height}%` }}
              >
                <span className="absolute -top-5 left-1/2 hidden -translate-x-1/2 text-[0.55rem] text-[var(--v2-muted)] group-hover:block">
                  {height}
                </span>
              </div>
            ))}
          </div>
          <div className="space-y-2">
            <div className="rounded-xl border border-[var(--v2-line)] p-3">
              <span className="text-[0.6rem] text-[var(--v2-muted)]">
                Hoàn thành
              </span>
              <div className="mt-1 text-2xl font-semibold text-[var(--v2-ink)]">
                84%
              </div>
              <span className="text-[0.6rem] font-semibold text-emerald-600">
                +12% tuần này
              </span>
            </div>
            <div className="rounded-xl border border-[var(--v2-line)] p-3">
              <span className="text-[0.6rem] text-[var(--v2-muted)]">
                Đúng hạn
              </span>
              <div className="mt-1 text-2xl font-semibold text-[var(--v2-ink)]">
                91%
              </div>
              <span className="text-[0.6rem] font-semibold text-[var(--v2-cobalt)]">
                Ổn định
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

const stories = [
  {
    eyebrow: "01 · Nền tảng làm việc",
    title: "Mọi ngữ cảnh của công việc, ở đúng nơi cần nó.",
    description:
      "Từ brief đầu tiên đến BoardItem cuối cùng, Notrelix giúp tài liệu và hành động luôn đi cùng nhau.",
    benefits: [
      "Tài liệu dạng block linh hoạt",
      "BoardField phù hợp mọi quy trình",
      "Liên kết giữa docs và work item",
    ],
    visual: WorkspaceVisual,
  },
  {
    eyebrow: "02 · Quy trình tự động",
    title: "Giảm việc lặp lại. Tăng thời gian cho quyết định quan trọng.",
    description:
      "Thiết lập những quy tắc nhỏ để cập nhật status, thông báo và deadline tự chạy theo cách đội ngũ vận hành.",
    benefits: [
      "Kích hoạt từ thay đổi trên Board",
      "Luồng hành động dễ đọc, dễ kiểm soát",
      "Thông báo đúng người, đúng lúc",
    ],
    visual: AutomationVisual,
  },
  {
    eyebrow: "03 · Tín hiệu cho lãnh đạo",
    title: "Thấy tiến độ thật, trước khi nó trở thành vấn đề.",
    description:
      "Các view và báo cáo biến dữ liệu công việc thành tín hiệu rõ ràng để team chủ động điều chỉnh.",
    benefits: [
      "View theo vai trò và mục tiêu",
      "Theo dõi owner, deadline, rủi ro",
      "Quyết định dựa trên dữ liệu mới nhất",
    ],
    visual: InsightVisual,
  },
] as const;

export function StorySections() {
  return (
    <section id="features" className="v2-section overflow-hidden">
      <div className="v2-container">
        <div className="mb-16 max-w-xl">
          <Reveal>
            <span className="v2-eyebrow">
              Một hệ thống, nhiều cách làm việc
            </span>
          </Reveal>
          <Reveal delay={70}>
            <h2 className="mt-4 text-3xl font-semibold tracking-[-0.045em] text-[var(--v2-ink)] sm:text-4xl lg:text-[3.45rem] lg:leading-[1.03]">
              Được thiết kế để công việc{" "}
              <span className="v2-gradient-text">chảy tự nhiên.</span>
            </h2>
          </Reveal>
        </div>

        <div className="space-y-24 sm:space-y-32">
          {stories.map((story, index) => {
            const Visual = story.visual;
            const reversed = index % 2 === 1;
            return (
              <div
                key={story.eyebrow}
                className={`grid items-center gap-10 lg:grid-cols-2 lg:gap-20 ${reversed ? "lg:[&>div:first-child]:order-2" : ""}`}
              >
                <Reveal>
                  <div>
                    <span className="v2-eyebrow">{story.eyebrow}</span>
                    <h3 className="mt-4 max-w-xl text-3xl font-semibold tracking-[-0.045em] text-[var(--v2-ink)] sm:text-4xl sm:leading-[1.08]">
                      {story.title}
                    </h3>
                    <p className="mt-5 max-w-lg text-base leading-7 text-[var(--v2-muted)]">
                      {story.description}
                    </p>
                    <ul className="mt-7 space-y-3">
                      {story.benefits.map((benefit) => (
                        <li
                          key={benefit}
                          className="flex items-center gap-2.5 text-sm font-medium text-[var(--v2-ink)]"
                        >
                          <span className="flex size-5 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
                            <Check className="size-3" />
                          </span>
                          {benefit}
                        </li>
                      ))}
                    </ul>
                    <a
                      href="#showcase"
                      className="mt-8 inline-flex items-center gap-2 text-sm font-semibold text-[var(--v2-cobalt)] hover:gap-3"
                    >
                      Xem cách hoạt động <ArrowUpRight className="size-4" />
                    </a>
                  </div>
                </Reveal>
                <Reveal delay={100} className="min-w-0">
                  <Visual />
                </Reveal>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
