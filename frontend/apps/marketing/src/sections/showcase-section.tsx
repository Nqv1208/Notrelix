import { getTranslations } from "next-intl/server";
import type { Messages } from "../messages/en";

import { ProductShotTabs, type ShotBox } from "../components/product-shot-tabs";
import { Reveal } from "../components/reveal";
import { SectionHeading } from "../components/section-heading";
import annotations from "./showcase-annotations.json";

type Boxes = Record<string, Record<string, ShotBox>>;

/**
 * How work gets done: four real screens of the web app, captured by
 * `pnpm marketing:previews`, with notes pinned to the regions they describe.
 */
export async function ShowcaseSection() {
  const t = await getTranslations("showcase");
  const copy = t.raw("tabs") as Messages["showcase"]["tabs"];
  const boxes = annotations as Boxes;

  const tabs = copy.map((tab) => ({
    id: tab.id,
    label: tab.label,
    alt: tab.alt,
    notes: Object.entries(tab.notes).flatMap(([key, label]) => {
      const box = boxes[tab.id]?.[key];
      return box ? [{ key, label, box }] : [];
    }),
  }));

  return (
    <section id="showcase" className="section bg-[var(--surface)]">
      <div className="container">
        <Reveal>
          <SectionHeading title={t("title")} description={t("description")} />
        </Reveal>
        <Reveal delay={100} className="mt-10 sm:mt-12">
          <ProductShotTabs
            tabs={tabs}
            idPrefix="showcase"
            label={t("tabsLabel")}
            srcPrefix="/product/showcase-"
            width={2880}
            height={1800}
          />
        </Reveal>
      </div>
    </section>
  );
}
