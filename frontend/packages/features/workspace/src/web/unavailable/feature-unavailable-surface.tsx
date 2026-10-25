import { Construction } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { BOX, BOX_TEXT, PageHead } from "../billing/billing-parts";
import type { FeatureAvailabilityCopy } from "./feature-availability.copy";

export type UnavailableFeature = "integrations" | "reports";

export interface FeatureUnavailableSurfaceProps {
  readonly copy: FeatureAvailabilityCopy;
  /** The page whose backend does not exist yet. */
  readonly feature: UnavailableFeature;
}

/**
 * A page whose backend is not available yet: said plainly, with no retry,
 * because trying again cannot change the answer.
 */
export function FeatureUnavailableSurface({
  copy,
  feature,
}: FeatureUnavailableSurfaceProps) {
  const text = copy[feature];
  return (
    <section
      aria-labelledby={`unavailable-${feature}`}
      className="flex flex-col gap-4"
    >
      <PageHead
        id={`unavailable-${feature}`}
        title={text.title}
        lede={text.lede}
      />
      <div role="status" className={cn(BOX, "max-w-[560px]")}>
        <span className="inline-flex items-center gap-1.5 text-crumb font-semibold text-meta-foreground">
          <Construction aria-hidden="true" className="size-3.5" />
          {copy.badge}
        </span>
        <h3 className="m-0 text-compact leading-[1.45] font-semibold text-foreground">
          {text.heading}
        </h3>
        <p className={BOX_TEXT}>{text.body}</p>
      </div>
    </section>
  );
}
