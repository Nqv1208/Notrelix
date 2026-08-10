import type { ReactNode } from "react";

type SectionHeadingProps = {
  eyebrow: string;
  title: ReactNode;
  description?: string;
  align?: "left" | "center";
  tone?: "default" | "light";
};

export function SectionHeading({
  eyebrow,
  title,
  description,
  align = "left",
  tone = "default",
}: SectionHeadingProps) {
  return (
    <div
      className={`${align === "center" ? "mx-auto text-center" : ""} max-w-2xl`}
    >
      <span className={`v2-eyebrow ${tone === "light" ? "text-white/70" : ""}`}>
        {eyebrow}
      </span>
      <h2
        className={`mt-4 text-3xl font-semibold tracking-[-0.045em] sm:text-4xl lg:text-[3.45rem] lg:leading-[1.03] ${
          tone === "light" ? "text-white" : "text-[var(--v2-ink)]"
        }`}
      >
        {title}
      </h2>
      {description ? (
        <p
          className={`mt-5 max-w-xl text-base leading-7 sm:text-lg ${
            tone === "light" ? "text-white/72" : "text-[var(--v2-muted)]"
          }`}
        >
          {description}
        </p>
      ) : null}
    </div>
  );
}
