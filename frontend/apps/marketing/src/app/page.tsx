import { AmbientBackground } from "../components/ambient-background";
import { FinalCtaSection } from "../sections/final-cta-section";
import { HeroSection } from "../sections/hero-section";
import { MetricsSection } from "../sections/metrics-section";
import { PricingTeaserSection } from "../sections/pricing-teaser-section";
import { ProblemSolutionSection } from "../sections/problem-solution-section";

import { ShowcaseSection } from "../sections/showcase-section";
import {
  SocialProofSection,
  TrustStrip,
} from "../sections/social-proof-section";
import { StorySections } from "../sections/story-sections";
import { TestimonialsSection } from "../sections/testimonials-section";
import { UseCasesSection } from "../sections/use-cases-section";

export default function MarketingHomePage() {
  return (
    <div className="page min-h-screen overflow-clip">
      <AmbientBackground />
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
