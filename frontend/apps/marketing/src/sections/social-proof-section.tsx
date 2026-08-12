import { CheckCircle2 } from "lucide-react";
import { getTranslations } from "next-intl/server";

import { Reveal } from "../components/reveal";

const teams = [
  "Northstar",
  "Morrow",
  "Cedar Labs",
  "Orbit",
  "Kite Studio",
  "Aster",
];

export async function SocialProofSection() {
  const t = await getTranslations("socialProof");

  return (
    <section className="border-y border-[var(--line)] bg-[var(--mkt-surface)]/55 py-9">
      <div className="container">
        <Reveal className="flex flex-col items-center gap-7 lg:flex-row lg:justify-between">
          <p className="text-center text-sm font-medium text-[var(--muted-text)] lg:text-left">
            {t("line")}
          </p>
          <div className="grid w-full grid-cols-2 items-center gap-x-7 gap-y-4 sm:grid-cols-3 lg:w-auto lg:grid-cols-6">
            {teams.map((team) => (
              <span
                key={team}
                className="text-center text-sm font-semibold tracking-[-0.03em] text-[var(--ink)]/45 transition-colors hover:text-[var(--ink)]/75"
              >
                {team}
              </span>
            ))}
          </div>
        </Reveal>
      </div>
    </section>
  );
}

export async function TrustStrip() {
  const t = await getTranslations("socialProof");
  const trust = t.raw("trust") as string[];

  return (
    <div className="mt-8 flex flex-wrap items-center justify-center gap-4 text-xs text-[var(--muted-text)]">
      {trust.map((item) => (
        <span key={item} className="inline-flex items-center gap-1.5">
          <CheckCircle2 className="size-3.5 text-[var(--mkt-brand-blue-400)]" />{" "}
          {item}
        </span>
      ))}
    </div>
  );
}
