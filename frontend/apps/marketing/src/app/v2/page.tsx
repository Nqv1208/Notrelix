import { DecorativeBackground } from "../../sections/home/decorative-background";
import { FinalCtaSection } from "../../sections/v2/final-cta-section";
import { HeroSection } from "../../sections/v2/hero-section";
import { MetricsSection } from "../../sections/v2/metrics-section";
import { ProblemSolutionSection } from "../../sections/v2/problem-solution-section";
import { PricingTeaserSection } from "../../sections/v2/pricing-teaser-section";
import { ShowcaseSection } from "../../sections/v2/showcase-section";
import {
  SocialProofSection,
  TrustStrip,
} from "../../sections/v2/social-proof-section";
import { StorySections } from "../../sections/v2/story-sections";
import { TestimonialsSection } from "../../sections/v2/testimonials-section";
import { UseCasesSection } from "../../sections/v2/use-cases-section";

export default function MarketingV2Page() {
  return (
    <div className="v2-page min-h-screen overflow-clip">
      <DecorativeBackground />
      <main>
        <HeroSection />
        <SocialProofSection />
        <ProblemSolutionSection />
        <StorySections />
        <ShowcaseSection />
        <UseCasesSection />
        <MetricsSection />
        <TestimonialsSection />
        <PricingTeaserSection />
        <FinalCtaSection />
        <TrustStrip />
      </main>
    </div>
  );
}
