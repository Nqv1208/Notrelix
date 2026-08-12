"use client";

import * as React from "react";
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
} from "lucide-react";
import { useTranslations } from "next-intl";
import type { Messages } from "../messages/en";

import { Reveal } from "../components/reveal";
import { SectionHeading } from "../components/section-heading";

type ShowcaseTab = "plan" | "progress" | "automation" | "reporting";

const tabIcons: Record<ShowcaseTab, typeof LayoutGrid> = {
  plan: CalendarDays,
  progress: CircleDot,
  automation: GitBranch,
  reporting: BarChart3,
};

function PlanPanel({ data }: { data: Messages["showcase"]["calendar"] }) {
  return (
    <div className="grid gap-5 lg:grid-cols-[1.15fr_0.85fr]">
      <div className="rounded-2xl border border-[var(--line)] bg-[var(--mkt-surface-raised)] p-5">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold text-[var(--ink)]">
              {data.title}
            </p>
            <p className="mt-1 text-[0.65rem] text-[var(--muted-text)]">
              {data.subtitle}
            </p>
          </div>
          <button
            type="button"
            aria-label={data.addAria}
            className="flex size-8 items-center justify-center rounded-lg bg-[var(--lilac)] text-[var(--cobalt)]"
          >
            <CalendarDays className="size-3.5" />
          </button>
        </div>
        <div className="mt-6 grid grid-cols-7 gap-1 text-center text-[0.55rem] text-[var(--muted-text)]">
          {data.days.map((day) => (
            <span key={day}>{day}</span>
          ))}
        </div>
        <div className="mt-2 grid grid-cols-7 gap-1.5">
          {Array.from({ length: 28 }, (_, index) => (
            <span
              key={index}
              className={`flex aspect-square items-center justify-center rounded-md text-[0.6rem] ${index === 14 ? "bg-[var(--cobalt)] font-semibold text-[var(--mkt-text-on-brand)]" : index > 9 && index < 14 ? "bg-[var(--lilac)] text-[var(--cobalt)]" : "text-[var(--muted-text)] hover:bg-[var(--surface)]"}`}
            >
              {index + 1}
            </span>
          ))}
        </div>
      </div>
      <div className="rounded-2xl border border-[var(--line)] bg-[var(--mkt-surface-raised)] p-5">
        <div className="flex items-center gap-2 text-xs font-semibold text-[var(--ink)]">
          <ListChecks className="size-4 text-[var(--mkt-brand-blue-400)]" />{" "}
          {data.milestones.title}
        </div>
        <div className="mt-5 space-y-3">
          {data.milestones.items.map((item, index) => (
            <div
              key={item.label}
              className="flex items-start gap-3 rounded-xl bg-[var(--surface)] p-3"
            >
              <span
                className={`mt-0.5 flex size-5 items-center justify-center rounded-full ${index === 2 ? "bg-[var(--mkt-brand-blue-100)] text-[var(--mkt-brand-blue-600)]" : "bg-[var(--mkt-surface)] text-[var(--cobalt)]"}`}
              >
                {index === 2 ? (
                  <Check className="size-3" />
                ) : (
                  <span className="text-[0.6rem] font-bold">{index + 1}</span>
                )}
              </span>
              <span className="text-xs font-medium text-[var(--ink)]">
                {item.label}
                <small className="mt-1 block text-[0.62rem] font-normal text-[var(--muted-text)]">
                  {item.meta}
                </small>
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function ProgressPanel({
  data,
  people,
}: {
  data: Messages["showcase"]["progress"]["columns"];
  people: (count: number) => string;
}) {
  return (
    <div className="grid gap-4 lg:grid-cols-3">
      {data.map((column) => (
        <div
          key={column.title}
          className="rounded-2xl border border-[var(--line)] bg-[var(--mkt-surface-raised)] p-4"
        >
          <div className="flex items-center gap-2 text-xs font-semibold text-[var(--ink)]">
            <span className={`size-2 rounded-full ${column.tone}`} />
            {column.title}
            <span className="ml-auto text-[0.65rem] text-[var(--muted-text)]">
              {column.items.length}
            </span>
          </div>
          <div className="mt-4 space-y-2.5">
            {column.items.map((item, index) => (
              <div
                key={item}
                className="rounded-xl border border-[var(--line)] p-3 shadow-sm"
              >
                <div className="flex items-start justify-between gap-2">
                  <span className="text-xs font-medium leading-5 text-[var(--ink)]">
                    {item}
                  </span>
                  <span
                    className={`size-5 shrink-0 rounded-full ${index === 0 ? "bg-[var(--mkt-surface-brand-strong)]" : "bg-[var(--mkt-surface-brand)]"}`}
                  />
                </div>
                <div className="mt-3 flex items-center justify-between text-[0.6rem] text-[var(--muted-text)]">
                  <span>{people(index + 2)}</span>
                  <Timer className="size-3" />
                </div>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

function AutomationPanel({
  data,
}: {
  data: Messages["showcase"]["automation"];
}) {
  return (
    <div className="mx-auto max-w-3xl rounded-2xl border border-[var(--line)] bg-[var(--mkt-surface-raised)] p-5 sm:p-7">
      <div className="flex items-center justify-between border-b border-[var(--line)] pb-5">
        <div>
          <p className="text-sm font-semibold text-[var(--ink)]">
            {data.title}
          </p>
          <p className="mt-1 text-xs text-[var(--muted-text)]">
            {data.subtitle}
          </p>
        </div>
        <span className="inline-flex items-center gap-1.5 rounded-full bg-[var(--mkt-brand-blue-100)] px-2.5 py-1 text-[0.65rem] font-semibold text-[var(--mkt-brand-blue-700)]">
          <span className="size-1.5 rounded-full bg-[var(--mkt-brand-blue-500)]" />{" "}
          {data.enabled}
        </span>
      </div>
      <div className="mt-7 grid gap-3 sm:grid-cols-[1fr_auto_1fr_auto_1fr] sm:items-center">
        <div className="rounded-xl border border-[var(--line)] bg-[var(--surface)] p-4">
          <GitBranch className="size-4 text-[var(--cobalt)]" />
          <p className="mt-3 text-xs font-semibold text-[var(--ink)]">
            {data.trigger}
          </p>
          <p className="mt-1 text-[0.65rem] text-[var(--muted-text)]">
            {data.triggerDetail}
          </p>
        </div>
        <ArrowRight className="hidden size-4 text-[var(--muted-text)] sm:block" />
        <div className="rounded-xl border border-[var(--line)] bg-[var(--surface)] p-4">
          <Sparkles className="size-4 text-[var(--mkt-brand-blue-500)]" />
          <p className="mt-3 text-xs font-semibold text-[var(--ink)]">
            {data.condition}
          </p>
          <p className="mt-1 text-[0.65rem] text-[var(--muted-text)]">
            {data.conditionDetail}
          </p>
        </div>
        <ArrowRight className="hidden size-4 text-[var(--muted-text)] sm:block" />
        <div className="rounded-xl border border-[var(--line)] bg-[var(--surface)] p-4">
          <Check className="size-4 text-[var(--mkt-brand-blue-400)]" />
          <p className="mt-3 text-xs font-semibold text-[var(--ink)]">
            {data.action}
          </p>
          <p className="mt-1 text-[0.65rem] text-[var(--muted-text)]">
            {data.actionDetail}
          </p>
        </div>
      </div>
    </div>
  );
}

function ReportingPanel({
  data,
  goal,
}: {
  data: Messages["showcase"]["reporting"];
  goal: (percent: number) => string;
}) {
  return (
    <div className="grid gap-4 sm:grid-cols-3">
      <div className="rounded-2xl border border-[var(--line)] bg-[var(--mkt-surface-raised)] p-5 sm:col-span-2">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold text-[var(--ink)]">
              {data.title}
            </p>
            <p className="mt-1 text-[0.65rem] text-[var(--muted-text)]">
              {data.subtitle}
            </p>
          </div>
          <BarChart3 className="size-4 text-[var(--mkt-brand-blue-400)]" />
        </div>
        <div className="mt-8 flex h-32 items-end gap-2">
          {[32, 46, 39, 58, 52, 76, 68, 92].map((height, index) => (
            <div
              key={index}
              className="relative flex-1 rounded-t-lg bg-gradient-to-t from-[var(--cobalt)] to-[var(--mkt-brand-blue-300)]"
              style={{ height: `${height}%` }}
            >
              <span className="absolute -top-5 left-1/2 -translate-x-1/2 text-[0.55rem] text-[var(--muted-text)]">
                {height}
              </span>
            </div>
          ))}
        </div>
      </div>
      <div className="rounded-2xl border border-[var(--line)] bg-[var(--mkt-surface-raised)] p-5">
        <p className="text-xs font-semibold text-[var(--ink)]">
          {data.signalTitle}
        </p>
        <div className="mt-6 text-4xl font-semibold tracking-[-0.06em] text-[var(--ink)]">
          +28%
        </div>
        <p className="mt-1 text-xs text-[var(--mkt-brand-blue-600)]">
          {data.signalDelta}
        </p>
        <div className="mt-7 h-2 overflow-hidden rounded-full bg-[var(--surface)]">
          <div className="h-full w-[78%] rounded-full bg-[var(--mkt-brand-blue-500)]" />
        </div>
        <p className="mt-2 text-[0.62rem] text-[var(--muted-text)]">
          {goal(78)}
        </p>
      </div>
    </div>
  );
}

export function ShowcaseSection() {
  const t = useTranslations("showcase");
  const tabs = t.raw("tabs") as Messages["showcase"]["tabs"];

  const [activeTab, setActiveTab] = React.useState<ShowcaseTab>("plan");
  const tabRefs = React.useRef<Array<HTMLButtonElement | null>>([]);

  const moveTab = (direction: 1 | -1) => {
    const index = tabs.findIndex((tab) => tab.id === activeTab);
    const nextIndex = (index + direction + tabs.length) % tabs.length;
    const nextTab = tabs[nextIndex];
    if (!nextTab) return;
    setActiveTab(nextTab.id as ShowcaseTab);
    tabRefs.current[nextIndex]?.focus();
  };

  const calendar = t.raw("calendar") as Messages["showcase"]["calendar"];
  const progress = t.raw("progress") as Messages["showcase"]["progress"];
  const automation = t.raw("automation") as Messages["showcase"]["automation"];
  const reporting = t.raw("reporting") as Messages["showcase"]["reporting"];

  const panels: Record<ShowcaseTab, React.ReactNode> = {
    plan: <PlanPanel data={calendar} />,
    progress: (
      <ProgressPanel
        data={progress.columns}
        people={(count) => t("progress.people", { count })}
      />
    ),
    automation: <AutomationPanel data={automation} />,
    reporting: (
      <ReportingPanel
        data={reporting}
        goal={(percent) => t("reporting.goal", { percent })}
      />
    ),
  };

  return (
    <section id="showcase" className="section bg-[var(--surface)]">
      <div className="container">
        <Reveal>
          <SectionHeading
            align="center"
            eyebrow={t("eyebrow")}
            title={
              <>
                {t("title")}{" "}
                <span className="gradient-text">{t("titleHighlight")}</span>
              </>
            }
            description={t("description")}
          />
        </Reveal>

        <Reveal delay={100} className="mt-12">
          <div role="tablist" aria-label={t("tabsLabel")} className="tabs-wrap">
            {tabs.map((tab, index) => {
              const Icon = tabIcons[tab.id as ShowcaseTab];
              const selected = activeTab === tab.id;
              return (
                <button
                  key={tab.id}
                  ref={(element) => {
                    tabRefs.current[index] = element;
                  }}
                  type="button"
                  role="tab"
                  id={`tab-${tab.id}`}
                  aria-selected={selected}
                  aria-controls={`panel-${tab.id}`}
                  tabIndex={selected ? 0 : -1}
                  onClick={() => setActiveTab(tab.id as ShowcaseTab)}
                  onKeyDown={(event) => {
                    if (event.key === "ArrowRight") moveTab(1);
                    if (event.key === "ArrowLeft") moveTab(-1);
                  }}
                  className={`tab ${selected ? "is-active" : ""}`}
                >
                  <Icon className="size-4" />
                  {tab.label}
                </button>
              );
            })}
          </div>
        </Reveal>

        <div
          className="showcase-shell"
          role="tabpanel"
          id={`panel-${activeTab}`}
          aria-labelledby={`tab-${activeTab}`}
        >
          <div className="flex items-center justify-between border-b border-[var(--line)] px-4 py-3 sm:px-6">
            <div className="flex items-center gap-1.5">
              <span className="size-2 rounded-full bg-[var(--mkt-border-strong)]" />
              <span className="size-2 rounded-full bg-[var(--mkt-border-strong)]" />
              <span className="size-2 rounded-full bg-[var(--mkt-border-strong)]" />
            </div>
            <span className="text-[0.65rem] font-medium text-[var(--muted-text)]">
              workspace.notrelix.com
            </span>
            <div className="hidden items-center gap-2 text-[var(--muted-text)] sm:flex">
              <Play className="size-3.5" />
              <span className="text-[0.62rem]">{t("live")}</span>
            </div>
          </div>
          <div className="min-h-[360px] p-4 sm:p-8">{panels[activeTab]}</div>
        </div>
      </div>
    </section>
  );
}
