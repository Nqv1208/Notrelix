import {
  ArrowUpRight,
  Megaphone,
  PackageOpen,
  Settings2,
  Target,
  UsersRound,
} from "lucide-react";

import { Reveal } from "../../components/v2/reveal";
import { SectionHeading } from "../../components/v2/section-heading";

const useCases = [
  {
    icon: Megaphone,
    title: "Marketing",
    text: "Giữ campaign brief, timeline và feedback trong một nhịp triển khai.",
    benefits: ["Campaign rõ owner", "Content calendar chung"],
    tone: "coral",
  },
  {
    icon: PackageOpen,
    title: "Product",
    text: "Nối insight, roadmap và quyết định sản phẩm với công việc hằng ngày.",
    benefits: ["Roadmap có ngữ cảnh", "Ưu tiên minh bạch"],
    tone: "violet",
  },
  {
    icon: Settings2,
    title: "Operations",
    text: "Chuẩn hóa playbook và quy trình để mọi đội nhóm vận hành nhất quán.",
    benefits: ["SOP dễ tìm", "Workflow lặp lại"],
    tone: "teal",
  },
  {
    icon: Target,
    title: "Sales",
    text: "Theo dõi deal, handoff và tài liệu khách hàng mà không bỏ sót bước nào.",
    benefits: ["Handoff không rơi việc", "Tín hiệu theo pipeline"],
    tone: "amber",
  },
  {
    icon: UsersRound,
    title: "Leadership",
    text: "Có một bức tranh tiến độ đủ sâu để ra quyết định mà không micromanage.",
    benefits: ["Dashboard theo mục tiêu", "Rủi ro thấy sớm"],
    tone: "ink",
  },
] as const;

export function UseCasesSection() {
  return (
    <section id="use-cases" className="v2-section">
      <div className="v2-container">
        <Reveal>
          <SectionHeading
            eyebrow="Cho mọi đội ngũ"
            title={
              <>
                Cùng một nền tảng.{" "}
                <span className="v2-gradient-text">Mỗi đội một cách dùng.</span>
              </>
            }
            description="Notrelix thích nghi với nhịp làm việc của từng team mà vẫn giữ mọi người trong cùng một hệ thống."
          />
        </Reveal>
        <div className="mt-14 grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
          {useCases.map((item, index) => {
            const Icon = item.icon;
            return (
              <Reveal key={item.title} delay={index * 60} className="h-full">
                <article className="v2-use-case-card h-full">
                  <div className={`v2-icon-box v2-icon-box--${item.tone}`}>
                    <Icon className="size-5" />
                  </div>
                  <h3 className="mt-7 text-lg font-semibold tracking-[-0.035em] text-[var(--v2-ink)]">
                    {item.title}
                  </h3>
                  <p className="mt-3 text-sm leading-6 text-[var(--v2-muted)]">
                    {item.text}
                  </p>
                  <ul className="mt-5 space-y-2">
                    {item.benefits.map((benefit) => (
                      <li
                        key={benefit}
                        className="text-xs font-medium text-[var(--v2-ink)]"
                      >
                        {benefit}
                      </li>
                    ))}
                  </ul>
                  <a
                    href="#pricing"
                    className="mt-7 inline-flex items-center gap-1.5 text-xs font-semibold text-[var(--v2-cobalt)] transition-[gap] hover:gap-2.5"
                  >
                    Khám phá <ArrowUpRight className="size-3.5" />
                  </a>
                </article>
              </Reveal>
            );
          })}
        </div>
      </div>
    </section>
  );
}
