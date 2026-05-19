const logos = [
  "OpenFabric",
  "Northwind",
  "Stacklane",
  "Bluehour",
  "Convexly",
  "Railcraft",
] as const

export function LandingV2LogoCloud() {
  return (
    <section
      id="customers"
      className="border-y border-zinc-200 bg-white py-10 dark:border-zinc-800 dark:bg-zinc-950"
    >
      <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <p className="text-center text-xs font-semibold uppercase tracking-wider text-zinc-500">
          Trusted by teams who ship
        </p>
        <div className="mt-6 flex flex-wrap items-center justify-center gap-x-10 gap-y-6 opacity-60 grayscale">
          {logos.map((name) => (
            <span
              key={name}
              className="text-sm font-semibold tracking-tight text-zinc-800 dark:text-zinc-200"
            >
              {name}
            </span>
          ))}
        </div>
      </div>
    </section>
  )
}
