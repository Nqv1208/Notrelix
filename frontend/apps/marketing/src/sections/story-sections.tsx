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
import { getTranslations } from "next-intl/server";
import type { Messages } from "../messages/en";

import { Reveal } from "../components/reveal";

type StoryT = Awaited<ReturnType<typeof getTranslations<"story">>>;

function WorkspaceVisual({ t }: { t: StoryT }) {
  const v = t.raw("visuals") as Messages["story"]["visuals"];
  return (
    <div className="story-visual story-visual--a">
      <div className="mini-window">
        <div className="flex items-center justify-between border-b border-[var(--line)] px-4 py-3">
          <div className="flex items-center gap-2 text-xs font-semibold text-[var(--ink)]">
            <FileText className="size-3.5 text-[var(--cobalt)]" /> Product brief
          </div>
          <span className="text-[0.62rem] text-[var(--muted-text)]">
            {v.saved}
          </span>
        </div>
        <div className="grid grid-cols-[1fr_115px] gap-4 p-5">
          <div>
            <div className="h-3 w-3/4 rounded-full bg-[var(--ink)]/85" />
            <div className="mt-3 h-2 w-full rounded-full bg-[var(--ink)]/10" />
            <div className="mt-2 h-2 w-11/12 rounded-full bg-[var(--ink)]/10" />
            <div className="mt-7 rounded-xl border border-[var(--line)] bg-[var(--mkt-surface-raised)] p-3">
              <div className="flex items-center gap-2 text-[0.62rem] font-semibold text-[var(--ink)]">
                <LayoutGrid className="size-3 text-[var(--cobalt)]" />{" "}
                {v.relatedItems}
              </div>
              <div className="mt-3 space-y-2">
                {v.relatedList.map((item, index) => (
                  <div
                    key={item}
                    className="flex items-center gap-2 text-[0.62rem] text-[var(--muted-text)]"
                  >
                    <span
                      className={`size-2 rounded-full ${index === 1 ? "bg-[var(--mkt-brand-500)]" : "bg-[var(--mkt-brand-300)]"}`}
                    />
                    {item}
                  </div>
                ))}
              </div>
            </div>
          </div>
          <div className="hidden rounded-xl bg-[var(--surface)] p-3 sm:block">
            <span className="text-[0.58rem] font-semibold uppercase tracking-[0.12em] text-[var(--muted-text)]">
              {v.viewers}
            </span>
            <div className="mt-3 flex -space-x-2">
              {["AN", "ML", "QN"].map((initials) => (
                <span
                  key={initials}
                  className="flex size-7 items-center justify-center rounded-full border-2 border-[var(--surface)] bg-[var(--mkt-surface-brand-strong)] text-[0.55rem] font-bold text-[var(--mkt-text-on-brand)]"
                >
                  {initials}
                </span>
              ))}
            </div>
            <div className="mt-8 rounded-lg bg-[var(--mkt-surface-raised)] p-2 text-[0.6rem] text-[var(--muted-text)] shadow-sm">
              <MessageCircle className="mb-1 size-3 text-[var(--cobalt)]" />{" "}
              {v.comments}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function AutomationVisual({ t }: { t: StoryT }) {
  const v = t.raw("visuals") as Messages["story"]["visuals"];
  return (
    <div className="story-visual story-visual--b">
      <div className="mx-auto max-w-sm rounded-2xl border border-[var(--mkt-border)] bg-[var(--mkt-surface-raised)] p-5 shadow-xl shadow-rgb-[var(--mkt-shadow-rgb)]">
        <div className="flex items-center justify-between">
          <div>
            <div className="text-[0.65rem] font-semibold uppercase tracking-[0.15em] text-[var(--muted-text)]">
              {v.automationLabel}
            </div>
            <div className="mt-1 text-lg font-semibold tracking-[-0.03em] text-[var(--ink)]">
              {v.automationTitle}
            </div>
          </div>
          <span className="flex size-9 items-center justify-center rounded-xl bg-[var(--mkt-surface-brand)] text-[var(--mkt-text-on-brand)]">
            <Sparkles className="size-4" />
          </span>
        </div>
        <div className="mt-6 space-y-3">
          {v.automationSteps.map((step, index) => (
            <div key={step.title}>
              {index > 0 ? (
                <div className="ml-4 h-4 border-l border-dashed border-[var(--mkt-brand-400)]/50" />
              ) : null}
              <div className="flow-node">
                <span
                  className={`flex size-7 items-center justify-center rounded-lg ${
                    index === 0
                      ? "bg-[var(--mkt-brand-200)] text-[var(--mkt-brand-700)]"
                      : index === 1
                        ? "bg-[var(--mkt-brand-100)] text-[var(--mkt-brand-600)]"
                        : "bg-[var(--mkt-surface-brand)] text-[var(--mkt-text-on-brand)]"
                  }`}
                >
                  {index === 0 ? (
                    <GitBranch className="size-3.5" />
                  ) : index === 1 ? (
                    <MessageCircle className="size-3.5" />
                  ) : (
                    <CalendarDays className="size-3.5" />
                  )}
                </span>
                <span>
                  <b>{step.title}</b>
                  <small>{step.detail}</small>
                </span>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function InsightVisual({ t }: { t: StoryT }) {
  const v = t.raw("visuals") as Messages["story"]["visuals"];
  return (
    <div className="story-visual story-visual--c">
      <div className="mx-auto max-w-lg rounded-2xl border border-[var(--mkt-border)] bg-[var(--mkt-surface-raised)] p-5 shadow-xl shadow-rgb-[var(--mkt-shadow-rgb)]">
        <div className="flex items-center justify-between">
          <div>
            <div className="text-[0.65rem] font-semibold uppercase tracking-[0.15em] text-[var(--muted-text)]">
              {v.pulseLabel}
            </div>
            <div className="mt-1 text-lg font-semibold tracking-[-0.03em] text-[var(--ink)]">
              {v.pulseTitle}
            </div>
          </div>
          <BarChart3 className="size-5 text-[var(--mkt-brand-500)]" />
        </div>
        <div className="mt-6 grid grid-cols-[1fr_110px] gap-4">
          <div className="flex h-36 items-end gap-2 rounded-xl bg-[var(--surface)] px-4 pb-4 pt-6">
            {[38, 52, 44, 74, 62, 88, 78, 96].map((height, index) => (
              <div
                key={index}
                className="group relative flex-1 rounded-t-md bg-gradient-to-t from-[var(--mkt-brand-400)] to-[var(--mkt-brand-200)]"
                style={{ height: `${height}%` }}
              >
                <span className="absolute -top-5 left-1/2 hidden -translate-x-1/2 text-[0.55rem] text-[var(--muted-text)] group-hover:block">
                  {height}
                </span>
              </div>
            ))}
          </div>
          <div className="space-y-2">
            <div className="rounded-xl border border-[var(--line)] p-3">
              <span className="text-[0.6rem] text-[var(--muted-text)]">
                {v.completion}
              </span>
              <div className="mt-1 text-2xl font-semibold text-[var(--ink)]">
                84%
              </div>
              <span className="text-[0.6rem] font-semibold text-[var(--mkt-brand-600)]">
                {v.completionDelta}
              </span>
            </div>
            <div className="rounded-xl border border-[var(--line)] p-3">
              <span className="text-[0.6rem] text-[var(--muted-text)]">
                {v.onTime}
              </span>
              <div className="mt-1 text-2xl font-semibold text-[var(--ink)]">
                91%
              </div>
              <span className="text-[0.6rem] font-semibold text-[var(--cobalt)]">
                {v.onTimeDelta}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

const storyVisuals = [WorkspaceVisual, AutomationVisual, InsightVisual];

export async function StorySections() {
  const t = await getTranslations("story");
  const stories = t.raw("stories") as Messages["story"]["stories"];

  return (
    <section id="features" className="section overflow-hidden">
      <div className="container">
        <div className="mb-16 max-w-xl">
          <Reveal>
            <span className="eyebrow">{t("eyebrow")}</span>
          </Reveal>
          <Reveal delay={70}>
            <h2 className="mt-4 text-3xl font-semibold tracking-[-0.045em] text-[var(--ink)] sm:text-4xl lg:text-[3.45rem] lg:leading-[1.03]">
              {t("title")}{" "}
              <span className="gradient-text">{t("titleHighlight")}</span>
            </h2>
          </Reveal>
        </div>

        <div className="space-y-24 sm:space-y-32">
          {stories.map((story, index) => {
            const Visual = storyVisuals[index] ?? WorkspaceVisual;
            const reversed = index % 2 === 1;
            return (
              <div
                key={story.eyebrow}
                className={`grid items-center gap-10 lg:grid-cols-2 lg:gap-20 ${reversed ? "lg:[&>div:first-child]:order-2" : ""}`}
              >
                <Reveal>
                  <div>
                    <span className="eyebrow">{story.eyebrow}</span>
                    <h3 className="mt-4 max-w-xl text-3xl font-semibold tracking-[-0.045em] text-[var(--ink)] sm:text-4xl sm:leading-[1.08]">
                      {story.title}
                    </h3>
                    <p className="mt-5 max-w-lg text-base leading-7 text-[var(--muted-text)]">
                      {story.description}
                    </p>
                    <ul className="mt-7 space-y-3">
                      {story.benefits.map((benefit) => (
                        <li
                          key={benefit}
                          className="flex items-center gap-2.5 text-sm font-medium text-[var(--ink)]"
                        >
                          <span className="flex size-5 items-center justify-center rounded-full bg-[var(--mkt-brand-100)] text-[var(--mkt-brand-600)]">
                            <Check className="size-3" />
                          </span>
                          {benefit}
                        </li>
                      ))}
                    </ul>
                    <a
                      href="#showcase"
                      className="mt-8 inline-flex items-center gap-2 text-sm font-semibold text-[var(--cobalt)] hover:gap-3"
                    >
                      {t("link")} <ArrowUpRight className="size-4" />
                    </a>
                  </div>
                </Reveal>
                <Reveal delay={100} className="min-w-0">
                  <Visual t={t} />
                </Reveal>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
