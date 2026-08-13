import * as React from "react";

export type SectionVariant =
  | "default"
  | "soft"
  | "brand"
  | "inverse"
  | "transparent";

export type SectionSpacing = "sm" | "md" | "lg" | "xl" | "none";

interface MarketingSectionProps extends React.HTMLAttributes<HTMLElement> {
  children: React.ReactNode;
  variant?: SectionVariant;
  spacing?: SectionSpacing;
  className?: string;
  id?: string;
  as?: React.ElementType;
}

const variantClasses: Record<SectionVariant, string> = {
  default: "bg-transparent text-[var(--mkt-text)]",
  soft: "bg-[color-mix(in_srgb,var(--mkt-surface)_88%,transparent)] text-[var(--mkt-text)] border-y border-[color-mix(in_srgb,var(--mkt-border)_60%,transparent)]",
  brand: "bg-[var(--mkt-surface-brand)] text-[var(--mkt-text)] border-y border-[var(--mkt-border-brand)]",
  inverse: "bg-[var(--mkt-brand-blue-950)] text-white dark:bg-[#070b14]",
  transparent: "bg-transparent text-[var(--mkt-text)]",
};

const spacingClasses: Record<SectionSpacing, string> = {
  none: "py-0",
  sm: "py-10 lg:py-14",
  md: "py-14 lg:py-20",
  lg: "py-20 lg:py-28",
  xl: "py-24 lg:py-36",
};

export function MarketingSection({
  children,
  variant = "default",
  spacing = "md",
  className = "",
  id,
  as: Component = "section",
  ...props
}: MarketingSectionProps) {
  return (
    <Component
      id={id}
      className={`relative w-full overflow-hidden ${variantClasses[variant]} ${spacingClasses[spacing]} ${className}`}
      {...props}
    >
      {children}
    </Component>
  );
}
