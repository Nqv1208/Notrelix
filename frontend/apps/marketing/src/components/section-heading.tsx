import type { ReactNode } from "react";

type SectionHeadingProps = {
  eyebrow: string;
  title: ReactNode;
  description?: string;
  align?: "left" | "center";
};

export function SectionHeading({
  eyebrow,
  title,
  description,
  align = "left",
}: SectionHeadingProps) {
  return (
    <div
      className={`${align === "center" ? "mx-auto text-center" : ""} max-w-2xl`}
    >
      <span className="eyebrow">{eyebrow}</span>
      <h2 className="mt-4 text-3xl font-semibold tracking-[-0.045em] text-[var(--ink)] sm:text-4xl lg:text-[3.45rem] lg:leading-[1.03]">
        {title}
      </h2>
      {description ? (
        <p className="mt-5 max-w-xl text-base leading-7 text-[var(--muted-text)] sm:text-lg">
          {description}
        </p>
      ) : null}
    </div>
  );
}
