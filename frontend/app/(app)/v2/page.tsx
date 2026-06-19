import {
  EditorialNav,
  EditorialHero,
  EditorialProof,
  EditorialPillars,
  EditorialStats,
  EditorialShowcase,
  EditorialScale,
  EditorialEnterprise,
  EditorialPricing,
  EditorialFinalCta,
  EditorialFooter,
} from "@/app/(app)/v2/_components"

export default function LandingV2Page() {
  return (
    <>
      <EditorialNav />
      <main>
        <EditorialHero />
        <EditorialProof />
        <EditorialPillars />
        <EditorialStats />
        <EditorialShowcase />
        <EditorialScale />
        <EditorialEnterprise />
        <EditorialPricing />
        <EditorialFinalCta />
      </main>
      <EditorialFooter />
    </>
  )
}
