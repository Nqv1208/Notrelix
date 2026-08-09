import { ArrowRight, Check } from "lucide-react";
import Logo from "@notrelix/ui-web/assets/logo.svg";

import { Reveal } from "../../components/v2/reveal";
import { env } from "../../config/env";

export function FinalCtaSection() {
  return (
    <section id="final-cta" className="v2-section pt-8 sm:pt-12">
      <div className="v2-container">
        <Reveal>
          <div className="v2-final-cta">
            <div className="v2-final-cta-grid" aria-hidden="true" />
            <div className="relative z-10 mx-auto max-w-2xl text-center">
              <span className="mx-auto flex size-12 items-center justify-center rounded-2xl bg-white/12">
                <Logo className="size-6 brightness-0 invert" />
              </span>
              <h2 className="mt-6 text-3xl font-semibold tracking-[-0.05em] text-white sm:text-5xl sm:leading-[1.05]">
                Cho đội ngũ một nơi để cùng tiến về phía trước.
              </h2>
              <p className="mx-auto mt-5 max-w-xl text-base leading-7 text-white/70">
                Tạo workspace đầu tiên trong vài phút và để Notrelix kết nối
                phần còn lại.
              </p>
              <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
                <a
                  href={`${env.webAppUrl}/sign-up`}
                  className="inline-flex h-12 w-full items-center justify-center gap-2 rounded-xl bg-white px-6 text-sm font-semibold text-[var(--v2-ink)] shadow-lg transition-transform hover:-translate-y-0.5 sm:w-auto"
                >
                  Bắt đầu miễn phí <ArrowRight className="size-4" />
                </a>
                <a
                  href="/contact"
                  className="inline-flex h-12 w-full items-center justify-center gap-2 rounded-xl border border-white/20 px-6 text-sm font-semibold text-white transition-colors hover:bg-white/10 sm:w-auto"
                >
                  Đặt lịch demo
                </a>
              </div>
              <div className="mt-7 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 text-xs text-white/60">
                <span className="inline-flex items-center gap-1.5">
                  <Check className="size-3.5 text-[#a7efc9]" /> Không cần thẻ
                  tín dụng
                </span>
                <span className="inline-flex items-center gap-1.5">
                  <Check className="size-3.5 text-[#a7efc9]" /> Hủy bất cứ lúc
                  nào
                </span>
              </div>
            </div>
          </div>
        </Reveal>
      </div>
    </section>
  );
}
