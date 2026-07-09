import { HeroSection } from '../sections/home/hero-section';
import { FeaturesSection } from '../sections/home/features-section';
import { PricingSection } from '../sections/home/pricing-section';
import { CTASection } from '../sections/home/cta-section';

export default function HomePage() {
  return (
    <div className="min-h-screen bg-background">
      <HeroSection />
      <FeaturesSection />
      <PricingSection />
      <CTASection />
    </div>
  );
}
