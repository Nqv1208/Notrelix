import * as React from "react";

interface MarketingContainerProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode;
  className?: string;
  as?: React.ElementType;
}

export function MarketingContainer({
  children,
  className = "",
  as: Component = "div",
  ...props
}: MarketingContainerProps) {
  return (
    <Component
      className={`mx-auto w-full max-w-[1180px] px-4 sm:px-6 lg:px-8 ${className}`}
      {...props}
    >
      {children}
    </Component>
  );
}
