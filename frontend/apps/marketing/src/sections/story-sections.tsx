import { ArrowUpRight, Check } from "lucide-react";
import type * as React from "react";
import Image from "next/image";
import { getTranslations } from "next-intl/server";
import type { Messages } from "../messages/en";

import { env } from "../config/env";
import { Reveal } from "../components/reveal";
import crops from "./story-crops.json";

type Story = Messages["story"]["stories"][number];
type StoryId = keyof typeof crops;

/** The crop each story shows and where its link goes. */
const storyMeta: Record<StoryId, { card?: number; href: string }> = {
  inbox: { href: `${env.webAppUrl}/sign-up` },
  sharing: { card: 576, href: "/legal/privacy" },
  departments: { card: 460, href: "#pricing" },
};

/**
 * Three proofs of how a team works, each one real screen cropped to the region
 * that carries the claim (captured by `pnpm marketing:previews`).
 */
export async function StorySections() {
  const t = await getTranslations("story");
  const stories = t.raw("stories") as Story[];

  return (
    <section id="features" className="section overflow-hidden">
      <div className="container">
        <Reveal>
          <h2 className="mb-16 max-w-xl text-3xl font-semibold tracking-[-0.045em] text-[var(--ink)] sm:text-4xl lg:text-[3.45rem] lg:leading-[1.03]">
            {t("title")}
          </h2>
        </Reveal>

        <div className="space-y-24 sm:space-y-32">
          {stories.map((story, index) => {
            const id = story.id as StoryId;
            const meta = storyMeta[id];
            const crop = crops[id];
            return (
              <Reveal
                key={story.id}
                className={`story-row ${index % 2 === 1 ? "story-row--reverse" : ""}`}
              >
                <div className="story-copy min-w-0">
                  <h3 className="max-w-xl text-3xl font-semibold tracking-[-0.045em] text-[var(--ink)] sm:text-4xl sm:leading-[1.08]">
                    {story.title}
                  </h3>
                  <p className="mt-5 max-w-lg text-base leading-7 text-[var(--muted-text)]">
                    {story.description}
                  </p>
                  <ul className="story-benefits">
                    {story.benefits.map((benefit) => (
                      <li key={benefit}>
                        <span className="mt-0.5 flex size-5 shrink-0 items-center justify-center rounded-full bg-[var(--mkt-brand-blue-100)] text-[var(--mkt-brand-blue-600)]">
                          <Check className="size-3" />
                        </span>
                        {benefit}
                      </li>
                    ))}
                  </ul>
                  <a
                    href={meta.href}
                    className="mt-8 inline-flex items-center gap-2 text-sm font-semibold text-[var(--cobalt)] hover:gap-3"
                  >
                    {story.link} <ArrowUpRight className="size-4" />
                  </a>
                </div>
                <figure
                  className={`story-shot min-w-0 ${meta.card ? "story-shot--card" : ""}`}
                >
                  {meta.card ? (
                    <div
                      className="story-shot__card"
                      style={
                        { "--card-w": `${meta.card}px` } as React.CSSProperties
                      }
                    >
                      <StoryImage id={id} alt={story.alt} crop={crop} />
                    </div>
                  ) : (
                    <StoryImage id={id} alt={story.alt} crop={crop} />
                  )}
                </figure>
              </Reveal>
            );
          })}
        </div>
      </div>
    </section>
  );
}

function StoryImage({
  id,
  alt,
  crop,
}: {
  id: StoryId;
  alt: string;
  crop: { width: number; height: number };
}) {
  return (
    <>
      {(["light", "dark"] as const).map((theme) => (
        <Image
          key={theme}
          src={`/product/story-${id}-${theme}.png`}
          alt={theme === "light" ? alt : ""}
          aria-hidden={theme === "light" ? undefined : true}
          width={crop.width}
          height={crop.height}
          sizes="(min-width: 1024px) 760px, 100vw"
          quality={90}
          className={`shot-img shot-img--${theme}`}
        />
      ))}
    </>
  );
}
