import {
  DecorativeBackground,
  Header,
  HeroSection,
  FeaturesSection,
  DemoSection,
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
        <TestimonialsSection />
        <CTASection />
      </main>

      <Footer />
    </div>
  )
}
