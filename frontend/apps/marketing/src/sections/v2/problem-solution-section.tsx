import { ArrowRight, CircleAlert, Layers3, Route } from "lucide-react";

import { Reveal } from "../../components/v2/reveal";
import { SectionHeading } from "../../components/v2/section-heading";

const shifts = [
  {
    icon: CircleAlert,
    before: "Tài liệu nằm rải rác",
    after: "Một nguồn sự thật duy nhất",
    text: "Kết nối brief, quyết định và công việc vào cùng ngữ cảnh thay vì săn tìm qua nhiều tab.",
    color: "coral",
  },
  {
    icon: Layers3,
    before: "Công việc bị đứt đoạn",
    after: "Board nhìn thấy toàn cảnh",
    text: "BoardField linh hoạt phản ánh đúng cách đội ngũ vận hành, từ status đến owner và deadline.",
    color: "violet",
  },
  {
    icon: Route,
    before: "Theo dõi bằng cảm giác",
    after: "Tiến độ có tín hiệu rõ ràng",
    text: "Automation và báo cáo giúp mọi người biết việc tiếp theo, rủi ro và điểm cần quyết định.",
    color: "teal",
  },
] as const;

export function ProblemSolutionSection() {
  return (
    <section className="v2-section bg-[var(--v2-surface)]">
      <div className="v2-container">
        <Reveal>
          <SectionHeading
            eyebrow="Từ rối thành rõ"
            title={
              <>
                Đội ngũ không cần thêm công cụ. Họ cần{" "}
                <span className="v2-gradient-text">một nhịp làm việc.</span>
              </>
            }
            description="Notrelix đưa ngữ cảnh, trách nhiệm và hành động về cùng một nơi để công việc không bị rơi giữa các phòng ban."
          />
        </Reveal>

        <div className="mt-14 grid gap-4 lg:grid-cols-3">
          {shifts.map((shift, index) => {
            const Icon = shift.icon;
            return (
              <Reveal key={shift.before} delay={index * 80}>
                <article className="v2-shift-card h-full">
                  <div className={`v2-icon-box v2-icon-box--${shift.color}`}>
                    <Icon className="size-5" />
                  </div>
                  <div className="mt-7 flex items-center gap-2 text-sm text-[var(--v2-muted)]">
                    <span className="line-through decoration-[var(--v2-coral)]/60">
                      {shift.before}
                    </span>
                    <ArrowRight className="size-3.5" />
                  </div>
                  <h3 className="mt-2 text-xl font-semibold tracking-[-0.035em] text-[var(--v2-ink)]">
                    {shift.after}
                  </h3>
                  <p className="mt-3 text-sm leading-6 text-[var(--v2-muted)]">
                    {shift.text}
                  </p>
                </article>
              </Reveal>
            );
          })}
        </div>
      </div>
    </section>
  );
}
