import * as React from "react";
import qrcode from "qrcode-generator";

// The library's default keeps one byte per character, which garbles a
// non-ASCII account name in the address; encode as UTF-8 instead.
qrcode.stringToBytes = (text: string) =>
  Array.from(new TextEncoder().encode(text));

/** Modules of white around the code (the standard quiet zone is four). */
const QUIET_ZONE = 4;

/** The dark modules of the code for `value`, drawn as one path of unit squares. */
export function qrModules(value: string): {
  readonly size: number;
  readonly path: string;
} {
  // Error correction M, the smallest version that fits. Everything is computed
  // here in the browser: the value never leaves the page.
  const code = qrcode(0, "M");
  code.addData(value, "Byte");
  code.make();
  const size = code.getModuleCount();
  let path = "";
  for (let row = 0; row < size; row += 1)
    for (let column = 0; column < size; column += 1)
      if (code.isDark(row, column))
        path += `M${column + QUIET_ZONE} ${row + QUIET_ZONE}h1v1h-1z`;
  return { size, path };
}

/**
 * A QR code as an SVG. It is always dark on white, whatever the theme, so a
 * phone camera can read it. `label` is what a screen reader hears; the key to
 * type by hand belongs next to it.
 */
export function AccountQrCode({
  value,
  label,
  className,
}: {
  value: string;
  label: string;
  className?: string;
}) {
  const { size, path } = React.useMemo(() => qrModules(value), [value]);
  const box = size + QUIET_ZONE * 2;
  return (
    <svg
      role="img"
      aria-label={label}
      viewBox={`0 0 ${box} ${box}`}
      shapeRendering="crispEdges"
      className={className}
    >
      <rect width={box} height={box} className="fill-white" />
      <path d={path} className="fill-black" />
    </svg>
  );
}
