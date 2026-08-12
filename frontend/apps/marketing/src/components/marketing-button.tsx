import * as React from "react";

type MarketingButtonVariant =
  "primary" | "secondary" | "ghost" | "inverse" | "inverse-ghost";

type MarketingButtonSize = "sm" | "md" | "lg";

type MarketingButtonProps = {
  variant?: MarketingButtonVariant;
  size?: MarketingButtonSize;
  href?: string;
  className?: string;
  disabled?: boolean;
  children: React.ReactNode;
} & Pick<
  React.AnchorHTMLAttributes<HTMLAnchorElement>,
  "aria-label" | "onClick" | "target" | "rel"
> &
  Pick<
    React.ButtonHTMLAttributes<HTMLButtonElement>,
    "aria-label" | "onClick" | "type"
  >;

export function MarketingButton({
  variant = "primary",
  size = "md",
  href,
  className = "",
  disabled = false,
  children,
  ...rest
}: MarketingButtonProps) {
  const classes = [
    "v2-cta",
    `v2-cta--${variant}`,
    `v2-cta--${size}`,
    className,
  ].join(" ");

  if (href) {
    return (
      <a
        href={href}
        aria-disabled={disabled || undefined}
        className={classes}
        {...rest}
      >
        {children}
      </a>
    );
  }

  return (
    <button type="button" disabled={disabled} className={classes} {...rest}>
      {children}
    </button>
  );
}
