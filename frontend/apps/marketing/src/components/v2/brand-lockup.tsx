import Logo from "@notrelix/ui-web/assets/logo.svg";

type BrandLockupProps = {
  compact?: boolean;
  light?: boolean;
};

export function BrandLockup({
  compact = false,
  light = false,
}: BrandLockupProps) {
  return (
    <span className="inline-flex items-center gap-2.5">
      <Logo
        aria-hidden="true"
        className={`h-9 w-auto ${light ? "brightness-0 invert" : ""}`}
      />
      <span
        className={`font-semibold tracking-[-0.04em] ${
          compact ? "text-base" : "text-[1.1rem]"
        } ${light ? "text-white" : "text-[var(--v2-ink)]"}`}
      >
        notrelix
      </span>
    </span>
  );
}
