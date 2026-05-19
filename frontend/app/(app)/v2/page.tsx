import {
  LandingV2BentoCollab,
  LandingV2BentoWorkflows,
  LandingV2Enterprise,
  LandingV2FeatureOwn,
  LandingV2FinalCta,
  LandingV2Footer,
  LandingV2Header,
  LandingV2Hero,
  LandingV2LogoCloud,
  LandingV2LovedByBuilders,
  LandingV2MoreFeatures,
  LandingV2PricingTeaser,
} from "@/app/(app)/v2/_components"

export default function LandingV2Page() {
  return (
    <>
      <LandingV2Header />
      <main>
        <LandingV2Hero />
        <LandingV2LogoCloud />
        <LandingV2FeatureOwn />
        <LandingV2BentoWorkflows />
        <LandingV2BentoCollab />
        <LandingV2Enterprise />
        <LandingV2MoreFeatures />
        <LandingV2LovedByBuilders />
        <LandingV2PricingTeaser />
        <LandingV2FinalCta />
      </main>
      <LandingV2Footer />
    </>
  )
}
