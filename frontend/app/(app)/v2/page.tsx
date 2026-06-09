import {
  LandingV2Footer,
  LandingV2Header,
  LandingV2LogoCloud,
} from "@/app/(app)/v2/_components"
import { LandingHero } from "@/components/marketing/LandingHero"
import { FeatureGrid } from "@/components/marketing/FeatureGrid"
import { EnterpriseSection } from "@/components/marketing/EnterpriseSection"
import { UseCaseSection } from "@/components/marketing/UseCaseSection"
import { FinalCTA } from "@/components/marketing/FinalCTA"

export default function LandingV2Page() {
  return (
    <>
      <LandingV2Header />
      <main>
        <LandingHero />
        <LandingV2LogoCloud />
        <FeatureGrid />
        <EnterpriseSection />
        <UseCaseSection />
        <FinalCTA />
      </main>
      <LandingV2Footer />
    </>
  )
}
