import type { CSSProperties } from "react";
import { getTranslations } from "next-intl/server";
import type { Messages } from "../messages/en";
import { HeroOrbitParallax } from "../components/hero-orbit-parallax";
import { ORBIT_BAND_Y, ORBIT_ITEMS, type OrbitItem } from "./hero-orbit-items";

/** Stagger: the pieces nearest the headline leave first. */
const ORDER = [...ORBIT_ITEMS]
  .sort((a, b) => a.g - b.g || Math.abs(a.y) - Math.abs(b.y))
  .map((item) => item.id);

function styleOf(item: OrbitItem): CSSProperties {
  const vars: Record<string, string | number> = {
    "--sx": item.side,
    "--i": ORDER.indexOf(item.id),
    "--sz": `${item.size}px`,
    "--d": item.depth,
    "--lg-g": item.g,
    "--lg-y": `${item.y}px`,
    "--fs": `${item.float.s}s`,
    "--amp": `${item.float.amp}px`,
    "--rot": `${item.float.rot}deg`,
  };
  if (item.md) {
    vars["--md-g"] = item.md.g;
    vars["--md-y"] = `${item.md.y}px`;
  }
  if (item.sm !== undefined || item.xs !== undefined)
    vars["--sm-y"] = `${ORBIT_BAND_Y}px`;
  if (item.sm !== undefined) vars["--sm-x"] = item.sm * item.side;
  if (item.xs !== undefined) vars["--xs-x"] = item.xs * item.side;
  return vars as CSSProperties;
}

function Pointer({ name }: { name: string }) {
  return (
    <span className="orbit-pointer">
      <svg viewBox="0 0 18 22" aria-hidden="true">
        <path
          d="M2.4 2.2 15.2 9l-5.6 1.7-2.5 5.7z"
          fill="currentColor"
          stroke="var(--mkt-surface-raised)"
          strokeWidth="2.4"
          strokeLinejoin="round"
        />
      </svg>
      <b>{name}</b>
    </span>
  );
}

function Body({
  item,
  copy,
}: {
  item: OrbitItem;
  copy: Messages["hero"]["orbit"];
}) {
  if (item.kind === "avatar") {
    return (
      <span className="orbit-avatar" data-tone={item.tone}>
        <span className="orbit-emoji">{item.emoji}</span>
      </span>
    );
  }
  if (item.kind === "tile") {
    return (
      <>
        <span
          className="orbit-tile"
          data-tone={item.tone}
          data-click={item.click ? "" : undefined}
        >
          <span className="orbit-emoji">{item.emoji}</span>
        </span>
        {item.click ? (
          <Pointer name={copy.cursors[item.side < 0 ? 0 : 1]} />
        ) : null}
      </>
    );
  }
  return (
    <span className="orbit-chip" data-tone={item.tone}>
      <span className="orbit-emoji">{item.emoji}</span>
      {item.chip === "dueToday" ? copy.dueToday : copy.inReview}
    </span>
  );
}

/**
 * Avatars, product tiles and small UI chips around the headline: on load they fly out from the
 * headline's centre to their places, then float, and follow the pointer a little (see
 * `HeroOrbitParallax`). Purely decorative: hidden from assistive tech, never in the way of a click.
 */
export async function HeroOrbit() {
  const t = await getTranslations("hero");
  const copy = t.raw("orbit") as Messages["hero"]["orbit"];
  return (
    <HeroOrbitParallax>
      {ORBIT_ITEMS.map((item) => (
        <div
          key={item.id}
          className="orbit-item"
          data-kind={item.kind}
          data-md={item.md ? "" : undefined}
          data-sm={item.sm ? "" : undefined}
          data-xs={item.xs ? "" : undefined}
          style={styleOf(item)}
        >
          <div className="orbit-float">
            <div className="orbit-body">
              <Body item={item} copy={copy} />
            </div>
          </div>
        </div>
      ))}
    </HeroOrbitParallax>
  );
}
