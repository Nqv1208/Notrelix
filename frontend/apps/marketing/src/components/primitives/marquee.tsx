import * as React from "react";

interface MarqueeProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode;
  direction?: "left" | "right";
  speedSeconds?: number;
  pauseOnHover?: boolean;
  className?: string;
}

export function Marquee({
  children,
  direction = "left",
  speedSeconds = 35,
  pauseOnHover = true,
  className = "",
  ...props
}: MarqueeProps) {
  return (
    <div
      className={`group relative flex overflow-hidden select-none [mask-image:linear-gradient(to_right,transparent,black_10%,black_90%,transparent)] ${className}`}
      {...props}
    >
      <div
        className={`flex min-w-full shrink-0 items-center justify-around gap-6 py-2 transition-transform ${
          pauseOnHover ? "group-hover:[animation-play-state:paused]" : ""
        } ${direction === "left" ? "animate-marquee-left" : "animate-marquee-right"}`}
        style={{
          animationDuration: `${speedSeconds}s`,
        }}
      >
        {children}
      </div>
      <div
        aria-hidden="true"
        className={`flex min-w-full shrink-0 items-center justify-around gap-6 py-2 transition-transform ${
          pauseOnHover ? "group-hover:[animation-play-state:paused]" : ""
        } ${direction === "left" ? "animate-marquee-left" : "animate-marquee-right"}`}
        style={{
          animationDuration: `${speedSeconds}s`,
        }}
      >
        {children}
      </div>
    </div>
  );
}
