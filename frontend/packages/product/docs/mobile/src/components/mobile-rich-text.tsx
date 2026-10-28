import React from "react";
import { Text, StyleSheet } from "react-native";
import type { Run } from "@notrelix/docs-core";
import { presentRun } from "../render/doc-tree";
import {
  LINE_SPACING_SCALE,
  scriptCharacters,
  type BlockLayout,
} from "../render/doc-layout";
import { DOC_COLORS, HIGHLIGHT_COLORS, TEXT_COLORS } from "../render/doc-theme";

export interface MobileRichTextProps {
  readonly runs: readonly Run[];
  /** A safe link was pressed (the host opens it, e.g. with Linking). */
  readonly onOpenLink?: (href: string) => void;
  /** A page reference was pressed. */
  readonly onOpenPage?: (pageId: string) => void;
  /** A mention was pressed. */
  readonly onOpenMember?: (userId: string) => void;
  readonly style?: object;
  /** The block's alignment and line spacing. */
  readonly layout?: BlockLayout | null;
}

/** The runs of one block as nested text, read-only. */
export function MobileRichText({
  runs,
  onOpenLink,
  onOpenPage,
  onOpenMember,
  style,
  layout,
}: MobileRichTextProps): React.ReactNode {
  const baseLeading =
    (style as { lineHeight?: number } | undefined)?.lineHeight ??
    styles.base.lineHeight;
  return (
    <Text
      style={[
        styles.base,
        style,
        layout?.align ? { textAlign: layout.align } : null,
        layout?.lineSpacing
          ? {
              lineHeight: Math.round(
                baseLeading * LINE_SPACING_SCALE[layout.lineSpacing],
              ),
            }
          : null,
      ]}
    >
      {runs.map((run, index) => {
        const view = presentRun(run.marks);
        const decoration =
          view.underline || view.href
            ? view.strike
              ? "underline line-through"
              : "underline"
            : view.strike
              ? "line-through"
              : "none";
        const onPress = view.href
          ? () => onOpenLink?.(view.href as string)
          : view.pageId
            ? () => onOpenPage?.(view.pageId as string)
            : view.mentionUserId
              ? () => onOpenMember?.(view.mentionUserId as string)
              : undefined;
        // Raised or lowered text: the matching characters when they exist,
        // otherwise a smaller run (a text run cannot shift its baseline on every platform).
        const scripted = view.script
          ? scriptCharacters(run.text, view.script)
          : null;
        const pressable = view.href || view.pageId || view.mentionUserId;
        return (
          <Text
            key={index}
            onPress={onPress}
            accessibilityRole={
              view.href ? "link" : pressable ? "button" : undefined
            }
            style={[
              view.bold && styles.bold,
              view.italic && styles.italic,
              view.code && styles.code,
              view.script && !scripted && styles.smallScript,
              { textDecorationLine: decoration },
              view.color ? { color: TEXT_COLORS[view.color] } : null,
              view.href || view.pageId || view.mentionUserId
                ? { color: DOC_COLORS.link }
                : null,
              view.highlight
                ? { backgroundColor: HIGHLIGHT_COLORS[view.highlight] }
                : null,
            ]}
          >
            {scripted ?? run.text}
          </Text>
        );
      })}
    </Text>
  );
}

const styles = StyleSheet.create({
  base: { fontSize: 16, lineHeight: 24, color: DOC_COLORS.text },
  bold: { fontWeight: "700" },
  italic: { fontStyle: "italic" },
  smallScript: { fontSize: 12 },
  code: {
    fontFamily: "Courier",
    backgroundColor: DOC_COLORS.codeBackground,
  },
});
