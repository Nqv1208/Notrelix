"use client";

import type { Messages } from "../messages/en";
import { ProductShotTabs } from "../components/product-shot-tabs";
import hotspots from "./hero-preview-hotspots.json";

type PreviewCopy = Messages["hero"]["preview"];

/**
 * The screenshots carry the web app's own view tabs. Transparent buttons sit
 * exactly over those tabs (boxes recorded by `pnpm marketing:previews`), so the
 * app's tabs are the switcher.
 */
export function HeroProductPreview({ copy }: { copy: PreviewCopy }) {
  return (
    <ProductShotTabs
      tabs={copy.tabs}
      idPrefix="hero-preview"
      label={copy.tabsLabel}
      srcPrefix="/product/"
      hotspots={hotspots}
      chrome={copy.urlLabel}
      autoAdvanceMs={6000}
      priorityFirst
      width={2880}
      height={1800}
      className="hero-preview"
    />
  );
}
