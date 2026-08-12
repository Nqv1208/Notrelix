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
import { getTranslations } from "next-intl/server";
import type { Messages } from "../messages/en";

import { Badge } from "@notrelix/ui-web/components/ui/badge";
import { env } from "../config/env";
import { MarketingButton } from "../components/marketing-button";
import { Reveal } from "../components/reveal";

type HeroT = Awaited<ReturnType<typeof getTranslations<"hero">>>;

const statusStyles = {
  amber: "bg-[var(--mkt-brand-200)] text-[var(--mkt-brand-700)]",
  violet: "bg-[var(--mkt-brand-100)] text-[var(--mkt-brand-700)]",
  emerald: "bg-[var(--mkt-surface-brand-strong)] text-[var(--mkt-brand-600)]",
  slate: "bg-[var(--mkt-bg-soft)] text-[var(--mkt-text-muted)]",
} as const;

const sidebarIcons = [Sparkles, LayoutGrid, FileText];

function ProductWindow({ t }: { t: HeroT }) {
  const window = t.raw("window") as Messages["hero"]["window"];
  return (
    <div className="product-window">
      <div className="flex items-center justify-between border-b border-[var(--line)] px-4 py-3">
        <div className="flex items-center gap-2">
          <span className="flex size-6 items-center justify-center rounded-lg bg-[var(--lilac)] text-[var(--cobalt)]">
            <LayoutGrid className="size-3.5" />
          </span>
          <span className="text-xs font-semibold text-[var(--ink)]">
            {window.workspace}
          </span>
          <span className="rounded-md bg-[var(--surface)] px-2 py-1 text-[0.65rem] text-[var(--muted-text)]">
            {window.boardType}
          </span>
        </div>
        <div className="flex items-center gap-2 text-[var(--muted-text)]">
          <MessageCircle className="size-3.5" />
          <MoreHorizontal className="size-4" />
        </div>
      </div>
      <div className="grid min-h-[330px] grid-cols-[150px_1fr]">
        <aside className="hidden border-r border-[var(--line)] bg-[var(--surface)] p-3 sm:block">
          <div className="mb-4 flex items-center gap-2 px-2">
            <span className="flex size-7 items-center justify-center rounded-lg bg-[var(--ink)] text-[0.65rem] font-bold text-[var(--bg)]">
              A
            </span>
            <span className="text-[0.68rem] font-semibold text-[var(--ink)]">
              {window.teamLabel}
            </span>
          </div>
          <div className="space-y-1 text-[0.68rem]">
            {window.sidebarItems.map((item, index) => {
              const Icon = sidebarIcons[index] ?? FileText;
              return (
                <div
                  key={item.label}
                  className={`flex items-center gap-2 rounded-lg px-2 py-2 ${item.active ? "bg-[var(--mkt-surface)] font-semibold text-[var(--ink)] shadow-sm" : "text-[var(--muted-text)]"}`}
                >
                  <Icon className="size-3.5" />
                  {item.label}
                </div>
              );
            })}
          </div>
          <div className="mt-7 px-2 text-[0.58rem] font-bold uppercase tracking-[0.16em] text-[var(--muted-text)]">
            {window.viewsLabel}
          </div>
          <div className="mt-2 space-y-1 text-[0.68rem] text-[var(--muted-text)]">
            {window.views.map((view) => (
              <div key={view} className="rounded-lg px-2 py-2">
                {view}
              </div>
            ))}
          </div>
        </aside>
        <div className="min-w-0 bg-[var(--mkt-surface-raised)] p-4 sm:p-5">
          <div className="mb-5 flex items-start justify-between gap-3">
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-base font-semibold tracking-[-0.02em] text-[var(--ink)] sm:text-lg">
                  {window.boardTitle}
                </h3>
                <span className="rounded-full bg-[var(--mkt-brand-100)] px-2 py-0.5 text-[0.6rem] font-semibold text-[var(--mkt-brand-700)]">
                  {window.live}
                </span>
              </div>
              <p className="mt-1 text-[0.68rem] text-[var(--muted-text)]">
                {window.meta}
              </p>
            </div>
            <button
              type="button"
              aria-label={window.addItemAria}
              className="flex size-8 items-center justify-center rounded-lg border border-[var(--line)] text-[var(--muted-text)]"
            >
              <Plus className="size-3.5" />
            </button>
          </div>
          <div className="overflow-hidden rounded-xl border border-[var(--line)]">
            <div className="grid grid-cols-[minmax(130px,1.8fr)_0.8fr_0.9fr_0.7fr] gap-2 border-b border-[var(--line)] bg-[var(--surface)] px-3 py-2 text-[0.58rem] font-semibold uppercase tracking-[0.12em] text-[var(--muted-text)]">
              {window.columns.map((column) => (
                <span key={column}>{column}</span>
              ))}
            </div>
            {window.rows.map((row) => (
              <div
                key={row.title}
                className="grid grid-cols-[minmax(130px,1.8fr)_0.8fr_0.9fr_0.7fr] items-center gap-2 border-b border-[var(--line)] px-3 py-3 text-[0.64rem] last:border-b-0 sm:text-[0.68rem]"
              >
                <span className="truncate font-medium text-[var(--ink)]">
                  {row.title}
                </span>
                <span className="flex size-6 items-center justify-center rounded-full bg-[var(--mkt-surface-brand-strong)] text-[0.55rem] font-bold text-[var(--cobalt)]">
                  {row.owner}
                </span>
                <span
                  className={`w-fit rounded-full px-2 py-1 text-[0.55rem] font-semibold ${statusStyles[row.tone as keyof typeof statusStyles]}`}
                >
                  {row.status}
                </span>
                <span className="text-[var(--muted-text)]">{row.due}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

export async function HeroSection() {
  const t = await getTranslations("hero");
  const floatCards = t.raw("floatCards") as Messages["hero"]["floatCards"];
  const checks = t.raw("checks") as string[];

  return (
    <section
      id="hero"
      className="relative overflow-hidden pb-24 pt-16 sm:pb-32 sm:pt-24 lg:pt-28"
    >
      <div className="container">
        <div className="mx-auto max-w-4xl text-center">
          <Reveal>
            <Badge className="beta-badge">
              <span
                className="size-1.5 rounded-full bg-[var(--cobalt)]"
                aria-hidden="true"
              />
              {t("badge")}
            </Badge>
          </Reveal>
          <Reveal delay={70}>
            <h1 className="mt-7 text-5xl font-semibold leading-[0.98] tracking-[-0.065em] text-[var(--ink)] sm:text-6xl lg:text-[5.7rem]">
              {t("title")}
              <span className="gradient-text block">{t("titleHighlight")}</span>
            </h1>
          </Reveal>
          <Reveal delay={140}>
            <p className="mx-auto mt-7 max-w-2xl text-base leading-7 text-[var(--muted-text)] sm:text-lg sm:leading-8">
              {t("subtitle")}
            </p>
          </Reveal>
          <Reveal delay={210}>
            <div className="mt-9 flex flex-col items-center justify-center gap-3 sm:flex-row">
              <MarketingButton
                variant="primary"
                size="lg"
                href={`${env.webAppUrl}/sign-up`}
                className="w-full sm:w-auto"
              >
                {t("ctaPrimary")} <ArrowRight className="size-4" />
              </MarketingButton>
              <MarketingButton
                variant="secondary"
                size="lg"
                href="#showcase"
                className="w-full sm:w-auto"
              >
                {t("ctaSecondary")}
                <span className="flex size-5 items-center justify-center rounded-full bg-[var(--lilac)] text-[var(--cobalt)]">
                  ▶
                </span>
              </MarketingButton>
            </div>
          </Reveal>
          <Reveal delay={280}>
            <div className="mt-7 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 text-xs text-[var(--muted-text)] sm:text-sm">
              {checks.map((item) => (
                <span key={item} className="inline-flex items-center gap-1.5">
                  <Check className="size-3.5 text-[var(--cobalt)]" /> {item}
                </span>
              ))}
            </div>
          </Reveal>
        </div>

        <Reveal
          delay={120}
          className="relative mx-auto mt-16 max-w-6xl sm:mt-20"
        >
          <div className="hero-glow" aria-hidden="true" />
          <ProductWindow t={t} />
          <div className="float-card float-card--left hidden sm:block">
            <span className="flex size-8 items-center justify-center rounded-xl bg-[var(--mkt-brand-100)] text-[var(--mkt-brand-600)]">
              <Check className="size-4" />
            </span>
            <span>
              <strong>{floatCards.tasks.strong}</strong>
              <small>{floatCards.tasks.small}</small>
            </span>
          </div>
          <div className="float-card float-card--right hidden sm:flex">
            <span className="flex size-8 items-center justify-center rounded-xl bg-[var(--surface-brand)] text-[var(--cobalt)]">
              <Sparkles className="size-4" />
            </span>
            <span>
              <strong>{floatCards.progress.strong}</strong>
              <small>{floatCards.progress.small}</small>
            </span>
          </div>
        </Reveal>
      </div>
    </section>
  );
}
