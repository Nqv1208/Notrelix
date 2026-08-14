import { AmbientBackground } from "../components/ambient-background";
import { OutcomeProgressRail } from "../components/outcome-progress/outcome-progress-rail";
import { ComparisonSection } from "../sections/comparison-section";
import { FaqSection } from "../sections/faq-section";
import { FinalCtaSection } from "../sections/final-cta-section";
import { HeroSection } from "../sections/hero-section";
import { MetricsSection } from "../sections/metrics-section";
import { PricingTeaserSection } from "../sections/pricing-teaser-section";
import { ProblemMarqueeSection } from "../sections/problem-marquee-section";
import { ProblemSolutionSection } from "../sections/problem-solution-section";
import { ProblemTransitionSection } from "../sections/problem-transition-section";
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
        <ProblemMarqueeSection />
        <ProblemTransitionSection />
        <OutcomeProgressRail />
        <ShowcaseSection />
        <MetricsSection />
        <UseCasesSection />
        <ComparisonSection />
        <TestimonialsSection />
        <PricingTeaserSection />
        <FaqSection />
        <FinalCtaSection />
        <TrustStrip />
      </main>
    </div>
  );
}
