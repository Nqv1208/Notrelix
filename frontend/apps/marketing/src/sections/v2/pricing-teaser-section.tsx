"use client";

import * as React from "react";
import { Check, Sparkles } from "lucide-react";

import { Reveal } from "../../components/v2/reveal";
import { SectionHeading } from "../../components/v2/section-heading";
import { env } from "../../config/env";

type BillingCycle = "monthly" | "yearly";

const plans = [
  {
    name: "Starter",
    description: "Cho cá nhân và đội ngũ nhỏ bắt đầu có nhịp làm việc chung.",
    monthly: "0",
    yearly: "0",
    suffix: "miễn phí",
    features: [
      "Tài liệu và Board không giới hạn",
      "Tối đa 5 thành viên",
      "View cơ bản",
    ],
    tone: "neutral",
  },
  {
    name: "Growth",
    description: "Cho đội ngũ đang tăng tốc và cần quy trình rõ hơn.",
    monthly: "12",
    yearly: "9.6",
    suffix: "người dùng / tháng",
    features: [
      "Mọi tính năng Starter",
      "Automation và báo cáo",
      "Phân quyền nâng cao",
      "Hỗ trợ ưu tiên",
    ],
    tone: "featured",
  },
  {
    name: "Enterprise",
    description:
      "Cho tổ chức cần kiểm soát, bảo mật và linh hoạt ở quy mô lớn.",
    monthly: "Liên hệ",
    yearly: "Liên hệ",
    suffix: "theo nhu cầu",
    features: [
      "SSO và audit log",
      "Workspace không giới hạn",
      "Tích hợp tùy chỉnh",
      "SLA và hỗ trợ riêng",
    ],
    tone: "dark",
  },
] as const;

export function PricingTeaserSection() {
  const [cycle, setCycle] = React.useState<BillingCycle>("monthly");

  return (
    <section id="pricing" className="v2-section bg-[var(--v2-surface)]">
      <div className="v2-container">
        <Reveal>
          <SectionHeading
            align="center"
            eyebrow="Bảng giá đơn giản"
            title={
              <>
                Bắt đầu nhỏ.{" "}
                <span className="v2-gradient-text">Mở rộng tự nhiên.</span>
              </>
            }
            description="Dùng thử miễn phí, nâng cấp khi đội ngũ cần thêm sức mạnh. Không có phí ẩn."
          />
        </Reveal>
        <Reveal delay={80} className="mt-8 flex justify-center">
          <div
            className="v2-billing-toggle"
            role="group"
            aria-label="Chu kỳ thanh toán"
          >
            <button
              type="button"
              aria-pressed={cycle === "monthly"}
              onClick={() => setCycle("monthly")}
              className={cycle === "monthly" ? "is-active" : ""}
            >
              Theo tháng
            </button>
            <button
              type="button"
              aria-pressed={cycle === "yearly"}
              onClick={() => setCycle("yearly")}
              className={cycle === "yearly" ? "is-active" : ""}
            >
              Theo năm <span>Tiết kiệm 20%</span>
            </button>
          </div>
        </Reveal>
        <div className="mt-12 grid gap-4 lg:grid-cols-3">
          {plans.map((plan, index) => (
            <Reveal key={plan.name} delay={index * 70}>
              <article
                className={`v2-price-card ${plan.tone === "featured" ? "is-featured" : ""} ${plan.tone === "dark" ? "is-dark" : ""}`}
              >
                {plan.tone === "featured" ? (
                  <div className="v2-popular-label">
                    <Sparkles className="size-3" /> Được chọn nhiều nhất
                  </div>
                ) : null}
                <h3 className="text-lg font-semibold tracking-[-0.03em]">
                  {plan.name}
                </h3>
                <p className="mt-3 min-h-12 text-sm leading-6 opacity-70">
                  {plan.description}
                </p>
                <div className="mt-7 flex items-baseline gap-1">
                  <span className="text-4xl font-semibold tracking-[-0.06em]">
                    {plan[cycle]}
                  </span>
                  {plan[cycle] !== "0" && plan[cycle] !== "Liên hệ" ? (
                    <span className="text-xs opacity-65">
                      $ / {plan.suffix}
                    </span>
                  ) : (
                    <span className="text-xs opacity-65">{plan.suffix}</span>
                  )}
                </div>
                <a
                  href={
                    plan.name === "Enterprise"
                      ? "/contact"
                      : `${env.webAppUrl}/sign-up`
                  }
                  className={`mt-7 flex h-11 items-center justify-center rounded-xl px-4 text-sm font-semibold transition-transform hover:-translate-y-0.5 ${plan.tone === "featured" ? "bg-[var(--v2-ink)] text-white" : plan.tone === "dark" ? "bg-white text-[var(--v2-ink)]" : "border border-[var(--v2-line-strong)] text-[var(--v2-ink)] hover:bg-[var(--v2-surface)]"}`}
                >
                  {plan.name === "Enterprise"
                    ? "Trao đổi với sales"
                    : "Bắt đầu miễn phí"}
                </a>
                <ul className="mt-8 space-y-3 border-t border-current/10 pt-7">
                  {plan.features.map((feature) => (
                    <li
                      key={feature}
                      className="flex gap-2.5 text-sm opacity-80"
                    >
                      <Check className="mt-0.5 size-4 shrink-0 text-[var(--v2-teal)]" />
                      {feature}
                    </li>
                  ))}
                </ul>
              </article>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
}
