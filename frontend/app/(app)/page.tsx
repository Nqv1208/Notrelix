import {
  DecorativeBackground,
  Header,
  HeroSection,
  FeaturesSection,
  DemoSection,
  HowItWorksSection,
  PricingSection,
  TestimonialsSection,
  CTASection,
  Footer,
} from "@/app/(app)/_components"

export default function HomePage() {
  return (
    <div className="min-h-screen bg-background">
      <DecorativeBackground />
      <Header />

      <main>
        <HeroSection />
        <FeaturesSection />
        <DemoSection />
        <HowItWorksSection />
        <PricingSection />
        <TestimonialsSection />
        <CTASection />
      </main>

      <Footer />
    </div>
  )
}
