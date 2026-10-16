import { cn } from "@notrelix/ui-web";

/**
 * The stroke glyphs of the Reports mockup at its 24 x 24 path data
 * (Final-Reports `.i`). A part is a path, `rect:x,y,w,h,r` or `circle:cx,cy,r`.
 */
const GLYPHS = {
  chevron: ["m6 9 6 6 6-6"],
  x: ["M6 6l12 12M18 6 6 18"],
  plus: ["M12 5v14M5 12h14"],
  filter: ["M3 5h18l-7 8v6l-4 2v-8z"],
  download: ["M12 3v12M7 10l5 5 5-5M5 21h14"],
  edit: ["M4 20h4L19 9l-4-4L4 16z"],
  more: ["circle:5,12,1", "circle:12,12,1", "circle:19,12,1"],
  refresh: ["M20 11a8 8 0 1 0-2.3 5.7M20 4v7h-7"],
  clock: ["circle:12,12,9", "M12 7v5l3 2"],
  flag: ["M5 21V4M5 4h11l-2 4 2 4H5"],
  pole: ["M5 21V4"],
  warn: ["M12 4 2 20h20z", "M12 10v4M12 17h.01"],
  info: ["circle:12,12,9", "M12 11v5M12 8h.01"],
  grip: [
    "circle:9,6,1",
    "circle:15,6,1",
    "circle:9,12,1",
    "circle:15,12,1",
    "circle:9,18,1",
    "circle:15,18,1",
  ],
  move: ["M12 3v18M3 12h18M9 6l3-3 3 3M9 18l3 3 3-3M6 9l-3 3 3 3M18 9l3 3-3 3"],
  resize: ["M14 4h6v6M10 20H4v-6M20 4l-7 7M4 20l7-7"],
  share: [
    "circle:18,5,2.5",
    "circle:6,12,2.5",
    "circle:18,19,2.5",
    "m8.2 10.8 7.6-4.4M8.2 13.2l7.6 4.4",
  ],
  board: ["rect:3,4,18,16,2", "M9 4v16M15 4v10"],
  eye: ["M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z", "circle:12,12,3"],
  bars: ["M4 20V11M10 20V5M16 20v-7M21 20H3"],
  layout: [
    "rect:3,3,7,7,1.5",
    "rect:14,3,7,7,1.5",
    "rect:3,14,7,7,1.5",
    "rect:14,14,7,7,1.5",
  ],
  number: ["M5 8h4v10M14 8h5l-3 5a3 3 0 1 1-2 4"],
  line: ["M3 17l6-6 4 4 8-8"],
  donut: ["circle:12,12,9", "M12 3v9l7 5"],
  progress: ["rect:3,10,18,5,2.5", "M3 12.5h10"],
  workload: ["M4 6h12M4 12h16M4 18h8"],
  table: ["rect:3,4,18,16,2", "M3 10h18M3 15h18M9 4v16"],
  note: ["M4 6h16M4 11h16M4 16h10"],
  out: ["M7 17 17 7M9 7h8v8"],
  lock: ["rect:4,11,16,10,2", "M8 11V7a4 4 0 0 1 8 0v4"],
  check: ["M5 12l5 5 9-10"],
  trash: ["M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3"],
  copy: ["rect:8,8,12,12,2", "M16 8V6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h2"],
} as const satisfies Record<string, readonly string[]>;

export type ReportGlyphName = keyof typeof GLYPHS;

export function ReportGlyph({
  name,
  className,
}: {
  name: ReportGlyphName;
  className?: string;
}) {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={cn("size-4 shrink-0", className)}
    >
      {GLYPHS[name].map((part, index) => {
        if (part.startsWith("rect:")) {
          const [x, y, w, h, r] = part.slice(5).split(",").map(Number);
          return <rect key={index} x={x} y={y} width={w} height={h} rx={r} />;
        }
        if (part.startsWith("circle:")) {
          const [cx, cy, r] = part.slice(7).split(",").map(Number);
          return <circle key={index} cx={cx} cy={cy} r={r} />;
        }
        return <path key={index} d={part} />;
      })}
    </svg>
  );
}
