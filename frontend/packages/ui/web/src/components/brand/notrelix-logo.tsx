import * as React from "react";
import { cn } from "../../lib/cn";

export interface NotrelixLogoProps extends React.HTMLAttributes<HTMLDivElement> {
  size?: "sm" | "md" | "lg";
  showWordmark?: boolean;
}

export function NotrelixLogoMark({
  className,
  ...props
}: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      viewBox="0 0 408 318"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={cn("shrink-0", className)}
      {...props}
    >
      <path
        d="M336.1 186.99L156.821 29.9495C116.321 -7.55052 51.8207 9.44948 40.3233 67.2522L27.6001 137.5C34.6001 122.5 53.1001 117 68.6001 128C116.439 169.786 240.005 278.449 240.005 278.449C298.505 329.949 354.505 299.949 367.823 247.752C367.823 247.752 373.206 218.026 380 179.949C371.1 198.49 349.1 199.49 336.1 186.99Z"
        fill="url(#notrelix_paint0_linear)"
      />
      <path
        d="M380.005 179.96C371.1 198.5 349.1 199.5 336.005 186.96L276.505 134.762C281.972 103.325 284.537 86.1995 290.005 54.7624C291.982 40.763 296.401 36.6727 305.505 30.7624L377.505 3.26241C392.005 -4.23777 409.141 1.07168 407.504 20.4597C406.485 32.5378 391.23 117.047 380.005 179.96Z"
        fill="url(#notrelix_paint1_linear)"
      />
      <path
        d="M27.6 137.5C36.5045 118.96 58.5133 118.039 71.6088 130.579L131.109 182.777C125.641 214.214 123.076 231.34 117.609 262.777C115.631 276.776 111.212 280.866 102.109 286.777L30.1087 314.277C15.6087 321.777 -1.52779 316.467 0.10881 297.079C1.12836 285.001 16.3745 200.412 27.6 137.5Z"
        fill="url(#notrelix_paint2_linear)"
      />
      <defs>
        <linearGradient
          id="notrelix_paint0_linear"
          x1="52.1001"
          y1="46.9897"
          x2="350.6"
          y2="274.49"
          gradientUnits="userSpaceOnUse"
        >
          <stop stopColor="#FF1E56" />
          <stop offset="0.25" stopColor="#FC744C" />
          <stop offset="0.5" stopColor="#F9C942" />
          <stop offset="0.75" stopColor="#8CADA1" />
          <stop offset="1" stopColor="#1E90FF" />
        </linearGradient>
        <linearGradient
          id="notrelix_paint1_linear"
          x1="353.6"
          y1="13.5"
          x2="323.6"
          y2="176"
          gradientUnits="userSpaceOnUse"
        >
          <stop stopColor="#1E90FF" />
          <stop offset="1" stopColor="#1E90FF" stopOpacity="0.9" />
        </linearGradient>
        <linearGradient
          id="notrelix_paint2_linear"
          x1="54.0133"
          y1="304.039"
          x2="84.0133"
          y2="141.539"
          gradientUnits="userSpaceOnUse"
        >
          <stop stopColor="#FF1E56" />
          <stop offset="1" stopColor="#FE3154" stopOpacity="0.9" />
        </linearGradient>
      </defs>
    </svg>
  );
}

const sizeClasses = {
  sm: "h-6 w-auto",
  md: "h-8 w-auto",
  lg: "h-10 w-auto",
};

const textSizeClasses = {
  sm: "text-lg",
  md: "text-xl",
  lg: "text-2xl",
};

export function NotrelixLogo({
  size = "md",
  showWordmark = true,
  className,
  ...props
}: NotrelixLogoProps) {
  return (
    <div
      className={cn("inline-flex items-center gap-2.5 select-none", className)}
      {...props}
    >
      <NotrelixLogoMark className={sizeClasses[size]} />
      {showWordmark && (
        <span
          className={cn(
            "font-bold tracking-tight text-foreground",
            textSizeClasses[size],
          )}
        >
          Notrelix
        </span>
      )}
    </div>
  );
}
