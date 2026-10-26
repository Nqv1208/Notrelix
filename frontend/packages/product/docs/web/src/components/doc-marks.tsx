import type { ReactNode } from "react";
import type { Mark } from "@notrelix/docs-core";
import { COLOR_CLASS, HIGHLIGHT_CLASS } from "../editor/mark-classes";

/** Text wrapped in the elements its marks stand for (read-only rendering). */
export function renderMarked(
  text: string,
  marks: readonly Mark[] | undefined,
): ReactNode {
  let node: ReactNode = text;
  for (const mark of marks ?? []) {
    switch (mark.type) {
      case "bold":
        node = <strong className="font-semibold">{node}</strong>;
        break;
      case "italic":
        node = <em>{node}</em>;
        break;
      case "underline":
        node = <u>{node}</u>;
        break;
      case "strike":
        node = <s>{node}</s>;
        break;
      case "superscript":
        node = <sup className="text-[0.75em]">{node}</sup>;
        break;
      case "subscript":
        node = <sub className="text-[0.75em]">{node}</sub>;
        break;
      case "code":
        node = (
          <code className="rounded-[4px] bg-state-hover px-1 py-0.5 font-mono text-[0.9em]">
            {node}
          </code>
        );
        break;
      case "link":
        node = (
          <a
            href={mark.href}
            target="_blank"
            rel="noopener noreferrer nofollow"
            className="text-brand-subtle-foreground underline underline-offset-2"
          >
            {node}
          </a>
        );
        break;
      case "color":
        node = <span className={COLOR_CLASS[mark.token]}>{node}</span>;
        break;
      case "highlight":
        node = (
          <mark
            className={`rounded-[3px] text-inherit ${HIGHLIGHT_CLASS[mark.token]}`}
          >
            {node}
          </mark>
        );
        break;
      case "mention":
        node = (
          <span
            data-user-id={mark.userId}
            className="rounded-[4px] bg-brand-subtle px-0.5 text-brand-subtle-foreground"
          >
            {node}
          </span>
        );
        break;
      default:
        break;
    }
  }
  return node;
}
